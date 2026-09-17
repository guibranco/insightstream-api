using System.Security.Cryptography;
using System.Text;
using InsightStream.Core.Abstractions;
using InsightStream.Core.Dtos;
using InsightStream.Core.Dtos.Ingest;
using InsightStream.Core.Options;
using InsightStream.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InsightStream.Api.Controllers;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Queued newsletter ingest for EmailHash {EmailHash}")]
    public static partial void QueuedNewsletterIngest(ILogger logger, string emailHash);
}

[ApiController]
[Route("api/ingest")]
[AllowAnonymous]
public class IngestController(
    InsightStreamDbContext dbContext,
    IIngestQueuePublisher publisher,
    IRateLimiter rateLimiter,
    IOptions<IngestOptions> ingestOptions,
    ILogger<IngestController> logger
) : ControllerBase
{
    private static readonly string[] AcceptedContentTypes = ["message/rfc822", "application/octet-stream"];

    [HttpPost("email")]
    public async Task<IActionResult> IngestEmail(
        [FromHeader(Name = "X-Ingest-Token")] string? ingestToken,
        CancellationToken cancellationToken
    )
    {
        var options = ingestOptions.Value;

        if (!IsAuthorizedIngestToken(ingestToken, options.Token))
        {
            return Unauthorized(ApiResponse.Fail("Invalid or missing ingest token."));
        }

        var rateLimitKey = "ratelimit:ingest:" + HashToken(options.Token);
        var allowed = await rateLimiter.TryConsumeAsync(
            rateLimitKey,
            options.RateLimitPerHour,
            TimeSpan.FromHours(1),
            cancellationToken
        );

        if (!allowed)
        {
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                ApiResponse.Fail("Ingest rate limit exceeded. Try again later.")
            );
        }

        var contentType = Request.ContentType?.Split(';')[0].Trim();
        if (contentType is null || !AcceptedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return StatusCode(
                StatusCodes.Status415UnsupportedMediaType,
                ApiResponse.Fail("Content-Type must be message/rfc822 or application/octet-stream.")
            );
        }

        if (Request.ContentLength is long contentLength && contentLength > options.MaxRequestBodyBytes)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                ApiResponse.Fail("Email exceeds the maximum allowed size.")
            );
        }

        var (exceeded, rawEmail) = await ReadBodyAsync(
            Request.Body,
            options.MaxRequestBodyBytes,
            cancellationToken
        );

        if (exceeded)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                ApiResponse.Fail("Email exceeds the maximum allowed size.")
            );
        }

        var emailHash = Convert.ToHexStringLower(SHA256.HashData(rawEmail));

        var duplicate = await dbContext.Newsletters.AnyAsync(
            n => n.EmailHash == emailHash,
            cancellationToken
        );

        if (duplicate)
        {
            return Ok(ApiResponse<IngestResponseData>.Ok(new IngestResponseData { Duplicate = true }));
        }

        await publisher.PublishAsync(
            new IngestQueueMessage
            {
                EmailHash = emailHash,
                RawEmailBase64 = Convert.ToBase64String(rawEmail),
                RetryCount = 0,
            },
            cancellationToken
        );

        Log.QueuedNewsletterIngest(logger, emailHash);

        return AcceptedAtAction(
            nameof(IngestEmail),
            ApiResponse<IngestResponseData>.Ok(new IngestResponseData { Duplicate = false })
        );
    }

    private static bool IsAuthorizedIngestToken(string? provided, string configuredToken)
    {
        if (string.IsNullOrEmpty(provided))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredToken);

        return providedBytes.Length == configuredBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
    }

    private static string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static async Task<(bool Exceeded, byte[] Bytes)> ReadBodyAsync(
        Stream body,
        long maxBytes,
        CancellationToken cancellationToken
    )
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        long total = 0;
        int read;

        while ((read = await body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                return (true, []);
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return (false, buffer.ToArray());
    }
}
