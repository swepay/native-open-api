namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Represents an endpoint discovered from the source code.
/// </summary>
internal sealed class EndpointInfo
{
    /// <summary>
    /// The HTTP method (GET, POST, PUT, DELETE, PATCH).
    /// </summary>
    public string Method { get; set; } = "";

    /// <summary>
    /// The route path (e.g., "/items", "/items/{id}").
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// The fully qualified type name of the Command.
    /// </summary>
    public string CommandTypeName { get; set; } = "";

    /// <summary>
    /// The fully qualified type name of the Response.
    /// </summary>
    public string ResponseTypeName { get; set; } = "";

    /// <summary>
    /// The simple name of the Command type (without namespace).
    /// </summary>
    public string CommandSimpleName { get; set; } = "";

    /// <summary>
    /// The simple name of the Response type (without namespace).
    /// </summary>
    public string ResponseSimpleName { get; set; } = "";

    /// <summary>
    /// Whether the endpoint requires authorization.
    /// </summary>
    public bool RequiresAuth { get; set; } = true;

    /// <summary>
    /// The content type produced by this endpoint.
    /// </summary>
    public string? ProducesContentType { get; set; }

    /// <summary>
    /// The content type accepted by this endpoint for request body.
    /// When null, defaults to "application/json" for POST/PUT/PATCH methods.
    /// Set via .Accepts("application/x-www-form-urlencoded") or [Accepts].
    /// </summary>
    public string? AcceptsContentType { get; set; }

    /// <summary>
    /// Custom operationId specified via .WithName() or [EndpointName].
    /// When null, the generator auto-generates one from the path.
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Custom summary specified via .WithSummary() or [EndpointSummary].
    /// When null, the generator auto-generates one from the HTTP method and response type.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Custom description specified via .WithDescription() or [EndpointDescription].
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Custom tags specified via .WithTags() or [Tags].
    /// When empty, the generator auto-generates one from the path.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Additional response definitions specified via .ProducesProblem() or [ApiResponse] attributes.
    /// Each entry is (StatusCode, ResponseTypeSimpleName, ContentType).
    /// </summary>
    public List<ProducesInfo> AdditionalProduces { get; set; } = new();

    /// <summary>
    /// The resolved properties for the Command type.
    /// </summary>
    public List<SchemaPropertyInfo> CommandProperties { get; set; } = new();

    /// <summary>
    /// The resolved properties for the Response type.
    /// </summary>
    public List<SchemaPropertyInfo> ResponseProperties { get; set; } = new();

    /// <summary>
    /// Whether the Command type properties were resolved from the semantic model.
    /// </summary>
    public bool CommandPropertiesResolved { get; set; }

    /// <summary>
    /// Whether the Response type properties were resolved from the semantic model.
    /// </summary>
    public bool ResponsePropertiesResolved { get; set; }

    /// <summary>
    /// Additional schema type infos discovered while processing command/response types.
    /// Includes polymorphism metadata (allOf base, oneOf sub-types, discriminator).
    /// These schemas are merged into <c>components/schemas</c> by the YAML generator.
    /// </summary>
    public List<SchemaTypeInfo> ReferencedSchemas { get; set; } = new();

    /// <summary>
    /// The source file where this endpoint was defined.
    /// </summary>
    public string? SourceFile { get; set; }

    /// <summary>
    /// The line number where this endpoint was defined.
    /// </summary>
    public int LineNumber { get; set; }

    // ------------------------------------------------------------------
    // RFC-DOCUMENTACAO-UX § Wave 1 additions.
    // ------------------------------------------------------------------

    /// <summary>
    /// True when the endpoint should be omitted from the generated YAML.
    /// Set by <c>[HideFromDocs]</c> on the TCommand or by
    /// <c>.ExcludeFromDocs()</c> in the fluent chain (RFC § F01).
    /// </summary>
    public bool ExcludedFromDocs { get; set; }

    /// <summary>
    /// Deprecation metadata resolved from <c>[Deprecated]</c> on the TCommand.
    /// <c>null</c> when the endpoint is not deprecated (RFC § F03).
    /// </summary>
    public DeprecationInfo? Deprecation { get; set; }

    /// <summary>
    /// Named examples resolved from one or more <c>[ApiExample]</c> attributes
    /// on the TCommand (RFC § F09). Empty when none.
    /// </summary>
    public List<ApiExampleInfo> Examples { get; set; } = new();

    /// <summary>
    /// Fully-qualified type name of the catalog referenced via
    /// <c>[ErrorCatalog(typeof(...))]</c>, or <c>null</c> (RFC § F12).
    /// </summary>
    public string? ErrorCatalogTypeName { get; set; }

    /// <summary>
    /// Subset of error codes (from the catalog) whose <c>httpStatus</c> matches
    /// at least one response declared on this endpoint. Emitted as
    /// <c>x-swepay-errors</c>.
    /// </summary>
    public List<string> MatchedErrorCodes { get; set; } = new();

    /// <summary>
    /// Per-operation external documentation link resolved from
    /// <c>[EndpointExternalDocs(url, description?)]</c> on the TCommand type.
    /// <c>null</c> when not declared.
    /// </summary>
    public ExternalDocsInfo? ExternalDocs { get; set; }

    // ------------------------------------------------------------------
    // Operation Richness § Wave 2 additions.
    // ------------------------------------------------------------------

    /// <summary>
    /// Code samples resolved from one or more <c>[CodeSample]</c> attributes
    /// on the TCommand type. Emitted as <c>x-codeSamples</c> (Redoc/Scalar).
    /// Empty when none declared.
    /// </summary>
    public List<CodeSampleInfo> CodeSamples { get; set; } = new();

    /// <summary>
    /// Badges resolved from one or more <c>[OperationBadge]</c> attributes
    /// on the TCommand type. Emitted as <c>x-badges</c>.
    /// Empty when none declared.
    /// </summary>
    public List<OperationBadgeInfo> Badges { get; set; } = new();

    /// <summary>
    /// Scalar stability marker resolved from <c>[ScalarStability]</c> on the TCommand.
    /// Valid values: "stable", "experimental", "deprecated". <c>null</c> when not declared.
    /// Emitted as <c>x-scalar-stability</c> (Scalar-first, no Redoc equivalent).
    /// </summary>
    public string? ScalarStability { get; set; }

    // ------------------------------------------------------------------
    // Structural OpenAPI 3.1 § Wave 5 additions.
    // ------------------------------------------------------------------

    /// <summary>
    /// Explicit query parameters resolved from one or more <c>[QueryParameter]</c>
    /// attributes on the TCommand type. Emitted as <c>parameters</c> entries
    /// with <c>in: query</c>.
    /// </summary>
    public List<ExplicitParameterInfo> QueryParameters { get; set; } = new();

    /// <summary>
    /// Explicit header parameters resolved from one or more <c>[HeaderParameter]</c>
    /// attributes on the TCommand type. Emitted as <c>parameters</c> entries
    /// with <c>in: header</c>.
    /// </summary>
    public List<ExplicitParameterInfo> HeaderParameters { get; set; } = new();

    /// <summary>
    /// Response headers resolved from one or more <c>[ResponseHeader]</c> attributes
    /// on the TCommand type. Emitted inside <c>responses.{code}.headers</c>.
    /// </summary>
    public List<ResponseHeaderInfo> ResponseHeaders { get; set; } = new();

    /// <summary>
    /// Response links resolved from one or more <c>[ResponseLink]</c> attributes
    /// on the TCommand type. Emitted inside <c>responses.{code}.links</c>.
    /// </summary>
    public List<ResponseLinkInfo> ResponseLinks { get; set; } = new();

    /// <summary>
    /// Callbacks resolved from one or more <c>[Callback]</c> attributes on the TCommand type.
    /// Emitted as <c>callbacks</c> on the operation. Minimal form: name + expression + HTTP method.
    /// </summary>
    public List<CallbackInfo> Callbacks { get; set; } = new();
}

/// <summary>
/// Deprecation metadata captured from <c>[Deprecated(sunset, alternative, reason)]</c>.
/// </summary>
internal sealed class DeprecationInfo
{
    public string Sunset { get; set; } = "";
    public string Alternative { get; set; } = "";
    public string Reason { get; set; } = "";
}

/// <summary>
/// Named example captured from an <c>[ApiExample]</c> attribute.
/// </summary>
internal sealed class ApiExampleInfo
{
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";

    // ── externalValue paths (original Wave 1 mechanism) ──
    public string? RequestJsonPath { get; set; }
    public int ResponseStatus { get; set; }
    public string? ResponseJsonPath { get; set; }

    // ── inline value (Wave 3: value: <json>) ──
    /// <summary>
    /// Raw JSON string to embed as <c>value:</c> in the request example.
    /// When non-null, takes precedence over <see cref="RequestJsonPath"/>.
    /// </summary>
    public string? RequestInlineValue { get; set; }

    /// <summary>
    /// Raw JSON string to embed as <c>value:</c> in the response example.
    /// When non-null, takes precedence over <see cref="ResponseJsonPath"/>.
    /// </summary>
    public string? ResponseInlineValue { get; set; }
}

/// <summary>
/// Rich <c>info</c> block metadata captured from <c>[OpenApiInfo]</c> at assembly level.
/// </summary>
internal sealed class OpenApiInfoMetadata
{
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public string? TermsOfService { get; set; }
    public string? ContactName { get; set; }
    public string? ContactUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? LicenseName { get; set; }
    /// <summary>
    /// SPDX identifier (e.g. "Apache-2.0") or a URL. Mutually exclusive with
    /// <see cref="LicenseUrl"/> per OpenAPI 3.1.
    /// </summary>
    public string? LicenseUrl { get; set; }
}

/// <summary>
/// A single server entry captured from <c>[OpenApiServer]</c> at assembly level.
/// Emitted as an entry in the root <c>servers:</c> array.
/// </summary>
internal sealed class OpenApiServerInfo
{
    public string Url { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>
/// External documentation reference captured from
/// <c>[EndpointExternalDocs(url, description?)]</c> on a command type, or from
/// <c>[OpenApiExternalDocs(url, description?)]</c> at assembly level.
/// </summary>
internal sealed class ExternalDocsInfo
{
    public string Url { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>
/// Tag metadata captured from <c>[TagMetadata(name, ...)]</c> at assembly level.
/// </summary>
internal sealed class TagMetadataInfo
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? DisplayName { get; set; }
    public ExternalDocsInfo? ExternalDocs { get; set; }
}

/// <summary>
/// Tag group captured from <c>[TagGroup(name, tags[])]</c> at assembly level.
/// </summary>
internal sealed class TagGroupInfo
{
    public string Name { get; set; } = "";
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Entry captured from a field tagged with
/// <c>[ErrorDefinition(code, httpStatus, userMessage, recovery, docUrl?)]</c>
/// inside a catalog class.
/// </summary>
internal sealed class ErrorCatalogEntry
{
    public string Code { get; set; } = "";
    public int HttpStatus { get; set; }
    public string UserMessage { get; set; } = "";
    public string Recovery { get; set; } = "";
    public string? DocUrl { get; set; }
}

/// <summary>
/// A single code sample captured from a <c>[CodeSample]</c> attribute.
/// Emitted as one entry in the <c>x-codeSamples</c> array.
/// </summary>
internal sealed class CodeSampleInfo
{
    /// <summary>Language identifier, e.g. "curl", "csharp", "python", "javascript".</summary>
    public string Lang { get; set; } = "";

    /// <summary>Optional human-readable label, e.g. "cURL (Linux)".</summary>
    public string? Label { get; set; }

    /// <summary>The source code of the sample.</summary>
    public string Source { get; set; } = "";
}

/// <summary>
/// A badge captured from an <c>[OperationBadge]</c> attribute.
/// Emitted as one entry in the <c>x-badges</c> array.
/// </summary>
internal sealed class OperationBadgeInfo
{
    /// <summary>Display name of the badge, e.g. "beta", "internal".</summary>
    public string Name { get; set; } = "";

    /// <summary>Optional position hint: "before" or "after" (UI-specific).</summary>
    public string? Position { get; set; }

    /// <summary>Optional CSS color string or named color, e.g. "#f00", "red".</summary>
    public string? Color { get; set; }
}

// ── Structural OpenAPI 3.1 § Wave 5 model types ──────────────────────────────

/// <summary>
/// An explicit query or header parameter captured from <c>[QueryParameter]</c>
/// or <c>[HeaderParameter]</c> on a command type.
/// </summary>
internal sealed class ExplicitParameterInfo
{
    /// <summary>Parameter name (e.g. "page", "X-Tenant-Id").</summary>
    public string Name { get; set; } = "";

    /// <summary>"query" or "header".</summary>
    public string In { get; set; } = "";

    /// <summary>OpenAPI schema type (string, integer, boolean, number). Default: string.</summary>
    public string SchemaType { get; set; } = "string";

    /// <summary>OpenAPI format (int32, int64, etc.). Null when not applicable.</summary>
    public string? SchemaFormat { get; set; }

    /// <summary>Whether the parameter is required.</summary>
    public bool Required { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// A response header captured from <c>[ResponseHeader]</c> on a command type.
/// Emitted as <c>responses.{statusCode}.headers.{name}</c>.
/// </summary>
internal sealed class ResponseHeaderInfo
{
    /// <summary>HTTP status code this header applies to (e.g. 200, 201).</summary>
    public int StatusCode { get; set; }

    /// <summary>Header name (e.g. "X-RateLimit-Limit").</summary>
    public string Name { get; set; } = "";

    /// <summary>OpenAPI schema type (string, integer, number, boolean). Default: string.</summary>
    public string SchemaType { get; set; } = "string";

    /// <summary>OpenAPI format (int32, int64, etc.). Null when not applicable.</summary>
    public string? SchemaFormat { get; set; }

    /// <summary>Optional description of the header.</summary>
    public string? Description { get; set; }

    /// <summary>Whether the header is required. Default: false.</summary>
    public bool Required { get; set; }
}

/// <summary>
/// A response link captured from <c>[ResponseLink]</c> on a command type.
/// Emitted as <c>responses.{statusCode}.links.{linkId}</c>.
/// </summary>
internal sealed class ResponseLinkInfo
{
    /// <summary>HTTP status code this link is attached to (e.g. 201).</summary>
    public int StatusCode { get; set; }

    /// <summary>Link identifier used as the key in the links map.</summary>
    public string LinkId { get; set; } = "";

    /// <summary>operationId of the target operation.</summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Runtime expression mapping for link parameters.
    /// Format used in the attribute: "paramName=$response.body#/path".
    /// Emitted as-is under <c>parameters</c>.
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>Optional human-readable description of the link.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// A callback captured from <c>[Callback]</c> on a command type.
/// Emitted as <c>callbacks.{name}</c> on the operation.
/// Minimal implementation: name, path expression, HTTP method and optional summary.
/// </summary>
internal sealed class CallbackInfo
{
    /// <summary>Callback key name (e.g. "onPaymentStatusChange").</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Runtime expression for the callback URL
    /// (e.g. "{$request.body#/callbackUrl}").
    /// </summary>
    public string Expression { get; set; } = "";

    /// <summary>HTTP method for the callback request (lowercase, e.g. "post").</summary>
    public string Method { get; set; } = "post";

    /// <summary>Optional summary for the callback operation.</summary>
    public string? Summary { get; set; }

    /// <summary>Optional requestBody schema simple type name (goes into components.schemas).</summary>
    public string? PayloadTypeName { get; set; }
}

// ── Assembly-level webhook model ──────────────────────────────────────────────

/// <summary>
/// A webhook entry captured from <c>[assembly: Webhook(...)]</c>.
/// Emitted under the top-level <c>webhooks:</c> section (OpenAPI 3.1).
/// </summary>
internal sealed class WebhookInfo
{
    /// <summary>Webhook name / key (e.g. "orderCreated").</summary>
    public string Name { get; set; } = "";

    /// <summary>HTTP method in lowercase (e.g. "post"). Default: "post".</summary>
    public string Method { get; set; } = "post";

    /// <summary>Simple name of the payload type (becomes $ref in components.schemas).</summary>
    public string PayloadTypeName { get; set; } = "";

    /// <summary>Optional summary of the webhook operation.</summary>
    public string? Summary { get; set; }

    /// <summary>Optional description of the webhook operation.</summary>
    public string? Description { get; set; }

    /// <summary>Tags for the webhook operation (optional).</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Schema types discovered when resolving the payload type through
    /// <see cref="TypePropertyExtractor"/>. Merged into <c>components/schemas</c>
    /// by the YAML generator so the webhook payload schema is fully documented
    /// (MELHORIA-4 fix).
    /// </summary>
    public List<SchemaTypeInfo> ReferencedSchemas { get; set; } = new();
}
