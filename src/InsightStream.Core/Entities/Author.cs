namespace InsightStream.Core.Entities;

public class Author : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Name { get; set; }

    public string? MediumHandle { get; set; }

    public string? CustomDomain { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<LinkAuthor> LinkAuthors { get; set; } = new List<LinkAuthor>();
}
