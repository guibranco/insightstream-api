namespace InsightStream.Core.Dtos.Links;

public class LinkQueryParameters
{
    public const int DefaultPerPage = 20;
    public const int MaxPerPage = 100;

    public static readonly IReadOnlyCollection<string> AllowedSortValues =
        ["priority", "newest", "oldest", "title"];

    public string? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PerPage { get; init; } = DefaultPerPage;

    public string Sort { get; init; } = "priority";

    public string? Search { get; init; }
}
