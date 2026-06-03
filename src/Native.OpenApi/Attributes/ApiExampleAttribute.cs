namespace Native.OpenApi.Attributes;

/// <summary>
/// Associates a named example (OpenAPI 3.1 <c>examples</c> map) with the operation
/// carried by the decorated command type.
/// </summary>
/// <remarks>
/// <para>RFC-DOCUMENTACAO-UX § F09. Replaces generic <c>foo/bar</c> samples with
/// real-domain, named examples so partners can pick a scenario (happy path,
/// validation, conflict) from Redoc/Scalar without leaving the page.</para>
/// <para>This attribute is <b>multi-use</b>: declare one per scenario.</para>
/// <para>Both <see cref="RequestJson"/> and <see cref="ResponseJson"/> point to
/// embedded-resource paths relative to the project root. The generator resolves
/// the bytes at compile time (AOT-safe) and inlines the payload in the YAML.</para>
/// <para>When <see cref="ResponseStatus"/> is left at 0, the example is attached
/// <b>only</b> to the request side. When set, it is also attached to the matching
/// response entry in <c>responses</c>.</para>
/// </remarks>
/// <example>
/// <code>
/// [ApiExample(
///     name: "happy-path",
///     summary: "Simple order with 1 item",
///     requestJson: "examples/create-order/happy.json",
///     responseStatus: 201,
///     responseJson: "examples/create-order/happy-response.json")]
/// [ApiExample(
///     name: "validation-error",
///     summary: "Invalid CNPJ",
///     responseStatus: 422,
///     responseJson: "examples/create-order/invalid-cnpj.json")]
/// public sealed record CreateOrderCommand(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class ApiExampleAttribute : Attribute
{
    /// <summary>
    /// Stable identifier for the example. Must match the OpenAPI regex
    /// <c>^[A-Za-z0-9_-]+$</c>. Used as the key inside the <c>examples</c> map.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// One-line description rendered above the example in the docs UI.
    /// Keep under 60 chars (linter rule #2).
    /// </summary>
    public string Summary { get; }

    /// <summary>
    /// Embedded-resource path of the request JSON payload. <c>null</c> means
    /// the example only carries a response (common for error scenarios).
    /// Takes precedence over <see cref="RequestValue"/> when both are set.
    /// </summary>
    public string? RequestJson { get; set; }

    /// <summary>
    /// HTTP status code the <see cref="ResponseJson"/> / <see cref="ResponseValue"/> is attached to.
    /// <c>0</c> means the example is request-only.
    /// </summary>
    public int ResponseStatus { get; set; }

    /// <summary>
    /// Embedded-resource path of the response JSON payload.
    /// Takes precedence over <see cref="ResponseValue"/> when both are set.
    /// </summary>
    public string? ResponseJson { get; set; }

    // ── Inline value support (Wave 3) ──────────────────────────────────

    /// <summary>
    /// Raw JSON string to embed inline as the request example <c>value</c>.
    /// Use when the payload is small enough to be declared in-source.
    /// Ignored when <see cref="RequestJson"/> is also set (path wins).
    /// </summary>
    /// <example><c>RequestValue = "{\"amount\": 100, \"currency\": \"BRL\"}"</c></example>
    public string? RequestValue { get; set; }

    /// <summary>
    /// Raw JSON string to embed inline as the response example <c>value</c>.
    /// Use when the payload is small enough to be declared in-source.
    /// Ignored when <see cref="ResponseJson"/> is also set (path wins).
    /// </summary>
    /// <example><c>ResponseValue = "{\"id\": \"ord_123\", \"status\": \"pending\"}"</c></example>
    public string? ResponseValue { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="ApiExampleAttribute"/>.
    /// </summary>
    /// <param name="name">Stable identifier for the example.</param>
    /// <param name="summary">One-line UX-written description.</param>
    public ApiExampleAttribute(string name, string summary)
    {
        Name = name;
        Summary = summary;
    }
}
