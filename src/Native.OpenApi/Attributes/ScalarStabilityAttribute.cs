namespace Native.OpenApi.Attributes;

/// <summary>
/// Sets the Scalar stability indicator for the OpenAPI operation of the decorated command type.
/// Emitted as <c>x-scalar-stability</c> inside the operation object, rendered by Scalar as a
/// stability badge in the API reference sidebar and operation header.
/// </summary>
/// <remarks>
/// <para>Apply this attribute on the <c>TCommand</c> class. It is <b>single-use</b>
/// (<c>AllowMultiple = false</c>): only one stability level per operation.</para>
/// <para>This is a <b>Scalar-first</b> extension. No Redoc equivalent is emitted because
/// Redoc does not natively render <c>x-scalar-stability</c>. Teams using Redoc exclusively
/// can safely ignore this attribute — the YAML remains valid.</para>
/// <para>This is a <b>compile-time-only</b> marker consumed by the Roslyn Source Generator.
/// Zero reflection at runtime.</para>
/// <para>Emitted YAML (inside the operation object):</para>
/// <code>
/// x-scalar-stability: experimental
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [ScalarStability(Stability.Experimental)]
/// public sealed record CreateItemCommand(...);
///
/// [ScalarStability(Stability.Stable)]
/// public sealed record GetItemByIdCommand(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ScalarStabilityAttribute : Attribute
{
    /// <summary>
    /// The stability level of the operation.
    /// </summary>
    public Stability Stability { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ScalarStabilityAttribute"/>.
    /// </summary>
    /// <param name="stability">The stability level to advertise.</param>
    public ScalarStabilityAttribute(Stability stability)
    {
        Stability = stability;
    }
}

/// <summary>
/// Stability levels for <see cref="ScalarStabilityAttribute"/>.
/// Serialized to lowercase strings in the emitted YAML.
/// </summary>
public enum Stability
{
    /// <summary>
    /// The operation is stable and safe for production use.
    /// Emitted as <c>x-scalar-stability: stable</c>.
    /// </summary>
    Stable,

    /// <summary>
    /// The operation is experimental: API shape may change without a major version bump.
    /// Emitted as <c>x-scalar-stability: experimental</c>.
    /// </summary>
    Experimental,

    /// <summary>
    /// The operation is deprecated at the Scalar display level.
    /// Prefer combining with <c>[Deprecated]</c> for full deprecation metadata.
    /// Emitted as <c>x-scalar-stability: deprecated</c>.
    /// </summary>
    Deprecated
}
