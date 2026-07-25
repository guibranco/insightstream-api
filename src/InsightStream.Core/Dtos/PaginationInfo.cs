namespace InsightStream.Core.Dtos;

public class PaginationInfo
{
    public required int Page { get; init; }

    public required int PerPage { get; init; }

    public required int TotalItems { get; init; }

    public int TotalPages => PerPage == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PerPage);
}
