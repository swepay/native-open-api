namespace Native.OpenApi.Attributes;

/// <summary>
/// Associates a code sample with the OpenAPI operation of the decorated command type.
/// Emitted as an entry in the <c>x-codeSamples</c> extension array, which is rendered
/// by both Redoc and Scalar as syntax-highlighted tabs next to the request/response schema.
/// </summary>
/// <remarks>
/// <para>Apply this attribute on the <c>TCommand</c> class. It is <b>multi-use</b>:
/// declare one per language (or per variant within the same language).</para>
/// <para>This is a <b>compile-time-only</b> marker consumed by the Roslyn Source Generator.
/// Zero reflection at runtime.</para>
/// <para>Emitted YAML (inside the operation object):</para>
/// <code>
/// x-codeSamples:
///   - lang: curl
///     label: "cURL"
///     source: |
///       curl -X POST https://api.example.com/v1/items \
///            -H "Authorization: Bearer {token}" \
///            -d '{"name":"Widget"}'
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [CodeSample(lang: "curl", source: "curl -X POST https://api.example.com/v1/items")]
/// [CodeSample(lang: "csharp", label: "C# (HttpClient)", source: "var resp = await http.PostAsync(...);")]
/// public sealed record CreateItemCommand(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class CodeSampleAttribute : Attribute
{
    /// <summary>
    /// Language identifier used by the syntax highlighter in Redoc/Scalar.
    /// Common values: <c>curl</c>, <c>csharp</c>, <c>python</c>, <c>javascript</c>,
    /// <c>go</c>, <c>java</c>, <c>php</c>, <c>ruby</c>, <c>shell</c>.
    /// </summary>
    public string Lang { get; }

    /// <summary>
    /// The source code of the sample (verbatim string literal recommended for multi-line).
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Optional human-readable tab label shown in the docs UI.
    /// When <c>null</c>, the UI falls back to <see cref="Lang"/>.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="CodeSampleAttribute"/>.
    /// </summary>
    /// <param name="lang">Language identifier for the syntax highlighter.</param>
    /// <param name="source">The sample source code.</param>
    public CodeSampleAttribute(string lang, string source)
    {
        Lang = lang;
        Source = source;
    }
}
