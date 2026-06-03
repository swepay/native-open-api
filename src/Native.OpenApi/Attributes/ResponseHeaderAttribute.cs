namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares a response header on the specified HTTP status code for the OpenAPI operation
/// of the decorated command type. Emitted as an entry under
/// <c>responses.{statusCode}.headers</c>.
/// </summary>
/// <remarks>
/// <para>Apply this attribute on the <c>TCommand</c> class. It is <b>multi-use</b>:
/// declare one per response header (across any status code).</para>
/// <para>This is a <b>compile-time-only</b> marker consumed by the Roslyn Source Generator.
/// Zero reflection at runtime.</para>
/// <para>Emitted YAML example (inside responses.200.headers):</para>
/// <code>
/// responses:
///   '200':
///     headers:
///       X-RateLimit-Limit:
///         description: "Maximum number of requests per window."
///         required: false
///         schema:
///           type: integer
///           format: int32
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [ResponseHeader(200, "X-RateLimit-Limit", typeof(int), Description = "Max requests per window.")]
/// [ResponseHeader(200, "X-RateLimit-Remaining", typeof(int), Description = "Remaining requests.")]
/// [ResponseHeader(201, "Location", typeof(string), Required = true, Description = "URL of created resource.")]
/// public sealed record CreateItemCommand(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class ResponseHeaderAttribute : Attribute
{
    /// <summary>HTTP status code this header is associated with (e.g. 200, 201).</summary>
    public int StatusCode { get; }

    /// <summary>Header name (e.g. "X-RateLimit-Limit", "Location").</summary>
    public string Name { get; }

    /// <summary>
    /// CLR type used to derive the OpenAPI schema type and format.
    /// Supported: <see cref="string"/>, <see cref="int"/>, <see cref="long"/>,
    /// <see cref="bool"/>, <see cref="double"/>, <see cref="decimal"/>, <see cref="System.Guid"/>.
    /// Defaults to <see cref="string"/> when <c>null</c>.
    /// </summary>
    public Type? HeaderType { get; }

    /// <summary>Human-readable description of the header.</summary>
    public string? Description { get; set; }

    /// <summary>Whether the header is required. Defaults to <c>false</c>.</summary>
    public bool Required { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="ResponseHeaderAttribute"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code this header belongs to.</param>
    /// <param name="name">The response header name.</param>
    /// <param name="headerType">The CLR type used to derive the schema type/format.</param>
    public ResponseHeaderAttribute(int statusCode, string name, Type? headerType = null)
    {
        StatusCode = statusCode;
        Name = name;
        HeaderType = headerType;
    }
}
