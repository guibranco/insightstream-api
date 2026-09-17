using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InsightStream.Api.Middleware;
using InsightStream.Api.Workers;
using InsightStream.Core.Abstractions;
using InsightStream.Core.Dtos;
using InsightStream.Core.Options;
using InsightStream.Infrastructure.Caching;
using InsightStream.Infrastructure.Ingest;
using InsightStream.Infrastructure.Messaging;
using InsightStream.Infrastructure.Parsing;
using InsightStream.Infrastructure.Persistence;
using InsightStream.Infrastructure.Scoring;
using InsightStream.Infrastructure.Search;
using InsightStream.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/insightstream-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

var postgresConnectionString =
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres.");
var redisConnectionString =
    builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Redis.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<IngestOptions>(builder.Configuration.GetSection(IngestOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<ScoringOptions>(builder.Configuration.GetSection(ScoringOptions.SectionName));

var maxIngestBodyBytes =
    builder.Configuration.GetValue<long?>("Ingest:MaxRequestBodyBytes") ?? 10 * 1024 * 1024;
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = maxIngestBodyBytes);

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder
    .Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o =>
        o.InvalidModelStateResponseFactory = context =>
        {
            var message = string.Join(
                " ",
                context.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            );
            return new BadRequestObjectResult(
                ApiResponse.Fail(string.IsNullOrWhiteSpace(message) ? "Invalid request." : message)
            );
        }
    );

builder.Services.AddDbContext<InsightStreamDbContext>(o =>
    o.UseInsightStreamPostgres(postgresConnectionString)
);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString)
);

builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<IIngestQueuePublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<IngestConsumerService>();

builder.Services.AddScoped<IEmailParser, MimeKitEmailParser>();
builder.Services.AddScoped<ILinkExtractor, AngleSharpLinkExtractor>();
builder.Services.AddScoped<IUrlNormalizer, UrlNormalizer>();
builder.Services.AddScoped<INewsletterParsingService, NewsletterParsingService>();
builder.Services.AddScoped<INewsletterIngestProcessor, NewsletterIngestProcessor>();
builder.Services.AddScoped<IScoringService, ScoringService>();
builder.Services.AddScoped<IPreferenceLearningService, PreferenceLearningService>();
builder.Services.AddScoped<ISearchService, EfSearchService>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<IScoreCache, RedisScoreCache>();
builder.Services.AddSingleton<IRateLimiter, RedisRateLimiter>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(ApiResponse.Fail("Missing or invalid authentication token."))
                );
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(ApiResponse.Fail("You do not have access to this resource."))
                );
            },
        };
    });
builder.Services.AddAuthorization();

var corsAllowedOrigin =
    builder.Configuration["Cors:AllowedOrigin"]
    ?? throw new InvalidOperationException("Missing Cors:AllowedOrigin.");

builder.Services.AddCors(options =>
    options.AddPolicy(
        CorsOptions.PolicyName,
        policy =>
            policy
                .WithOrigins(corsAllowedOrigin)
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "Authorization")
    )
);

builder
    .Services.AddHealthChecks()
    .AddNpgSql(postgresConnectionString, name: "postgres")
    .AddRedis(redisConnectionString, name: "redis")
    .AddRabbitMQ(sp => sp.GetRequiredService<RabbitMqConnection>().GetConnectionAsync(), name: "rabbitmq");

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(CorsOptions.PolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteHealthCheckResponseAsync });

app.Run();

static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var payload = ApiResponse<object>.Ok(
        new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString(), description = e.Value.Description }
            ),
        }
    );

    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
