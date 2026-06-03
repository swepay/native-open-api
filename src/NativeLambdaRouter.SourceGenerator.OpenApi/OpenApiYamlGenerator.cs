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
    /// Back-compat overload for Wave 1 callers that do not pass navigation data.
    /// </summary>
    public static string Generate(
        IReadOnlyList<EndpointInfo> endpoints,
        string apiTitle,
        string apiVersion,
        IReadOnlyList<ErrorCatalogEntry> errorCatalog)
        => Generate(endpoints, apiTitle, apiVersion, errorCatalog,
            new List<TagMetadataInfo>(), new List<TagGroupInfo>(), null,
            null, new List<OpenApiServerInfo>());

    /// <summary>
    /// Back-compat overload for Wave 2 callers that do not pass Wave 3 data.
    /// </summary>
    public static string Generate(
        IReadOnlyList<EndpointInfo> endpoints,
        string apiTitle,
        string apiVersion,
        IReadOnlyList<ErrorCatalogEntry> errorCatalog,
        IReadOnlyList<TagMetadataInfo> tagMetadata,
        IReadOnlyList<TagGroupInfo> tagGroups,
        ExternalDocsInfo? rootExternalDocs)
        => Generate(endpoints, apiTitle, apiVersion, errorCatalog,
            tagMetadata, tagGroups, rootExternalDocs,
            null, new List<OpenApiServerInfo>(), new List<WebhookInfo>());

    /// <summary>
    /// Back-compat overload for Wave 3 callers that do not pass Wave 5 webhook data.
    /// </summary>
    public static string Generate(
        IReadOnlyList<EndpointInfo> endpoints,
        string apiTitle,
        string apiVersion,
        IReadOnlyList<ErrorCatalogEntry> errorCatalog,
        IReadOnlyList<TagMetadataInfo> tagMetadata,
        IReadOnlyList<TagGroupInfo> tagGroups,
        ExternalDocsInfo? rootExternalDocs,
        OpenApiInfoMetadata? infoMetadata,
        IReadOnlyList<OpenApiServerInfo> servers)
        => Generate(endpoints, apiTitle, apiVersion, errorCatalog,
            tagMetadata, tagGroups, rootExternalDocs,
            infoMetadata, servers, new List<WebhookInfo>());

    /// <summary>
    /// Generates OpenAPI YAML content for the given endpoints, with all Wave 1–5
    /// enrichments: deprecation (F03), named examples (F09, inline + externalValue),
    /// error catalog (F12), <c>SwepayProblemDetails</c> schema (F13), Navigation
    /// Foundation (tags, x-tagGroups, externalDocs), rich <c>info</c> block
    /// (description, summary, termsOfService, contact, license), root
    /// <c>servers</c> array, and structural features (query/header parameters,
    /// response headers, response links, callbacks, and top-level webhooks).
    /// When <paramref name="emitStandardComponents"/> is <c>false</c>, the
    /// <c>components/responses</c> (BadRequest, Unauthorized, InternalServerError) and
    /// <c>components/securitySchemes</c> (JwtBearer) are suppressed. Use this in
    /// merge-producer projects where those definitions are provided by a shared common file.
    /// The <c>SwepayProblemDetails</c> schema is still emitted whenever at least one
    /// operation directly references it via <c>application/problem+json</c>, regardless
    /// of <paramref name="emitStandardComponents"/>. Operation-level <c>$ref</c>s and
    /// <c>security:</c> entries are preserved regardless.
    /// </summary>
    public static string Generate(
        IReadOnlyList<EndpointInfo> endpoints,
        string apiTitle,
        string apiVersion,
        IReadOnlyList<ErrorCatalogEntry> errorCatalog,
        IReadOnlyList<TagMetadataInfo> tagMetadata,
        IReadOnlyList<TagGroupInfo> tagGroups,
        ExternalDocsInfo? rootExternalDocs,
        OpenApiInfoMetadata? infoMetadata,
        IReadOnlyList<OpenApiServerInfo> servers,
        IReadOnlyList<WebhookInfo> webhooks,
        bool emitStandardComponents = true)
    {
        var sb = new StringBuilder();

        // OpenAPI header
        sb.AppendLine("openapi: \"3.1.0\"");
        sb.AppendLine();
        AppendInfoBlock(sb, apiTitle, apiVersion, infoMetadata);
        sb.AppendLine();

        // servers: (Wave 3 — declared environments)
        if (servers != null && servers.Count > 0)
        {
            AppendServers(sb, servers);
        }

        // Navigation foundation — document-level externalDocs (OpenAPI 3.1 §4.8.2).
        if (rootExternalDocs != null && !string.IsNullOrEmpty(rootExternalDocs.Url))
        {
            sb.AppendLine("externalDocs:");
            if (!string.IsNullOrEmpty(rootExternalDocs.Description))
                sb.AppendLine($"  description: \"{EscapeYamlString(rootExternalDocs.Description!)}\"");
            sb.AppendLine($"  url: \"{EscapeYamlString(rootExternalDocs.Url)}\"");
            sb.AppendLine();
        }

        // Navigation foundation — x-tagGroups (Redoc/Scalar extension for sidebar grouping).
        if (tagGroups.Count > 0)
        {
            sb.AppendLine("x-tagGroups:");
            foreach (var group in tagGroups)
            {
                sb.AppendLine($"  - name: \"{EscapeYamlString(group.Name)}\"");
                sb.AppendLine("    tags:");
                foreach (var tag in group.Tags)
                {
                    sb.AppendLine($"      - \"{EscapeYamlString(tag)}\"");
                }
            }
            sb.AppendLine();
        }

        // Navigation foundation — root tags object with metadata per tag.
        // Collect all tag names from both declared metadata and auto-generated endpoint tags.
        var allTagNames = CollectAllTagNames(endpoints);
        // Emit tags that have explicit metadata first, then any remaining tags (no metadata).
        var metadataByName = new Dictionary<string, TagMetadataInfo>(StringComparer.Ordinal);
        foreach (var tm in tagMetadata)
        {
            metadataByName[tm.Name] = tm;
        }

        // Emit tags section only when there is something interesting to say
        // (metadata for at least one tag, or when there are tags at all).
        if (allTagNames.Count > 0)
        {
            sb.AppendLine("tags:");
            foreach (var tagName in allTagNames)
            {
                sb.AppendLine($"  - name: \"{EscapeYamlString(tagName)}\"");
                if (metadataByName.TryGetValue(tagName, out var meta))
                {
                    if (!string.IsNullOrEmpty(meta.Description))
                        sb.AppendLine($"    description: \"{EscapeYamlString(meta.Description!)}\"");
                    if (!string.IsNullOrEmpty(meta.DisplayName))
                        sb.AppendLine($"    x-displayName: \"{EscapeYamlString(meta.DisplayName!)}\"");
                    if (meta.ExternalDocs != null && !string.IsNullOrEmpty(meta.ExternalDocs.Url))
                    {
                        sb.AppendLine("    externalDocs:");
                        if (!string.IsNullOrEmpty(meta.ExternalDocs.Description))
                            sb.AppendLine($"      description: \"{EscapeYamlString(meta.ExternalDocs.Description!)}\"");
                        sb.AppendLine($"      url: \"{EscapeYamlString(meta.ExternalDocs.Url)}\"");
                    }
                }
            }
            sb.AppendLine();
        }

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

                // Navigation foundation — per-operation externalDocs.
                if (endpoint.ExternalDocs != null && !string.IsNullOrEmpty(endpoint.ExternalDocs.Url))
                {
                    sb.AppendLine("      externalDocs:");
                    if (!string.IsNullOrEmpty(endpoint.ExternalDocs.Description))
                        sb.AppendLine($"        description: \"{EscapeYamlString(endpoint.ExternalDocs.Description!)}\"");
                    sb.AppendLine($"        url: \"{EscapeYamlString(endpoint.ExternalDocs.Url)}\"");
                }

                // Operation Richness — x-codeSamples (Redoc + Scalar).
                AppendCodeSamples(sb, endpoint);

                // Operation Richness — x-badges (Redoc + Scalar).
                AppendBadges(sb, endpoint);

                // Operation Richness — x-scalar-stability (Scalar-first, no Redoc equivalent).
                if (!string.IsNullOrEmpty(endpoint.ScalarStability))
                {
                    sb.AppendLine($"      x-scalar-stability: {endpoint.ScalarStability}");
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
                        sb.AppendLine($"        - \"{EscapeYamlString(tag)}\"");
                    }
                }
                else
                {
                    var tag = ExtractTag(endpoint.Path);
                    sb.AppendLine("      tags:");
                    sb.AppendLine($"        - \"{EscapeYamlString(tag)}\"");
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

                // Parameters: path (derived from route template) + explicit query/header params
                var pathParams = ExtractPathParameters(endpoint.Path);
                var hasParams = pathParams.Count > 0
                    || endpoint.QueryParameters.Count > 0
                    || endpoint.HeaderParameters.Count > 0;

                if (hasParams)
                {
                    sb.AppendLine("      parameters:");

                    // 1. Path parameters first (required, type: string)
                    foreach (var param in pathParams)
                    {
                        sb.AppendLine($"        - name: {param}");
                        sb.AppendLine("          in: path");
                        sb.AppendLine("          required: true");
                        sb.AppendLine("          schema:");
                        sb.AppendLine("            type: string");
                    }

                    // 2. Explicit query parameters (sorted by name for determinism)
                    var sortedQuery = new List<ExplicitParameterInfo>(endpoint.QueryParameters);
                    sortedQuery.Sort(static (a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));
                    foreach (var param in sortedQuery)
                    {
                        AppendExplicitParameter(sb, param);
                    }

                    // 3. Explicit header parameters (sorted by name for determinism)
                    var sortedHeaders = new List<ExplicitParameterInfo>(endpoint.HeaderParameters);
                    sortedHeaders.Sort(static (a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));
                    foreach (var param in sortedHeaders)
                    {
                        AppendExplicitParameter(sb, param);
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

                // Callbacks (emitted before responses per OpenAPI spec order)
                AppendCallbacks(sb, endpoint);

                // Responses
                sb.AppendLine("      responses:");

                // Check if there's already a 200 response in AdditionalProduces
                var has200Response = endpoint.AdditionalProduces.Any(p => p.StatusCode == 200);

                if (!has200Response)
                {
                    // Only emit the default 200 response if not already defined
                    sb.AppendLine("        \"200\":");
                    sb.AppendLine($"          description: \"Successful response\"");
                    AppendResponseHeaders(sb, endpoint, 200);
                    AppendResponseLinks(sb, endpoint, 200);
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
                    AppendResponseHeaders(sb, endpoint, produces.StatusCode);
                    AppendResponseLinks(sb, endpoint, produces.StatusCode);
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

        // Webhooks (top-level OpenAPI 3.1, sibling of paths)
        if (webhooks != null && webhooks.Count > 0)
        {
            sb.AppendLine();
            AppendWebhooks(sb, webhooks);
        }

        sb.AppendLine();
        sb.AppendLine("components:");
        sb.AppendLine("  schemas:");

        // Build schema type info with resolved properties.
        // Include webhook payload types as plain schemas (same as endpoint schemas).
        var allSchemas = BuildSchemaTypesWithWebhooks(endpoints, webhooks);

        // MELHORIA-6: Build the set of discriminated-union base schema names that carry
        // shared properties. These require a synthetic "{Name}__Core" schema so that:
        //   • The base schema emits purely oneOf + discriminator (no `properties` sibling),
        //     which satisfies strict Spectral / OWASP validators.
        //   • Shared properties live in ONE place (the synthetic Core schema) and are NOT
        //     duplicated across every derived type.
        //   • Derived types redirect their allOf $ref to "{BaseSchemaName}__Core".
        var discriminatedBasesWithSharedProps = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in allSchemas)
        {
            if (s.SubTypes.Count > 0 && s.IsResolved && s.Properties.Count > 0)
                discriminatedBasesWithSharedProps.Add(s.TypeName);
        }

        // Collect schemas whose shared properties need to be emitted as synthetic Core schemas.
        // We emit them after the main schema loop in deterministic (alphabetical) order.
        var syntheticCoreSchemas = new List<SchemaTypeInfo>();

        foreach (var schema in allSchemas)
        {
            sb.AppendLine($"    {schema.TypeName}:");

            // ── oneOf / discriminator (base of a polymorphic hierarchy) ───
            if (schema.SubTypes.Count > 0)
            {
                AppendPolymorphicBaseSchema(sb, schema);
                // If this base has shared properties, schedule them for a synthetic Core schema.
                if (schema.IsResolved && schema.Properties.Count > 0)
                    syntheticCoreSchemas.Add(schema);
                continue;
            }

            // ── allOf (derived type that extends a base schema) ────────
            if (schema.BaseSchemaName != null)
            {
                AppendAllOfSchema(sb, schema, discriminatedBasesWithSharedProps);
                continue;
            }

            // ── Regular object schema ──────────────────────────────────
            sb.AppendLine("      type: object");

            if (schema.IsResolved && schema.Properties.Count > 0)
            {
                AppendSchemaProperties(sb, schema);
            }
            else
            {
                sb.AppendLine($"      description: \"{EscapeYamlString(schema.TypeKind)} type - properties to be documented\"");
            }
        }

        // Emit synthetic Core schemas (sorted for determinism).
        syntheticCoreSchemas.Sort(static (a, b) => StringComparer.Ordinal.Compare(a.TypeName, b.TypeName));
        foreach (var baseSchema in syntheticCoreSchemas)
        {
            sb.AppendLine($"    {baseSchema.TypeName}__Core:");
            sb.AppendLine("      type: object");
            // Scalar-first policy: hide the structural __Core helper from the sidebar/navigation.
            // The $ref to this schema remains fully functional — only the display entry is suppressed.
            sb.AppendLine("      x-scalar-ignore: true");
            AppendSchemaProperties(sb, baseSchema);
        }

        // RFC § F13 — inject the canonical SwepayProblemDetails schema.
        // Emitted when emitStandardComponents is true (because components/responses/BadRequest
        // references it via $ref), OR when any endpoint directly serves application/problem+json
        // (those operations emit an inline $ref to this schema).
        // In merge-producer mode (emitStandardComponents=false) without any problem+json
        // endpoint, the common file owns the definition and we omit it to avoid conflicts.
        if (emitStandardComponents || AnyProblemJsonResponse(endpoints))
        {
            AppendSwepayProblemDetailsSchema(sb);
        }

        if (emitStandardComponents)
        {
            // BUG-2 fix: emit components/responses to back the $ref used in every operation.
            // Always emitted in standalone mode (the $refs are always present unless
            // the caller explicitly covers all three status codes via AdditionalProduces).
            AppendComponentsResponses(sb);

            // BUG-3 fix: emit components/securitySchemes when at least one endpoint requires auth.
            if (AnyAuthenticatedEndpoint(endpoints))
            {
                AppendSecuritySchemes(sb);
            }
        }
        // else: emitStandardComponents=false (merge-producer mode).
        // components/responses and securitySchemes are expected from the shared common files;
        // skip them to avoid merge conflicts.
        // SwepayProblemDetails is emitted above when directly referenced by operations.

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

    private static bool AnyAuthenticatedEndpoint(IReadOnlyList<EndpointInfo> endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            if (endpoint.RequiresAuth) return true;
        }
        return false;
    }

    /// <summary>
    /// Emits the <c>components/responses</c> section with the three standard error
    /// responses referenced by every operation: BadRequest, Unauthorized, InternalServerError.
    /// <para>
    /// BadRequest carries a <c>application/problem+json</c> body pointing at
    /// <c>SwepayProblemDetails</c>; the other two are description-only because
    /// they carry no structured payload.
    /// </para>
    /// </summary>
    private static void AppendComponentsResponses(StringBuilder sb)
    {
        sb.AppendLine("  responses:");
        sb.AppendLine("    BadRequest:");
        sb.AppendLine("      description: \"The request was invalid or malformed.\"");
        sb.AppendLine("      content:");
        sb.AppendLine("        application/problem+json:");
        sb.AppendLine("          schema:");
        sb.AppendLine("            $ref: \"#/components/schemas/SwepayProblemDetails\"");
        sb.AppendLine("    Unauthorized:");
        sb.AppendLine("      description: \"Authentication is required or the supplied credentials are invalid.\"");
        sb.AppendLine("    InternalServerError:");
        sb.AppendLine("      description: \"An unexpected error occurred on the server.\"");
    }

    /// <summary>
    /// Emits the <c>components/securitySchemes</c> section with a single
    /// <c>JwtBearer</c> scheme (HTTP Bearer / JWT). Emitted only when at least
    /// one endpoint is authenticated so the spec stays minimal when all routes
    /// are anonymous.
    /// </summary>
    private static void AppendSecuritySchemes(StringBuilder sb)
    {
        sb.AppendLine("  securitySchemes:");
        sb.AppendLine("    JwtBearer:");
        sb.AppendLine("      type: http");
        sb.AppendLine("      scheme: bearer");
        sb.AppendLine("      bearerFormat: JWT");
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
    /// Emits the <c>examples</c> sub-node of a request body.
    /// Supports both <c>externalValue</c> (path-based) and <c>value</c> (inline JSON).
    /// When both <c>RequestJsonPath</c> and <c>RequestInlineValue</c> are set,
    /// <c>externalValue</c> takes precedence.
    /// </summary>
    private static void AppendRequestExamples(StringBuilder sb, EndpointInfo endpoint)
    {
        var withRequest = endpoint.Examples
            .Where(e => !string.IsNullOrEmpty(e.RequestJsonPath) || !string.IsNullOrEmpty(e.RequestInlineValue))
            .ToList();
        if (withRequest.Count == 0) return;

        sb.AppendLine("            examples:");
        foreach (var example in withRequest)
        {
            sb.AppendLine($"              {example.Name}:");
            sb.AppendLine($"                summary: \"{EscapeYamlString(example.Summary)}\"");
            if (!string.IsNullOrEmpty(example.RequestJsonPath))
            {
                sb.AppendLine($"                externalValue: \"{EscapeYamlString(example.RequestJsonPath!)}\"");
            }
            else if (!string.IsNullOrEmpty(example.RequestInlineValue))
            {
                AppendInlineExampleValue(sb, example.RequestInlineValue!, indent: "                ");
            }
        }
    }

    /// <summary>
    /// Emits the <c>examples</c> sub-node of a response block, filtered by status code.
    /// Supports both <c>externalValue</c> (path-based) and <c>value</c> (inline JSON).
    /// When both are set, <c>externalValue</c> takes precedence.
    /// </summary>
    private static void AppendResponseExamples(StringBuilder sb, EndpointInfo endpoint, int statusCode)
    {
        var matching = endpoint.Examples
            .Where(e => e.ResponseStatus == statusCode
                && (!string.IsNullOrEmpty(e.ResponseJsonPath) || !string.IsNullOrEmpty(e.ResponseInlineValue)))
            .ToList();
        if (matching.Count == 0) return;

        sb.AppendLine("              examples:");
        foreach (var example in matching)
        {
            sb.AppendLine($"                {example.Name}:");
            sb.AppendLine($"                  summary: \"{EscapeYamlString(example.Summary)}\"");
            if (!string.IsNullOrEmpty(example.ResponseJsonPath))
            {
                sb.AppendLine($"                  externalValue: \"{EscapeYamlString(example.ResponseJsonPath!)}\"");
            }
            else if (!string.IsNullOrEmpty(example.ResponseInlineValue))
            {
                AppendInlineExampleValue(sb, example.ResponseInlineValue!, indent: "                  ");
            }
        }
    }

    /// <summary>
    /// Emits a <c>value:</c> key for an inline JSON example.
    /// Single-line JSON is emitted as a flow scalar on the same line.
    /// Multi-line JSON is emitted using the YAML literal block scalar (<c>|</c>).
    /// </summary>
    private static void AppendInlineExampleValue(StringBuilder sb, string json, string indent)
    {
        // Normalise line endings.
        var normalised = json.Replace("\r\n", "\n").Replace("\r", "\n");

        if (!normalised.Contains('\n'))
        {
            // Single-line: emit as a plain flow scalar. We do NOT quote it —
            // YAML parsers treat a JSON object/array as a valid flow scalar.
            // Use single quotes if the JSON contains a colon that might be
            // misinterpreted, but since we are inside a double-quoted context
            // already (the whole YAML string is not itself quoted) we emit raw.
            sb.AppendLine($"{indent}value: {normalised}");
        }
        else
        {
            // Multi-line: use block literal scalar.
            sb.AppendLine($"{indent}value: |");
            var innerIndent = indent + "  ";
            var lines = normalised.Split('\n');
            foreach (var line in lines)
            {
                sb.AppendLine(innerIndent + line);
            }
        }
    }

    /// <summary>
    /// Builds deduplicated and sorted schema types from all endpoints.
    /// When the same type appears in multiple endpoints, the version with resolved properties wins.
    /// Polymorphism metadata (allOf base, oneOf sub-types, discriminator) from
    /// <see cref="EndpointInfo.ReferencedSchemas"/> takes precedence over the simple
    /// command/response registration.
    /// </summary>
    private static List<SchemaTypeInfo> BuildSchemaTypes(IReadOnlyList<EndpointInfo> endpoints)
    {
        var schemaMap = new Dictionary<string, SchemaTypeInfo>(StringComparer.Ordinal);

        // 1. Merge polymorphism-aware schemas discovered during source generation.
        foreach (var endpoint in endpoints)
        {
            foreach (var schema in endpoint.ReferencedSchemas)
            {
                MergeSchemaInfo(schemaMap, schema);
            }
        }

        // 2. Register the canonical command/response schemas (may already exist from step 1).
        foreach (var endpoint in endpoints)
        {
            // Command schema — only if not already registered with richer data
            MergeSchema(schemaMap, endpoint.CommandSimpleName, "Request",
                endpoint.CommandProperties, endpoint.CommandPropertiesResolved);

            // Response schema
            MergeSchema(schemaMap, endpoint.ResponseSimpleName, "Response",
                endpoint.ResponseProperties, endpoint.ResponsePropertiesResolved);
        }

        return schemaMap.Values.OrderBy(s => s.TypeName, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Wraps <see cref="BuildSchemaTypes"/> and additionally registers any webhook payload
    /// type simple names as plain object schemas so they appear in <c>components/schemas</c>.
    /// </summary>
    private static List<SchemaTypeInfo> BuildSchemaTypesWithWebhooks(
        IReadOnlyList<EndpointInfo> endpoints,
        IReadOnlyList<WebhookInfo>? webhooks)
    {
        var schemaMap = new Dictionary<string, SchemaTypeInfo>(StringComparer.Ordinal);

        // Step 1: merge polymorphism-aware schemas from endpoints.
        foreach (var endpoint in endpoints)
        {
            foreach (var schema in endpoint.ReferencedSchemas)
                MergeSchemaInfo(schemaMap, schema);
        }

        // Step 2: register command/response schemas.
        foreach (var endpoint in endpoints)
        {
            MergeSchema(schemaMap, endpoint.CommandSimpleName, "Request",
                endpoint.CommandProperties, endpoint.CommandPropertiesResolved);
            MergeSchema(schemaMap, endpoint.ResponseSimpleName, "Response",
                endpoint.ResponseProperties, endpoint.ResponsePropertiesResolved);
        }

        // Step 3: register webhook payload schemas.
        // If the webhook carries pre-resolved ReferencedSchemas (MELHORIA-4), merge
        // those so the payload schema contains real properties. Fall back to an
        // unresolved stub only when no schema data is available (e.g., unit tests
        // that construct WebhookInfo by hand without running the source generator).
        if (webhooks != null)
        {
            foreach (var webhook in webhooks)
            {
                if (!string.IsNullOrEmpty(webhook.PayloadTypeName))
                {
                    if (webhook.ReferencedSchemas != null && webhook.ReferencedSchemas.Count > 0)
                    {
                        // Merge all discovered schemas (payload + transitive refs).
                        foreach (var schema in webhook.ReferencedSchemas)
                            MergeSchemaInfo(schemaMap, schema);
                    }
                    else
                    {
                        // Fallback: unresolved stub so the $ref target still exists.
                        MergeSchema(schemaMap, webhook.PayloadTypeName, "WebhookPayload",
                            new List<SchemaPropertyInfo>(), false);
                    }
                }
            }

            // Also register callback payload schemas.
            foreach (var endpoint in endpoints)
            {
                foreach (var cb in endpoint.Callbacks)
                {
                    if (!string.IsNullOrEmpty(cb.PayloadTypeName))
                    {
                        MergeSchema(schemaMap, cb.PayloadTypeName!, "CallbackPayload",
                            new List<SchemaPropertyInfo>(), false);
                    }
                }
            }
        }

        return schemaMap.Values.OrderBy(s => s.TypeName, StringComparer.Ordinal).ToList();
    }

    private static void MergeSchemaInfo(Dictionary<string, SchemaTypeInfo> map, SchemaTypeInfo incoming)
    {
        if (map.TryGetValue(incoming.TypeName, out var existing))
        {
            // Prefer resolved over unresolved
            if (!existing.IsResolved && incoming.IsResolved)
            {
                existing.Properties = incoming.Properties;
                existing.IsResolved = true;
            }

            // Polymorphism metadata: only set when not already present on existing entry.
            if (existing.BaseSchemaName == null && incoming.BaseSchemaName != null)
                existing.BaseSchemaName = incoming.BaseSchemaName;

            if (existing.SubTypes.Count == 0 && incoming.SubTypes.Count > 0)
                existing.SubTypes = incoming.SubTypes;

            if (existing.DiscriminatorPropertyName == null && incoming.DiscriminatorPropertyName != null)
                existing.DiscriminatorPropertyName = incoming.DiscriminatorPropertyName;
        }
        else
        {
            map[incoming.TypeName] = incoming;
        }
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
            // Prefer resolved over unresolved for properties
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

    // ── Polymorphism emitters ─────────────────────────────────────────

    /// <summary>
    /// Emits a polymorphic <b>base</b> schema as a pure <c>oneOf</c> + optional <c>discriminator</c>
    /// with NO <c>properties</c> at the same level (MELHORIA-6).
    /// Shared base properties (if any) are moved to a synthetic <c>{TypeName}__Core</c> schema
    /// emitted separately; derived types redirect their <c>allOf $ref</c> to the Core schema.
    /// </summary>
    /// <remarks>
    /// Emitted form (with discriminator):
    /// <code>
    /// Animal:
    ///   oneOf:
    ///     - $ref: '#/components/schemas/Cat'
    ///     - $ref: '#/components/schemas/Dog'
    ///   discriminator:
    ///     propertyName: kind
    ///     mapping:
    ///       cat: '#/components/schemas/Cat'
    ///       dog: '#/components/schemas/Dog'
    ///
    /// Animal__Core:
    ///   type: object
    ///   properties:
    ///     kind:
    ///       type: string
    /// </code>
    /// When no discriminator is declared only <c>oneOf</c> is emitted.
    /// </remarks>
    private static void AppendPolymorphicBaseSchema(StringBuilder sb, SchemaTypeInfo schema)
    {
        // Sort sub-types by schema name for deterministic emission.
        var sortedSubs = new List<SubTypeEntry>(schema.SubTypes);
        sortedSubs.Sort(static (a, b) => StringComparer.Ordinal.Compare(a.SchemaName, b.SchemaName));

        sb.AppendLine("      oneOf:");
        foreach (var sub in sortedSubs)
        {
            sb.AppendLine($"        - $ref: \"#/components/schemas/{EscapeYamlString(sub.SchemaName)}\"");
        }

        if (schema.DiscriminatorPropertyName != null)
        {
            sb.AppendLine("      discriminator:");
            sb.AppendLine($"        propertyName: {EscapeYamlString(schema.DiscriminatorPropertyName)}");
            sb.AppendLine("        mapping:");
            foreach (var sub in sortedSubs)
            {
                sb.AppendLine($"          {EscapeYamlString(sub.DiscriminatorValue)}: \"#/components/schemas/{EscapeYamlString(sub.SchemaName)}\"");
            }
        }

        // MELHORIA-6 (2026-06-02): Shared base properties are NO LONGER emitted here.
        // They are moved to a synthetic "{TypeName}__Core" schema (see the schema emission
        // loop in Generate) so that the discriminated-union base contains ONLY oneOf +
        // discriminator, satisfying strict Spectral/OWASP validators and the JSON Schema
        // 2020-12 recommendation.  Derived types redirect their allOf $ref to the Core
        // schema so shared properties remain in a single, non-duplicated location.
    }

    /// <summary>
    /// Emits a derived schema using <c>allOf</c>: a <c>$ref</c> to the base schema
    /// (or to the synthetic <c>{BaseSchemaName}__Core</c> schema when the base is a
    /// discriminated union with shared properties — MELHORIA-6), followed by an inline
    /// object with only the properties declared directly on the derived type.
    /// </summary>
    /// <remarks>
    /// Emitted form when base is a plain object (no shared props):
    /// <code>
    /// Cat:
    ///   allOf:
    ///     - $ref: '#/components/schemas/Animal'
    ///     - type: object
    ///       properties:
    ///         indoor:
    ///           type: boolean
    /// </code>
    /// Emitted form when base is a discriminated union with shared props (MELHORIA-6):
    /// <code>
    /// Cat:
    ///   allOf:
    ///     - $ref: '#/components/schemas/Animal__Core'
    ///     - type: object
    ///       properties:
    ///         indoor:
    ///           type: boolean
    /// </code>
    /// </remarks>
    private static void AppendAllOfSchema(
        StringBuilder sb,
        SchemaTypeInfo schema,
        HashSet<string>? discriminatedBasesWithSharedProps = null)
    {
        // MELHORIA-6: When the base is a discriminated union that carries shared properties,
        // those properties now live in a synthetic "{BaseSchemaName}__Core" schema. Redirect
        // the allOf $ref so derived types still inherit all shared properties without
        // duplicating them, while the discriminated-union base remains a pure oneOf schema.
        var baseName = schema.BaseSchemaName!;
        var refTarget = (discriminatedBasesWithSharedProps != null && discriminatedBasesWithSharedProps.Contains(baseName))
            ? baseName + "__Core"
            : baseName;

        sb.AppendLine("      allOf:");
        sb.AppendLine($"        - $ref: \"#/components/schemas/{EscapeYamlString(refTarget)}\"");

        if (schema.IsResolved && schema.Properties.Count > 0)
        {
            sb.AppendLine("        - type: object");
            AppendSchemaPropertiesIndented(sb, schema, baseIndent: "          ");
        }
        else
        {
            sb.AppendLine("        - type: object");
        }
    }

    /// <summary>
    /// Appends OpenAPI YAML property definitions for a resolved schema type.
    /// </summary>
    private static void AppendSchemaProperties(StringBuilder sb, SchemaTypeInfo schema)
        => AppendSchemaPropertiesIndented(sb, schema, baseIndent: "      ");

    /// <summary>
    /// Core implementation — emits properties, required list at a configurable indentation.
    /// </summary>
    private static void AppendSchemaPropertiesIndented(StringBuilder sb, SchemaTypeInfo schema, string baseIndent)
    {
        var propIndent = baseIndent + "  "; // 2 extra spaces for property name
        var fieldIndent = baseIndent + "    "; // 4 extra spaces for field value

        sb.AppendLine($"{baseIndent}properties:");

        foreach (var prop in schema.Properties)
        {
            sb.AppendLine($"{propIndent}{prop.JsonName}:");

            if (prop.RefSchemaName != null)
            {
                sb.AppendLine($"{fieldIndent}$ref: \"#/components/schemas/{prop.RefSchemaName}\"");
                AppendPropertyEnrichment(sb, prop, indent: fieldIndent);
                continue;
            }

            if (prop.IsEnum)
            {
                sb.AppendLine($"{fieldIndent}type: string");
                if (prop.EnumValues.Count > 0)
                {
                    sb.AppendLine($"{fieldIndent}enum:");
                    foreach (var val in prop.EnumValues)
                    {
                        sb.AppendLine($"{fieldIndent}  - {val}");
                    }
                }
                AppendPropertyEnrichment(sb, prop, indent: fieldIndent);
                AppendEnumExtensions(sb, prop, indent: fieldIndent);
                continue;
            }

            if (prop.OpenApiType == "array")
            {
                sb.AppendLine($"{fieldIndent}type: array");
                sb.AppendLine($"{fieldIndent}items:");
                if (prop.ArrayItemRefSchemaName != null)
                {
                    sb.AppendLine($"{fieldIndent}  $ref: \"#/components/schemas/{prop.ArrayItemRefSchemaName}\"");
                }
                else
                {
                    sb.AppendLine($"{fieldIndent}  type: {prop.ArrayItemType ?? "string"}");
                    if (prop.ArrayItemFormat != null)
                    {
                        sb.AppendLine($"{fieldIndent}  format: {prop.ArrayItemFormat}");
                    }
                }
                AppendPropertyEnrichment(sb, prop, indent: fieldIndent);
                AppendArrayConstraints(sb, prop, indent: fieldIndent);
                continue;
            }

            // Scalar or object types
            sb.AppendLine($"{fieldIndent}type: {prop.OpenApiType}");
            if (prop.OpenApiFormat != null)
            {
                sb.AppendLine($"{fieldIndent}format: {prop.OpenApiFormat}");
            }
            AppendPropertyEnrichment(sb, prop, indent: fieldIndent);

            if (prop.IsDictionary)
            {
                sb.AppendLine($"{fieldIndent}additionalProperties: true");
                if (!string.IsNullOrEmpty(prop.AdditionalPropertiesName))
                {
                    sb.AppendLine($"{fieldIndent}x-additionalPropertiesName: \"{EscapeYamlString(prop.AdditionalPropertiesName!)}\"");
                }
            }

            AppendStringConstraints(sb, prop, indent: fieldIndent);
            AppendNumericConstraints(sb, prop, indent: fieldIndent);
        }

        // Required fields
        var requiredProps = schema.Properties.Where(p => p.IsRequired).ToList();
        if (requiredProps.Count > 0)
        {
            sb.AppendLine($"{baseIndent}required:");
            foreach (var prop in requiredProps)
            {
                sb.AppendLine($"{propIndent}- {prop.JsonName}");
            }
        }
    }

    /// <summary>
    /// Emits common enrichment fields: description, example, default, x-order.
    /// Called for all property types (string, number, array, enum, $ref).
    /// </summary>
    private static void AppendPropertyEnrichment(StringBuilder sb, SchemaPropertyInfo prop, string indent)
    {
        if (prop.Description != null)
            sb.AppendLine($"{indent}description: \"{EscapeYamlString(prop.Description)}\"");

        if (prop.Example != null)
            sb.AppendLine($"{indent}example: {FormatScalarValue(prop.Example)}");

        if (prop.Default != null)
            sb.AppendLine($"{indent}default: {FormatScalarValue(prop.Default)}");

        if (prop.Order >= 0)
            sb.AppendLine($"{indent}x-order: {prop.Order}");
    }

    /// <summary>
    /// Emits string-specific constraints: minLength, maxLength, pattern.
    /// </summary>
    private static void AppendStringConstraints(StringBuilder sb, SchemaPropertyInfo prop, string indent)
    {
        if (prop.MinLength >= 0)
            sb.AppendLine($"{indent}minLength: {prop.MinLength}");

        if (prop.MaxLength >= 0)
            sb.AppendLine($"{indent}maxLength: {prop.MaxLength}");

        if (prop.Pattern != null)
            sb.AppendLine($"{indent}pattern: \"{EscapeYamlString(prop.Pattern)}\"");
    }

    /// <summary>
    /// Emits numeric-specific constraints: minimum, maximum, exclusiveMinimum, exclusiveMaximum, multipleOf.
    /// </summary>
    private static void AppendNumericConstraints(StringBuilder sb, SchemaPropertyInfo prop, string indent)
    {
        if (!double.IsNaN(prop.Minimum))
            sb.AppendLine($"{indent}minimum: {FormatDouble(prop.Minimum)}");

        if (!double.IsNaN(prop.Maximum))
            sb.AppendLine($"{indent}maximum: {FormatDouble(prop.Maximum)}");

        if (!double.IsNaN(prop.ExclusiveMinimum))
            sb.AppendLine($"{indent}exclusiveMinimum: {FormatDouble(prop.ExclusiveMinimum)}");

        if (!double.IsNaN(prop.ExclusiveMaximum))
            sb.AppendLine($"{indent}exclusiveMaximum: {FormatDouble(prop.ExclusiveMaximum)}");

        if (!double.IsNaN(prop.MultipleOf))
            sb.AppendLine($"{indent}multipleOf: {FormatDouble(prop.MultipleOf)}");
    }

    /// <summary>
    /// Emits array-specific constraints: minItems, maxItems, uniqueItems.
    /// </summary>
    private static void AppendArrayConstraints(StringBuilder sb, SchemaPropertyInfo prop, string indent)
    {
        if (prop.MinItems >= 0)
            sb.AppendLine($"{indent}minItems: {prop.MinItems}");

        if (prop.MaxItems >= 0)
            sb.AppendLine($"{indent}maxItems: {prop.MaxItems}");

        if (prop.UniqueItems)
            sb.AppendLine($"{indent}uniqueItems: true");
    }

    /// <summary>
    /// Emits Scalar-first enum extensions: x-enum-descriptions, x-enum-varnames.
    /// These are parallel arrays to the <c>enum</c> values list.
    /// </summary>
    private static void AppendEnumExtensions(StringBuilder sb, SchemaPropertyInfo prop, string indent)
    {
        if (prop.EnumDescriptions != null && prop.EnumDescriptions.Count > 0)
        {
            sb.AppendLine($"{indent}x-enum-descriptions:");
            foreach (var desc in prop.EnumDescriptions)
            {
                var safe = desc != null ? $"\"{EscapeYamlString(desc)}\"" : "\"\"";
                sb.AppendLine($"{indent}  - {safe}");
            }
        }

        if (prop.EnumVarNames != null && prop.EnumVarNames.Count > 0)
        {
            sb.AppendLine($"{indent}x-enum-varnames:");
            foreach (var varName in prop.EnumVarNames)
            {
                var safe = varName != null ? $"\"{EscapeYamlString(varName)}\"" : "\"\"";
                sb.AppendLine($"{indent}  - {safe}");
            }
        }
    }

    /// <summary>
    /// Formats a double value for YAML emission.
    /// Integers are emitted without decimal point; fractional values use invariant culture.
    /// </summary>
    private static string FormatDouble(double value)
    {
        // Emit as integer when it is a whole number (avoids "1.0" noise in spec)
        if (value == System.Math.Floor(value) && !double.IsInfinity(value))
            return ((long)value).ToString(System.Globalization.CultureInfo.InvariantCulture);

        return value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a string value for YAML scalar emission.
    /// Numbers and booleans are emitted unquoted; everything else is double-quoted.
    /// </summary>
    private static string FormatScalarValue(string value)
    {
        // Numeric literal?
        if (double.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            return value;
        }

        // Boolean literal?
        if (value == "true" || value == "false" || value == "null")
            return value;

        return $"\"{EscapeYamlString(value)}\"";
    }

    /// <summary>
    /// Collects all distinct tag names from the endpoint list, in the same order they
    /// would be emitted in operations (first occurrence wins for ordering). This ensures
    /// the root <c>tags:</c> list references exactly the tags actually used.
    /// </summary>
    private static List<string> CollectAllTagNames(IReadOnlyList<EndpointInfo> endpoints)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();

        foreach (var endpoint in endpoints.OrderBy(e => e.Path).ThenBy(e => e.Method))
        {
            IEnumerable<string> tags = endpoint.Tags.Count > 0
                ? endpoint.Tags
                : new[] { ExtractTag(endpoint.Path) };

            foreach (var tag in tags)
            {
                if (seen.Add(tag))
                    ordered.Add(tag);
            }
        }

        // Sort for determinism.
        ordered.Sort(StringComparer.Ordinal);
        return ordered;
    }

    /// <summary>
    /// Emits the <c>x-codeSamples</c> array for an operation.
    /// Each sample is sorted by <c>lang</c> then by <c>label</c> (or <c>lang</c>
    /// when label is absent) for deterministic output across builds.
    /// Multi-line source is emitted using the YAML block-scalar literal style (<c>|</c>)
    /// with 8-space base indentation so the content sits correctly inside the operation object.
    /// Single-line source is emitted as a quoted scalar to avoid ambiguity with special chars.
    /// </summary>
    private static void AppendCodeSamples(StringBuilder sb, EndpointInfo endpoint)
    {
        if (endpoint.CodeSamples.Count == 0) return;

        // Deterministic order: lang asc, then label asc (nulls-last via empty string fallback).
        var sorted = new List<CodeSampleInfo>(endpoint.CodeSamples);
        sorted.Sort(static (a, b) =>
        {
            var langCmp = string.Compare(a.Lang, b.Lang, System.StringComparison.OrdinalIgnoreCase);
            if (langCmp != 0) return langCmp;
            var labelA = a.Label ?? a.Lang;
            var labelB = b.Label ?? b.Lang;
            return string.Compare(labelA, labelB, System.StringComparison.OrdinalIgnoreCase);
        });

        sb.AppendLine("      x-codeSamples:");
        foreach (var sample in sorted)
        {
            sb.AppendLine($"        - lang: {EscapeYamlString(sample.Lang)}");
            if (!string.IsNullOrEmpty(sample.Label))
                sb.AppendLine($"          label: \"{EscapeYamlString(sample.Label!)}\"");

            // Decide between block-scalar (multi-line) and quoted scalar (single-line).
            if (sample.Source.Contains('\n') || sample.Source.Contains('\r'))
            {
                sb.AppendLine("          source: |");
                // Indent every line of the source by 12 spaces (inside the list item).
                var lines = sample.Source.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                foreach (var line in lines)
                {
                    sb.AppendLine("            " + line);
                }
            }
            else
            {
                sb.AppendLine($"          source: \"{EscapeYamlString(sample.Source)}\"");
            }
        }
    }

    /// <summary>
    /// Emits the <c>x-badges</c> array for an operation.
    /// Badges are sorted by name for determinism.
    /// </summary>
    private static void AppendBadges(StringBuilder sb, EndpointInfo endpoint)
    {
        if (endpoint.Badges.Count == 0) return;

        // Deterministic order: name asc.
        var sorted = new List<OperationBadgeInfo>(endpoint.Badges);
        sorted.Sort(static (a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));

        sb.AppendLine("      x-badges:");
        foreach (var badge in sorted)
        {
            sb.AppendLine($"        - name: \"{EscapeYamlString(badge.Name)}\"");
            if (!string.IsNullOrEmpty(badge.Position))
                sb.AppendLine($"          position: \"{EscapeYamlString(badge.Position!)}\"");
            if (!string.IsNullOrEmpty(badge.Color))
                sb.AppendLine($"          color: \"{EscapeYamlString(badge.Color!)}\"");
        }
    }

    // ── Structural Wave 5: parameters, response headers/links, callbacks, webhooks ──

    /// <summary>
    /// Emits a single explicit parameter entry (query or header) at the standard
    /// 8-space operation parameters indentation.
    /// </summary>
    private static void AppendExplicitParameter(StringBuilder sb, ExplicitParameterInfo param)
    {
        sb.AppendLine($"        - name: \"{EscapeYamlString(param.Name)}\"");
        sb.AppendLine($"          in: {param.In}");
        sb.AppendLine($"          required: {(param.Required ? "true" : "false")}");
        if (!string.IsNullOrEmpty(param.Description))
            sb.AppendLine($"          description: \"{EscapeYamlString(param.Description!)}\"");
        sb.AppendLine("          schema:");
        sb.AppendLine($"            type: {param.SchemaType}");
        if (!string.IsNullOrEmpty(param.SchemaFormat))
            sb.AppendLine($"            format: {param.SchemaFormat}");
    }

    /// <summary>
    /// Emits <c>headers:</c> under a response object for the given status code.
    /// Sorted by header name for determinism.
    /// </summary>
    private static void AppendResponseHeaders(StringBuilder sb, EndpointInfo endpoint, int statusCode)
    {
        var matching = new List<ResponseHeaderInfo>();
        foreach (var h in endpoint.ResponseHeaders)
        {
            if (h.StatusCode == statusCode) matching.Add(h);
        }
        if (matching.Count == 0) return;

        matching.Sort(static (a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));

        sb.AppendLine("          headers:");
        foreach (var header in matching)
        {
            sb.AppendLine($"            \"{EscapeYamlString(header.Name)}\":");
            if (!string.IsNullOrEmpty(header.Description))
                sb.AppendLine($"              description: \"{EscapeYamlString(header.Description!)}\"");
            sb.AppendLine($"              required: {(header.Required ? "true" : "false")}");
            sb.AppendLine("              schema:");
            sb.AppendLine($"                type: {header.SchemaType}");
            if (!string.IsNullOrEmpty(header.SchemaFormat))
                sb.AppendLine($"                format: {header.SchemaFormat}");
        }
    }

    /// <summary>
    /// Emits <c>links:</c> under a response object for the given status code.
    /// Sorted by link id for determinism.
    /// </summary>
    private static void AppendResponseLinks(StringBuilder sb, EndpointInfo endpoint, int statusCode)
    {
        var matching = new List<ResponseLinkInfo>();
        foreach (var l in endpoint.ResponseLinks)
        {
            if (l.StatusCode == statusCode) matching.Add(l);
        }
        if (matching.Count == 0) return;

        matching.Sort(static (a, b) => string.Compare(a.LinkId, b.LinkId, System.StringComparison.Ordinal));

        sb.AppendLine("          links:");
        foreach (var link in matching)
        {
            sb.AppendLine($"            {EscapeYamlString(link.LinkId)}:");
            if (!string.IsNullOrEmpty(link.OperationId))
                sb.AppendLine($"              operationId: {EscapeYamlString(link.OperationId!)}");
            if (!string.IsNullOrEmpty(link.Description))
                sb.AppendLine($"              description: \"{EscapeYamlString(link.Description!)}\"");
            if (!string.IsNullOrEmpty(link.Parameters))
                AppendLinkParameters(sb, link.Parameters!);
        }
    }

    /// <summary>
    /// Parses the compact "key=$expr,key2=$expr2" notation from <c>[ResponseLink]</c>
    /// and emits as the YAML <c>parameters:</c> map under a link object.
    /// Splitting is done manually to stay netstandard2.0-safe (no Span, no ranges).
    /// </summary>
    private static void AppendLinkParameters(StringBuilder sb, string parametersString)
    {
        sb.AppendLine("              parameters:");
        // Split on comma, then split each pair on the first '='
        var pairs = parametersString.Split(',');
        foreach (var pair in pairs)
        {
            var trimmed = pair.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var eqIdx = trimmed.IndexOf('=');
            if (eqIdx <= 0) continue;

            var key = trimmed.Substring(0, eqIdx).Trim();
            var expr = trimmed.Substring(eqIdx + 1).Trim();
            sb.AppendLine($"                {EscapeYamlString(key)}: \"{EscapeYamlString(expr)}\"");
        }
    }

    /// <summary>
    /// Emits the <c>callbacks</c> map for an operation.
    /// Sorted by callback name for determinism.
    /// </summary>
    private static void AppendCallbacks(StringBuilder sb, EndpointInfo endpoint)
    {
        if (endpoint.Callbacks.Count == 0) return;

        var sorted = new List<CallbackInfo>(endpoint.Callbacks);
        sorted.Sort(static (a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));

        sb.AppendLine("      callbacks:");
        foreach (var cb in sorted)
        {
            sb.AppendLine($"        {EscapeYamlString(cb.Name)}:");
            // Expression is the path-expression key
            var expr = string.IsNullOrEmpty(cb.Expression) ? "{$request.body#/callbackUrl}" : cb.Expression;
            sb.AppendLine($"          \"{EscapeYamlString(expr)}\":");
            var method = string.IsNullOrEmpty(cb.Method) ? "post" : cb.Method.ToLowerInvariant();
            sb.AppendLine($"            {method}:");
            if (!string.IsNullOrEmpty(cb.Summary))
                sb.AppendLine($"              summary: \"{EscapeYamlString(cb.Summary!)}\"");
            if (!string.IsNullOrEmpty(cb.PayloadTypeName))
            {
                sb.AppendLine("              requestBody:");
                sb.AppendLine("                required: true");
                sb.AppendLine("                content:");
                sb.AppendLine("                  application/json:");
                sb.AppendLine("                    schema:");
                sb.AppendLine($"                      $ref: \"#/components/schemas/{EscapeYamlString(cb.PayloadTypeName!)}\"");
            }
            sb.AppendLine("              responses:");
            sb.AppendLine("                \"200\":");
            sb.AppendLine("                  description: \"Callback received\"");
        }
    }

    /// <summary>
    /// Emits the top-level <c>webhooks:</c> section (OpenAPI 3.1 §4.8.10).
    /// Each webhook is sorted alphabetically by name for determinism regardless
    /// of the declaration order in the caller.
    /// </summary>
    private static void AppendWebhooks(StringBuilder sb, IReadOnlyList<WebhookInfo> webhooks)
    {
        // Sort defensively — ReadWebhooks already sorts, but tests may pass unsorted lists.
        var sorted = new List<WebhookInfo>(webhooks);
        sorted.Sort(static (a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));

        sb.AppendLine("webhooks:");
        foreach (var wh in sorted)
        {
            var method = string.IsNullOrEmpty(wh.Method) ? "post" : wh.Method.ToLowerInvariant();
            sb.AppendLine($"  {EscapeYamlString(wh.Name)}:");
            sb.AppendLine($"    {method}:");
            if (!string.IsNullOrEmpty(wh.Summary))
                sb.AppendLine($"      summary: \"{EscapeYamlString(wh.Summary!)}\"");
            if (!string.IsNullOrEmpty(wh.Description))
                sb.AppendLine($"      description: \"{EscapeYamlString(wh.Description!)}\"");
            if (wh.Tags != null && wh.Tags.Count > 0)
            {
                sb.AppendLine("      tags:");
                foreach (var tag in wh.Tags)
                    sb.AppendLine($"        - \"{EscapeYamlString(tag)}\"");
            }
            sb.AppendLine("      requestBody:");
            sb.AppendLine("        required: true");
            sb.AppendLine("        content:");
            sb.AppendLine("          application/json:");
            sb.AppendLine("            schema:");
            sb.AppendLine($"              $ref: \"#/components/schemas/{EscapeYamlString(wh.PayloadTypeName)}\"");
            sb.AppendLine("      responses:");
            sb.AppendLine("        \"200\":");
            sb.AppendLine("          description: \"Webhook received successfully\"");
        }
    }

    // ── Wave 3: info block + servers ─────────────────────────────────

    /// <summary>
    /// Emits the <c>info:</c> block with title, version, and optional rich fields
    /// (summary, description, termsOfService, contact, license).
    /// </summary>
    private static void AppendInfoBlock(StringBuilder sb, string apiTitle, string apiVersion, OpenApiInfoMetadata? meta)
    {
        sb.AppendLine("info:");
        sb.AppendLine($"  title: \"{EscapeYamlString(apiTitle)}\"");
        sb.AppendLine($"  version: \"{EscapeYamlString(apiVersion)}\"");

        if (meta == null) return;

        if (!string.IsNullOrEmpty(meta.Summary))
            sb.AppendLine($"  summary: \"{EscapeYamlString(meta.Summary!)}\"");

        if (!string.IsNullOrEmpty(meta.Description))
            sb.AppendLine($"  description: \"{EscapeYamlString(meta.Description!)}\"");

        if (!string.IsNullOrEmpty(meta.TermsOfService))
            sb.AppendLine($"  termsOfService: \"{EscapeYamlString(meta.TermsOfService!)}\"");

        // contact:
        var hasContact = !string.IsNullOrEmpty(meta.ContactName)
            || !string.IsNullOrEmpty(meta.ContactUrl)
            || !string.IsNullOrEmpty(meta.ContactEmail);
        if (hasContact)
        {
            sb.AppendLine("  contact:");
            if (!string.IsNullOrEmpty(meta.ContactName))
                sb.AppendLine($"    name: \"{EscapeYamlString(meta.ContactName!)}\"");
            if (!string.IsNullOrEmpty(meta.ContactUrl))
                sb.AppendLine($"    url: \"{EscapeYamlString(meta.ContactUrl!)}\"");
            if (!string.IsNullOrEmpty(meta.ContactEmail))
                sb.AppendLine($"    email: \"{EscapeYamlString(meta.ContactEmail!)}\"");
        }

        // license:
        if (!string.IsNullOrEmpty(meta.LicenseName))
        {
            sb.AppendLine("  license:");
            sb.AppendLine($"    name: \"{EscapeYamlString(meta.LicenseName!)}\"");
            if (!string.IsNullOrEmpty(meta.LicenseUrl))
            {
                // Per OpenAPI 3.1: use 'identifier' when no "://" present (SPDX id), else 'url'.
                if (meta.LicenseUrl!.Contains("://"))
                    sb.AppendLine($"    url: \"{EscapeYamlString(meta.LicenseUrl)}\"");
                else
                    sb.AppendLine($"    identifier: \"{EscapeYamlString(meta.LicenseUrl)}\"");
            }
        }
    }

    /// <summary>
    /// Emits the root <c>servers:</c> array.
    /// Servers are emitted in the order they were declared (declaration-order stability).
    /// </summary>
    private static void AppendServers(StringBuilder sb, IReadOnlyList<OpenApiServerInfo> servers)
    {
        sb.AppendLine("servers:");
        foreach (var server in servers)
        {
            sb.AppendLine($"  - url: \"{EscapeYamlString(server.Url)}\"");
            if (!string.IsNullOrEmpty(server.Description))
                sb.AppendLine($"    description: \"{EscapeYamlString(server.Description!)}\"");
        }
        sb.AppendLine();
    }

    private static string EscapeYamlString(string value)
    {
        // Order matters: escape backslash first, then double-quote, then control chars.
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
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
