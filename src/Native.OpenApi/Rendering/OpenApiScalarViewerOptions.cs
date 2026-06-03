namespace Native.OpenApi.Rendering;

/// <summary>
/// Scalar-specific viewer configuration surfaced through
/// <see cref="OpenApiRendererOptions.ScalarViewer"/>.
/// </summary>
/// <remarks>
/// <para>These map directly to Scalar's <c>data-configuration</c> JSON object.
/// Features that are automatic in Scalar (x-tagGroups, x-codeSamples,
/// x-badges, x-scalar-stability, x-displayName, oneOf/discriminator) require
/// no configuration — Scalar renders them from the spec itself. This class only
/// exposes knobs that must be explicitly set in the config object.</para>
/// <para>Scalar documentation: https://github.com/scalar/scalar/blob/main/documentation/configuration.md</para>
/// </remarks>
public sealed record OpenApiScalarViewerOptions
{
    /// <summary>
    /// Scalar built-in colour theme. Defaults to <c>"default"</c>.
    /// Other built-in values: <c>"purple"</c>, <c>"blue"</c>, <c>"green"</c>,
    /// <c>"yellow"</c>, <c>"orange"</c>, <c>"red"</c>, <c>"mars"</c>,
    /// <c>"moon"</c>, <c>"midnight"</c>, <c>"kepler"</c>, <c>"elysiajs"</c>,
    /// <c>"deepSpace"</c>, <c>"solarized"</c>, <c>"none"</c>.
    /// </summary>
    public string Theme { get; init; } = "default";

    /// <summary>
    /// When <c>true</c> the Scalar UI starts in dark mode.
    /// <c>false</c> (light) is the default.
    /// </summary>
    public bool DarkMode { get; init; }

    /// <summary>
    /// Scalar layout. <c>"sidebar"</c> (default) renders the three-panel
    /// layout; <c>"classic"</c> renders a single-column layout similar to
    /// Redoc's presentation.
    /// </summary>
    public string Layout { get; init; } = "sidebar";

    /// <summary>
    /// When <c>true</c>, the model/schema section is hidden from the sidebar
    /// and the content area. Defaults to <c>false</c>.
    /// </summary>
    public bool HideModels { get; init; }

    /// <summary>
    /// When <c>true</c>, the download-spec button is hidden from the
    /// Scalar header bar. Defaults to <c>false</c>.
    /// </summary>
    public bool HideDownloadButton { get; init; }

    /// <summary>
    /// When <c>true</c>, the sidebar is hidden on initial render. Defaults
    /// to <c>false</c> (sidebar visible).
    /// </summary>
    public bool HideSidebar { get; init; }

    /// <summary>
    /// When <c>true</c>, the Scalar "Try it out" panel starts in the
    /// closed/collapsed state. Defaults to <c>false</c>.
    /// </summary>
    public bool HideTestRequestButton { get; init; }

    /// <summary>
    /// Default HTTP client language shown in the code-sample panel.
    /// Examples: <c>"Shell"</c>, <c>"Node"</c>, <c>"Python"</c>,
    /// <c>"PHP"</c>, <c>"Java"</c>, <c>"Go"</c>, <c>"Ruby"</c>.
    /// <c>null</c> keeps Scalar's default (Shell/curl).
    /// </summary>
    public string? DefaultHttpClientTargetKey { get; init; }

    /// <summary>
    /// Client library within the chosen <see cref="DefaultHttpClientTargetKey"/>.
    /// Examples for Shell: <c>"curl"</c>, <c>"wget"</c>.
    /// <c>null</c> keeps Scalar's default.
    /// </summary>
    public string? DefaultHttpClientClientKey { get; init; }

    /// <summary>
    /// When non-null and non-empty, Scalar loads its JS bundle from this
    /// local path instead of the CDN. Use in air-gapped deployments.
    /// Example: <c>"./assets/scalar-api-reference.js"</c>.
    /// </summary>
    public string? LocalAssetPath { get; init; }
}
