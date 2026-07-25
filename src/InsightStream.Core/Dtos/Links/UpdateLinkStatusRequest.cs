using InsightStream.Core.Enums;

namespace InsightStream.Core.Dtos.Links;

public class UpdateLinkStatusRequest
{
    public required LinkStatus Status { get; init; }
}
