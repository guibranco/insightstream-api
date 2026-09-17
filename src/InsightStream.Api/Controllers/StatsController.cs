using InsightStream.Core.Dtos;
using InsightStream.Core.Dtos.Newsletters;
using InsightStream.Core.Dtos.Stats;
using InsightStream.Core.Enums;
using InsightStream.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsightStream.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController(InsightStreamDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var statusCounts = await dbContext
            .Links.GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        var allStatuses = Enum.GetValues<LinkStatus>()
            .ToDictionary(s => s, s => statusCounts.GetValueOrDefault(s, 0));

        var totalLinks = allStatuses.Values.Sum();
        var totalNewsletters = await dbContext.Newsletters.CountAsync(cancellationToken);
        var totalAuthors = await dbContext.Authors.CountAsync(cancellationToken);

        var recentNewsletters = await dbContext
            .Newsletters.OrderByDescending(n => n.ReceivedDate)
            .Take(5)
            .Select(n => new NewsletterDto
            {
                Id = n.Id,
                Title = n.Title,
                ReceivedDate = n.ReceivedDate,
                DestinationEmail = n.DestinationEmail,
                LinkCount = n.NewsletterLinks.Count,
            })
            .ToListAsync(cancellationToken);

        return Ok(
            ApiResponse<StatsDto>.Ok(
                new StatsDto
                {
                    StatusCounts = allStatuses,
                    TotalLinks = totalLinks,
                    TotalNewsletters = totalNewsletters,
                    TotalAuthors = totalAuthors,
                    RecentNewsletters = recentNewsletters,
                }
            )
        );
    }
}
