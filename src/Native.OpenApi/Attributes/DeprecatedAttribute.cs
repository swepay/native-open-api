namespace Native.OpenApi.Attributes;

/// <summary>
/// Marks an operation as deprecated with a sunset date, a pointer to the
/// replacement operation, and a reason string.
/// </summary>
/// <remarks>
/// <para>RFC-DOCUMENTACAO-UX § F03. Before this attribute, deprecation required
/// hand-editing the YAML: <c>deprecated: true</c> plus an <c>x-sunset</c> extension.
/// Now the generator emits all of it from a single declarative source.</para>
/// <para>Emitted OpenAPI fields:</para>
/// <list type="bullet">
/// <item><description><c>deprecated: true</c></description></item>
/// <item><description><c>x-sunset</c> — ISO-8601 date string (copied verbatim from <see cref="Sunset"/>)</description></item>
/// <item><description><c>x-swepay-alternative</c> — usually a method+path string, e.g. <c>"POST /v2/orders"</c></description></item>
/// <item><description><c>x-swepay-deprecation-reason</c> — free-form rationale consumed by the Redoc banner</description></item>
/// </list>
/// <para>Not conflicting with <c>[Obsolete]</c>: this attribute is about
/// API-surface deprecation (public-facing), not C# method obsolescence.</para>
/// </remarks>
/// <example>
/// <code>
/// [Deprecated(
///     sunset: "2026-12-31",
///     alternative: "POST /v2/orders",
///     reason: "v1 does not support installment splits")]
/// public sealed record CreateOrderV1Command(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class DeprecatedAttribute : Attribute
{
    /// <summary>
    /// ISO-8601 calendar date (e.g. <c>2026-12-31</c>) after which the endpoint
    /// will be removed. The generator does <b>not</b> validate the format — the
    /// linter does. Passed through to the <c>x-sunset</c> YAML field.
    /// </summary>
    public string Sunset { get; }

    /// <summary>
    /// Suggested replacement, usually in the <c>"METHOD /path"</c> format.
    /// Rendered as a clickable chip in Redoc's deprecation banner.
    /// </summary>
    public string Alternative { get; }

    /// <summary>
    /// Reason the endpoint is being retired. Rendered under the banner title.
    /// Keep under ~120 chars for readability.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="DeprecatedAttribute"/>.
    /// </summary>
    /// <param name="sunset">ISO-8601 date of removal.</param>
    /// <param name="alternative">Replacement endpoint, e.g. <c>"POST /v2/orders"</c>.</param>
    /// <param name="reason">User-facing reason for deprecation.</param>
    public DeprecatedAttribute(string sunset, string alternative, string reason)
    {
        Sunset = sunset;
        Alternative = alternative;
        Reason = reason;
    }
}
