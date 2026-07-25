namespace InsightStream.Core.Entities;

/// <summary>Join entity tracking which authors are attributed to a link.</summary>
public class LinkAuthor
{
    public Guid LinkId { get; set; }

    public Link Link { get; set; } = null!;

    public Guid AuthorId { get; set; }

    public Author Author { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
