namespace InsightStream.Core.Entities;

public interface IHasTimestamps
{
    DateTimeOffset CreatedAt { get; set; }

    DateTimeOffset UpdatedAt { get; set; }
}
