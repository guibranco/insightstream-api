using InsightStream.Core.Dtos;
using InsightStream.Core.Dtos.Newsletters;
using InsightStream.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsightStream.Api.Controllers;

[ApiController]
[Route("api/newsletters")]
[Authorize]
public class NewslettersController(InsightStreamDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var newsletters = await dbContext
            .Newsletters.OrderByDescending(n => n.ReceivedDate)
            .Select(n => new NewsletterDto
            {
                Id = n.Id,
                Title = n.Title,
                ReceivedDate = n.ReceivedDate,
                DestinationEmail = n.DestinationEmail,
                LinkCount = n.NewsletterLinks.Count,
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<NewsletterDto>>.Ok(newsletters));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var newsletter = await dbContext
            .Newsletters.Include(n => n.NewsletterLinks)
            .ThenInclude(nl => nl.Link)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (newsletter is null)
        {
            return NotFound(ApiResponse.Fail("Newsletter not found."));
        }

        var detail = new NewsletterDetailDto
        {
            Id = newsletter.Id,
            Title = newsletter.Title,
            ReceivedDate = newsletter.ReceivedDate,
            DestinationEmail = newsletter.DestinationEmail,
            Links = newsletter
                .NewsletterLinks.Select(nl => new NewsletterLinkSummaryDto
                {
                    Id = nl.Link.Id,
                    Url = nl.Link.Url,
                    Title = nl.Link.Title,
                    Status = nl.Link.Status,
                })
                .ToList(),
        };

        return Ok(ApiResponse<NewsletterDetailDto>.Ok(detail));
    }
}
