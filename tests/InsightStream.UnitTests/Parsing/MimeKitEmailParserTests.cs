using FluentAssertions;
using InsightStream.Infrastructure.Parsing;

namespace InsightStream.UnitTests.Parsing;

public class MimeKitEmailParserTests
{
    private static byte[] LoadFixture() =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "sample_medium.eml"));

    [Fact]
    public void Parse_DecodesRfc2047EncodedSubject()
    {
        var parser = new MimeKitEmailParser();

        var result = parser.Parse(LoadFixture());

        result.Subject.Should().Be("Your Daily Digest \U0001F680 — 3 stories for you");
    }

    [Fact]
    public void Parse_ReadsDestinationEmailFromToHeader()
    {
        var parser = new MimeKitEmailParser();

        var result = parser.Parse(LoadFixture());

        result.DestinationEmail.Should().Be("reader@example.com");
    }

    [Fact]
    public void Parse_SelectsHtmlPartFromMultipartAlternative_AndDecodesBase64()
    {
        var parser = new MimeKitEmailParser();

        var result = parser.Parse(LoadFixture());

        result.HtmlBody.Should().Contain("Understanding Rust Ownership in 10 Minutes");
        result.HtmlBody.Should().Contain("How Caches Work Under the Hood");
    }

    [Fact]
    public void Parse_ReadsDateHeader()
    {
        var parser = new MimeKitEmailParser();

        var result = parser.Parse(LoadFixture());

        result.Date.Should().Be(new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero));
    }
}
