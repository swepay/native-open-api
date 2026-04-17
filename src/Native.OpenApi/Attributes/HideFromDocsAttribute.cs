namespace Native.OpenApi.Attributes;

/// <summary>
/// Hides an operation from the generated OpenAPI specification.
/// Applied either to the <c>TCommand</c> type of a mapped endpoint, or (via the
/// fluent extension <c>.ExcludeFromDocs()</c>) on the route chain itself.
/// </summary>
/// <remarks>
/// <para>Rationale (RFC-DOCUMENTACAO-UX § F01): internal/admin endpoints were
/// leaking into partner-facing documentation. This attribute is the declarative,
/// Native-AOT-safe replacement for ASP.NET Core's <c>[ApiExplorerSettings(IgnoreApi = true)]</c>
/// in the NativeLambdaRouter pipeline.</para>
/// <para>Effect: the Source Generator skips the operation when writing the YAML.
/// If <b>every</b> operation on a path is hidden, the entire path is omitted.</para>
/// <para>Runtime semantics: <b>none</b>. This attribute is consumed exclusively
/// at compile-time by the Roslyn generator. Zero reflection at runtime.</para>
/// </remarks>
/// <example>
/// <code>
/// [HideFromDocs]
/// public sealed record InternalHealthCommand(string Secret);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class HideFromDocsAttribute : Attribute
{
    /// <summary>
    /// Optional human-readable reason the operation is hidden.
    /// Purely documentary — not emitted anywhere.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HideFromDocsAttribute"/>.
    /// </summary>
    /// <param name="reason">Optional justification kept in source for reviewers.</param>
    public HideFromDocsAttribute(string? reason = null)
    {
        Reason = reason;
    }
}
