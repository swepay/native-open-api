using NativeMediator;
using Functions.OpenId.Commands;
using Functions.OpenId.Responses;

namespace Functions.OpenId.Handlers;

public sealed class OpenIdConfigurationHandler : IRequestHandler<OpenIdConfigurationCommand, OpenIdConfigurationResponse>
{
    public ValueTask<OpenIdConfigurationResponse> Handle(OpenIdConfigurationCommand request, CancellationToken cancellationToken)
    {
        return new ValueTask<OpenIdConfigurationResponse>(new OpenIdConfigurationResponse(
            Issuer: "https://auth.example.com",
            AuthorizationEndpoint: "https://auth.example.com/authorize",
            TokenEndpoint: "https://auth.example.com/v1/openid/token",
            JwksUri: "https://auth.example.com/.well-known/jwks.json",
            ResponseTypesSupported: ["code", "id_token", "token"],
            SubjectTypesSupported: ["public"],
            IdTokenSigningAlgValuesSupported: ["RS256"]));
    }
}

public sealed class JwksHandler : IRequestHandler<JwksCommand, JwksResponse>
{
    public ValueTask<JwksResponse> Handle(JwksCommand request, CancellationToken cancellationToken)
    {
        var keys = new[]
        {
            new JwkKey(Kty: "RSA", Use: "sig", Kid: "key-1", N: "sample-modulus", E: "AQAB")
        };
        return new ValueTask<JwksResponse>(new JwksResponse(keys));
    }
}

public sealed class TokenHandler : IRequestHandler<TokenCommand, TokenResponse>
{
    public ValueTask<TokenResponse> Handle(TokenCommand request, CancellationToken cancellationToken)
    {
        return new ValueTask<TokenResponse>(new TokenResponse(
            AccessToken: "access-token-sample",
            TokenType: "Bearer",
            ExpiresIn: 3600,
            IdToken: "id-token-sample"));
    }
}
