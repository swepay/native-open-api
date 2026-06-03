namespace Native.OpenApi.Attributes;

/// <summary>
/// Defines metadata for a named OpenAPI tag at the document root level.
/// Emitted under the <c>tags:</c> root key of the generated spec.
/// </summary>
/// <remarks>
/// <para>Apply this attribute at the <b>assembly level</b> — once per tag name — to enrich
/// the tag with a description, a display name rendered in the navigation menu, and
/// an optional external documentation link.</para>
/// <para>This is a <b>compile-time-only</b> marker: the Source Generator reads the
/// attribute from the assembly's attributes during code generation. Zero reflection
/// at runtime.</para>
/// <para>Emitted OpenAPI fields:</para>
/// <list type="bullet">
/// <item><description><c>tags[].name</c> — the tag name used in operations</description></item>
/// <item><description><c>tags[].description</c> — rendered below the tag header in Redoc/Scalar</description></item>
/// <item><description><c>tags[].x-displayName</c> — friendly label in the Redoc/Scalar side-menu</description></item>
/// <item><description><c>tags[].externalDocs.description</c> and <c>tags[].externalDocs.url</c>
/// — link to further reference material</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [assembly: TagMetadata(
///     name: "Orders",
///     description: "Operations related to order lifecycle — creation, updates and cancellation.",
///     displayName: "Order Management",
///     externalDocsDescription: "Order domain guide",
///     externalDocsUrl: "https://docs.swepay.com/orders")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class TagMetadataAttribute : Attribute
{
    /// <summary>
    /// The tag name. Must match exactly the value used in <c>.WithTags()</c> or <c>[Tags]</c>
    /// on route commands. Case-sensitive.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Long-form description rendered under the tag header in the documentation UI.
    /// Supports CommonMark Markdown.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Friendly display label shown in the Redoc / Scalar side-navigation menu.
    /// Emitted as <c>x-displayName</c>. When <c>null</c>, the UI falls back to <see cref="Name"/>.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Short label for the external documentation link (e.g. <c>"Domain guide"</c>).
    /// Only emitted when <see cref="ExternalDocsUrl"/> is also set.
    /// </summary>
    public string? ExternalDocsDescription { get; set; }

    /// <summary>
    /// URL for the external documentation link.
    /// When set, <c>externalDocs: {{ description: ..., url: ... }}</c> is emitted under the tag.
    /// </summary>
    public string? ExternalDocsUrl { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="TagMetadataAttribute"/>.
    /// </summary>
    /// <param name="name">The tag name, must match values used in routing declarations.</param>
    public TagMetadataAttribute(string name)
    {
        Name = name;
    }
}
