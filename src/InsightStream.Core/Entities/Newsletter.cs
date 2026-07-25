namespace InsightStream.Core.Entities;

public class Newsletter : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Title { get; set; }

    public DateTimeOffset ReceivedDate { get; set; }

    public required string DestinationEmail { get; set; }

    public required string RawContent { get; set; }

    /// <summary>SHA-256 of the raw RFC 822 email, used for ingest idempotency.</summary>
    public required string EmailHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<NewsletterLink> NewsletterLinks { get; set; } = new List<NewsletterLink>();
}
