using FluentAssertions;
using InsightStream.Core.Entities;
using InsightStream.Core.Enums;
using InsightStream.Core.Options;
using InsightStream.Infrastructure.Persistence;
using InsightStream.Infrastructure.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InsightStream.UnitTests.Scoring;

public class ScoringServiceTests
{
    private static InsightStreamDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InsightStreamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InsightStreamDbContext(options);
    }

    private static ScoringService CreateService(InsightStreamDbContext db, FakeScoreCache? cache = null) =>
        new(db, Options.Create(new ScoringOptions()), cache ?? new FakeScoreCache());

    private static Link NewLink(DateTimeOffset firstSeen, string title = "Some Generic Article Title") =>
        new()
        {
            Url = $"https://example.com/{Guid.NewGuid()}",
            UrlHash = Guid.NewGuid().ToString("N"),
            Title = title,
            FirstSeen = firstSeen,
            LastSeen = firstSeen,
        };

    [Fact]
    public async Task ComputeAsync_AlwaysReturnsAScoreWithinZeroToTen()
    {
        await using var db = CreateContext();
        var link = NewLink(DateTimeOffset.UtcNow.AddDays(-400));
        db.Links.Add(link);
        await db.SaveChangesAsync();

        var score = await CreateService(db).ComputeAsync(link);

        score.Should().BeInRange(0m, 10m);
    }

    [Fact]
    public async Task ComputeAsync_AuthorWithMostlyLikedHistory_ScoresHigherThanAuthorWithMostlyDiscardedHistory()
    {
        await using var db = CreateContext();

        var likedAuthor = new Author { Name = "Liked Author" };
        var discardedAuthor = new Author { Name = "Discarded Author" };
        db.Authors.AddRange(likedAuthor, discardedAuthor);

        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 5; i++)
        {
            var likedHistory = NewLink(now);
            likedHistory.Status = LinkStatus.Liked;
            db.Links.Add(likedHistory);
            db.LinkAuthors.Add(new LinkAuthor { Link = likedHistory, Author = likedAuthor });

            var discardedHistory = NewLink(now);
            discardedHistory.Status = LinkStatus.Discarded;
            db.Links.Add(discardedHistory);
            db.LinkAuthors.Add(new LinkAuthor { Link = discardedHistory, Author = discardedAuthor });
        }

        var candidateForLikedAuthor = NewLink(now);
        candidateForLikedAuthor.LinkAuthors.Add(new LinkAuthor { Author = likedAuthor });
        db.Links.Add(candidateForLikedAuthor);

        var candidateForDiscardedAuthor = NewLink(now);
        candidateForDiscardedAuthor.LinkAuthors.Add(new LinkAuthor { Author = discardedAuthor });
        db.Links.Add(candidateForDiscardedAuthor);

        await db.SaveChangesAsync();

        var service = CreateService(db);
        var likedScore = await service.ComputeAsync(candidateForLikedAuthor);
        var discardedScore = await service.ComputeAsync(candidateForDiscardedAuthor);

        likedScore.Should().BeGreaterThan(discardedScore);
    }

    [Fact]
    public async Task ComputeAsync_NewerLink_ScoresHigherThanOlderLink_AllElseEqual()
    {
        await using var db = CreateContext();

        var newerLink = NewLink(DateTimeOffset.UtcNow);
        var olderLink = NewLink(DateTimeOffset.UtcNow.AddDays(-60));
        db.Links.AddRange(newerLink, olderLink);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var newerScore = await service.ComputeAsync(newerLink);
        var olderScore = await service.ComputeAsync(olderLink);

        newerScore.Should().BeGreaterThan(olderScore);
    }

    [Fact]
    public async Task ComputeAsync_MorePopularLink_ScoresHigherThanLessPopular_AllElseEqual()
    {
        await using var db = CreateContext();
        var now = DateTimeOffset.UtcNow;

        var popularLink = NewLink(now);
        var unpopularLink = NewLink(now);
        db.Links.AddRange(popularLink, unpopularLink);

        for (var i = 0; i < 3; i++)
        {
            db.Newsletters.Add(
                new Newsletter
                {
                    Title = $"Newsletter {i}",
                    ReceivedDate = now,
                    DestinationEmail = "reader@example.com",
                    RawContent = "raw",
                    EmailHash = Guid.NewGuid().ToString("N"),
                }
            );
        }

        await db.SaveChangesAsync();

        foreach (var newsletter in db.Newsletters)
        {
            db.NewsletterLinks.Add(new NewsletterLink { NewsletterId = newsletter.Id, LinkId = popularLink.Id });
        }

        await db.SaveChangesAsync();

        var service = CreateService(db);
        var popularScore = await service.ComputeAsync(popularLink);
        var unpopularScore = await service.ComputeAsync(unpopularLink);

        popularScore.Should().BeGreaterThan(unpopularScore);
    }

    [Fact]
    public async Task ComputeAsync_PositiveKeywordPreference_IncreasesScore_ComparedToNoMatchingPreference()
    {
        await using var db = CreateContext();
        var now = DateTimeOffset.UtcNow;

        var user = new User { Username = "admin", PasswordHash = "hash" };
        db.Users.Add(user);
        db.UserPreferences.Add(
            new UserPreference
            {
                UserId = user.Id,
                PreferenceType = PreferenceType.Keyword,
                PreferenceValue = "rust",
                Weight = 1.00m,
            }
        );

        var matchingLink = NewLink(now, "Understanding Rust Ownership");
        var nonMatchingLink = NewLink(now, "Understanding Golang Channels");
        db.Links.AddRange(matchingLink, nonMatchingLink);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var matchingScore = await service.ComputeAsync(matchingLink);
        var nonMatchingScore = await service.ComputeAsync(nonMatchingLink);

        matchingScore.Should().BeGreaterThan(nonMatchingScore);
    }

    [Fact]
    public async Task GetScoreAsync_CachesTheComputedScore_AndDoesNotRecomputeOnSubsequentCalls()
    {
        await using var db = CreateContext();
        var now = DateTimeOffset.UtcNow;

        var author = new Author { Name = "Author" };
        db.Authors.Add(author);
        var link = NewLink(now);
        link.LinkAuthors.Add(new LinkAuthor { Author = author });
        db.Links.Add(link);
        await db.SaveChangesAsync();

        var cache = new FakeScoreCache();
        var service = CreateService(db, cache);

        var firstScore = await service.GetScoreAsync(link);

        // Mutate the author's history after the first call; a live recompute would change the score.
        for (var i = 0; i < 5; i++)
        {
            var likedHistory = NewLink(now);
            likedHistory.Status = LinkStatus.Liked;
            db.Links.Add(likedHistory);
            db.LinkAuthors.Add(new LinkAuthor { Link = likedHistory, Author = author });
        }
        await db.SaveChangesAsync();

        var secondScore = await service.GetScoreAsync(link);

        secondScore.Should().Be(firstScore);
        cache.SetCallCount.Should().Be(1);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesTheCachedScore()
    {
        await using var db = CreateContext();
        var link = NewLink(DateTimeOffset.UtcNow);
        db.Links.Add(link);
        await db.SaveChangesAsync();

        var cache = new FakeScoreCache();
        var service = CreateService(db, cache);

        var firstScore = await service.GetScoreAsync(link);
        await service.InvalidateAsync(link.Id);

        (await cache.GetAsync(link.Id)).Should().BeNull();
        firstScore.Should().BeInRange(0m, 10m);
    }
}
