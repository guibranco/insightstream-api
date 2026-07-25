using InsightStream.Core.Entities;

namespace InsightStream.Core.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
