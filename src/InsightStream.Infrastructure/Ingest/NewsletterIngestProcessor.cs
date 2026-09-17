using InsightStream.Core.Abstractions;
using InsightStream.Core.Dtos.Ingest;
using InsightStream.Core.Entities;
using InsightStream.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace InsightStream.Infrastructure.Ingest;

internal static partial class Log
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Newsletter with EmailHash {EmailHash} already processed, skipping"
    )]
    public static partial void NewsletterAlreadyProcessed(ILogger logger, string emailHash);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ingested newsletter {EmailHash} with {LinkCount} link(s)")]
    public static partial void NewsletterIngested(ILogger logger, string emailHash, int linkCount);
}

public class NewsletterIngestProcessor(
    InsightStreamDbContext dbContext,
    INewsletterParsingService parsingService,
    IScoringService scoringService,
    ILogger<NewsletterIngestProcessor> logger
) : INewsletterIngestProcessor
{
    public async Task ProcessAsync(
        IngestQueueMessage message,
        CancellationToken cancellationToken = default
    )
    {
        var alreadyProcessed = await dbContext.Newsletters.AnyAsync(
            n => n.EmailHash == message.EmailHash,
            cancellationToken
        );

        if (alreadyProcessed)
        {
            Log.NewsletterAlreadyProcessed(logger, message.EmailHash);
            return;
        }

        var rawEmail = Convert.FromBase64String(message.RawEmailBase64);
        var parsed = parsingService.Parse(rawEmail);

        try
        {
            await PersistAsync(message, rawEmail, parsed, cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Another consumer (or a redelivery) inserted this newsletter between the AnyAsync
            // check above and our SaveChangesAsync. Only swallow the failure when that is really
            // what happened: a collision on a link/author index means this newsletter is still
            // unprocessed and must go back through the consumer's retry path.
            dbContext.ChangeTracker.Clear();
            var processedConcurrently = await dbContext.Newsletters.AnyAsync(
                n => n.EmailHash == message.EmailHash,
                cancellationToken
            );

            if (!processedConcurrently)
            {
                throw;
            }

            Log.NewsletterAlreadyProcessed(logger, message.EmailHash);
            return;
        }

        Log.NewsletterIngested(logger, message.EmailHash, parsed.Links.Count);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private async Task PersistAsync(
        IngestQueueMessage message,
        byte[] rawEmail,
        ParsedNewsletter parsed,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var newsletter = new Newsletter
        {
            Title = parsed.Subject,
            ReceivedDate = parsed.Date,
            DestinationEmail = parsed.DestinationEmail,
            RawContent = System.Text.Encoding.UTF8.GetString(rawEmail),
            EmailHash = message.EmailHash,
        };
        dbContext.Newsletters.Add(newsletter);

        foreach (var processedLink in parsed.Links)
        {
            var link = await dbContext
                .Links.Include(l => l.LinkAuthors)
                .FirstOrDefaultAsync(l => l.UrlHash == processedLink.UrlHash, cancellationToken);

            var isNewLink = link is null;

            if (link is null)
            {
                link = new Link
                {
                    Url = processedLink.Url,
                    UrlHash = processedLink.UrlHash,
                    Title = processedLink.Title,
                    FirstSeen = now,
                    LastSeen = now,
                };
                dbContext.Links.Add(link);
            }
            else
            {
                link.LastSeen = now;
            }

            dbContext.NewsletterLinks.Add(
                new NewsletterLink
                {
                    NewsletterId = newsletter.Id,
                    LinkId = link.Id,
                    CreatedAt = now,
                }
            );

            var author = await ResolveAuthorAsync(processedLink, cancellationToken);
            if (author is not null && link.LinkAuthors.All(la => la.AuthorId != author.Id))
            {
                link.LinkAuthors.Add(
                    new LinkAuthor
                    {
                        LinkId = link.Id,
                        AuthorId = author.Id,
                        CreatedAt = now,
                    }
                );
            }

            if (isNewLink)
            {
                link.PriorityScore = await scoringService.ComputeAsync(link, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<Author?> ResolveAuthorAsync(
        ProcessedLink processedLink,
        CancellationToken cancellationToken
    )
    {
        if (processedLink.AuthorHandle is not null)
        {
            var author =
                dbContext.Authors.Local.FirstOrDefault(a => a.MediumHandle == processedLink.AuthorHandle)
                ?? await dbContext.Authors.FirstOrDefaultAsync(
                    a => a.MediumHandle == processedLink.AuthorHandle,
                    cancellationToken
                );

            if (author is not null)
            {
                return author;
            }

            author = new Author
            {
                Name = processedLink.AuthorName ?? processedLink.AuthorHandle,
                MediumHandle = processedLink.AuthorHandle,
            };
            dbContext.Authors.Add(author);
            return author;
        }

        if (processedLink.AuthorDomain is not null && processedLink.AuthorName is not null)
        {
            var author =
                dbContext.Authors.Local.FirstOrDefault(a => a.CustomDomain == processedLink.AuthorDomain)
                ?? await dbContext.Authors.FirstOrDefaultAsync(
                    a => a.CustomDomain == processedLink.AuthorDomain,
                    cancellationToken
                );

            if (author is not null)
            {
                return author;
            }

            author = new Author { Name = processedLink.AuthorName, CustomDomain = processedLink.AuthorDomain };
            dbContext.Authors.Add(author);
            return author;
        }

        return null;
    }
}
