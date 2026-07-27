using InsightStream.Core.Abstractions;
using InsightStream.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsightStream.Infrastructure.Search;

/// <summary>
/// ILIKE-based search over link title and attributed author names. Swap this implementation for an
/// Elasticsearch-backed one later without touching callers of <see cref="ISearchService"/>.
/// </summary>
public class EfSearchService(InsightStreamDbContext dbContext) : ISearchService
{
    public async Task<IReadOnlyCollection<Guid>> SearchLinkIdsAsync(
        string searchTerm,
        CancellationToken cancellationToken = default
    )
    {
        var pattern = $"%{searchTerm}%";

        var ids = await dbContext
            .Links.Where(l =>
                EF.Functions.ILike(l.Title, pattern)
                || l.LinkAuthors.Any(la => EF.Functions.ILike(la.Author.Name, pattern))
            )
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        return ids;
    }
}
