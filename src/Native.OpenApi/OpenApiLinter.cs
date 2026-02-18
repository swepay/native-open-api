using System.Text.Json.Nodes;

namespace Native.OpenApi;

/// <summary>
/// Validates OpenAPI specifications against configurable rules.
/// </summary>
public sealed class OpenApiLinter
{
    private static readonly string[] Methods = ["get", "post", "put", "patch", "delete", "options", "head"];
    private readonly OpenApiLintOptions _options;

    /// <summary>
    /// Initializes a new linter with the specified options.
    /// </summary>
    public OpenApiLinter(OpenApiLintOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Lints an OpenAPI document part.
    /// </summary>
    /// <param name="sourceName">A name identifying the source for error messages.</param>
    /// <param name="root">The JSON root of the document to lint.</param>
    /// <param name="requirePaths">Whether to require at least one path definition.</param>
    /// <returns>A list of validation error messages.</returns>
    public IReadOnlyList<string> Lint(string sourceName, JsonNode root, bool requirePaths = true)
    {
        var errors = new List<string>();

        if (root["openapi"] is null)
        {
            errors.Add($"{sourceName}: missing 'openapi' version field");
            return errors;
        }

        var version = root["openapi"]!.GetValue<string>();
        if (version != "3.1.0")
        {
            errors.Add($"{sourceName}: OpenAPI version must be 3.1.0, found '{version}'");
        }

        if (root["paths"] is not JsonObject paths || paths.Count == 0)
        {
            if (requirePaths)
            {
                errors.Add($"{sourceName}: at least one path is required");
            }
            return errors;
        }

        foreach (var pathPair in paths)
        {
            var path = pathPair.Key;

            if (!path.Contains("/v", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{sourceName}: path '{path}' must include version (e.g., /v1/)");
            }

            if (IsGenericPath(path))
            {
                errors.Add($"{sourceName}: path '{path}' is too generic");
            }

            if (pathPair.Value is not JsonObject pathItem)
            {
                continue;
            }

            foreach (var method in Methods)
            {
                if (pathItem[method] is not JsonObject operation)
                {
                    continue;
                }

                // OpenAPI 3.1: security: [] (empty array) explicitly disables security (anonymous endpoint).
                // Missing security block means security is unspecified.
                if (operation["security"] is JsonArray security)
                {
                    // Explicit empty array → anonymous endpoint, no security required
                    if (security.Count > 0 && !ContainsSecurityScheme(security, "JwtBearer") && !ContainsSecurityScheme(security, "OAuth2"))
                    {
                        errors.Add($"{sourceName}: JwtBearer or OAuth2 required for '{method} {path}'");
                    }
                }
                else
                {
                    errors.Add($"{sourceName}: security required for '{method} {path}'");
                }

                var responses = operation["responses"] as JsonObject;
                if (responses is null || responses.Count == 0)
                {
                    errors.Add($"{sourceName}: at least one response required for '{method} {path}'");
                }
                else
                {
                    foreach (var required in _options.RequiredErrorResponses)
                    {
                        if (!responses.ContainsKey(required))
                        {
                            errors.Add($"{sourceName}: response {required} is required for '{method} {path}'");
                        }
                    }
                }

                if (operation["requestBody"] is JsonObject requestBody)
                {
                    ValidateContentSchemas(sourceName, $"{method} {path}", requestBody, errors);
                }

                if (responses is not null)
                {
                    foreach (var response in responses)
                    {
                        if (response.Value is JsonObject responseObject && responseObject["$ref"] is null)
                        {
                            ValidateContentSchemas(sourceName, $"{method} {path} response {response.Key}", responseObject, errors);
                        }
                    }
                }
            }
        }

        ValidateSensitiveSchemas(sourceName, root, errors);

        return errors;
    }

    private void ValidateContentSchemas(string sourceName, string location, JsonObject container, List<string> errors)
    {
        if (container["content"] is not JsonObject content)
        {
            errors.Add($"{sourceName}: content required for {location}");
            return;
        }

        foreach (var media in content)
        {
            if (media.Value is not JsonObject mediaObject)
            {
                continue;
            }

            if (mediaObject["schema"] is null)
            {
                errors.Add($"{sourceName}: schema required for {location} ({media.Key})");
            }
        }
    }

    private void ValidateSensitiveSchemas(string sourceName, JsonNode root, List<string> errors)
    {
        if (root["components"] is not JsonObject components || components["schemas"] is not JsonObject schemas)
        {
            return;
        }

        foreach (var schemaPair in schemas)
        {
            if (schemaPair.Value is not JsonObject schema || schema["properties"] is not JsonObject properties)
            {
                continue;
            }

            foreach (var property in properties)
            {
                if (!_options.SensitiveFieldNames.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value is not JsonObject propertySchema || string.IsNullOrWhiteSpace(propertySchema["description"]?.GetValue<string>()))
                {
                    errors.Add($"{sourceName}: sensitive field '{property.Key}' in schema '{schemaPair.Key}' must include description");
                }
            }
        }
    }

    private static bool ContainsSecurityScheme(JsonArray security, string scheme)
    {
        foreach (var entry in security)
        {
            if (entry is JsonObject obj && obj.ContainsKey(scheme))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsGenericPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var versionIndex = Array.FindIndex(segments, segment => segment.StartsWith("v", StringComparison.OrdinalIgnoreCase) && segment.Length <= 3);
        if (versionIndex < 0 || versionIndex == segments.Length - 1)
        {
            return false;
        }

        var postVersionSegments = segments[(versionIndex + 1)..];
        return postVersionSegments.All(segment => segment.StartsWith("{", StringComparison.Ordinal) && segment.EndsWith("}", StringComparison.Ordinal))
               || postVersionSegments.Any(segment => _options.DisallowedGenericSegments.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }
}
