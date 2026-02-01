using System.Text.Json.Serialization;

namespace Native.OpenApi;

/// <summary>
/// Configuration options for the OpenAPI linter.
/// </summary>
/// <param name="RequiredErrorResponses">HTTP status codes that must be present in all operations (e.g., 400, 401, 500).</param>
/// <param name="SensitiveFieldNames">Field names that require descriptions (e.g., password, token).</param>
/// <param name="DisallowedGenericSegments">Path segments that are too generic (e.g., 'data', 'items').</param>
public sealed record OpenApiLintOptions(
    [property: JsonPropertyName("requiredErrorResponses")] IReadOnlyList<string> RequiredErrorResponses,
    [property: JsonPropertyName("sensitiveFieldNames")] IReadOnlyList<string> SensitiveFieldNames,
    [property: JsonPropertyName("disallowedGenericSegments")] IReadOnlyList<string> DisallowedGenericSegments)
{
    /// <summary>
    /// Creates default lint options with empty lists.
    /// </summary>
    public static OpenApiLintOptions Empty => new(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
}
