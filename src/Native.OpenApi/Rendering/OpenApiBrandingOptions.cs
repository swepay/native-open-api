namespace Native.OpenApi.Rendering;

/// <summary>
/// Visual identity applied to Redoc and Scalar HTML pages.
/// </summary>
/// <remarks>
/// <para>RFC-DOCUMENTACAO-UX § F15. Populate from MSBuild properties
/// (<c>OpenApiBrand*</c>) or environment variables — the renderer stays free of
/// <c>AppDomain</c> reflection (AOT invariant).</para>
/// <para>Instances are immutable and intended to be cached as a single static
/// singleton per process.</para>
/// </remarks>
public sealed record OpenApiBrandingOptions
{
    /// <summary>
    /// Primary brand colour in <c>#rrggbb</c>. Applied to Redoc's
    /// <c>theme.colors.primary.main</c> and Scalar's <c>--scalar-color-1</c>.
    /// Defaults to Swepay midnight blue.
    /// </summary>
    public string PrimaryColor { get; init; } = "#1976d2";

    /// <summary>
    /// Accent colour used for CTAs and badges. <c>null</c> keeps renderer defaults.
    /// </summary>
    public string? AccentColor { get; init; }

    /// <summary>
    /// URL of the logo rendered in the Redoc header. SVG is preferred;
    /// Scalar uses the same URL via <c>data-logo</c>.
    /// </summary>
    public string? LogoUrl { get; init; }

    /// <summary>
    /// URL of the favicon. Emitted as <c>&lt;link rel="icon"&gt;</c>.
    /// </summary>
    public string? FaviconUrl { get; init; }

    /// <summary>
    /// CSS <c>font-family</c> cascade. Defaults to Roboto + system sans.
    /// </summary>
    public string FontFamily { get; init; } = "Roboto, sans-serif";

    /// <summary>
    /// Optional JSON blob merged into Redoc's <c>theme</c> parameter. Intended
    /// as an escape hatch for full theme overrides without bloating this record.
    /// Must be valid JSON or the renderer will emit it inside a JS comment to
    /// avoid breaking the page.
    /// </summary>
    public string? ThemeJsonOverride { get; init; }
}
