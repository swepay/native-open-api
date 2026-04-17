namespace Native.OpenApi.Rendering;

/// <summary>
/// Institutional footer links injected below the Redoc/Scalar viewport.
/// </summary>
/// <remarks>
/// <para>RFC-DOCUMENTACAO-UX § F16. Every link is optional; the footer
/// auto-collapses when all URLs are <c>null</c>. Copy for each label is under
/// UX-writer custody and intentionally kept terse.</para>
/// </remarks>
public sealed record OpenApiFooterOptions
{
    /// <summary>Status page, e.g. <c>https://status.swepay.com.br</c>.</summary>
    public string? StatusUrl { get; init; }

    /// <summary>Support portal / knowledge base.</summary>
    public string? SupportUrl { get; init; }

    /// <summary>Release notes / changelog page.</summary>
    public string? ChangelogUrl { get; init; }

    /// <summary>SLA / availability commitments.</summary>
    public string? SlaUrl { get; init; }

    /// <summary>Terms of service / acceptable use policy.</summary>
    public string? TermsUrl { get; init; }

    /// <summary>
    /// Gets whether <b>any</b> link is configured. When <c>false</c> the renderer
    /// omits the <c>&lt;footer&gt;</c> element entirely.
    /// </summary>
    public bool HasAnyLink =>
        !string.IsNullOrWhiteSpace(StatusUrl)
        || !string.IsNullOrWhiteSpace(SupportUrl)
        || !string.IsNullOrWhiteSpace(ChangelogUrl)
        || !string.IsNullOrWhiteSpace(SlaUrl)
        || !string.IsNullOrWhiteSpace(TermsUrl);
}
