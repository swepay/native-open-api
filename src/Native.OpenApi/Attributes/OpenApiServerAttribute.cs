namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares a server entry in the OpenAPI <c>servers</c> root array (OpenAPI 3.1 §4.8.5).
/// </summary>
/// <remarks>
/// <para>Apply this attribute at the <b>assembly level</b>, once per server environment.
/// Entries are emitted in declaration order for determinism within a build.</para>
/// <para>This is a <b>compile-time-only</b> marker. Zero reflection at runtime.</para>
/// <para>Emitted YAML:</para>
/// <code>
/// servers:
///   - url: "https://api.swepay.com"
///     description: "Production"
///   - url: "https://api.sandbox.swepay.com"
///     description: "Sandbox"
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [assembly: OpenApiServer("https://api.swepay.com",       Description = "Production")]
/// [assembly: OpenApiServer("https://api.sandbox.swepay.com", Description = "Sandbox")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class OpenApiServerAttribute : Attribute
{
    /// <summary>
    /// The URL of the server. Must be an absolute URL or a URL template
    /// (RFC 6570) when variables are used.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Optional human-readable description of the server (supports CommonMark Markdown).
    /// Displayed in the Redoc / Scalar server picker.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiServerAttribute"/>.
    /// </summary>
    /// <param name="url">Server URL (required).</param>
    public OpenApiServerAttribute(string url)
    {
        Url = url;
    }
}
