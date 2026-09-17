using System.Text.Json;
using InsightStream.Core.Dtos;

namespace InsightStream.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var body = ApiResponse.Fail("An unexpected error occurred.");
            // Use the web defaults (camelCase) so this envelope matches what the controllers emit.
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(body, JsonSerializerOptions.Web),
                context.RequestAborted
            );
        }
    }
}
