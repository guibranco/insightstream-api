namespace InsightStream.Core.Dtos.Auth;

public class UserDto
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }

    public DateTimeOffset? LastLogin { get; init; }
}
