using InsightStream.Core.Abstractions;
using InsightStream.Core.Dtos;
using InsightStream.Core.Dtos.Auth;
using InsightStream.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsightStream.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(
    InsightStreamDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IRateLimiter rateLimiter
) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var allowed = await rateLimiter.TryConsumeAsync(
            $"ratelimit:login:{ip}",
            limit: 5,
            window: TimeSpan.FromMinutes(15),
            cancellationToken
        );

        if (!allowed)
        {
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                ApiResponse.Fail("Too many login attempts. Try again later.")
            );
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(
            u => u.Username == request.Username,
            cancellationToken
        );

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(ApiResponse.Fail("Invalid username or password."));
        }

        user.LastLogin = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = jwtTokenGenerator.GenerateToken(user);

        return Ok(
            ApiResponse<LoginResponseData>.Ok(
                new LoginResponseData
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        LastLogin = user.LastLogin,
                    },
                }
            )
        );
    }
}
