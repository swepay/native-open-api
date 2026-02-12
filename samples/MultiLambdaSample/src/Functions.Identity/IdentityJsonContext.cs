using Functions.Identity.Commands;
using Functions.Identity.Responses;
using NativeLambdaRouter;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Functions.Identity;

[JsonSerializable(typeof(LoginCommand))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(RegisterCommand))]
[JsonSerializable(typeof(RegisterResponse))]
[JsonSerializable(typeof(RefreshTokenCommand))]
[JsonSerializable(typeof(RefreshTokenResponse))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(RouteNotFoundResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class IdentityJsonContext : JsonSerializerContext
{
}
