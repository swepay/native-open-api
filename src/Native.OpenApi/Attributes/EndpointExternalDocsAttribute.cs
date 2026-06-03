namespace Native.OpenApi.Attributes;

/// <summary>
/// Adds an <c>externalDocs</c> object to the generated OpenAPI operation.
/// </summary>
/// <remarks>
/// <para>Apply this attribute on the <c>TCommand</c> class to link the operation to
/// external reference material (e.g., a domain guide, a run-book, or a changelog entry).</para>
/// <para>This is a <b>compile-time-only</b> marker consumed by the Roslyn Source Generator.
/// Zero reflection at runtime.</para>
/// <para>Emitted YAML (inside the operation object):</para>
/// <code>
/// externalDocs:
///   description: "Order creation flow diagram"
///   url: "https://docs.swepay.com/orders/create"
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [EndpointExternalDocs(
///     url: "https://docs.swepay.com/orders/create",
///     description: "Order creation flow diagram")]
/// public sealed record CreateOrderCommand(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class EndpointExternalDocsAttribute : Attribute
{
    /// <summary>
    /// URL of the external documentation resource. Required by the OpenAPI spec.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Short description of the link, rendered as the anchor label in documentation UIs.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="EndpointExternalDocsAttribute"/>.
    /// </summary>
    /// <param name="url">URL pointing to the external documentation resource.</param>
    public EndpointExternalDocsAttribute(string url)
    {
        Url = url;
    }
}
