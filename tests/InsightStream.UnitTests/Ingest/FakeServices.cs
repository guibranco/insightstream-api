using InsightStream.Core.Abstractions;
using InsightStream.Core.Entities;

namespace InsightStream.UnitTests.Ingest;

internal class FakeNewsletterParsingService : INewsletterParsingService
{
    public required ParsedNewsletter Result { get; init; }

    public int CallCount { get; private set; }

    public ParsedNewsletter Parse(byte[] rawEmail)
    {
        CallCount++;
        return Result;
    }
}

internal class FakeScoringService : IScoringService
{
    public decimal ScoreToReturn { get; init; } = 7.5m;

    public int ComputeCallCount { get; private set; }

    public Task<decimal> GetScoreAsync(Link link, CancellationToken cancellationToken = default) =>
        Task.FromResult(ScoreToReturn);

    public Task<decimal> ComputeAsync(Link link, CancellationToken cancellationToken = default)
    {
        ComputeCallCount++;
        return Task.FromResult(ScoreToReturn);
    }

    public Task InvalidateAsync(Guid linkId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
