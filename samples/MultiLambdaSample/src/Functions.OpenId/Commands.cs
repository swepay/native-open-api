using NativeMediator;
using Functions.OpenId.Responses;

namespace Functions.OpenId.Commands;

public sealed class OpenIdConfigurationCommand : IRequest<OpenIdConfigurationResponse> { }

public sealed class JwksCommand : IRequest<JwksResponse> { }

public sealed class TokenCommand : IRequest<TokenResponse>
{
    public string ClientId { get; init; } = string.Empty;
    public string? ClientSecret { get; init; }
    public string GrantType { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
}
