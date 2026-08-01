using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using InsightStream.Core.Entities;
using InsightStream.Core.Options;
using InsightStream.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InsightStream.UnitTests.Security;

public class JwtTokenGeneratorTests
{
    private static JwtOptions CreateOptions() =>
        new()
        {
            Key = "unit-test-signing-key-at-least-32-bytes-long!!",
            Issuer = "InsightStream.Tests",
            Audience = "InsightStream.Tests",
            ExpiryMinutes = 30,
        };

    private static User CreateUser() =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Username = "test-user",
            PasswordHash = "irrelevant",
        };

    [Fact]
    public void GenerateToken_ProducesAWellFormedJwt()
    {
        var options = CreateOptions();
        var generator = new JwtTokenGenerator(Options.Create(options));
        var user = CreateUser();

        var token = generator.GenerateToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        new JwtSecurityTokenHandler().CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_EmbedsTheUserIdAndUsernameAsClaims()
    {
        var options = CreateOptions();
        var generator = new JwtTokenGenerator(Options.Create(options));
        var user = CreateUser();

        var token = generator.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should()
            .Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.Username);
    }

    [Fact]
    public void GenerateToken_IsValidAgainstTheConfiguredSigningKeyIssuerAndAudience()
    {
        var options = CreateOptions();
        var generator = new JwtTokenGenerator(Options.Create(options));
        var user = CreateUser();

        var token = generator.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            },
            out _
        );

        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(user.Id.ToString());
    }

    [Fact]
    public void GenerateToken_ExpiresAfterTheConfiguredNumberOfMinutes()
    {
        var options = CreateOptions();
        var generator = new JwtTokenGenerator(Options.Create(options));
        var user = CreateUser();

        var token = generator.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(options.ExpiryMinutes), TimeSpan.FromMinutes(1));
    }
}
