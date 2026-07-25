using InsightStream.Core.Enums;

namespace InsightStream.Core.Entities;

public class UserPreference : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public PreferenceType PreferenceType { get; set; }

    public required string PreferenceValue { get; set; }

    /// <summary>Clamped to [-1.00, 1.00].</summary>
    public decimal Weight { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
