using Functions.OpenId.Commands;
using Functions.OpenId.Responses;
using NativeLambdaRouter;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Functions.OpenId;

[JsonSerializable(typeof(OpenIdConfigurationCommand))]
[JsonSerializable(typeof(OpenIdConfigurationResponse))]
[JsonSerializable(typeof(JwksCommand))]
[JsonSerializable(typeof(JwksResponse))]
[JsonSerializable(typeof(JwkKey))]
[JsonSerializable(typeof(JwkKey[]))]
[JsonSerializable(typeof(TokenCommand))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(RouteNotFoundResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class OpenIdJsonContext : JsonSerializerContext
{
}
