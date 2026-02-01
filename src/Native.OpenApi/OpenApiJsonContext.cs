using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Native.OpenApi;

/// <summary>
/// JSON serialization context for Native AOT compatibility.
/// </summary>
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(JsonArray))]
internal partial class OpenApiJsonContext : JsonSerializerContext
{
}
