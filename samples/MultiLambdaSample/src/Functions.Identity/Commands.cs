using NativeMediator;
using Functions.Identity.Responses;

namespace Functions.Identity.Commands;

public sealed class LoginCommand : IRequest<LoginResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class RegisterCommand : IRequest<RegisterResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class RefreshTokenCommand : IRequest<RefreshTokenResponse>
{
    public string RefreshToken { get; init; } = string.Empty;
}
