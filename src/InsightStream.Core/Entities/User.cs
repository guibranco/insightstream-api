namespace InsightStream.Core.Entities;

public class User : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Username { get; set; }

    public required string PasswordHash { get; set; }

    public DateTimeOffset? LastLogin { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();
}
