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
/// </remarks>
public sealed class OpenApiHtmlRenderer
{
    // ------------------------------------------------------------------
    // Back-compat overloads — behaviour identical to pre-Wave-1 library.
    // ------------------------------------------------------------------

    /// <summary>
    /// Renders a Redoc HTML page using the library defaults
    /// (primary <c>#1976d2</c>, no footer, no Mermaid).
    /// </summary>
    /// <param name="specPath">Relative URL to the spec JSON.</param>
    /// <param name="title">HTML page title.</param>
    public string RenderRedoc(string specPath, string title)
        => RenderRedoc(specPath, title, OpenApiRendererOptions.Default);

    /// <summary>
    /// Renders a Scalar HTML page using the library defaults.
    /// </summary>
    /// <param name="specPath">Relative URL to the spec JSON.</param>
    /// <param name="title">HTML page title.</param>
    public string RenderScalar(string specPath, string title)
        => RenderScalar(specPath, title, OpenApiRendererOptions.Default);

    // ------------------------------------------------------------------
    // New overloads — F15 branding, F16 footer, F17 Mermaid.
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
        sb.Append("    <script src=\"https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js\"></script>\n");

        if (options.EnableMermaid)
        {
            sb.Append("    <script src=\"").Append(MermaidScriptSrc(options)).Append("\"></script>\n");
        }

        sb.Append("    <script>\n");
        sb.Append("        (function() {\n");
        sb.Append("            var basePath = window.location.pathname.replace(/\\/docs\\/redoc.*/, '');\n");
        sb.Append("            var specUrl = basePath + '").Append(JsString(specPath)).Append("';\n");
        sb.Append("            Redoc.init(specUrl, ").Append(BuildRedocTheme(options)).Append(", document.querySelector('redoc'));\n");
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
    /// Renders a Scalar page applying branding, footer, and (optionally)
    /// Mermaid rendering from <paramref name="options"/>.
    /// </summary>
    public string RenderScalar(string specPath, string title, OpenApiRendererOptions options)
    {
        var sb = new StringBuilder(4096);

        sb.Append("<!DOCTYPE html>\n<html>\n<head>\n");
        sb.Append("    <title>").Append(HtmlEscape(title)).Append("</title>\n");
        sb.Append("    <meta charset=\"utf-8\"/>\n");
        sb.Append("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        AppendFavicon(sb, options);
        sb.Append("    <style>").Append(BuildBaseCss(options)).Append("</style>\n");
        sb.Append("</head>\n<body>\n");
        sb.Append("    <script id=\"api-reference\"></script>\n");

        if (options.EnableMermaid)
        {
            sb.Append("    <script src=\"").Append(MermaidScriptSrc(options)).Append("\"></script>\n");
        }

        sb.Append("    <script>\n");
        sb.Append("        (function() {\n");
        sb.Append("            var basePath = window.location.pathname.replace(/\\/docs\\/scalar.*/, '');\n");
        sb.Append("            var specUrl = basePath + '").Append(JsString(specPath)).Append("';\n");
        sb.Append("            var ref = document.getElementById('api-reference');\n");
        sb.Append("            ref.setAttribute('data-url', specUrl);\n");
        sb.Append("            ref.setAttribute('data-configuration', ").Append(BuildScalarConfigJson(options)).Append(");\n");
        sb.Append("            var script = document.createElement('script');\n");
        sb.Append("            script.src = 'https://cdn.jsdelivr.net/npm/@scalar/api-reference';\n");
        sb.Append("            document.body.appendChild(script);\n");
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
    /// Builds the JSON fragment passed to Scalar's <c>data-configuration</c>
    /// attribute. Scalar reads it via <c>JSON.parse</c>, so the returned value
    /// is a JSON-encoded string (i.e. wrapped in quotes and escaped).
    /// </summary>
    private static string BuildScalarConfigJson(OpenApiRendererOptions options)
    {
        var cfg = new StringBuilder(128);
        cfg.Append('{');
        cfg.Append("\"theme\":\"default\",");
        cfg.Append("\"customCss\":\":root{--scalar-color-1:").Append(JsString(options.Branding.PrimaryColor));
        if (!string.IsNullOrWhiteSpace(options.Branding.AccentColor))
        {
            cfg.Append(";--scalar-color-accent:").Append(JsString(options.Branding.AccentColor!));
        }
        cfg.Append("}\"");
        cfg.Append('}');
        return "'" + cfg.ToString().Replace("'", "\\'") + "'";
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

    private static string HtmlEscape(string s) => s
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");

    private static string JsString(string s) => s
        .Replace("\\", "\\\\")
        .Replace("'", "\\'")
        .Replace("\"", "\\\"")
        .Replace("\n", "\\n")
        .Replace("\r", "\\r");
}
