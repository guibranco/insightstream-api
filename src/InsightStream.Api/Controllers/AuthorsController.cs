using InsightStream.Core.Dtos;
using InsightStream.Core.Dtos.Authors;
using InsightStream.Core.Enums;
using InsightStream.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsightStream.Api.Controllers;

[ApiController]
[Route("api/authors")]
[Authorize]
public class AuthorsController(InsightStreamDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var authors = await dbContext
            .Authors.OrderBy(a => a.Name)
            .Select(a => new AuthorDto
            {
                Id = a.Id,
                Name = a.Name,
                MediumHandle = a.MediumHandle,
                CustomDomain = a.CustomDomain,
                LinkCount = a.LinkAuthors.Count,
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<AuthorDto>>.Ok(authors));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var author = await dbContext
            .Authors.Include(a => a.LinkAuthors)
            .ThenInclude(la => la.Link)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (author is null)
        {
            return NotFound(ApiResponse.Fail("Author not found."));
        }

        var interactionCounts = Enum.GetValues<LinkStatus>()
            .ToDictionary(
                status => status,
                status => author.LinkAuthors.Count(la => la.Link.Status == status)
            );

        var detail = new AuthorDetailDto
        {
            Id = author.Id,
            Name = author.Name,
            MediumHandle = author.MediumHandle,
            CustomDomain = author.CustomDomain,
            InteractionCounts = interactionCounts,
            Links = author
                .LinkAuthors.Select(la => new AuthorLinkSummaryDto
                {
                    Id = la.Link.Id,
                    Url = la.Link.Url,
                    Title = la.Link.Title,
                    Status = la.Link.Status,
                })
                .ToList(),
        };

        return Ok(ApiResponse<AuthorDetailDto>.Ok(detail));
    }
}
