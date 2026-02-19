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
    /// Additional response definitions specified via .Produces() / .ProducesProblem().
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
}
