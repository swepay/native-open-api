namespace Native.OpenApi.Attributes;

/// <summary>
/// Attaches a human-readable description and an optional display name (varname) to an enum member
/// for OpenAPI documentation purposes.
/// </summary>
/// <remarks>
/// <para>Apply on individual <b>enum fields</b>. The Roslyn Source Generator reads these attributes
/// when it encounters an enum type used as a property type and emits:</para>
/// <list type="bullet">
///   <item><c>x-enum-descriptions</c> — parallel array of descriptions (Scalar).</item>
///   <item><c>x-enum-varnames</c> — parallel array of display names (Scalar), only when at
///   least one member declares a <see cref="DisplayName"/>.</item>
/// </list>
/// <para>Both arrays are parallel to the <c>enum</c> values array and are ordered by the C#
/// declaration order of enum fields for determinism.</para>
/// <para>This attribute is a <b>compile-time-only</b> marker. Zero reflection at runtime.</para>
/// <para>Emitted YAML snippet (inside the schema property):</para>
/// <code>
/// status:
///   type: string
///   enum:
///     - Active
///     - Inactive
///     - Suspended
///   x-enum-descriptions:
///     - Account is in good standing
///     - Account has been deactivated
///     - Account is temporarily suspended
///   x-enum-varnames:
///     - ACTIVE
///     - INACTIVE
///     - SUSPENDED
/// </code>
/// </remarks>
/// <example>
/// <code>
/// public enum AccountStatus
/// {
///     [OpenApiEnumMember(Description = "Account is in good standing", DisplayName = "ACTIVE")]
///     Active,
///
///     [OpenApiEnumMember(Description = "Account has been deactivated", DisplayName = "INACTIVE")]
///     Inactive,
///
///     [OpenApiEnumMember(Description = "Account is temporarily suspended", DisplayName = "SUSPENDED")]
///     Suspended
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class OpenApiEnumMemberAttribute : Attribute
{
    /// <summary>
    /// Human-readable description for this enum member.
    /// Emitted in the <c>x-enum-descriptions</c> parallel array.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional display name / varname for this enum member (e.g., an ALL_CAPS constant name).
    /// Emitted in the <c>x-enum-varnames</c> parallel array.
    /// When no member in the enum declares a <see cref="DisplayName"/>, the
    /// <c>x-enum-varnames</c> array is omitted entirely.
    /// </summary>
    public string? DisplayName { get; set; }
}
