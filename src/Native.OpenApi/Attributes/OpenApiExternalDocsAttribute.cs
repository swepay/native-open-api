namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares the <c>externalDocs</c> object at the OpenAPI document root level.
/// </summary>
/// <remarks>
/// <para>Apply this attribute at the <b>assembly level</b> to provide a document-level
/// reference link that appears in documentation UIs (e.g., Redoc's top-level "External Docs"
/// button, Scalar's header link).</para>
/// <para>Per the OpenAPI 3.1 spec, only one <c>externalDocs</c> entry is allowed at the
/// root. If the attribute is applied more than once, only the first occurrence (alphabetically
/// by description, for determinism) is used.</para>
/// <para>This is a <b>compile-time-only</b> marker. Zero reflection at runtime.</para>
/// <para>Emitted YAML:</para>
/// <code>
/// externalDocs:
///   description: "Full developer guide"
///   url: "https://docs.swepay.com"
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [assembly: OpenApiExternalDocs(
///     url: "https://docs.swepay.com",
///     description: "Full developer guide")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class OpenApiExternalDocsAttribute : Attribute
{
    /// <summary>
    /// The URL pointing to the external documentation. Required by the OpenAPI spec.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Short description of the external documentation link.
    /// Rendered as the link label in documentation UIs.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiExternalDocsAttribute"/>.
    /// </summary>
    /// <param name="url">URL of the external documentation resource.</param>
    public OpenApiExternalDocsAttribute(string url)
    {
        Url = url;
    }
}
