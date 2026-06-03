namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares the discriminator property name for a polymorphic base class.
/// Apply on the <b>abstract base class</b> together with one or more
/// <see cref="OpenApiSubTypeAttribute"/> instances that list the concrete sub-types.
/// </summary>
/// <remarks>
/// <para>
/// The generator emits a <c>discriminator</c> object on the base schema with the given
/// <c>propertyName</c> and a <c>mapping</c> dictionary derived from the <see cref="OpenApiSubTypeAttribute"/>
/// values on the same class.
/// </para>
/// <para>
/// The <c>oneOf</c> keyword is emitted on the base schema listing <c>$ref</c> entries
/// for every declared sub-type, in deterministic (sorted by type name) order.
/// </para>
/// <para>
/// When this attribute is absent on a base class that has <see cref="OpenApiSubTypeAttribute"/>,
/// only <c>oneOf</c> is emitted (no discriminator object).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [OpenApiDiscriminator("kind")]
/// [OpenApiSubType(typeof(Cat), "cat")]
/// [OpenApiSubType(typeof(Dog), "dog")]
/// public abstract class Animal
/// {
///     public string Kind { get; set; } = "";
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OpenApiDiscriminatorAttribute : Attribute
{
    /// <summary>
    /// The name of the JSON property used to discriminate between sub-types.
    /// Must be present on the base schema.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiDiscriminatorAttribute"/>.
    /// </summary>
    /// <param name="propertyName">Name of the JSON discriminator property (e.g. "kind", "type").</param>
    public OpenApiDiscriminatorAttribute(string propertyName)
    {
        PropertyName = propertyName;
    }
}
