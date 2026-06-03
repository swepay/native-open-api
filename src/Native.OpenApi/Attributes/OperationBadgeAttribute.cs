namespace Native.OpenApi.Attributes;

/// <summary>
/// Attaches a visual badge to the OpenAPI operation of the decorated command type.
/// Emitted as an entry in the <c>x-badges</c> extension array, rendered by Redoc and
/// Scalar as coloured pills next to the operation title.
/// </summary>
/// <remarks>
/// <para>Apply this attribute on the <c>TCommand</c> class. It is <b>multi-use</b>:
/// declare one per badge (e.g., "beta" + "internal").</para>
/// <para>This is a <b>compile-time-only</b> marker consumed by the Roslyn Source Generator.
/// Zero reflection at runtime.</para>
/// <para>Emitted YAML (inside the operation object):</para>
/// <code>
/// x-badges:
///   - name: beta
///     position: after
///     color: "#e5a505"
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [OperationBadge(name: "beta", position: "after", color: "#e5a505")]
/// [OperationBadge(name: "internal")]
/// public sealed record CreateItemCommand(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class OperationBadgeAttribute : Attribute
{
    /// <summary>
    /// Display name of the badge, e.g. <c>beta</c>, <c>internal</c>, <c>preview</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional position hint for the docs UI. Typical values: <c>before</c>, <c>after</c>.
    /// When <c>null</c>, the UI uses its default positioning.
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// Optional color for the badge pill. Accepts CSS named colors (e.g., <c>red</c>)
    /// or hex strings (e.g., <c>#e5a505</c>). When <c>null</c>, the UI applies its default.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="OperationBadgeAttribute"/>.
    /// </summary>
    /// <param name="name">Display name of the badge.</param>
    public OperationBadgeAttribute(string name)
    {
        Name = name;
    }
}
