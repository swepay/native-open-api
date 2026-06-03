namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares a concrete sub-type (discriminated variant) for a polymorphic base class.
/// Apply one or more instances on the <b>abstract base class</b>. Each attribute registers
/// one concrete sub-type together with the string discriminator value that identifies it
/// in the JSON payload.
/// </summary>
/// <remarks>
/// <para>
/// Use together with <see cref="OpenApiDiscriminatorAttribute"/> on the same base class.
/// The source generator will:
/// <list type="number">
///   <item>Emit the base schema with <c>discriminator</c> and <c>oneOf</c>.</item>
///   <item>Emit each sub-type schema with <c>allOf: [$ref base, {own properties}]</c>.</item>
/// </list>
/// </para>
/// <para>
/// The sub-type <c>Type</c> argument must be a concrete class whose schema will be auto-discovered.
/// The <c>discriminatorValue</c> becomes the mapping key in the <c>discriminator.mapping</c> dict.
/// </para>
/// <para>Emitted YAML (on the base schema):</para>
/// <code>
/// Animal:
///   oneOf:
///     - $ref: '#/components/schemas/Cat'
///     - $ref: '#/components/schemas/Dog'
///   discriminator:
///     propertyName: kind
///     mapping:
///       cat: '#/components/schemas/Cat'
///       dog: '#/components/schemas/Dog'
/// </code>
/// <para>Emitted YAML (on a sub-type schema):</para>
/// <code>
/// Cat:
///   allOf:
///     - $ref: '#/components/schemas/Animal'
///     - type: object
///       properties:
///         indoor:
///           type: boolean
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [OpenApiDiscriminator("kind")]
/// [OpenApiSubType(typeof(Cat), "cat")]
/// [OpenApiSubType(typeof(Dog), "dog")]
/// public abstract class Animal
/// {
///     public string Kind { get; set; } = "";
///     public string Name { get; set; } = "";
/// }
///
/// public sealed class Cat : Animal
/// {
///     public bool Indoor { get; set; }
/// }
///
/// public sealed class Dog : Animal
/// {
///     public string Breed { get; set; } = "";
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class OpenApiSubTypeAttribute : Attribute
{
    /// <summary>
    /// The concrete sub-type whose schema will be emitted with <c>allOf</c>.
    /// </summary>
    public Type SubType { get; }

    /// <summary>
    /// The discriminator value string for this sub-type (used in the mapping dictionary).
    /// </summary>
    public string DiscriminatorValue { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiSubTypeAttribute"/>.
    /// </summary>
    /// <param name="subType">Concrete class that extends the annotated base.</param>
    /// <param name="discriminatorValue">Discriminator string value for this sub-type.</param>
    public OpenApiSubTypeAttribute(Type subType, string discriminatorValue)
    {
        SubType = subType;
        DiscriminatorValue = discriminatorValue;
    }
}
