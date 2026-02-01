namespace Native.OpenApi;

/// <summary>
/// Renders HTML pages for OpenAPI documentation viewers.
/// </summary>
public sealed class OpenApiHtmlRenderer
{
    /// <summary>
    /// Renders a Redoc HTML page for the given OpenAPI spec.
    /// </summary>
    /// <param name="specPath">The relative path to the OpenAPI JSON spec.</param>
    /// <param name="title">The page title.</param>
    /// <returns>The HTML content.</returns>
    public string RenderRedoc(string specPath, string title)
    {
        return "<!DOCTYPE html>\n" +
            "<html>\n" +
            "<head>\n" +
            $"    <title>{title}</title>\n" +
            "    <meta charset=\"utf-8\"/>\n" +
            "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n" +
            "    <link href=\"https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700\" rel=\"stylesheet\">\n" +
            "    <style>body { margin: 0; padding: 0; }</style>\n" +
            "</head>\n" +
            "<body>\n" +
            "    <redoc spec-url=\"\"></redoc>\n" +
            "    <script src=\"https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js\"></script>\n" +
            "    <script>\n" +
            "        (function() {\n" +
            "            var basePath = window.location.pathname.replace(/\\/docs\\/redoc.*/, '');\n" +
            $"            var specUrl = basePath + '{specPath}';\n" +
            "            Redoc.init(specUrl, {\n" +
            "                theme: {\n" +
            "                    colors: { primary: { main: '#1976d2' } },\n" +
            "                    typography: { fontFamily: 'Roboto, sans-serif' }\n" +
            "                }\n" +
            "            }, document.querySelector('redoc'));\n" +
            "        })();\n" +
            "    </script>\n" +
            "</body>\n" +
            "</html>";
    }

    /// <summary>
    /// Renders a Scalar HTML page for the given OpenAPI spec.
    /// </summary>
    /// <param name="specPath">The relative path to the OpenAPI JSON spec.</param>
    /// <param name="title">The page title.</param>
    /// <returns>The HTML content.</returns>
    public string RenderScalar(string specPath, string title)
    {
        return "<!DOCTYPE html>\n" +
            "<html>\n" +
            "<head>\n" +
            $"    <title>{title}</title>\n" +
            "    <meta charset=\"utf-8\"/>\n" +
            "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n" +
            "</head>\n" +
            "<body>\n" +
            "    <script id=\"api-reference\"></script>\n" +
            "    <script>\n" +
            "        (function() {\n" +
            "            var basePath = window.location.pathname.replace(/\\/docs\\/scalar.*/, '');\n" +
            $"            var specUrl = basePath + '{specPath}';\n" +
            "            document.getElementById('api-reference').setAttribute('data-url', specUrl);\n" +
            "            var script = document.createElement('script');\n" +
            "            script.src = 'https://cdn.jsdelivr.net/npm/@scalar/api-reference';\n" +
            "            document.body.appendChild(script);\n" +
            "        })();\n" +
            "    </script>\n" +
            "</body>\n" +
            "</html>";
    }
}
