namespace InsightStream.Core.Dtos.Authors;

public class AuthorDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? MediumHandle { get; init; }

    public string? CustomDomain { get; init; }

    public required int LinkCount { get; init; }
}
