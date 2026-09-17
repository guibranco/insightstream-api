using FluentAssertions;
using InsightStream.Core.Entities;
using InsightStream.Core.Enums;
using InsightStream.Core.Options;
using InsightStream.Infrastructure.Persistence;
using InsightStream.Infrastructure.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InsightStream.UnitTests.Scoring;

public class PreferenceLearningServiceTests
{
    private static InsightStreamDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InsightStreamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InsightStreamDbContext(options);
    }

    private static PreferenceLearningService CreateService(InsightStreamDbContext db) =>
        new(db, Options.Create(new ScoringOptions()));

    [Fact]
    public async Task ApplyAsync_Liked_IncreasesAuthorAndKeywordPreferenceWeights()
    {
        await using var db = CreateContext();

        var user = new User { Username = "admin", PasswordHash = "hash" };
        var author = new Author { Name = "Jane Doe", MediumHandle = "janedoe" };
        db.Users.Add(user);
        db.Authors.Add(author);

        var link = new Link
        {
            Url = "https://medium.com/@janedoe/some-rust-article",
            UrlHash = Guid.NewGuid().ToString("N"),
            Title = "Some Rust Article",
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        };
        link.LinkAuthors.Add(new LinkAuthor { Author = author });
        db.Links.Add(link);
        await db.SaveChangesAsync();

        await CreateService(db).ApplyAsync(user.Id, link, LinkStatus.Liked);

        var authorPreference = await db.UserPreferences.SingleAsync(p =>
            p.PreferenceType == PreferenceType.Author && p.PreferenceValue == "janedoe"
        );
        authorPreference.Weight.Should().Be(0.20m);

        var keywordPreference = await db.UserPreferences.SingleAsync(p =>
            p.PreferenceType == PreferenceType.Keyword && p.PreferenceValue == "rust"
        );
        keywordPreference.Weight.Should().Be(0.10m);
    }

    [Fact]
    public async Task ApplyAsync_RevertingToAwaiting_AppliesNoAdjustment()
    {
        await using var db = CreateContext();
        var user = new User { Username = "admin", PasswordHash = "hash" };
        var author = new Author { Name = "Jane Doe", MediumHandle = "janedoe" };
        db.Users.Add(user);
        db.Authors.Add(author);

        var link = new Link
        {
            Url = "https://medium.com/@janedoe/some-article",
            UrlHash = Guid.NewGuid().ToString("N"),
            Title = "Some Article",
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        };
        link.LinkAuthors.Add(new LinkAuthor { Author = author });
        db.Links.Add(link);
        await db.SaveChangesAsync();

        await CreateService(db).ApplyAsync(user.Id, link, LinkStatus.Awaiting);

        (await db.UserPreferences.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ApplyAsync_RepeatedDiscards_ClampsAuthorWeightAtNegativeOne()
    {
        await using var db = CreateContext();
        var user = new User { Username = "admin", PasswordHash = "hash" };
        var author = new Author { Name = "Jane Doe", MediumHandle = "janedoe" };
        db.Users.Add(user);
        db.Authors.Add(author);

        var link = new Link
        {
            Url = "https://medium.com/@janedoe/some-article",
            UrlHash = Guid.NewGuid().ToString("N"),
            Title = "Xyzabc Qwerty",
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        };
        link.LinkAuthors.Add(new LinkAuthor { Author = author });
        db.Links.Add(link);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        for (var i = 0; i < 10; i++)
        {
            await service.ApplyAsync(user.Id, link, LinkStatus.Discarded);
        }

        var authorPreference = await db.UserPreferences.SingleAsync(p =>
            p.PreferenceType == PreferenceType.Author && p.PreferenceValue == "janedoe"
        );
        authorPreference.Weight.Should().Be(-1.00m);
    }

    [Fact]
    public async Task ApplyAsync_RepeatedLikes_ClampsAuthorWeightAtPositiveOne()
    {
        await using var db = CreateContext();
        var user = new User { Username = "admin", PasswordHash = "hash" };
        var author = new Author { Name = "Jane Doe", MediumHandle = "janedoe" };
        db.Users.Add(user);
        db.Authors.Add(author);

        var link = new Link
        {
            Url = "https://medium.com/@janedoe/some-article",
            UrlHash = Guid.NewGuid().ToString("N"),
            Title = "Xyzabc Qwerty",
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        };
        link.LinkAuthors.Add(new LinkAuthor { Author = author });
        db.Links.Add(link);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        for (var i = 0; i < 10; i++)
        {
            await service.ApplyAsync(user.Id, link, LinkStatus.Liked);
        }

        var authorPreference = await db.UserPreferences.SingleAsync(p =>
            p.PreferenceType == PreferenceType.Author && p.PreferenceValue == "janedoe"
        );
        authorPreference.Weight.Should().Be(1.00m);
    }

    [Fact]
    public async Task ApplyAsync_DiscardedAfterReview_AppliesHalfKeywordWeightOfAuthorWeight()
    {
        await using var db = CreateContext();
        var user = new User { Username = "admin", PasswordHash = "hash" };

        var link = new Link
        {
            Url = "https://example.com/some-article",
            UrlHash = Guid.NewGuid().ToString("N"),
            Title = "Kubernetes Networking Explained",
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        };
        db.Users.Add(user);
        db.Links.Add(link);
        await db.SaveChangesAsync();

        await CreateService(db).ApplyAsync(user.Id, link, LinkStatus.DiscardedAfterReview);

        var keywordPreference = await db.UserPreferences.SingleAsync(p =>
            p.PreferenceType == PreferenceType.Keyword && p.PreferenceValue == "kubernetes"
        );
        keywordPreference.Weight.Should().Be(-0.05m);
    }
}
