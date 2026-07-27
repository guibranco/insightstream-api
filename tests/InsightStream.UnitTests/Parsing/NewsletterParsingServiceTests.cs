using FluentAssertions;
using InsightStream.Infrastructure.Parsing;

namespace InsightStream.UnitTests.Parsing;

public class NewsletterParsingServiceTests
{
    private static byte[] LoadFixture() =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "sample_medium.eml"));

    private static NewsletterParsingService CreateService() =>
        new(new MimeKitEmailParser(), new AngleSharpLinkExtractor(), new UrlNormalizer());

    [Fact]
    public void Parse_ReturnsEmailMetadata()
    {
        var result = CreateService().Parse(LoadFixture());

        result.DestinationEmail.Should().Be("reader@example.com");
        result.Subject.Should().Contain("Your Daily Digest");
    }

    [Fact]
    public void Parse_DeduplicatesLinksRepeatedWithinTheSameEmail()
    {
        var result = CreateService().Parse(LoadFixture());

        result.Links.Should().HaveCount(3);
        result
            .Links.Count(l => l.Url == "https://medium.com/@janedoe/understanding-rust-ownership-8f3a2b1c9d4e")
            .Should()
            .Be(1);
    }

    [Fact]
    public void Parse_AttributesMediumHandleAuthor()
    {
        var result = CreateService().Parse(LoadFixture());

        var rustArticle = result.Links.First(l => l.Url.Contains("understanding-rust-ownership"));

        rustArticle.AuthorHandle.Should().Be("janedoe");
        rustArticle.AuthorDomain.Should().BeNull();
        rustArticle.AuthorName.Should().Be("Jane Doe");
        rustArticle.Title.Should().Be("Understanding Rust Ownership in 10 Minutes");
    }

    [Fact]
    public void Parse_AttributesCustomDomainAuthor()
    {
        var result = CreateService().Parse(LoadFixture());

        var cachesArticle = result.Links.First(l => l.Url.Contains("how-caches-work"));

        cachesArticle.AuthorDomain.Should().Be("blog.bytebytego.com");
        cachesArticle.AuthorHandle.Should().BeNull();
        cachesArticle.AuthorName.Should().Be("Alex Xu");
    }

    [Fact]
    public void Parse_UnwrapsRedirectLinks_AndAttributesTheResolvedDomain()
    {
        var result = CreateService().Parse(LoadFixture());

        var redirectArticle = result.Links.First(l => l.Url.Contains("danluu.com"));

        redirectArticle.Url.Should().Be("https://danluu.com/deconstruct-files");
        redirectArticle.AuthorDomain.Should().Be("danluu.com");
        redirectArticle.AuthorName.Should().BeNull();
    }
}
