using InsightStream.Core.Enums;

namespace InsightStream.Core.Dtos.Authors;

public class AuthorDetailDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? MediumHandle { get; init; }

    public string? CustomDomain { get; init; }

    public required IReadOnlyDictionary<LinkStatus, int> InteractionCounts { get; init; }

    public required IReadOnlyList<AuthorLinkSummaryDto> Links { get; init; }
}

public class AuthorLinkSummaryDto
{
    public required Guid Id { get; init; }

    public required string Url { get; init; }

    public required string Title { get; init; }

    public required LinkStatus Status { get; init; }
}
