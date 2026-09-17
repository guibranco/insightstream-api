using FluentAssertions;
using InsightStream.Infrastructure.Parsing;

namespace InsightStream.UnitTests.Parsing;

public class UrlNormalizerTests
{
    private readonly UrlNormalizer _normalizer = new();

    [Theory]
    [InlineData(
        "https://blog.bytebytego.com/p/how-caches-work?utm_source=substack&utm_medium=email",
        "https://blog.bytebytego.com/p/how-caches-work"
    )]
    [InlineData(
        "https://example.com/article?source=email-digest&foo=bar",
        "https://example.com/article?foo=bar"
    )]
    public void Normalize_StripsUtmAndSourceParams(string input, string expected)
    {
        _normalizer.Normalize(input).Url.Should().Be(expected);
    }

    [Fact]
    public void Normalize_UnwrapsMediumGlobalRedirectUrls()
    {
        const string input =
            "https://medium.com/m/global-redirect?url=https%3A%2F%2Fdanluu.com%2Fdeconstruct-files%2F&userId=abc123&operation=click";

        _normalizer.Normalize(input).Url.Should().Be("https://danluu.com/deconstruct-files");
    }

    [Fact]
    public void Normalize_DropsFragmentAndTrailingSlash()
    {
        const string input =
            "https://medium.com/@janedoe/understanding-rust-ownership-8f3a2b1c9d4e/?utm_campaign=digest&utm_content=story2#comments";

        _normalizer
            .Normalize(input)
            .Url.Should()
            .Be("https://medium.com/@janedoe/understanding-rust-ownership-8f3a2b1c9d4e");
    }

    [Fact]
    public void Normalize_ProducesSameHash_ForEquivalentUrlVariants()
    {
        const string variantA =
            "https://medium.com/@janedoe/understanding-rust-ownership-8f3a2b1c9d4e?source=email-digest---0-94--0&utm_medium=email";
        const string variantB =
            "https://medium.com/@janedoe/understanding-rust-ownership-8f3a2b1c9d4e/?utm_campaign=digest&utm_content=story2#comments";

        var a = _normalizer.Normalize(variantA);
        var b = _normalizer.Normalize(variantB);

        a.Hash.Should().Be(b.Hash);
        a.Url.Should().Be(b.Url);
    }

    [Fact]
    public void Normalize_ProducesDifferentHash_ForDifferentUrls()
    {
        var a = _normalizer.Normalize("https://example.com/a");
        var b = _normalizer.Normalize("https://example.com/b");

        a.Hash.Should().NotBe(b.Hash);
    }

    [Fact]
    public void Normalize_ProducesA64CharacterLowercaseHexHash()
    {
        var result = _normalizer.Normalize("https://example.com/article");

        result.Hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Normalize_LowercasesSchemeAndHost_ButPreservesPathCase()
    {
        var result = _normalizer.Normalize("HTTPS://Example.COM/Some-Article");

        result.Url.Should().Be("https://example.com/Some-Article");
    }
}
