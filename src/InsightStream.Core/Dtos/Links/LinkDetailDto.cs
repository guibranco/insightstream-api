using InsightStream.Core.Enums;

namespace InsightStream.Core.Dtos.Links;

public class LinkDetailDto
{
    public required Guid Id { get; init; }

    public required string Url { get; init; }

    public required string Title { get; init; }

    public required LinkStatus Status { get; init; }

    public required decimal PriorityScore { get; init; }

    public required DateTimeOffset FirstSeen { get; init; }

    public required DateTimeOffset LastSeen { get; init; }

    public required IReadOnlyList<AuthorSummaryDto> Authors { get; init; }

    public required IReadOnlyList<NewsletterSummaryDto> Newsletters { get; init; }
}

public class AuthorSummaryDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}

public class NewsletterSummaryDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required DateTimeOffset ReceivedDate { get; init; }
}
