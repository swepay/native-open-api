namespace Native.OpenApi.Generator;

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
    /// The source file where this endpoint was defined.
    /// </summary>
    public string? SourceFile { get; set; }

    /// <summary>
    /// The line number where this endpoint was defined.
    /// </summary>
    public int LineNumber { get; set; }
}
