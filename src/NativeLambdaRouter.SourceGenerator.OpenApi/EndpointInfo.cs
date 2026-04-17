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
    public string? RequestJsonPath { get; set; }
    public int ResponseStatus { get; set; }
    public string? ResponseJsonPath { get; set; }
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
