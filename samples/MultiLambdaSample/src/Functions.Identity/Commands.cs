using NativeMediator;
using Functions.Identity.Responses;
using NativeLambdaRouter.OpenApi.Attributes;

namespace Functions.Identity.Commands;

[EndpointName("LoginWithPassword")]
[EndpointSummary("Realiza login no Identity")]
[EndpointDescription("Endpoint de login com payload JSON")]
[Tags("Identity", "Auth")]
public sealed class LoginCommand : IRequest<LoginResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

[EndpointSummary("Registra um novo usuário")]
[Tags("Identity", "Auth")]
public sealed class RegisterCommand : IRequest<RegisterResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

[Accepts("application/x-www-form-urlencoded")]
[EndpointSummary("Renova access token")]
[EndpointDescription("Demonstra metadata via atributos para request form-urlencoded")]
[Tags("Identity", "OAuth2")]
public sealed class RefreshTokenCommand : IRequest<RefreshTokenResponse>
{
    public string ClientId { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string? Scope { get; init; }
}

public sealed class LoginOptionsCommand : IRequest<LoginOptionsResponse>
{
}
