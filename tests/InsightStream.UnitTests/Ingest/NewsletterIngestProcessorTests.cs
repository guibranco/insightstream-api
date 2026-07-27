using FluentAssertions;
using InsightStream.Core.Abstractions;
using InsightStream.Core.Dtos.Ingest;
using InsightStream.Core.Entities;
using InsightStream.Core.Enums;
using InsightStream.Infrastructure.Ingest;
using InsightStream.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsightStream.UnitTests.Ingest;

public class NewsletterIngestProcessorTests
{
    private static InsightStreamDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InsightStreamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new InsightStreamDbContext(options);
    }

    [Fact]
    public async Task ProcessAsync_SkipsProcessing_WhenEmailHashAlreadyExists()
    {
        await using var db = CreateContext();
        db.Newsletters.Add(
            new Newsletter
            {
                Title = "Existing",
                ReceivedDate = DateTimeOffset.UtcNow,
                DestinationEmail = "reader@example.com",
                RawContent = "raw",
                EmailHash = "duplicate-hash",
            }
        );
        await db.SaveChangesAsync();

        var parsingService = new FakeNewsletterParsingService
        {
            Result = new ParsedNewsletter("Subject", "reader@example.com", DateTimeOffset.UtcNow, []),
        };
        var scoringService = new FakeScoringService();
        var processor = new NewsletterIngestProcessor(
            db,
            parsingService,
            scoringService,
            NullLogger<NewsletterIngestProcessor>.Instance
        );

        await processor.ProcessAsync(
            new IngestQueueMessage
            {
                EmailHash = "duplicate-hash",
                RawEmailBase64 = Convert.ToBase64String("irrelevant"u8.ToArray()),
            }
        );

        parsingService.CallCount.Should().Be(0);
        (await db.Newsletters.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_PersistsNewsletterLinksAndAuthors_ForANewEmail()
    {
        await using var db = CreateContext();

        var links = new List<ProcessedLink>
        {
            new(
                Url: "https://medium.com/@janedoe/some-article",
                UrlHash: "hash-1",
                Title: "Some Article",
                AuthorName: "Jane Doe",
                AuthorHandle: "janedoe",
                AuthorDomain: null
            ),
            new(
                Url: "https://blog.example.com/p/other-article",
                UrlHash: "hash-2",
                Title: "Other Article",
                AuthorName: "Alex Xu",
                AuthorHandle: null,
                AuthorDomain: "blog.example.com"
            ),
        };

        var parsingService = new FakeNewsletterParsingService
        {
            Result = new ParsedNewsletter(
                "Daily Digest",
                "reader@example.com",
                DateTimeOffset.UtcNow,
                links
            ),
        };
        var scoringService = new FakeScoringService { ScoreToReturn = 8.25m };
        var processor = new NewsletterIngestProcessor(
            db,
            parsingService,
            scoringService,
            NullLogger<NewsletterIngestProcessor>.Instance
        );

        await processor.ProcessAsync(
            new IngestQueueMessage
            {
                EmailHash = "new-hash",
                RawEmailBase64 = Convert.ToBase64String("raw email bytes"u8.ToArray()),
            }
        );

        (await db.Newsletters.CountAsync()).Should().Be(1);
        (await db.Links.CountAsync()).Should().Be(2);
        (await db.Authors.CountAsync()).Should().Be(2);
        (await db.NewsletterLinks.CountAsync()).Should().Be(2);
        (await db.LinkAuthors.CountAsync()).Should().Be(2);

        var mediumLink = await db.Links.FirstAsync(l => l.UrlHash == "hash-1");
        mediumLink.Status.Should().Be(LinkStatus.Awaiting);
        mediumLink.PriorityScore.Should().Be(8.25m);

        var mediumAuthor = await db.Authors.FirstAsync(a => a.MediumHandle == "janedoe");
        mediumAuthor.Name.Should().Be("Jane Doe");

        var domainAuthor = await db.Authors.FirstAsync(a => a.CustomDomain == "blog.example.com");
        domainAuthor.Name.Should().Be("Alex Xu");
    }

    [Fact]
    public async Task ProcessAsync_BumpsLastSeenForExistingLink_WithoutRecomputingItsScore()
    {
        await using var db = CreateContext();

        var firstSeen = DateTimeOffset.UtcNow.AddDays(-10);
        db.Links.Add(
            new Link
            {
                Url = "https://medium.com/@janedoe/some-article",
                UrlHash = "hash-1",
                Title = "Some Article",
                FirstSeen = firstSeen,
                LastSeen = firstSeen,
                PriorityScore = 6.00m,
            }
        );
        await db.SaveChangesAsync();

        var parsingService = new FakeNewsletterParsingService
        {
            Result = new ParsedNewsletter(
                "Daily Digest",
                "reader@example.com",
                DateTimeOffset.UtcNow,
                [
                    new ProcessedLink(
                        Url: "https://medium.com/@janedoe/some-article",
                        UrlHash: "hash-1",
                        Title: "Some Article",
                        AuthorName: "Jane Doe",
                        AuthorHandle: "janedoe",
                        AuthorDomain: null
                    ),
                ]
            ),
        };
        var scoringService = new FakeScoringService { ScoreToReturn = 9.99m };
        var processor = new NewsletterIngestProcessor(
            db,
            parsingService,
            scoringService,
            NullLogger<NewsletterIngestProcessor>.Instance
        );

        await processor.ProcessAsync(
            new IngestQueueMessage
            {
                EmailHash = "new-hash",
                RawEmailBase64 = Convert.ToBase64String("raw"u8.ToArray()),
            }
        );

        (await db.Links.CountAsync()).Should().Be(1);
        var link = await db.Links.FirstAsync(l => l.UrlHash == "hash-1");
        link.LastSeen.Should().BeAfter(firstSeen);
        link.PriorityScore.Should().Be(6.00m);
        scoringService.ComputeCallCount.Should().Be(0);
    }
}
