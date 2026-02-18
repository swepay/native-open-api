namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Represents a response definition from .Produces() or .ProducesProblem() fluent calls.
/// </summary>
internal sealed class ProducesInfo
{
    /// <summary>
    /// The HTTP status code (e.g., 200, 404, 500).
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// The simple name of the response type (e.g., "ClientResponse").
    /// Null when the response has no body (e.g., 204 No Content).
    /// </summary>
    public string? ResponseTypeSimpleName { get; set; }

    /// <summary>
    /// The content type for this response (e.g., "application/json", "application/problem+json").
    /// </summary>
    public string ContentType { get; set; } = "application/json";
}
