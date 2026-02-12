using NativeLambdaRouter;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Functions.OpenApi;

[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(RouteNotFoundResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class OpenApiJsonSerializerContext : JsonSerializerContext
{
}
