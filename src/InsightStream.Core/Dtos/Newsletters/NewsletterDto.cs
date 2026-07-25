namespace InsightStream.Core.Dtos.Newsletters;

public class NewsletterDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required DateTimeOffset ReceivedDate { get; init; }

    public required string DestinationEmail { get; init; }

    public required int LinkCount { get; init; }
}
