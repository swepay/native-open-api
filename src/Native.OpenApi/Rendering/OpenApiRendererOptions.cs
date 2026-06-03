namespace Native.OpenApi.Rendering;

/// <summary>
/// Aggregate of all renderer-time choices (branding, footer, Mermaid, sandbox
/// announcements). Consumed by <see cref="OpenApiHtmlRenderer"/> overloads.
/// </summary>
/// <remarks>
/// <para>Keeping the options in one record rather than three constructor
/// parameters lets future features (servers list, code-samples toggle, custom
/// CSS) grow without breaking callers. <see cref="Default"/> reproduces the
/// pre-RFC behaviour exactly.</para>
/// </remarks>
public sealed record OpenApiRendererOptions
{
    /// <summary>
    /// Default (zero-brand, no footer, no Mermaid) — same rendering the library
    /// produced before Wave 1. Useful as a starting point to override only what
    /// is needed.
    /// </summary>
    public static OpenApiRendererOptions Default { get; } = new();

    /// <summary>Branding identity. <see cref="OpenApiBrandingOptions"/> supplies
    /// the Swepay default palette.</summary>
    public OpenApiBrandingOptions Branding { get; init; } = new();

    /// <summary>Footer links. When all URLs are <c>null</c> the footer is skipped.</summary>
    public OpenApiFooterOptions Footer { get; init; } = new();

    /// <summary>
    /// When <c>true</c> the renderer injects <b>Mermaid.js</b> and a lightweight
    /// pre-processor that converts <c>&lt;code class="language-mermaid"&gt;</c>
    /// blocks into inline SVG. Off by default to keep pages zero-script for
    /// consumers that opt out.
    /// </summary>
    public bool EnableMermaid { get; init; }

    /// <summary>
    /// When <c>true</c>, Mermaid.js is loaded from the host-local path
    /// <c>./assets/mermaid.min.js</c> instead of the CDN. Use in air-gapped
    /// deployments alongside <c>OpenApiInlineAssets=true</c>.
    /// </summary>
    public bool MermaidFromLocalAsset { get; init; }

    /// <summary>
    /// Scalar-specific viewer knobs (theme, dark mode, layout, hide-models,
    /// default HTTP client, air-gap asset path, etc.).
    /// When omitted the Scalar defaults apply — all spec extensions
    /// (x-tagGroups, x-codeSamples, x-badges, x-scalar-stability,
    /// x-displayName, oneOf/discriminator) are rendered automatically by
    /// Scalar from the spec itself and require no config here.
    /// </summary>
    public OpenApiScalarViewerOptions ScalarViewer { get; init; } = new();
}
