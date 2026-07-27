using FluentAssertions;
using InsightStream.Infrastructure.Parsing;

namespace InsightStream.UnitTests.Parsing;

public class AngleSharpLinkExtractorTests
{
    private const string Html = """
        <html><body>
        <div class="header">
          <a href="https://medium.com/m/view-in-browser?id=xyz">View this email in your browser</a>
        </div>
        <div class="content">
          <h2><a href="https://medium.com/@janedoe/understanding-rust-ownership-8f3a2b1c9d4e?source=email-digest&utm_medium=email">Understanding Rust Ownership in 10 Minutes</a></h2>
          <p class="byline">By <a href="https://medium.com/@janedoe">Jane Doe</a></p>
          <p>A whirlwind tour of borrowing, lifetimes, and the borrow checker.</p>

          <h3><a href="https://blog.bytebytego.com/p/how-caches-work?utm_source=substack">How Caches Work Under the Hood</a></h3>
          <p class="byline">By Alex Xu</p>

          <h2><a href="https://medium.com/m/global-redirect?url=https%3A%2F%2Fdanluu.com%2Fdeconstruct-files%2F">Deconstructing the File</a></h2>
          <p>An investigation into how filesystems really work.</p>

          <p>Read it again: <a href="https://medium.com/@janedoe/understanding-rust-ownership-8f3a2b1c9d4e/?utm_content=story2#comments">Understanding Rust Ownership in 10 Minutes</a></p>
        </div>
        <div class="footer">
          <a href="https://medium.com/@janedoe">View profile</a>
          <a href="https://medium.com/m/unsubscribe?userId=abc123">Unsubscribe</a>
          <a href="https://medium.com/me/settings">Manage your preferences</a>
          <a href="https://www.facebook.com/medium">Facebook</a>
          <a href="https://twitter.com/Medium">Twitter</a>
        </div>
        </body></html>
        """;

    [Fact]
    public void Extract_SkipsUnsubscribePreferencesProfileSocialAndFooterLinks()
    {
        var extractor = new AngleSharpLinkExtractor();

        var results = extractor.Extract(Html);

        results.Should().HaveCount(4);
        results.Select(r => r.Url).Should().NotContain(u => u.Contains("unsubscribe"));
        results.Select(r => r.Url).Should().NotContain(u => u.Contains("settings"));
        results.Select(r => r.Url).Should().NotContain(u => u.Contains("facebook"));
        results.Select(r => r.Url).Should().NotContain(u => u.Contains("twitter"));
        results.Select(r => r.Url).Should().NotContain(u => u.Contains("view-in-browser"));
    }

    [Fact]
    public void Extract_ReadsTitleFromHeadingAncestor_AndAuthorFromNearbyByline()
    {
        var extractor = new AngleSharpLinkExtractor();

        var results = extractor.Extract(Html);

        var rustArticle = results.First(r => r.Url.Contains("understanding-rust-ownership"));
        rustArticle.Title.Should().Be("Understanding Rust Ownership in 10 Minutes");
        rustArticle.AuthorName.Should().Be("Jane Doe");

        var cachesArticle = results.First(r => r.Url.Contains("how-caches-work"));
        cachesArticle.Title.Should().Be("How Caches Work Under the Hood");
        cachesArticle.AuthorName.Should().Be("Alex Xu");
    }

    [Fact]
    public void Extract_LeavesAuthorNullWhenNoBylineFollows()
    {
        var extractor = new AngleSharpLinkExtractor();

        var results = extractor.Extract(Html);

        var redirectArticle = results.First(r => r.Url.Contains("global-redirect"));
        redirectArticle.Title.Should().Be("Deconstructing the File");
        redirectArticle.AuthorName.Should().BeNull();
    }

    [Fact]
    public void Extract_DoesNotDeduplicateRawCandidates_LeavingThatToTheParsingService()
    {
        var extractor = new AngleSharpLinkExtractor();

        var results = extractor.Extract(Html);

        results.Count(r => r.Url.Contains("understanding-rust-ownership")).Should().Be(2);
    }
}
