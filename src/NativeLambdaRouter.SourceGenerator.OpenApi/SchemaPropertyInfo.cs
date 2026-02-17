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
}
