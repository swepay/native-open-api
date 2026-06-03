namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares an explicit request header parameter for the OpenAPI operation of the decorated
/// command type. Emitted as an entry in the <c>parameters</c> array with <c>in: header</c>.
/// </summary>
/// <remarks>
/// <para>Apply this attribute on the <c>TCommand</c> class. It is <b>multi-use</b>:
/// declare one per header parameter.</para>
/// <para>This is a <b>compile-time-only</b> marker consumed by the Roslyn Source Generator.
/// Zero reflection at runtime.</para>
/// <para>Emitted YAML example:</para>
/// <code>
/// parameters:
///   - name: X-Tenant-Id
///     in: header
///     required: true
///     schema:
///       type: string
///     description: "Tenant identifier for multi-tenant routing."
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [HeaderParameter("X-Tenant-Id", typeof(string), Required = true, Description = "Tenant identifier.")]
/// [HeaderParameter("X-Idempotency-Key", typeof(string), Required = false, Description = "Idempotency key.")]
/// public sealed record CreateOrderCommand(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class HeaderParameterAttribute : Attribute
{
    /// <summary>Header name (e.g. "X-Tenant-Id", "X-Idempotency-Key").</summary>
    public string Name { get; }

    /// <summary>
    /// CLR type used to derive the OpenAPI schema type and format.
    /// Supported: <see cref="string"/>, <see cref="int"/>, <see cref="long"/>,
    /// <see cref="bool"/>, <see cref="double"/>, <see cref="decimal"/>, <see cref="System.Guid"/>.
    /// Defaults to <see cref="string"/> when <c>null</c>.
    /// </summary>
    public Type? ParameterType { get; }

    /// <summary>Whether this header is required. Defaults to <c>false</c>.</summary>
    public bool Required { get; set; }

    /// <summary>Human-readable description of the header.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="HeaderParameterAttribute"/>.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="parameterType">The CLR type of the header value (used for schema inference).</param>
    public HeaderParameterAttribute(string name, Type? parameterType = null)
    {
        Name = name;
        ParameterType = parameterType;
    }
}
