namespace InsightStream.Core.Entities;

/// <summary>Join entity tracking which newsletters a link was sent in.</summary>
public class NewsletterLink
{
    public Guid NewsletterId { get; set; }

    public Newsletter Newsletter { get; set; } = null!;

    public Guid LinkId { get; set; }

    public Link Link { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
