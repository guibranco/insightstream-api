using InsightStream.Core.Entities;
using InsightStream.Core.Enums;

namespace InsightStream.Core.Abstractions;

public interface IPreferenceLearningService
{
    /// <summary>
    /// Adjusts author/keyword preference weights for the given user in response to a link status change.
    /// </summary>
    Task ApplyAsync(
        Guid userId,
        Link link,
        LinkStatus newStatus,
        CancellationToken cancellationToken = default
    );
}
