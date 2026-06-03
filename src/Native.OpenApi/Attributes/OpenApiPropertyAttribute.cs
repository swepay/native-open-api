namespace Native.OpenApi.Attributes;

/// <summary>
/// Enriches a C# property with OpenAPI schema metadata.
/// Consumed at compile-time by the Roslyn Source Generator — zero runtime reflection.
/// </summary>
/// <remarks>
/// <para>Apply on <b>properties</b> of request/response types that are used as TCommand or TResponse
/// in <c>MapGet&lt;TCommand, TResponse&gt;</c> (and other Map* overloads).</para>
/// <para>String constraints: <see cref="MinLength"/>, <see cref="MaxLength"/>, <see cref="Pattern"/>.</para>
/// <para>Numeric constraints: <see cref="Minimum"/>, <see cref="Maximum"/>,
/// <see cref="ExclusiveMinimum"/>, <see cref="ExclusiveMaximum"/>, <see cref="MultipleOf"/>.</para>
/// <para>Array constraints: <see cref="MinItems"/>, <see cref="MaxItems"/>, <see cref="UniqueItems"/>.</para>
/// <para>Display order (Scalar-first): <see cref="Order"/> maps to <c>x-order</c>.</para>
/// <para>Dictionary value name hint (Scalar-first): <see cref="AdditionalPropertiesName"/>
/// maps to <c>x-additionalPropertiesName</c>.</para>
/// <para>Emitted YAML snippet example (string property with constraints):</para>
/// <code>
/// name:
///   type: string
///   description: "The product name"
///   example: "Widget Pro"
///   default: "Unnamed"
///   minLength: 1
///   maxLength: 100
///   pattern: "^[A-Za-z0-9 ]+$"
///   x-order: 1
/// </code>
/// </remarks>
/// <example>
/// <code>
/// public sealed record CreateProductCommand(
///     [property: OpenApiProperty(
///         Description = "The product name",
///         Example = "Widget Pro",
///         MinLength = 1,
///         MaxLength = 100,
///         Pattern = @"^[A-Za-z0-9 ]+$",
///         Order = 1)]
///     string Name,
///
///     [property: OpenApiProperty(
///         Description = "Price in USD",
///         Example = "9.99",
///         Minimum = 0.01,
///         Maximum = 99999.99,
///         Order = 2)]
///     decimal Price,
///
///     [property: OpenApiProperty(
///         Description = "Tags applied to this product",
///         MinItems = 1,
///         MaxItems = 10,
///         Order = 3)]
///     List&lt;string&gt; Tags);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class OpenApiPropertyAttribute : Attribute
{
    /// <summary>
    /// Human-readable description of this property (maps to <c>description</c>).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Representative example value as a string (maps to <c>example</c>).
    /// Emitted as an unquoted scalar when the value parses as a number or boolean,
    /// otherwise as a quoted string.
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// Default value as a string (maps to <c>default</c>).
    /// Same quoting rules as <see cref="Example"/>.
    /// </summary>
    public string? Default { get; set; }

    // ── String constraints ────────────────────────────────────────────

    /// <summary>Minimum string length (maps to <c>minLength</c>). -1 means not set.</summary>
    public int MinLength { get; set; } = -1;

    /// <summary>Maximum string length (maps to <c>maxLength</c>). -1 means not set.</summary>
    public int MaxLength { get; set; } = -1;

    /// <summary>Regular-expression pattern the value must match (maps to <c>pattern</c>).</summary>
    public string? Pattern { get; set; }

    // ── Numeric constraints ───────────────────────────────────────────

    /// <summary>
    /// Inclusive minimum value (maps to <c>minimum</c>).
    /// Use <see cref="double.NaN"/> (the default) to leave unset.
    /// </summary>
    public double Minimum { get; set; } = double.NaN;

    /// <summary>
    /// Inclusive maximum value (maps to <c>maximum</c>).
    /// Use <see cref="double.NaN"/> (the default) to leave unset.
    /// </summary>
    public double Maximum { get; set; } = double.NaN;

    /// <summary>
    /// Exclusive minimum (OpenAPI 3.1 boolean form, maps to <c>exclusiveMinimum</c> as a number).
    /// Use <see cref="double.NaN"/> (the default) to leave unset.
    /// </summary>
    public double ExclusiveMinimum { get; set; } = double.NaN;

    /// <summary>
    /// Exclusive maximum (OpenAPI 3.1 boolean form, maps to <c>exclusiveMaximum</c> as a number).
    /// Use <see cref="double.NaN"/> (the default) to leave unset.
    /// </summary>
    public double ExclusiveMaximum { get; set; } = double.NaN;

    /// <summary>
    /// Multiple-of constraint (maps to <c>multipleOf</c>).
    /// Use <see cref="double.NaN"/> (the default) to leave unset.
    /// </summary>
    public double MultipleOf { get; set; } = double.NaN;

    // ── Array constraints ─────────────────────────────────────────────

    /// <summary>Minimum number of items (maps to <c>minItems</c>). -1 means not set.</summary>
    public int MinItems { get; set; } = -1;

    /// <summary>Maximum number of items (maps to <c>maxItems</c>). -1 means not set.</summary>
    public int MaxItems { get; set; } = -1;

    /// <summary>Whether all items must be unique (maps to <c>uniqueItems: true</c>).</summary>
    public bool UniqueItems { get; set; }

    // ── Scalar extensions ─────────────────────────────────────────────

    /// <summary>
    /// Display order hint for Scalar (maps to <c>x-order</c>).
    /// Lower numbers appear first. -1 means not set (omitted).
    /// </summary>
    public int Order { get; set; } = -1;

    /// <summary>
    /// Human-readable name for the dictionary value type when this property is
    /// a <c>Dictionary&lt;TKey, TValue&gt;</c> (maps to <c>x-additionalPropertiesName</c> — Scalar).
    /// </summary>
    public string? AdditionalPropertiesName { get; set; }
}
