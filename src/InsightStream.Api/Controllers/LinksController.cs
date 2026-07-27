using System.Security.Claims;
using InsightStream.Core.Abstractions;
using InsightStream.Core.Dtos;
using InsightStream.Core.Dtos.Links;
using InsightStream.Core.Enums;
using InsightStream.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsightStream.Api.Controllers;

[ApiController]
[Route("api/links")]
[Authorize]
public class LinksController(
    InsightStreamDbContext dbContext,
    ISearchService searchService,
    IScoringService scoringService,
    IPreferenceLearningService preferenceLearningService
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLinks(
        [FromQuery] string? status,
        [FromQuery] int page,
        [FromQuery(Name = "per_page")] int perPage,
        [FromQuery] string? sort,
        [FromQuery] string? search,
        CancellationToken cancellationToken
    )
    {
        page = page <= 0 ? 1 : page;
        perPage = perPage <= 0 ? LinkQueryParameters.DefaultPerPage : perPage;
        sort = string.IsNullOrWhiteSpace(sort) ? "priority" : sort;

        LinkStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<LinkStatus>(status, ignoreCase: true, out var s))
            {
                return UnprocessableEntity(ApiResponse.Fail($"Invalid status '{status}'."));
            }
            parsedStatus = s;
        }

        if (!LinkQueryParameters.AllowedSortValues.Contains(sort, StringComparer.OrdinalIgnoreCase))
        {
            return UnprocessableEntity(ApiResponse.Fail($"Invalid sort value '{sort}'."));
        }

        if (page < 1)
        {
            return UnprocessableEntity(ApiResponse.Fail("page must be >= 1."));
        }

        if (perPage < 1 || perPage > LinkQueryParameters.MaxPerPage)
        {
            return UnprocessableEntity(
                ApiResponse.Fail($"per_page must be between 1 and {LinkQueryParameters.MaxPerPage}.")
            );
        }

        IQueryable<Core.Entities.Link> query = dbContext.Links;

        if (parsedStatus is not null)
        {
            query = query.Where(l => l.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var matchingIds = await searchService.SearchLinkIdsAsync(search, cancellationToken);
            query = query.Where(l => matchingIds.Contains(l.Id));
        }

        query = sort.ToLowerInvariant() switch
        {
            "newest" => query.OrderByDescending(l => l.FirstSeen),
            "oldest" => query.OrderBy(l => l.FirstSeen),
            "title" => query.OrderBy(l => l.Title),
            _ => query.OrderByDescending(l => l.PriorityScore),
        };

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(l => new LinkDto
            {
                Id = l.Id,
                Url = l.Url,
                Title = l.Title,
                Status = l.Status,
                PriorityScore = l.PriorityScore,
                FirstSeen = l.FirstSeen,
                LastSeen = l.LastSeen,
                AuthorNames = l.LinkAuthors.Select(la => la.Author.Name).ToList(),
                NewsletterAppearanceCount = l.NewsletterLinks.Count,
            })
            .ToListAsync(cancellationToken);

        return Ok(
            ApiResponse<List<LinkDto>>.Ok(
                items,
                new PaginationInfo { Page = page, PerPage = perPage, TotalItems = totalItems }
            )
        );
    }

    [HttpGet("prioritized")]
    public async Task<IActionResult> GetPrioritized(CancellationToken cancellationToken)
    {
        var awaitingLinks = await dbContext
            .Links.Where(l => l.Status == LinkStatus.Awaiting)
            .Include(l => l.LinkAuthors)
            .ThenInclude(la => la.Author)
            .Include(l => l.NewsletterLinks)
            .ToListAsync(cancellationToken);

        foreach (var link in awaitingLinks)
        {
            var score = await scoringService.GetScoreAsync(link, cancellationToken);
            if (score != link.PriorityScore)
            {
                link.PriorityScore = score;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var ordered = awaitingLinks
            .OrderByDescending(l => l.PriorityScore)
            .Select(l => new LinkDto
            {
                Id = l.Id,
                Url = l.Url,
                Title = l.Title,
                Status = l.Status,
                PriorityScore = l.PriorityScore,
                FirstSeen = l.FirstSeen,
                LastSeen = l.LastSeen,
                AuthorNames = l.LinkAuthors.Select(la => la.Author.Name).ToList(),
                NewsletterAppearanceCount = l.NewsletterLinks.Count,
            })
            .ToList();

        return Ok(ApiResponse<List<LinkDto>>.Ok(ordered));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var link = await dbContext
            .Links.Include(l => l.LinkAuthors)
            .ThenInclude(la => la.Author)
            .Include(l => l.NewsletterLinks)
            .ThenInclude(nl => nl.Newsletter)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (link is null)
        {
            return NotFound(ApiResponse.Fail("Link not found."));
        }

        var detail = new LinkDetailDto
        {
            Id = link.Id,
            Url = link.Url,
            Title = link.Title,
            Status = link.Status,
            PriorityScore = link.PriorityScore,
            FirstSeen = link.FirstSeen,
            LastSeen = link.LastSeen,
            Authors = link
                .LinkAuthors.Select(la => new AuthorSummaryDto { Id = la.Author.Id, Name = la.Author.Name })
                .ToList(),
            Newsletters = link
                .NewsletterLinks.Select(nl => new NewsletterSummaryDto
                {
                    Id = nl.Newsletter.Id,
                    Title = nl.Newsletter.Title,
                    ReceivedDate = nl.Newsletter.ReceivedDate,
                })
                .ToList(),
        };

        return Ok(ApiResponse<LinkDetailDto>.Ok(detail));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateLinkStatusRequest request,
        CancellationToken cancellationToken
    )
    {
        var link = await dbContext
            .Links.Include(l => l.LinkAuthors)
            .ThenInclude(la => la.Author)
            .Include(l => l.NewsletterLinks)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (link is null)
        {
            return NotFound(ApiResponse.Fail("Link not found."));
        }

        link.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);

        var userId = GetUserId();
        await preferenceLearningService.ApplyAsync(userId, link, request.Status, cancellationToken);
        await scoringService.InvalidateAsync(link.Id, cancellationToken);

        return Ok(
            ApiResponse<LinkDto>.Ok(
                new LinkDto
                {
                    Id = link.Id,
                    Url = link.Url,
                    Title = link.Title,
                    Status = link.Status,
                    PriorityScore = link.PriorityScore,
                    FirstSeen = link.FirstSeen,
                    LastSeen = link.LastSeen,
                    AuthorNames = link.LinkAuthors.Select(la => la.Author.Name).ToList(),
                    NewsletterAppearanceCount = link.NewsletterLinks.Count,
                }
            )
        );
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!);
    }
}
