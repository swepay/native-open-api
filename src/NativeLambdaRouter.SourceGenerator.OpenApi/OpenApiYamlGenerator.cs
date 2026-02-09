using System.Text;
using Microsoft.CodeAnalysis;

namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Generates OpenAPI YAML content from discovered endpoints.
/// </summary>
internal static class OpenApiYamlGenerator
{
    /// <summary>
    /// Generates OpenAPI YAML content for the given endpoints.
    /// </summary>
    public static string Generate(IReadOnlyList<EndpointInfo> endpoints, string apiTitle, string apiVersion)
    {
        var sb = new StringBuilder();

        // OpenAPI header
        sb.AppendLine("openapi: \"3.1.0\"");
        sb.AppendLine();
        sb.AppendLine("info:");
        sb.AppendLine($"  title: \"{apiTitle}\"");
        sb.AppendLine($"  version: \"{apiVersion}\"");
        sb.AppendLine();

        // Paths
        sb.AppendLine("paths:");

        var pathGroups = endpoints
            .GroupBy(e => e.Path)
            .OrderBy(g => g.Key);

        foreach (var pathGroup in pathGroups)
        {
            sb.AppendLine($"  {pathGroup.Key}:");

            foreach (var endpoint in pathGroup.OrderBy(e => e.Method))
            {
                var method = endpoint.Method.ToLowerInvariant();
                var operationId = GenerateOperationId(endpoint);

                sb.AppendLine($"    {method}:");
                sb.AppendLine($"      operationId: {operationId}");
                sb.AppendLine($"      summary: \"{GenerateSummary(endpoint)}\"");

                // Tags based on path
                var tag = ExtractTag(endpoint.Path);
                sb.AppendLine("      tags:");
                sb.AppendLine($"        - {tag}");

                // Security
                if (endpoint.RequiresAuth)
                {
                    sb.AppendLine("      security:");
                    sb.AppendLine("        - JwtBearer: []");
                }

                // Parameters from path
                var pathParams = ExtractPathParameters(endpoint.Path);
                if (pathParams.Count > 0)
                {
                    sb.AppendLine("      parameters:");
                    foreach (var param in pathParams)
                    {
                        sb.AppendLine($"        - name: {param}");
                        sb.AppendLine("          in: path");
                        sb.AppendLine("          required: true");
                        sb.AppendLine("          schema:");
                        sb.AppendLine("            type: string");
                    }
                }

                // Request body for POST, PUT, PATCH
                if (endpoint.Method is "POST" or "PUT" or "PATCH")
                {
                    sb.AppendLine("      requestBody:");
                    sb.AppendLine("        required: true");
                    sb.AppendLine("        content:");
                    sb.AppendLine("          application/json:");
                    sb.AppendLine("            schema:");
                    sb.AppendLine($"              $ref: \"#/components/schemas/{endpoint.CommandSimpleName}\"");
                }

                // Responses
                sb.AppendLine("      responses:");
                sb.AppendLine("        \"200\":");
                sb.AppendLine($"          description: \"Successful response\"");
                var contentType = endpoint.ProducesContentType ?? "application/json";
                sb.AppendLine("          content:");
                sb.AppendLine($"            {contentType}:");
                sb.AppendLine("              schema:");
                sb.AppendLine($"                $ref: \"#/components/schemas/{endpoint.ResponseSimpleName}\"");

                // Standard error responses
                sb.AppendLine("        \"400\":");
                sb.AppendLine("          $ref: \"#/components/responses/BadRequest\"");
                sb.AppendLine("        \"401\":");
                sb.AppendLine("          $ref: \"#/components/responses/Unauthorized\"");
                sb.AppendLine("        \"500\":");
                sb.AppendLine("          $ref: \"#/components/responses/InternalServerError\"");
            }
        }

        sb.AppendLine();
        sb.AppendLine("components:");
        sb.AppendLine("  schemas:");

        // Generate schema placeholders for request/response types
        var allTypes = endpoints
            .SelectMany(e => new[] { new TypeInfo(e.CommandSimpleName, "Request"), new TypeInfo(e.ResponseSimpleName, "Response") })
            .GroupBy(t => t.TypeName)
            .Select(g => g.First())
            .OrderBy(t => t.TypeName);

        foreach (var typeInfo in allTypes)
        {
            sb.AppendLine($"    {typeInfo.TypeName}:");
            sb.AppendLine("      type: object");
            sb.AppendLine($"      description: \"{typeInfo.TypeKind} type - properties to be documented\"");
        }

        return sb.ToString();
    }

    private sealed class TypeInfo
    {
        public string TypeName { get; }
        public string TypeKind { get; }

        public TypeInfo(string typeName, string typeKind)
        {
            TypeName = typeName;
            TypeKind = typeKind;
        }
    }

    private static string GenerateOperationId(EndpointInfo endpoint)
    {
        // Convert path to operationId: /items/{id} -> getItemById
        var path = endpoint.Path.TrimStart('/');
        var parts = path.Split('/');

        var sb = new StringBuilder();
        sb.Append(endpoint.Method.ToLowerInvariant());

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
                continue;

            if (part.StartsWith("{") && part.EndsWith("}"))
            {
                // Path parameter: {id} -> ById
                var paramName = part.Trim('{', '}');
                sb.Append("By");
                sb.Append(char.ToUpperInvariant(paramName[0]));
                if (paramName.Length > 1)
                    sb.Append(paramName.Substring(1));
            }
            else
            {
                // Regular path segment: items -> Items
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    sb.Append(part.Substring(1));
            }
        }

        return sb.ToString();
    }

    private static string GenerateSummary(EndpointInfo endpoint)
    {
        var action = endpoint.Method switch
        {
            "GET" => "Get",
            "POST" => "Create",
            "PUT" => "Update",
            "DELETE" => "Delete",
            "PATCH" => "Patch",
            _ => endpoint.Method
        };

        // Extract resource name from response type
        var resourceName = endpoint.ResponseSimpleName
            .Replace("Response", "")
            .Replace("Command", "");

        return $"{action} {resourceName}";
    }

    private static string ExtractTag(string path)
    {
        // Extract first meaningful segment as tag
        var segments = path.TrimStart('/').Split('/');
        foreach (var segment in segments)
        {
            if (!string.IsNullOrEmpty(segment) && !segment.StartsWith("{"))
            {
                // Capitalize first letter
                return char.ToUpperInvariant(segment[0]) + segment.Substring(1);
            }
        }
        return "Default";
    }

    private static List<string> ExtractPathParameters(string path)
    {
        var parameters = new List<string>();
        var segments = path.Split('/');

        foreach (var segment in segments)
        {
            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                parameters.Add(segment.Trim('{', '}'));
            }
        }

        return parameters;
    }
}
