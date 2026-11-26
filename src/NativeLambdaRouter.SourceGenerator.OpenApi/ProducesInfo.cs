namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Represents a response definition from .ProducesProblem() fluent calls or [ApiResponse] attributes.
/// </summary>
internal sealed class ProducesInfo
{
    /// <summary>
    /// The HTTP status code (e.g., 200, 404, 500).
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// The simple name of the response type (e.g., "ClientResponse"), or the literal
    /// <c>"SwepayProblemDetails"</c> when the entry came from <c>.ProducesProblem(...)</c>.
    /// Null when the response has no body — check <see cref="NoBody"/> to tell an explicit
    /// no-body declaration (<c>.Produces(statusCode)</c>) apart from a call the generator
    /// could not resolve (which never reaches <see cref="EndpointInfo.AdditionalProduces"/>
    /// at all — it is reported as a <see cref="PendingDiagnostic"/> instead).
    /// </summary>
    public string? ResponseTypeSimpleName { get; set; }

    /// <summary>
    /// The content type for this response (e.g., "application/json", "application/problem+json").
    /// Multiple entries with the same <see cref="StatusCode"/> but different
    /// <see cref="ContentType"/> are merged into one response object with multiple
    /// <c>content</c> entries by <c>OpenApiYamlGenerator</c> (swepay/native-open-api#58).
    /// </summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// True when this entry was declared explicitly as having no response body
    /// (<c>.Produces(statusCode)</c> — no generic type argument, no content type), e.g. a
    /// 401/403 documented only by its <c>WWW-Authenticate</c> response header
    /// (<see cref="EndpointInfo.ResponseHeaders"/>). <c>OpenApiYamlGenerator</c> emits
    /// <c>x-no-content: true</c> instead of a <c>content</c> block, and
    /// <c>Native.OpenApi.OpenApiLinter</c> accepts the missing <c>content</c> only when this
    /// marker is present (swepay/native-open-api#58).
    /// </summary>
    public bool NoBody { get; set; }
}
