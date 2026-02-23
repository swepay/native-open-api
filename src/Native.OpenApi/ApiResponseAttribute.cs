namespace Native.OpenApi;

/// <summary>
/// Specifies the type of response and HTTP status code that will be returned by an action.
/// This attribute can be applied multiple times to document different response types.
/// </summary>
/// <example>
/// <code>
/// [ApiResponse(200, typeof(Product), "application/json")]
/// [ApiResponse(404, typeof(ProblemDetails), "application/problem+json")]
/// [ApiResponse(500)]
/// public ValueTask&lt;ProductResponse&gt; Handle(GetProductCommand request, CancellationToken cancellationToken)
/// {
///     // ...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ApiResponseAttribute : Attribute
{
    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the type of the response body.
    /// Null indicates no response body (e.g., 204 No Content, 404 Not Found).
    /// </summary>
    public Type? ResponseType { get; }

    /// <summary>
    /// Gets the content type of the response.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiResponseAttribute"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (e.g., 200, 404, 500).</param>
    /// <param name="responseType">The type of the response body. Null for responses without a body.</param>
    /// <param name="contentType">The content type (default: "application/json").</param>
    public ApiResponseAttribute(int statusCode, Type? responseType = null, string contentType = "application/json")
    {
        StatusCode = statusCode;
        ResponseType = responseType;
        ContentType = contentType;
    }
}
