using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Generates OpenAPI YAML content from discovered endpoints.
/// </summary>
internal static class OpenApiYamlGenerator
{
    /// <summary>
    /// Overload kept for back-compat with call sites outside this assembly
    /// that have not yet adopted the Wave 1 signature.
    /// </summary>
    public static string Generate(IReadOnlyList<EndpointInfo> endpoints, string apiTitle, string apiVersion)
        => Generate(endpoints, apiTitle, apiVersion, new List<ErrorCatalogEntry>());

    /// <summary>
    /// Generates OpenAPI YAML content for the given endpoints, with RFC Wave 1
    /// enrichments: deprecation metadata (F03), named examples (F09), error
    /// catalog (F12), and the <c>SwepayProblemDetails</c> schema auto-injected
    /// when any endpoint serves <c>application/problem+json</c> (F13).
    /// </summary>
    public static string Generate(
        IReadOnlyList<EndpointInfo> endpoints,
        string apiTitle,
        string apiVersion,
        IReadOnlyList<ErrorCatalogEntry> errorCatalog)
    {
        var sb = new StringBuilder();

        // OpenAPI header
        sb.AppendLine("openapi: \"3.1.0\"");
        sb.AppendLine();
        sb.AppendLine("info:");
        sb.AppendLine($"  title: \"{apiTitle}\"");
        sb.AppendLine($"  version: \"{apiVersion}\"");
        sb.AppendLine();

        // RFC § F12 — document-level error catalog. Emitted before paths so Redoc
        // can resolve x-swepay-errors references without waiting for operations.
        if (errorCatalog.Count > 0)
        {
            sb.AppendLine("x-swepay-error-catalog:");
            foreach (var entry in errorCatalog)
            {
                sb.AppendLine($"  - code: \"{EscapeYamlString(entry.Code)}\"");
                sb.AppendLine($"    httpStatus: {entry.HttpStatus}");
                sb.AppendLine($"    userMessage: \"{EscapeYamlString(entry.UserMessage)}\"");
                sb.AppendLine($"    recovery: \"{EscapeYamlString(entry.Recovery)}\"");
                if (!string.IsNullOrEmpty(entry.DocUrl))
                {
                    sb.AppendLine($"    docUrl: \"{EscapeYamlString(entry.DocUrl!)}\"");
                }
            }
            sb.AppendLine();
        }

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
                var operationId = endpoint.OperationName ?? GenerateOperationId(endpoint);
                var summary = endpoint.Summary ?? GenerateSummary(endpoint);

                sb.AppendLine($"    {method}:");
                sb.AppendLine($"      operationId: {operationId}");
                sb.AppendLine($"      summary: \"{EscapeYamlString(summary)}\"");

                // Description (optional)
                if (endpoint.Description != null)
                {
                    sb.AppendLine($"      description: \"{EscapeYamlString(endpoint.Description)}\"");
                }

                // RFC § F03 — deprecation block.
                if (endpoint.Deprecation != null)
                {
                    sb.AppendLine("      deprecated: true");
                    sb.AppendLine($"      x-sunset: \"{EscapeYamlString(endpoint.Deprecation.Sunset)}\"");
                    sb.AppendLine($"      x-swepay-alternative: \"{EscapeYamlString(endpoint.Deprecation.Alternative)}\"");
                    sb.AppendLine($"      x-swepay-deprecation-reason: \"{EscapeYamlString(endpoint.Deprecation.Reason)}\"");
                }

                // RFC § F12 — slice of catalog codes that apply to this operation.
                if (endpoint.MatchedErrorCodes.Count > 0)
                {
                    sb.AppendLine("      x-swepay-errors:");
                    foreach (var code in endpoint.MatchedErrorCodes)
                    {
                        sb.AppendLine($"        - \"{EscapeYamlString(code)}\"");
                    }
                }

                // Tags — from metadata or auto-generated from path
                if (endpoint.Tags.Count > 0)
                {
                    sb.AppendLine("      tags:");
                    foreach (var tag in endpoint.Tags)
                    {
                        sb.AppendLine($"        - {tag}");
                    }
                }
                else
                {
                    var tag = ExtractTag(endpoint.Path);
                    sb.AppendLine("      tags:");
                    sb.AppendLine($"        - {tag}");
                }

                // Security
                if (endpoint.RequiresAuth)
                {
                    sb.AppendLine("      security:");
                    sb.AppendLine("        - JwtBearer: []");
                }
                else
                {
                    // OpenAPI 3.1: empty security array explicitly marks the endpoint as anonymous
                    sb.AppendLine("      security: []");
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
                    var requestContentType = endpoint.AcceptsContentType ?? "application/json";

                    sb.AppendLine("      requestBody:");
                    sb.AppendLine("        required: true");
                    sb.AppendLine("        content:");
                    sb.AppendLine($"          {requestContentType}:");
                    sb.AppendLine("            schema:");

                    if (requestContentType == "application/x-www-form-urlencoded")
                    {
                        // Form-encoded endpoints: emit inline schema with string properties
                        AppendFormEncodedSchema(sb, endpoint);
                    }
                    else
                    {
                        sb.AppendLine($"              $ref: \"#/components/schemas/{endpoint.CommandSimpleName}\"");
                    }

                    // RFC § F09 — request-side named examples (externalValue reference,
                    // resolved by the docs host; embedded-resource inlining is a Wave 2 follow-up).
                    AppendRequestExamples(sb, endpoint);
                }

                // Responses
                sb.AppendLine("      responses:");
                
                // Check if there's already a 200 response in AdditionalProduces
                var has200Response = endpoint.AdditionalProduces.Any(p => p.StatusCode == 200);
                
                if (!has200Response)
                {
                    // Only emit the default 200 response if not already defined
                    sb.AppendLine("        \"200\":");
                    sb.AppendLine($"          description: \"Successful response\"");
                    var contentType = endpoint.ProducesContentType ?? "application/json";
                    sb.AppendLine("          content:");
                    sb.AppendLine($"            {contentType}:");
                    sb.AppendLine("              schema:");
                    sb.AppendLine($"                $ref: \"#/components/schemas/{endpoint.ResponseSimpleName}\"");
                    // RFC § F09 — attach any example whose responseStatus == 200.
                    AppendResponseExamples(sb, endpoint, 200);
                }

                // Additional .Produces<T>() / .ProducesProblem() responses
                foreach (var produces in endpoint.AdditionalProduces)
                {
                    var statusCodeStr = produces.StatusCode.ToString();
                    sb.AppendLine($"        \"{statusCodeStr}\":");
                    sb.AppendLine($"          description: \"{GetStatusCodeDescription(produces.StatusCode)}\"");
                    if (produces.ResponseTypeSimpleName != null)
                    {
                        sb.AppendLine("          content:");
                        sb.AppendLine($"            {produces.ContentType}:");
                        sb.AppendLine("              schema:");
                        sb.AppendLine($"                $ref: \"#/components/schemas/{produces.ResponseTypeSimpleName}\"");
                        AppendResponseExamples(sb, endpoint, produces.StatusCode);
                    }
                    else if (produces.ContentType == "application/problem+json")
                    {
                        // RFC § F13 — point all problem+json responses at the shared schema.
                        sb.AppendLine("          content:");
                        sb.AppendLine($"            {produces.ContentType}:");
                        sb.AppendLine("              schema:");
                        sb.AppendLine("                $ref: \"#/components/schemas/SwepayProblemDetails\"");
                        AppendResponseExamples(sb, endpoint, produces.StatusCode);
                    }
                }

                // Standard error responses (only emit if not already covered by AdditionalProduces)
                var coveredStatusCodes = new HashSet<int>(endpoint.AdditionalProduces.Select(p => p.StatusCode));
                if (!coveredStatusCodes.Contains(400))
                {
                    sb.AppendLine("        \"400\":");
                    sb.AppendLine("          $ref: \"#/components/responses/BadRequest\"");
                }
                if (!coveredStatusCodes.Contains(401))
                {
                    sb.AppendLine("        \"401\":");
                    sb.AppendLine("          $ref: \"#/components/responses/Unauthorized\"");
                }
                if (!coveredStatusCodes.Contains(500))
                {
                    sb.AppendLine("        \"500\":");
                    sb.AppendLine("          $ref: \"#/components/responses/InternalServerError\"");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("components:");
        sb.AppendLine("  schemas:");

        // Build schema type info with resolved properties
        var allSchemas = BuildSchemaTypes(endpoints);

        foreach (var schema in allSchemas)
        {
            sb.AppendLine($"    {schema.TypeName}:");
            sb.AppendLine("      type: object");

            if (schema.IsResolved && schema.Properties.Count > 0)
            {
                AppendSchemaProperties(sb, schema);
            }
            else
            {
                sb.AppendLine($"      description: \"{schema.TypeKind} type - properties to be documented\"");
            }
        }

        // RFC § F13 — inject the canonical SwepayProblemDetails schema when any
        // endpoint advertises application/problem+json. The check also covers
        // cases where only some operations carry problem+json content so we do
        // not leak an unused schema when nobody references it.
        if (AnyProblemJsonResponse(endpoints))
        {
            AppendSwepayProblemDetailsSchema(sb);
        }

        return sb.ToString();
    }

    private static bool AnyProblemJsonResponse(IReadOnlyList<EndpointInfo> endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            foreach (var produces in endpoint.AdditionalProduces)
            {
                if (produces.ContentType == "application/problem+json") return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Emits the <c>SwepayProblemDetails</c> schema — superset of RFC 9457 with
    /// <c>code</c>, <c>recovery</c> and <c>requestId</c>. Matches the record in
    /// <c>Native.OpenApi.Models.SwepayProblemDetails</c>.
    /// </summary>
    private static void AppendSwepayProblemDetailsSchema(StringBuilder sb)
    {
        sb.AppendLine("    SwepayProblemDetails:");
        sb.AppendLine("      type: object");
        sb.AppendLine("      description: \"Swepay error payload (RFC 9457 superset). Carries a machine-readable code, UX-written recovery hint and request correlation id.\"");
        sb.AppendLine("      properties:");
        sb.AppendLine("        type:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          format: uri");
        sb.AppendLine("          description: \"Canonical documentation URL for this error class.\"");
        sb.AppendLine("        title:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          description: \"Short, human-readable classification.\"");
        sb.AppendLine("        status:");
        sb.AppendLine("          type: integer");
        sb.AppendLine("          format: int32");
        sb.AppendLine("          description: \"HTTP status code echoed into the body.\"");
        sb.AppendLine("        detail:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          description: \"End-user message. Never contains stack traces or internal identifiers.\"");
        sb.AppendLine("        instance:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          description: \"URI of the specific occurrence (usually the resource path).\"");
        sb.AppendLine("        code:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          description: \"Machine-readable error code from the Swepay catalog.\"");
        sb.AppendLine("        recovery:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          description: \"Concrete next step the caller should take.\"");
        sb.AppendLine("        requestId:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          description: \"Correlation id, echoed from X-Request-Id when supplied.\"");
        sb.AppendLine("      required:");
        sb.AppendLine("        - type");
        sb.AppendLine("        - title");
        sb.AppendLine("        - status");
        sb.AppendLine("        - detail");
        sb.AppendLine("        - code");
        sb.AppendLine("        - recovery");
        sb.AppendLine("        - requestId");
    }

    /// <summary>
    /// Emits the <c>examples</c> sub-node of a request body, using
    /// <c>externalValue</c> to reference the embedded JSON file path.
    /// </summary>
    /// <remarks>
    /// Skips examples whose <c>RequestJsonPath</c> is null (response-only
    /// scenarios). The renderer / docs host is responsible for serving the
    /// referenced path — inline-payload support is a Wave 2 follow-up.
    /// </remarks>
    private static void AppendRequestExamples(StringBuilder sb, EndpointInfo endpoint)
    {
        var withRequest = endpoint.Examples.Where(e => !string.IsNullOrEmpty(e.RequestJsonPath)).ToList();
        if (withRequest.Count == 0) return;

        sb.AppendLine("            examples:");
        foreach (var example in withRequest)
        {
            sb.AppendLine($"              {example.Name}:");
            sb.AppendLine($"                summary: \"{EscapeYamlString(example.Summary)}\"");
            sb.AppendLine($"                externalValue: \"{EscapeYamlString(example.RequestJsonPath!)}\"");
        }
    }

    /// <summary>
    /// Emits the <c>examples</c> sub-node of a response block, filtered by the
    /// current status code.
    /// </summary>
    private static void AppendResponseExamples(StringBuilder sb, EndpointInfo endpoint, int statusCode)
    {
        var matching = endpoint.Examples
            .Where(e => e.ResponseStatus == statusCode && !string.IsNullOrEmpty(e.ResponseJsonPath))
            .ToList();
        if (matching.Count == 0) return;

        sb.AppendLine("              examples:");
        foreach (var example in matching)
        {
            sb.AppendLine($"                {example.Name}:");
            sb.AppendLine($"                  summary: \"{EscapeYamlString(example.Summary)}\"");
            sb.AppendLine($"                  externalValue: \"{EscapeYamlString(example.ResponseJsonPath!)}\"");
        }
    }

    /// <summary>
    /// Builds deduplicated and sorted schema types from all endpoints.
    /// When the same type appears in multiple endpoints, the version with resolved properties wins.
    /// </summary>
    private static List<SchemaTypeInfo> BuildSchemaTypes(IReadOnlyList<EndpointInfo> endpoints)
    {
        var schemaMap = new Dictionary<string, SchemaTypeInfo>();

        foreach (var endpoint in endpoints)
        {
            // Command schema
            MergeSchema(schemaMap, endpoint.CommandSimpleName, "Request",
                endpoint.CommandProperties, endpoint.CommandPropertiesResolved);

            // Response schema
            MergeSchema(schemaMap, endpoint.ResponseSimpleName, "Response",
                endpoint.ResponseProperties, endpoint.ResponsePropertiesResolved);
        }

        return schemaMap.Values.OrderBy(s => s.TypeName).ToList();
    }

    private static void MergeSchema(
        Dictionary<string, SchemaTypeInfo> map,
        string typeName,
        string typeKind,
        List<SchemaPropertyInfo> properties,
        bool isResolved)
    {
        if (map.TryGetValue(typeName, out var existing))
        {
            // Prefer resolved over unresolved
            if (!existing.IsResolved && isResolved)
            {
                existing.Properties = properties;
                existing.IsResolved = isResolved;
            }
        }
        else
        {
            map[typeName] = new SchemaTypeInfo
            {
                TypeName = typeName,
                TypeKind = typeKind,
                Properties = properties,
                IsResolved = isResolved
            };
        }
    }

    /// <summary>
    /// Appends OpenAPI YAML property definitions for a resolved schema type.
    /// </summary>
    private static void AppendSchemaProperties(StringBuilder sb, SchemaTypeInfo schema)
    {
        sb.AppendLine("      properties:");

        foreach (var prop in schema.Properties)
        {
            sb.AppendLine($"        {prop.JsonName}:");

            if (prop.RefSchemaName != null)
            {
                // Complex type → $ref
                sb.AppendLine($"          $ref: \"#/components/schemas/{prop.RefSchemaName}\"");
                continue;
            }

            if (prop.IsEnum)
            {
                sb.AppendLine("          type: string");
                if (prop.EnumValues.Count > 0)
                {
                    sb.AppendLine("          enum:");
                    foreach (var val in prop.EnumValues)
                    {
                        sb.AppendLine($"            - {val}");
                    }
                }
                continue;
            }

            if (prop.OpenApiType == "array")
            {
                sb.AppendLine("          type: array");
                sb.AppendLine("          items:");
                if (prop.ArrayItemRefSchemaName != null)
                {
                    sb.AppendLine($"            $ref: \"#/components/schemas/{prop.ArrayItemRefSchemaName}\"");
                }
                else
                {
                    sb.AppendLine($"            type: {prop.ArrayItemType ?? "string"}");
                    if (prop.ArrayItemFormat != null)
                    {
                        sb.AppendLine($"            format: {prop.ArrayItemFormat}");
                    }
                }
                continue;
            }

            sb.AppendLine($"          type: {prop.OpenApiType}");
            if (prop.OpenApiFormat != null)
            {
                sb.AppendLine($"          format: {prop.OpenApiFormat}");
            }
            if (prop.Description != null)
            {
                sb.AppendLine($"          description: \"{EscapeYamlString(prop.Description)}\"");
            }
        }

        // Required fields
        var requiredProps = schema.Properties.Where(p => p.IsRequired).ToList();
        if (requiredProps.Count > 0)
        {
            sb.AppendLine("      required:");
            foreach (var prop in requiredProps)
            {
                sb.AppendLine($"        - {prop.JsonName}");
            }
        }
    }

    private static string EscapeYamlString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>
    /// Appends an inline object schema for application/x-www-form-urlencoded request bodies.
    /// When properties are resolved from the Command type, emits each property as a form field.
    /// Properties from form-encoded bodies are always typed as <c>string</c> since HTTP forms
    /// transmit everything as text. Nullable properties are excluded from <c>required</c>.
    /// </summary>
    private static void AppendFormEncodedSchema(StringBuilder sb, EndpointInfo endpoint)
    {
        sb.AppendLine("              type: object");

        if (endpoint.CommandPropertiesResolved && endpoint.CommandProperties.Count > 0)
        {
            sb.AppendLine("              properties:");
            foreach (var prop in endpoint.CommandProperties)
            {
                sb.AppendLine($"                {prop.JsonName}:");
                sb.AppendLine("                  type: string");
            }

            var requiredProps = endpoint.CommandProperties.Where(p => p.IsRequired).ToList();
            if (requiredProps.Count > 0)
            {
                sb.AppendLine("              required:");
                foreach (var prop in requiredProps)
                {
                    sb.AppendLine($"                - {prop.JsonName}");
                }
            }
        }
        else
        {
            sb.AppendLine($"              description: \"{endpoint.CommandSimpleName} form fields\"");
        }
    }

    private static string GetStatusCodeDescription(int statusCode)
    {
        return statusCode switch
        {
            200 => "Successful response",
            201 => "Created",
            204 => "No Content",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            _ => $"Response {statusCode}"
        };
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
