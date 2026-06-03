namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares an OpenAPI 3.1 <c>link</c> on the specified response status code for the
/// operation of the decorated command type. Emitted under
/// <c>responses.{statusCode}.links.{linkId}</c>.
/// </summary>
/// <remarks>
/// <para>Links describe how a caller can navigate from the current response to a related
/// operation using runtime expressions (RFC 6901 JSON Pointer style). They are rendered
/// by both Redoc and Scalar as "Related Operations" navigation hints.</para>
/// <para>Apply this attribute on the <c>TCommand</c> class. It is <b>multi-use</b>.</para>
/// <para>This is a <b>compile-time-only</b> marker consumed by the Roslyn Source Generator.
/// Zero reflection at runtime.</para>
/// <para>Emitted YAML example:</para>
/// <code>
/// responses:
///   '201':
///     links:
///       GetCreatedItem:
///         operationId: GetItemById
///         parameters:
///           id: $response.body#/id
///         description: "Retrieve the item that was just created."
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [ResponseLink(201, "GetCreatedItem",
///     OperationId = "GetItemById",
///     Parameters = "id=$response.body#/id",
///     Description = "Retrieve the created item.")]
/// public sealed record CreateItemCommand(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class ResponseLinkAttribute : Attribute
{
    /// <summary>HTTP status code this link is attached to (e.g. 201).</summary>
    public int StatusCode { get; }

    /// <summary>Link identifier — used as the key in the links map.</summary>
    public string LinkId { get; }

    /// <summary>
    /// The <c>operationId</c> of the linked operation.
    /// Mutually exclusive with <c>operationRef</c> (operationRef not yet supported).
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Link parameter expressions as a single string, formatted as
    /// <c>paramName=$expression</c> pairs separated by commas.
    /// Example: <c>"id=$response.body#/id"</c> or
    /// <c>"userId=$response.body#/userId, role=$response.body#/role"</c>.
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>Optional human-readable description of the link.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="ResponseLinkAttribute"/>.
    /// </summary>
    /// <param name="statusCode">HTTP response status code the link is associated with.</param>
    /// <param name="linkId">Unique key for the link within the response object.</param>
    public ResponseLinkAttribute(int statusCode, string linkId)
    {
        StatusCode = statusCode;
        LinkId = linkId;
    }
}
