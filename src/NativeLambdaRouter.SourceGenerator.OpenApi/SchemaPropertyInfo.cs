namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Represents a property discovered from a request/response type for OpenAPI schema generation.
/// </summary>
internal sealed class SchemaPropertyInfo
{
    /// <summary>
    /// The property name as it appears in C# (PascalCase).
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The property name in camelCase for JSON serialization.
    /// </summary>
    public string JsonName { get; set; } = "";

    /// <summary>
    /// The OpenAPI type (string, integer, number, boolean, array, object).
    /// </summary>
    public string OpenApiType { get; set; } = "string";

    /// <summary>
    /// The OpenAPI format (e.g., int32, int64, double, date-time, email, uuid, uri, password).
    /// </summary>
    public string? OpenApiFormat { get; set; }

    /// <summary>
    /// Whether this property is nullable.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Whether this property is required (non-nullable, no default value).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// For array types, the OpenAPI type of the array items.
    /// </summary>
    public string? ArrayItemType { get; set; }

    /// <summary>
    /// For array types, the OpenAPI format of the array items.
    /// </summary>
    public string? ArrayItemFormat { get; set; }

    /// <summary>
    /// For complex object types, the simple name of the referenced schema.
    /// </summary>
    public string? RefSchemaName { get; set; }

    /// <summary>
    /// For array types with complex items, the simple name of the referenced schema.
    /// </summary>
    public string? ArrayItemRefSchemaName { get; set; }

    /// <summary>
    /// Whether this property is an enum.
    /// </summary>
    public bool IsEnum { get; set; }

    /// <summary>
    /// Enum values when IsEnum is true.
    /// </summary>
    public List<string> EnumValues { get; set; } = new();

    /// <summary>
    /// Description from XML doc comments or attributes.
    /// </summary>
    public string? Description { get; set; }

    // ------------------------------------------------------------------
    // Schema Richness additions (Wave 3).
    // ------------------------------------------------------------------

    /// <summary>
    /// Example value as a raw YAML scalar string (may be quoted or unquoted).
    /// Sourced from <c>[OpenApiProperty(Example = ...)]</c>.
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// Default value as a raw YAML scalar string.
    /// Sourced from <c>[OpenApiProperty(Default = ...)]</c>.
    /// </summary>
    public string? Default { get; set; }

    // ── String constraints ────────────────────────────────────────────

    /// <summary>Minimum string length. -1 means not set.</summary>
    public int MinLength { get; set; } = -1;

    /// <summary>Maximum string length. -1 means not set.</summary>
    public int MaxLength { get; set; } = -1;

    /// <summary>Regular-expression pattern. Null means not set.</summary>
    public string? Pattern { get; set; }

    // ── Numeric constraints ───────────────────────────────────────────

    /// <summary>Inclusive minimum. NaN means not set.</summary>
    public double Minimum { get; set; } = double.NaN;

    /// <summary>Inclusive maximum. NaN means not set.</summary>
    public double Maximum { get; set; } = double.NaN;

    /// <summary>Exclusive minimum (OpenAPI 3.1 numeric form). NaN means not set.</summary>
    public double ExclusiveMinimum { get; set; } = double.NaN;

    /// <summary>Exclusive maximum (OpenAPI 3.1 numeric form). NaN means not set.</summary>
    public double ExclusiveMaximum { get; set; } = double.NaN;

    /// <summary>Multiple-of constraint. NaN means not set.</summary>
    public double MultipleOf { get; set; } = double.NaN;

    // ── Array constraints ─────────────────────────────────────────────

    /// <summary>Minimum number of items. -1 means not set.</summary>
    public int MinItems { get; set; } = -1;

    /// <summary>Maximum number of items. -1 means not set.</summary>
    public int MaxItems { get; set; } = -1;

    /// <summary>Whether all items must be unique.</summary>
    public bool UniqueItems { get; set; }

    // ── Scalar extensions ─────────────────────────────────────────────

    /// <summary>
    /// Display order for Scalar (x-order). -1 means not set.
    /// </summary>
    public int Order { get; set; } = -1;

    /// <summary>
    /// Human-readable name for dictionary value type (x-additionalPropertiesName).
    /// Null means not set.
    /// </summary>
    public string? AdditionalPropertiesName { get; set; }

    /// <summary>
    /// Whether this is a dictionary type (additionalProperties / free-form object).
    /// </summary>
    public bool IsDictionary { get; set; }

    // ── Enum richness ─────────────────────────────────────────────────

    /// <summary>
    /// Per-member descriptions parallel to <see cref="EnumValues"/>.
    /// Non-null and same length as EnumValues when at least one member has a description.
    /// Emitted as <c>x-enum-descriptions</c> (Scalar).
    /// </summary>
    public List<string?>? EnumDescriptions { get; set; }

    /// <summary>
    /// Per-member display names parallel to <see cref="EnumValues"/>.
    /// Non-null and same length as EnumValues when at least one member declares a DisplayName.
    /// Emitted as <c>x-enum-varnames</c> (Scalar).
    /// </summary>
    public List<string?>? EnumVarNames { get; set; }
}
