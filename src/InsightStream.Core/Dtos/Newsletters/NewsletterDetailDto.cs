using InsightStream.Core.Enums;

namespace InsightStream.Core.Dtos.Newsletters;

public class NewsletterDetailDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required DateTimeOffset ReceivedDate { get; init; }

    public required string DestinationEmail { get; init; }

    public required IReadOnlyList<NewsletterLinkSummaryDto> Links { get; init; }
}

public class NewsletterLinkSummaryDto
{
    public required Guid Id { get; init; }

    public required string Url { get; init; }

    public required string Title { get; init; }

    public required LinkStatus Status { get; init; }
}
