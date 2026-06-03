namespace Native.OpenApi.Attributes;

/// <summary>
/// Defines a tag group for the <c>x-tagGroups</c> root extension used by Redoc and Scalar
/// to group related tags in the navigation sidebar.
/// </summary>
/// <remarks>
/// <para>Apply this attribute at the <b>assembly level</b> — one per group — to declare
/// how the documentation side-menu should be structured. Groups appear in declaration order.</para>
/// <para>Scalar-first policy: groups and their ordering follow Scalar's rendering contract.
/// Redoc supports <c>x-tagGroups</c> identically.</para>
/// <para>This is a <b>compile-time-only</b> marker consumed exclusively by the Roslyn
/// Source Generator. Zero reflection at runtime.</para>
/// <para>Emitted OpenAPI extension (document root):</para>
/// <code>
/// x-tagGroups:
///   - name: Core
///     tags:
///       - Orders
///       - Products
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [assembly: TagGroup(name: "Core Operations", tags: new[] { "Orders", "Products", "Customers" })]
/// [assembly: TagGroup(name: "Administration",  tags: new[] { "Users", "Roles" })]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class TagGroupAttribute : Attribute
{
    /// <summary>
    /// The display name of the group shown in the documentation UI navigation.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The ordered list of tag names that belong to this group.
    /// Each name must match an existing tag (either declared via <see cref="TagMetadataAttribute"/>
    /// or auto-generated from path segments).
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="TagGroupAttribute"/>.
    /// </summary>
    /// <param name="name">Display name for the group in the navigation UI.</param>
    /// <param name="tags">Ordered tag names that belong to this group.</param>
    public TagGroupAttribute(string name, string[] tags)
    {
        Name = name;
        Tags = tags;
    }
}
