using FluentAssertions;
using InsightStream.Infrastructure.Security;

namespace InsightStream.UnitTests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesABCryptFormattedString()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        hash.Should().StartWith("$2");
    }

    [Fact]
    public void Verify_WithTheCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        _hasher.Verify("correct-horse-battery-staple", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithTheWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        _hasher.Verify("definitely-wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_ProducesADifferentHashEachTime_ButBothStillVerify()
    {
        var hashA = _hasher.Hash("correct-horse-battery-staple");
        var hashB = _hasher.Hash("correct-horse-battery-staple");

        hashA.Should().NotBe(hashB);
        _hasher.Verify("correct-horse-battery-staple", hashA).Should().BeTrue();
        _hasher.Verify("correct-horse-battery-staple", hashB).Should().BeTrue();
    }
}
