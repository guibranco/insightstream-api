using InsightStream.Core.Dtos.Newsletters;
using InsightStream.Core.Enums;

namespace InsightStream.Core.Dtos.Stats;

public class StatsDto
{
    public required IReadOnlyDictionary<LinkStatus, int> StatusCounts { get; init; }

    public required int TotalLinks { get; init; }

    public required int TotalNewsletters { get; init; }

    public required int TotalAuthors { get; init; }

    public required IReadOnlyList<NewsletterDto> RecentNewsletters { get; init; }
}
