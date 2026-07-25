using InsightStream.Core.Enums;

namespace InsightStream.Core.Entities;

public class Link : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Url { get; set; }

    /// <summary>SHA-256 of the normalized URL, used for de-duplication.</summary>
    public required string UrlHash { get; set; }

    public required string Title { get; set; }

    public DateTimeOffset FirstSeen { get; set; }

    public DateTimeOffset LastSeen { get; set; }

    public LinkStatus Status { get; set; } = LinkStatus.Awaiting;

    public decimal PriorityScore { get; set; } = 5.00m;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<NewsletterLink> NewsletterLinks { get; set; } = new List<NewsletterLink>();

    public ICollection<LinkAuthor> LinkAuthors { get; set; } = new List<LinkAuthor>();
}
