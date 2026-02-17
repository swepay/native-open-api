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
}
