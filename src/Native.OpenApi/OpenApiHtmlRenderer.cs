using System.Text;
using Native.OpenApi.Rendering;

namespace Native.OpenApi;

/// <summary>
/// Renders HTML pages for OpenAPI documentation viewers (Redoc and Scalar).
/// </summary>
/// <remarks>
/// <para>Every method has two sibling overloads: the original signature (kept
/// unchanged for retrocompatibilidade — RFC § O5) and a new <c>options</c>
/// overload that carries branding, footer, and Mermaid toggles introduced by
/// RFC § F15/F16/F17.</para>
/// <para><b>Scalar-first policy:</b> spec extensions such as
/// <c>x-tagGroups</c>, <c>x-codeSamples</c>, <c>x-badges</c>,
/// <c>x-scalar-stability</c>, <c>x-displayName</c>, and
/// <c>oneOf</c>/discriminator are rendered <em>automatically</em> by Scalar
/// when they are present in the spec YAML — no renderer config is required to
/// enable them. The renderer only needs to avoid disabling them, which the
/// default Scalar config does not do.</para>
/// <para><b>Security:</b> all caller-supplied strings (title, URLs, colors)
/// are escaped for their emission context (HTML attribute or JS string) before
/// being inserted into the output.</para>
/// </remarks>
public sealed class OpenApiHtmlRenderer
{
    // CDN URLs — pinned to a major version band so patch releases auto-apply
    // but major breaking changes require an explicit update here.
    private const string RedocCdnUrl =
        "https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js";

    private const string ScalarCdnUrl =
        "https://cdn.jsdelivr.net/npm/@scalar/api-reference";

    // ------------------------------------------------------------------
    // Back-compat overloads — behaviour identical to pre-Wave-1 library.
    // ------------------------------------------------------------------

    /// <summary>
    /// Renders a Redoc HTML page using the library defaults
    /// (primary <c>#1976d2</c>, no footer, no Mermaid).
    /// </summary>
    /// <param name="specPath">Relative URL to the spec JSON/YAML.</param>
    /// <param name="title">HTML page title.</param>
    public string RenderRedoc(string specPath, string title)
        => RenderRedoc(specPath, title, OpenApiRendererOptions.Default);

    /// <summary>
    /// Renders a Scalar HTML page using the library defaults.
    /// </summary>
    /// <param name="specPath">Relative URL to the spec JSON/YAML.</param>
    /// <param name="title">HTML page title.</param>
    public string RenderScalar(string specPath, string title)
        => RenderScalar(specPath, title, OpenApiRendererOptions.Default);

    // ------------------------------------------------------------------
    // New overloads — F15 branding, F16 footer, F17 Mermaid,
    //                  Wave-2 Scalar viewer options.
    // ------------------------------------------------------------------

    /// <summary>
    /// Renders a Redoc page applying branding, footer, and (optionally)
    /// Mermaid rendering from <paramref name="options"/>.
    /// </summary>
    public string RenderRedoc(string specPath, string title, OpenApiRendererOptions options)
    {
        var sb = new StringBuilder(4096);

        sb.Append("<!DOCTYPE html>\n<html>\n<head>\n");
        sb.Append("    <title>").Append(HtmlEscape(title)).Append("</title>\n");
        sb.Append("    <meta charset=\"utf-8\"/>\n");
        sb.Append("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        AppendFavicon(sb, options);
        sb.Append("    <link href=\"https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700\" rel=\"stylesheet\">\n");
        sb.Append("    <style>").Append(BuildBaseCss(options)).Append("</style>\n");
        sb.Append("</head>\n<body>\n");
        sb.Append("    <redoc spec-url=\"\"></redoc>\n");
        sb.Append("    <script src=\"").Append(HtmlEscape(RedocCdnUrl)).Append("\"></script>\n");

        if (options.EnableMermaid)
        {
            sb.Append("    <script src=\"").Append(HtmlEscape(MermaidScriptSrc(options))).Append("\"></script>\n");
        }

        sb.Append("    <script>\n");
        sb.Append("        (function() {\n");
        // The spec URL is the caller-supplied specPath, honored verbatim. The
        // browser resolves it relative to the current document, so both an
        // absolute path ("/openapi.yaml") and a relative one ("openapi.yaml")
        // work without assuming any particular route convention.
        sb.Append("            Redoc.init('").Append(JsString(specPath)).Append("', ").Append(BuildRedocTheme(options)).Append(", document.querySelector('redoc'));\n");
        if (options.EnableMermaid)
        {
            sb.Append(MermaidPreprocessorJs());
        }
        sb.Append("        })();\n");
        sb.Append("    </script>\n");

        AppendFooter(sb, options);

        sb.Append("</body>\n</html>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders a Scalar page applying branding, footer, Scalar viewer
    /// options, and (optionally) Mermaid rendering from
    /// <paramref name="options"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>How Scalar features map to configuration:</b></para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>x-tagGroups / x-displayName</b> — automatic: Scalar reads them
    ///     from the spec root and groups sidebar entries with no config needed.
    ///   </description></item>
    ///   <item><description>
    ///     <b>x-codeSamples</b> — automatic: Scalar renders per-operation code
    ///     samples in the language tabs panel when present in the spec.
    ///   </description></item>
    ///   <item><description>
    ///     <b>x-badges</b> — automatic: badge chips appear next to the
    ///     operation title in the sidebar and detail panel.
    ///   </description></item>
    ///   <item><description>
    ///     <b>x-scalar-stability</b> — automatic: rendered as a stability pill
    ///     (stable/experimental/deprecated) next to the operation.
    ///   </description></item>
    ///   <item><description>
    ///     <b>oneOf / discriminator</b> — automatic: Scalar renders the
    ///     discriminator union picker with tabs per variant.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Logo</b> — set via <c>OpenApiBrandingOptions.LogoUrl</c>;
    ///     injected into the Scalar config object as <c>logo.src</c>.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Dark mode, layout, hide-models, etc.</b> — set via
    ///     <see cref="OpenApiScalarViewerOptions"/> on
    ///     <see cref="OpenApiRendererOptions.ScalarViewer"/>.
    ///   </description></item>
    /// </list>
    /// <para><b>Configuration delivery:</b> the config JSON is placed in the
    /// HTML as a <c>data-configuration</c> attribute value (HTML-escaped), not
    /// inside a JS string literal, eliminating a class of XSS injection via
    /// user-controlled property values.</para>
    /// </remarks>
    public string RenderScalar(string specPath, string title, OpenApiRendererOptions options)
    {
        var sb = new StringBuilder(4096);

        var configJson = BuildScalarConfigJson(options);
        var scriptSrc = ScalarScriptSrc(options);

        sb.Append("<!DOCTYPE html>\n<html>\n<head>\n");
        sb.Append("    <title>").Append(HtmlEscape(title)).Append("</title>\n");
        sb.Append("    <meta charset=\"utf-8\"/>\n");
        sb.Append("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        AppendFavicon(sb, options);
        sb.Append("    <style>").Append(BuildBaseCss(options)).Append("</style>\n");
        sb.Append("</head>\n<body>\n");

        // data-url is the caller-supplied specPath, honored verbatim (the
        // browser resolves it relative to the current document). Both data-url
        // and data-configuration are HTML-attribute-escaped.
        sb.Append("    <script\n");
        sb.Append("        id=\"api-reference\"\n");
        sb.Append("        data-url=\"").Append(HtmlEscape(specPath)).Append("\"\n");
        sb.Append("        data-configuration=\"").Append(HtmlEscape(configJson)).Append("\"\n");
        sb.Append("    ></script>\n");

        if (options.EnableMermaid)
        {
            sb.Append("    <script src=\"").Append(HtmlEscape(MermaidScriptSrc(options))).Append("\"></script>\n");
        }

        // Load Scalar; it reads data-url and data-configuration from the
        // script tag above on initialization.
        sb.Append("    <script src=\"").Append(HtmlEscape(scriptSrc)).Append("\"></script>\n");
        if (options.EnableMermaid)
        {
            sb.Append("    <script>\n");
            sb.Append("        (function() {\n");
            sb.Append(MermaidPreprocessorJs());
            sb.Append("        })();\n");
            sb.Append("    </script>\n");
        }

        AppendFooter(sb, options);

        sb.Append("</body>\n</html>");
        return sb.ToString();
    }

    // ------------------------------------------------------------------
    // Fragment builders.
    // ------------------------------------------------------------------

    private static void AppendFavicon(StringBuilder sb, OpenApiRendererOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Branding.FaviconUrl))
        {
            sb.Append("    <link rel=\"icon\" href=\"")
              .Append(HtmlEscape(options.Branding.FaviconUrl!))
              .Append("\"/>\n");
        }
    }

    /// <summary>
    /// Builds the JSON literal passed as Redoc's second argument.
    /// </summary>
    /// <remarks>
    /// When <see cref="OpenApiBrandingOptions.ThemeJsonOverride"/> is non-null
    /// and non-empty, it replaces the computed theme entirely. Otherwise a
    /// Swepay-flavoured default is emitted.
    /// </remarks>
    private static string BuildRedocTheme(OpenApiRendererOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Branding.ThemeJsonOverride))
        {
            // Trust the consumer-supplied JSON verbatim. The renderer does not
            // validate it — that is the linter's job.
            return options.Branding.ThemeJsonOverride!;
        }

        var sb = new StringBuilder(256);
        sb.Append('{');
        sb.Append("\"theme\":{");
        sb.Append("\"colors\":{\"primary\":{\"main\":\"").Append(JsString(options.Branding.PrimaryColor)).Append("\"}");
        if (!string.IsNullOrWhiteSpace(options.Branding.AccentColor))
        {
            sb.Append(",\"accent\":{\"main\":\"").Append(JsString(options.Branding.AccentColor!)).Append("\"}");
        }
        sb.Append("},\"typography\":{\"fontFamily\":\"").Append(JsString(options.Branding.FontFamily)).Append("\"}");
        if (!string.IsNullOrWhiteSpace(options.Branding.LogoUrl))
        {
            sb.Append(",\"logo\":{\"gutter\":\"16px\"}");
        }
        sb.Append('}');
        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Builds the JSON object placed in Scalar's <c>data-configuration</c>
    /// HTML attribute.
    /// </summary>
    /// <remarks>
    /// <para>The return value is a raw JSON string (not a JS string literal).
    /// The caller HTML-escapes it before placing it in the attribute.</para>
    /// <para><b>Automatic features that need no config key:</b>
    /// x-tagGroups, x-displayName, x-codeSamples, x-badges,
    /// x-scalar-stability, oneOf/discriminator. These are parsed directly
    /// from the spec by Scalar's renderer and appear without any config knob.</para>
    /// <para><b>Config keys emitted:</b>
    /// <c>theme</c>, <c>darkMode</c>, <c>layout</c>, <c>hideModels</c>,
    /// <c>hideDownloadButton</c>, <c>hideSidebar</c>,
    /// <c>hideTestRequestButton</c>, <c>defaultHttpClient</c>,
    /// <c>customCss</c> (carries brand colours), <c>logo</c>.</para>
    /// </remarks>
    internal static string BuildScalarConfigJson(OpenApiRendererOptions options)
    {
        var v = options.ScalarViewer;
        var b = options.Branding;

        var sb = new StringBuilder(256);
        sb.Append('{');

        // -- theme & appearance ------------------------------------------
        sb.Append("\"theme\":").Append(JsonString(v.Theme)).Append(',');
        sb.Append("\"darkMode\":").Append(v.DarkMode ? "true" : "false").Append(',');
        sb.Append("\"layout\":").Append(JsonString(v.Layout)).Append(',');

        // -- feature flags -----------------------------------------------
        // Scalar shows models and the download button by default; we only
        // emit the keys when the consumer explicitly disables them.
        if (v.HideModels)
            sb.Append("\"hideModels\":true,");
        if (v.HideDownloadButton)
            sb.Append("\"hideDownloadButton\":true,");
        if (v.HideSidebar)
            sb.Append("\"hideSidebar\":true,");
        if (v.HideTestRequestButton)
            sb.Append("\"hideTestRequestButton\":true,");

        // -- default HTTP client -----------------------------------------
        if (!string.IsNullOrWhiteSpace(v.DefaultHttpClientTargetKey)
            || !string.IsNullOrWhiteSpace(v.DefaultHttpClientClientKey))
        {
            sb.Append("\"defaultHttpClient\":{");
            if (!string.IsNullOrWhiteSpace(v.DefaultHttpClientTargetKey))
                sb.Append("\"targetKey\":").Append(JsonString(v.DefaultHttpClientTargetKey!)).Append(',');
            if (!string.IsNullOrWhiteSpace(v.DefaultHttpClientClientKey))
                sb.Append("\"clientKey\":").Append(JsonString(v.DefaultHttpClientClientKey!)).Append(',');
            // trim trailing comma
            if (sb[sb.Length - 1] == ',') sb.Length--;
            sb.Append("},");
        }

        // -- logo (Scalar native config) ---------------------------------
        // x-logo is Redoc-only; Scalar exposes logo.src/href/alt in config.
        if (!string.IsNullOrWhiteSpace(b.LogoUrl))
        {
            sb.Append("\"logo\":{\"src\":").Append(JsonString(b.LogoUrl!)).Append("},");
        }

        // -- brand colours via customCss ---------------------------------
        // --scalar-color-1 drives the primary action colour (buttons, links,
        // highlighted sidebar items).  We also forward the accent colour when
        // set.  A semi-colon-separated value is safe inside a CSS rule string.
        var css = new StringBuilder(128);
        css.Append(":root{--scalar-color-1:").Append(b.PrimaryColor);
        if (!string.IsNullOrWhiteSpace(b.AccentColor))
            css.Append(";--scalar-color-accent:").Append(b.AccentColor!);
        css.Append('}');
        sb.Append("\"customCss\":").Append(JsonString(css.ToString()));

        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Base CSS shared by Redoc and Scalar — margin reset + footer layout.
    /// </summary>
    private static string BuildBaseCss(OpenApiRendererOptions options)
    {
        var sb = new StringBuilder(512);
        sb.Append("body{margin:0;padding:0;font-family:").Append(options.Branding.FontFamily).Append(";}");
        if (options.Footer.HasAnyLink)
        {
            sb.Append(".swepay-footer{border-top:1px solid #e5e7eb;padding:16px 24px;");
            sb.Append("font-size:13px;color:#475569;display:flex;flex-wrap:wrap;gap:16px;");
            sb.Append("background:#fafbfd;}");
            sb.Append(".swepay-footer a{color:").Append(options.Branding.PrimaryColor).Append(";text-decoration:none;}");
            sb.Append(".swepay-footer a:hover{text-decoration:underline;}");
        }
        return sb.ToString();
    }

    private static void AppendFooter(StringBuilder sb, OpenApiRendererOptions options)
    {
        if (!options.Footer.HasAnyLink) return;

        sb.Append("    <footer class=\"swepay-footer\">\n");
        AppendFooterLink(sb, "Status", options.Footer.StatusUrl);
        AppendFooterLink(sb, "Support", options.Footer.SupportUrl);
        AppendFooterLink(sb, "Changelog", options.Footer.ChangelogUrl);
        AppendFooterLink(sb, "SLA", options.Footer.SlaUrl);
        AppendFooterLink(sb, "Terms", options.Footer.TermsUrl);
        sb.Append("    </footer>\n");
    }

    private static void AppendFooterLink(StringBuilder sb, string label, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        sb.Append("        <a href=\"").Append(HtmlEscape(url!)).Append("\" rel=\"noopener\">")
          .Append(HtmlEscape(label)).Append("</a>\n");
    }

    private static string MermaidScriptSrc(OpenApiRendererOptions options) =>
        options.MermaidFromLocalAsset
            ? "./assets/mermaid.min.js"
            : "https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js";

    private static string ScalarScriptSrc(OpenApiRendererOptions options) =>
        !string.IsNullOrWhiteSpace(options.ScalarViewer.LocalAssetPath)
            ? options.ScalarViewer.LocalAssetPath!
            : ScalarCdnUrl;

    /// <summary>
    /// Idempotent client-side pass that finds <c>&lt;code class="language-mermaid"&gt;</c>
    /// blocks rendered by Redoc/Scalar inside Markdown descriptions and rewrites
    /// them into <c>&lt;div class="mermaid"&gt;</c> blocks, then triggers a
    /// Mermaid <c>run()</c>. Uses a MutationObserver because both renderers
    /// mount content asynchronously.
    /// </summary>
    private static string MermaidPreprocessorJs() => @"
            function convertMermaidBlocks(){
                var blocks = document.querySelectorAll('code.language-mermaid');
                if (!blocks.length) return false;
                blocks.forEach(function(el){
                    var div = document.createElement('div');
                    div.className = 'mermaid';
                    div.textContent = el.textContent;
                    var parent = el.closest('pre') || el;
                    parent.replaceWith(div);
                });
                if (window.mermaid && typeof window.mermaid.run === 'function') {
                    window.mermaid.run({ querySelector: '.mermaid' });
                }
                return true;
            }
            function bootstrapMermaid(){
                if (!window.mermaid) return;
                window.mermaid.initialize({ startOnLoad: false, securityLevel: 'strict' });
                convertMermaidBlocks();
                var obs = new MutationObserver(function(){ convertMermaidBlocks(); });
                obs.observe(document.body, { childList: true, subtree: true });
            }
            if (document.readyState === 'complete') bootstrapMermaid();
            else window.addEventListener('load', bootstrapMermaid);
";

    // ------------------------------------------------------------------
    // Escapers. Conservative; purpose is defense in depth against the
    // unlikely event an MSBuild property ends up holding hostile content.
    // ------------------------------------------------------------------

    /// <summary>
    /// Escapes a string for safe use inside an HTML attribute value or
    /// HTML text content. Handles the five XML/HTML reserved characters.
    /// </summary>
    private static string HtmlEscape(string s) => s
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&#x27;");

    /// <summary>
    /// Escapes a string for safe use inside a single-quoted JS string
    /// literal (e.g. <c>'...'</c>).
    /// </summary>
    private static string JsString(string s) => s
        .Replace("\\", "\\\\")
        .Replace("'", "\\'")
        .Replace("\"", "\\\"")
        .Replace("\n", "\\n")
        .Replace("\r", "\\r");

    /// <summary>
    /// Serialises a C# string as a JSON string literal, including the
    /// surrounding double-quotes. Escapes the characters required by
    /// RFC 8259 §7.
    /// </summary>
    private static string JsonString(string s)
    {
        var sb = new StringBuilder(s.Length + 8);
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
