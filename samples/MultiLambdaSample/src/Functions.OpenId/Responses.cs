namespace Functions.OpenId.Responses;

public sealed record OpenIdConfigurationResponse(
    string Issuer,
    string AuthorizationEndpoint,
    string TokenEndpoint,
    string JwksUri,
    string[] ResponseTypesSupported,
    string[] SubjectTypesSupported,
    string[] IdTokenSigningAlgValuesSupported);

public sealed record JwksResponse(JwkKey[] Keys);

public sealed record JwkKey(string Kty, string Use, string Kid, string N, string E);

public sealed record TokenResponse(string AccessToken, string TokenType, long ExpiresIn, string IdToken);
