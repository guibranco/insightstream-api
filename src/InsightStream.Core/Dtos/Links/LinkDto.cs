using InsightStream.Core.Enums;

namespace InsightStream.Core.Dtos.Links;

public class LinkDto
{
    public required Guid Id { get; init; }

    public required string Url { get; init; }

    public required string Title { get; init; }

    public required LinkStatus Status { get; init; }

    public required decimal PriorityScore { get; init; }

    public required DateTimeOffset FirstSeen { get; init; }

    public required DateTimeOffset LastSeen { get; init; }

    public required IReadOnlyList<string> AuthorNames { get; init; }

    public required int NewsletterAppearanceCount { get; init; }
}
