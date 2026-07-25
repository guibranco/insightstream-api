namespace InsightStream.Core.Dtos.Auth;

public class LoginResponseData
{
    public required string Token { get; init; }

    public required UserDto User { get; init; }
}
