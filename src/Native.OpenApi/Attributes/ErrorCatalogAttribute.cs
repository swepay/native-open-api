namespace Native.OpenApi.Attributes;

/// <summary>
/// Points an operation at the static class that enumerates every error it may
/// emit. The generator walks the <see cref="CatalogType"/>'s fields looking for
/// <see cref="ErrorDefinitionAttribute"/> entries and wires them into the YAML.
/// </summary>
/// <remarks>
/// <para>RFC-DOCUMENTACAO-UX § F12. A single <c>SwepayErrors</c> catalog is
/// preferred across the ecosystem (consistency &gt; autonomy — Open Question #3
/// in the RFC).</para>
/// <para>Emitted OpenAPI artifacts:</para>
/// <list type="bullet">
/// <item><description>Per operation: <c>x-swepay-errors: [CODE_A, CODE_B, ...]</c>
/// with only the codes whose <c>httpStatus</c> matches a response defined for
/// that operation.</description></item>
/// <item><description>At document root: <c>x-swepay-error-catalog</c> array, one
/// object per <see cref="ErrorDefinitionAttribute"/>, with code/httpStatus/
/// userMessage/recovery/docUrl. Rendered as a searchable table.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [ErrorCatalog(typeof(SwepayErrors))]
/// public sealed record CreatePaymentCommand(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ErrorCatalogAttribute : Attribute
{
    /// <summary>
    /// Static class type holding <see cref="ErrorDefinitionAttribute"/>-decorated
    /// string fields.
    /// </summary>
    public Type CatalogType { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ErrorCatalogAttribute"/>.
    /// </summary>
    /// <param name="catalogType">Type such as <c>typeof(SwepayErrors)</c>.</param>
    public ErrorCatalogAttribute(Type catalogType)
    {
        CatalogType = catalogType;
    }
}
