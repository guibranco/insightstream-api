namespace InsightStream.Core.Abstractions;

/// <summary>
/// Abstracts link search so the ILIKE-based implementation can later be swapped for Elasticsearch
/// without touching callers.
/// </summary>
public interface ISearchService
{
    /// <summary>Returns the ids of links whose title or author name matches the search term.</summary>
    Task<IReadOnlyCollection<Guid>> SearchLinkIdsAsync(
        string searchTerm,
        CancellationToken cancellationToken = default
    );
}
