namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Represents a fully resolved schema type with its properties for OpenAPI generation.
/// </summary>
internal sealed class SchemaTypeInfo
{
    /// <summary>
    /// The simple type name (e.g., "CreateRoleRequest").
    /// </summary>
    public string TypeName { get; set; } = "";

    /// <summary>
    /// Whether this schema is for a Request or Response type.
    /// </summary>
    public string TypeKind { get; set; } = "";

    /// <summary>
    /// The resolved properties for this type.
    /// Empty if the type could not be resolved (fallback to placeholder).
    /// </summary>
    public List<SchemaPropertyInfo> Properties { get; set; } = new();

    /// <summary>
    /// Whether the type properties were successfully resolved from the semantic model.
    /// When false, the generator falls back to a placeholder schema.
    /// </summary>
    public bool IsResolved { get; set; }

    // ── Polymorphism (allOf / oneOf / discriminator) ───────────────────

    /// <summary>
    /// When non-null, this schema should be emitted as
    /// <c>allOf: [$ref BaseSchemaName, {own properties}]</c>.
    /// Set when the C# type has a concrete base class that is also a known schema.
    /// </summary>
    public string? BaseSchemaName { get; set; }

    /// <summary>
    /// Declared sub-type entries read from <c>[OpenApiSubType]</c> on this type.
    /// When non-empty the schema is emitted with <c>oneOf</c> (and optionally a
    /// <c>discriminator</c> if <see cref="DiscriminatorPropertyName"/> is set).
    /// The list is sorted by <see cref="SubTypeEntry.SchemaName"/> before emission.
    /// </summary>
    public List<SubTypeEntry> SubTypes { get; set; } = new();

    /// <summary>
    /// The JSON discriminator property name read from <c>[OpenApiDiscriminator]</c>.
    /// Null when no discriminator was declared.
    /// </summary>
    public string? DiscriminatorPropertyName { get; set; }
}

/// <summary>
/// A single sub-type entry declared via <c>[OpenApiSubType(typeof(T), "value")]</c>.
/// </summary>
internal sealed class SubTypeEntry
{
    /// <summary>Simple schema name for the sub-type (e.g., "Cat").</summary>
    public string SchemaName { get; set; } = "";

    /// <summary>Discriminator value string (e.g., "cat").</summary>
    public string DiscriminatorValue { get; set; } = "";
}
