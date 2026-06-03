using Native.OpenApi.Rendering;

namespace Native.OpenApi.Tests;

/// <summary>
/// Tests for <see cref="OpenApiHtmlRenderer"/>.
/// Covers: back-compat overloads, Scalar config object, branding, footer,
/// Mermaid, air-gap mode, XSS escaping, and new ScalarViewerOptions.
/// </summary>
public class OpenApiHtmlRendererTests
{
    private readonly OpenApiHtmlRenderer _renderer = new();

    // -------------------------------------------------------------------------
    // Back-compat: RenderRedoc(specPath, title)
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderRedoc_DefaultOverload_ReturnsValidHtml()
    {
        var html = _renderer.RenderRedoc("/openapi/v1/spec.json", "My API");

        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<html>");
        html.Should().Contain("</html>");
    }

    [Fact]
    public void RenderRedoc_DefaultOverload_IncludesTitle()
    {
        var html = _renderer.RenderRedoc("/openapi/v1/spec.json", "Custom Title");

        html.Should().Contain("<title>Custom Title</title>");
    }

    [Fact]
    public void RenderRedoc_DefaultOverload_IncludesSpecPath()
    {
        var html = _renderer.RenderRedoc("/api/v2/openapi.json", "API Docs");

        html.Should().Contain("/api/v2/openapi.json");
    }

    [Fact]
    public void RenderRedoc_DefaultOverload_IncludesRedocScriptAndElement()
    {
        var html = _renderer.RenderRedoc("/spec.json", "API");

        html.Should().Contain("redoc.standalone.js");
        html.Should().Contain("<redoc");
    }

    [Fact]
    public void RenderRedoc_DefaultOverload_IncludesMetaTags()
    {
        var html = _renderer.RenderRedoc("/spec.json", "API");

        html.Should().Contain("charset=\"utf-8\"");
        html.Should().Contain("viewport");
    }

    // -------------------------------------------------------------------------
    // Back-compat: RenderScalar(specPath, title)
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_DefaultOverload_ReturnsValidHtml()
    {
        var html = _renderer.RenderScalar("/openapi/v1/spec.json", "My API");

        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<html>");
        html.Should().Contain("</html>");
    }

    [Fact]
    public void RenderScalar_DefaultOverload_IncludesTitle()
    {
        var html = _renderer.RenderScalar("/openapi/v1/spec.json", "Scalar Title");

        html.Should().Contain("<title>Scalar Title</title>");
    }

    [Fact]
    public void RenderScalar_DefaultOverload_IncludesSpecPath()
    {
        var html = _renderer.RenderScalar("/api/v3/openapi.json", "API Docs");

        html.Should().Contain("/api/v3/openapi.json");
    }

    [Fact]
    public void RenderScalar_DefaultOverload_IncludesScalarScript()
    {
        var html = _renderer.RenderScalar("/spec.json", "API");

        html.Should().Contain("@scalar/api-reference");
        html.Should().Contain("api-reference");
    }

    [Fact]
    public void RenderScalar_DefaultOverload_IncludesMetaTags()
    {
        var html = _renderer.RenderScalar("/spec.json", "API");

        html.Should().Contain("charset=\"utf-8\"");
        html.Should().Contain("viewport");
    }

    // -------------------------------------------------------------------------
    // Scalar config object — automatic features (nothing disables them)
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_DefaultConfig_DoesNotDisableTagGroups()
    {
        // x-tagGroups is automatic in Scalar — no config key disables it.
        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(OpenApiRendererOptions.Default);

        config.Should().NotContain("\"tagGroups\":false");
        config.Should().NotContain("tagGroups");
    }

    [Fact]
    public void RenderScalar_DefaultConfig_DoesNotDisableCodeSamples()
    {
        // Scalar renders x-codeSamples automatically. Ensure no suppression key.
        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(OpenApiRendererOptions.Default);

        config.Should().NotContain("\"codeSamples\":false");
        config.Should().NotContain("\"showCodeSamples\":false");
    }

    [Fact]
    public void RenderScalar_DefaultConfig_DoesNotHideModels()
    {
        // Default: models are visible (hideModels key absent).
        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(OpenApiRendererOptions.Default);

        config.Should().NotContain("\"hideModels\":true");
    }

    [Fact]
    public void RenderScalar_DefaultConfig_ContainsDataConfigurationAttribute()
    {
        var html = _renderer.RenderScalar("/spec.json", "API");

        // Config is in an HTML attribute, not a JS string literal.
        html.Should().Contain("data-configuration=");
        // Must be inside the api-reference script element.
        html.Should().Contain("id=\"api-reference\"");
    }

    [Fact]
    public void RenderScalar_DefaultConfig_ContainsThemeDefault()
    {
        // The config JSON is HTML-escaped inside the data-configuration attribute.
        var html = _renderer.RenderScalar("/spec.json", "API");

        html.Should().Contain("&quot;theme&quot;:&quot;default&quot;");
    }

    [Fact]
    public void RenderScalar_DefaultConfig_ContainsDarkModeFalse()
    {
        var html = _renderer.RenderScalar("/spec.json", "API");

        html.Should().Contain("&quot;darkMode&quot;:false");
    }

    [Fact]
    public void RenderScalar_DefaultConfig_ContainsLayoutSidebar()
    {
        var html = _renderer.RenderScalar("/spec.json", "API");

        html.Should().Contain("&quot;layout&quot;:&quot;sidebar&quot;");
    }

    // -------------------------------------------------------------------------
    // Scalar config — branding colours in customCss
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_WithBranding_InjectsCustomCssWithPrimaryColor()
    {
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions { PrimaryColor = "#ff6600" }
        };

        // The data-configuration attribute is HTML-escaped, so embedded CSS
        // content is also HTML-escaped in the HTML source. Verify via the
        // BuildScalarConfigJson unit method for the raw JSON form.
        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().Contain("--scalar-color-1:#ff6600");
    }

    [Fact]
    public void RenderScalar_WithBranding_DataConfigurationAttributeContainsPrimaryColor()
    {
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions { PrimaryColor = "#ff6600" }
        };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        // In the HTML attribute the color appears unescaped (no special chars).
        html.Should().Contain("--scalar-color-1:#ff6600");
    }

    [Fact]
    public void RenderScalar_WithAccentColor_InjectsAccentInCustomCss()
    {
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions
            {
                PrimaryColor = "#1976d2",
                AccentColor = "#e91e63"
            }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().Contain("--scalar-color-accent:#e91e63");
    }

    [Fact]
    public void RenderScalar_WithoutAccentColor_DoesNotEmitAccentVar()
    {
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions { PrimaryColor = "#1976d2", AccentColor = null }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().NotContain("--scalar-color-accent");
    }

    // -------------------------------------------------------------------------
    // Scalar config — logo
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_WithLogoUrl_InjectsLogoInScalarConfig()
    {
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions { LogoUrl = "https://cdn.example.com/logo.svg" }
        };

        // Check the raw JSON config (before HTML-encoding).
        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().Contain("\"logo\":{\"src\":\"https://cdn.example.com/logo.svg\"}");
    }

    [Fact]
    public void RenderScalar_WithLogoUrl_HtmlContainsEscapedLogoKey()
    {
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions { LogoUrl = "https://cdn.example.com/logo.svg" }
        };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        // In the HTML attribute the key is &quot;logo&quot;.
        html.Should().Contain("&quot;logo&quot;");
    }

    [Fact]
    public void RenderScalar_WithoutLogoUrl_DoesNotEmitLogoKey()
    {
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions { LogoUrl = null }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().NotContain("\"logo\"");
    }

    [Fact]
    public void RenderRedoc_WithLogoUrl_IncludesLogoGutterInTheme()
    {
        // For Redoc, logo causes the gutter to be included in the theme object.
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions { LogoUrl = "https://cdn.example.com/logo.svg" }
        };

        var html = _renderer.RenderRedoc("/spec.json", "API", options);

        html.Should().Contain("\"logo\":{\"gutter\":\"16px\"}");
    }

    // -------------------------------------------------------------------------
    // Scalar viewer options — theme / dark mode / layout
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_WithDarkMode_EmitsDarkModeTrue()
    {
        var options = new OpenApiRendererOptions
        {
            ScalarViewer = new OpenApiScalarViewerOptions { DarkMode = true }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().Contain("\"darkMode\":true");
    }

    [Fact]
    public void RenderScalar_WithThemeMidnight_EmitsThemeMidnight()
    {
        var options = new OpenApiRendererOptions
        {
            ScalarViewer = new OpenApiScalarViewerOptions { Theme = "midnight" }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().Contain("\"theme\":\"midnight\"");
    }

    [Fact]
    public void RenderScalar_WithLayoutClassic_EmitsLayoutClassic()
    {
        var options = new OpenApiRendererOptions
        {
            ScalarViewer = new OpenApiScalarViewerOptions { Layout = "classic" }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().Contain("\"layout\":\"classic\"");
    }

    // -------------------------------------------------------------------------
    // Scalar viewer options — feature flags
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_WithHideModels_EmitsHideModelsTrue()
    {
        var options = new OpenApiRendererOptions
        {
            ScalarViewer = new OpenApiScalarViewerOptions { HideModels = true }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().Contain("\"hideModels\":true");
    }

    [Fact]
    public void RenderScalar_WithHideDownloadButton_EmitsHideDownloadButtonTrue()
    {
        var options = new OpenApiRendererOptions
        {
            ScalarViewer = new OpenApiScalarViewerOptions { HideDownloadButton = true }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().Contain("\"hideDownloadButton\":true");
    }

    [Fact]
    public void RenderScalar_WithHideSidebar_EmitsHideSidebarTrue()
    {
        var options = new OpenApiRendererOptions
        {
            ScalarViewer = new OpenApiScalarViewerOptions { HideSidebar = true }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().Contain("\"hideSidebar\":true");
    }

    [Fact]
    public void RenderScalar_WithHideTestRequestButton_EmitsHideTestRequestButtonTrue()
    {
        var options = new OpenApiRendererOptions
        {
            ScalarViewer = new OpenApiScalarViewerOptions { HideTestRequestButton = true }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);
        config.Should().Contain("\"hideTestRequestButton\":true");
    }

    [Fact]
    public void RenderScalar_WithDefaultFlagsOff_DoesNotEmitHideKeys()
    {
        // When feature flags are false (default), the keys must not appear
        // so Scalar keeps its own defaults (show everything).
        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(OpenApiRendererOptions.Default);

        config.Should().NotContain("\"hideModels\":true");
        config.Should().NotContain("\"hideDownloadButton\":true");
        config.Should().NotContain("\"hideSidebar\":true");
        config.Should().NotContain("\"hideTestRequestButton\":true");
    }

    // -------------------------------------------------------------------------
    // Scalar viewer options — default HTTP client
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_WithDefaultHttpClient_EmitsDefaultHttpClientObject()
    {
        var options = new OpenApiRendererOptions
        {
            ScalarViewer = new OpenApiScalarViewerOptions
            {
                DefaultHttpClientTargetKey = "Node",
                DefaultHttpClientClientKey = "axios"
            }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);

        config.Should().Contain("\"defaultHttpClient\":{");
        config.Should().Contain("\"targetKey\":\"Node\"");
        config.Should().Contain("\"clientKey\":\"axios\"");
    }

    [Fact]
    public void RenderScalar_WithOnlyTargetKey_EmitsDefaultHttpClientWithTargetOnly()
    {
        var options = new OpenApiRendererOptions
        {
            ScalarViewer = new OpenApiScalarViewerOptions
            {
                DefaultHttpClientTargetKey = "Python",
                DefaultHttpClientClientKey = null
            }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);

        config.Should().Contain("\"targetKey\":\"Python\"");
        config.Should().NotContain("\"clientKey\"");
    }

    [Fact]
    public void RenderScalar_WithoutDefaultHttpClient_DoesNotEmitDefaultHttpClientKey()
    {
        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(OpenApiRendererOptions.Default);

        config.Should().NotContain("\"defaultHttpClient\"");
    }

    // -------------------------------------------------------------------------
    // Air-gap mode — Scalar local asset
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_WithLocalAssetPath_UsesLocalScriptSrc()
    {
        var options = new OpenApiRendererOptions
        {
            ScalarViewer = new OpenApiScalarViewerOptions
            {
                LocalAssetPath = "./assets/scalar-api-reference.js"
            }
        };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        html.Should().Contain("./assets/scalar-api-reference.js");
        html.Should().NotContain("cdn.jsdelivr.net/npm/@scalar");
    }

    [Fact]
    public void RenderScalar_WithoutLocalAssetPath_UsesCdnUrl()
    {
        var html = _renderer.RenderScalar("/spec.json", "API");

        html.Should().Contain("cdn.jsdelivr.net/npm/@scalar/api-reference");
    }

    // -------------------------------------------------------------------------
    // Air-gap mode — Mermaid local asset
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_WithMermaidLocalAsset_UsesLocalMermaidSrc()
    {
        var options = new OpenApiRendererOptions
        {
            EnableMermaid = true,
            MermaidFromLocalAsset = true
        };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        html.Should().Contain("./assets/mermaid.min.js");
        html.Should().NotContain("cdn.jsdelivr.net/npm/mermaid");
    }

    [Fact]
    public void RenderScalar_WithMermaidCdn_UsesCdnMermaidSrc()
    {
        var options = new OpenApiRendererOptions
        {
            EnableMermaid = true,
            MermaidFromLocalAsset = false
        };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        html.Should().Contain("cdn.jsdelivr.net/npm/mermaid");
    }

    [Fact]
    public void RenderScalar_WithMermaidEnabled_InjectsMermaidPreprocessor()
    {
        var options = new OpenApiRendererOptions { EnableMermaid = true };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        html.Should().Contain("convertMermaidBlocks");
        html.Should().Contain("bootstrapMermaid");
        html.Should().Contain("MutationObserver");
    }

    [Fact]
    public void RenderScalar_WithMermaidDisabled_DoesNotInjectMermaidCode()
    {
        var options = new OpenApiRendererOptions { EnableMermaid = false };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        html.Should().NotContain("mermaid.min.js");
        html.Should().NotContain("convertMermaidBlocks");
    }

    // -------------------------------------------------------------------------
    // Favicon
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_WithFaviconUrl_EmitsFaviconLink()
    {
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions { FaviconUrl = "https://example.com/favicon.ico" }
        };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        html.Should().Contain("<link rel=\"icon\" href=\"https://example.com/favicon.ico\"/>");
    }

    [Fact]
    public void RenderScalar_WithoutFaviconUrl_DoesNotEmitFaviconLink()
    {
        var html = _renderer.RenderScalar("/spec.json", "API");

        html.Should().NotContain("rel=\"icon\"");
    }

    // -------------------------------------------------------------------------
    // Footer
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_WithFooterLinks_EmitsFooterElement()
    {
        var options = new OpenApiRendererOptions
        {
            Footer = new OpenApiFooterOptions
            {
                StatusUrl = "https://status.swepay.com.br",
                SupportUrl = "https://support.swepay.com.br"
            }
        };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        html.Should().Contain("<footer class=\"swepay-footer\">");
        html.Should().Contain("https://status.swepay.com.br");
        html.Should().Contain("https://support.swepay.com.br");
    }

    [Fact]
    public void RenderScalar_WithoutFooterLinks_OmitsFooterElement()
    {
        var html = _renderer.RenderScalar("/spec.json", "API");

        html.Should().NotContain("swepay-footer");
    }

    [Fact]
    public void RenderScalar_WithAllFooterLinks_EmitsAllLinks()
    {
        var options = new OpenApiRendererOptions
        {
            Footer = new OpenApiFooterOptions
            {
                StatusUrl = "https://status.example.com",
                SupportUrl = "https://support.example.com",
                ChangelogUrl = "https://changelog.example.com",
                SlaUrl = "https://sla.example.com",
                TermsUrl = "https://terms.example.com"
            }
        };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        html.Should().Contain("Status");
        html.Should().Contain("Support");
        html.Should().Contain("Changelog");
        html.Should().Contain("SLA");
        html.Should().Contain("Terms");
    }

    // -------------------------------------------------------------------------
    // XSS escaping
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_WithXssInTitle_EscapesHtmlInTitle()
    {
        var html = _renderer.RenderScalar("/spec.json", "<script>alert('xss')</script>");

        html.Should().Contain("&lt;script&gt;");
        html.Should().NotContain("<script>alert");
    }

    [Fact]
    public void RenderScalar_WithXssInSpecPath_EscapesInJsString()
    {
        // A single-quote in the spec path must be escaped inside the JS string.
        var html = _renderer.RenderScalar("/spec/o'malley.json", "API");

        html.Should().Contain("\\'");
        // The raw unescaped quote must not appear inside the JS string context.
        // We verify the path is present but with escaping applied.
        html.Should().Contain("o\\'malley.json");
    }

    [Fact]
    public void RenderScalar_WithXssInLogoUrl_HtmlEscapesInDataConfiguration()
    {
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions
            {
                LogoUrl = "https://example.com/logo.svg?a=1&b=2"
            }
        };

        var html = _renderer.RenderScalar("/spec.json", "API", options);

        // The data-configuration attribute value is HTML-escaped, so the JSON
        // string containing the URL must have & as &amp; in the attribute.
        html.Should().Contain("&amp;");
    }

    [Fact]
    public void RenderScalar_WithXssInPrimaryColor_JsonEscapesDoubleQuote()
    {
        // Adversarial primary color containing a double-quote that would
        // prematurely close the JSON string if not escaped.
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions { PrimaryColor = "#ff0000\"}};alert(1);//" }
        };

        var config = OpenApiHtmlRenderer.BuildScalarConfigJson(options);

        // The embedded double-quote must appear as \" inside the JSON string,
        // not as a raw " that would terminate the string early.
        config.Should().Contain("\\\"");

        // The customCss key must appear after a well-formed opening: the last
        // key in the object is customCss and the value must be a valid JSON
        // string (starts and ends with unescaped ").
        config.Should().StartWith("{");
        config.Should().EndWith("}");
    }

    [Fact]
    public void RenderRedoc_WithXssInTitle_EscapesHtml()
    {
        var html = _renderer.RenderRedoc("/spec.json", "<img src=x onerror=alert(1)>");

        html.Should().Contain("&lt;img");
        html.Should().NotContain("<img src=x");
    }

    [Fact]
    public void RenderRedoc_WithSingleQuoteInTitle_EscapedInHtml()
    {
        var html = _renderer.RenderRedoc("/spec.json", "O'Reilly API");

        // Single quote in title element must be HTML-escaped.
        html.Should().Contain("&#x27;");
    }

    // -------------------------------------------------------------------------
    // BuildScalarConfigJson unit-level tests
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildScalarConfigJson_DefaultOptions_ProducesValidJsonShape()
    {
        var json = OpenApiHtmlRenderer.BuildScalarConfigJson(OpenApiRendererOptions.Default);

        json.Should().StartWith("{");
        json.Should().EndWith("}");
        json.Should().Contain("\"theme\":");
        json.Should().Contain("\"darkMode\":");
        json.Should().Contain("\"layout\":");
        json.Should().Contain("\"customCss\":");
    }

    [Fact]
    public void BuildScalarConfigJson_DefaultOptions_NoPrimaryColorXssInJson()
    {
        // Default primary color is safe; just confirm the key is present.
        var json = OpenApiHtmlRenderer.BuildScalarConfigJson(OpenApiRendererOptions.Default);

        json.Should().Contain("--scalar-color-1:#1976d2");
    }

    [Fact]
    public void BuildScalarConfigJson_WithAllScalarViewerOptions_EmitsAllKeys()
    {
        var options = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions
            {
                PrimaryColor = "#abcdef",
                AccentColor = "#fedcba",
                LogoUrl = "https://logo.example.com/logo.png"
            },
            ScalarViewer = new OpenApiScalarViewerOptions
            {
                Theme = "purple",
                DarkMode = true,
                Layout = "classic",
                HideModels = true,
                HideDownloadButton = true,
                HideSidebar = true,
                HideTestRequestButton = true,
                DefaultHttpClientTargetKey = "Go",
                DefaultHttpClientClientKey = "native"
            }
        };

        var json = OpenApiHtmlRenderer.BuildScalarConfigJson(options);

        json.Should().Contain("\"theme\":\"purple\"");
        json.Should().Contain("\"darkMode\":true");
        json.Should().Contain("\"layout\":\"classic\"");
        json.Should().Contain("\"hideModels\":true");
        json.Should().Contain("\"hideDownloadButton\":true");
        json.Should().Contain("\"hideSidebar\":true");
        json.Should().Contain("\"hideTestRequestButton\":true");
        json.Should().Contain("\"targetKey\":\"Go\"");
        json.Should().Contain("\"clientKey\":\"native\"");
        json.Should().Contain("\"logo\":{\"src\":\"https://logo.example.com/logo.png\"}");
        json.Should().Contain("--scalar-color-1:#abcdef");
        json.Should().Contain("--scalar-color-accent:#fedcba");
    }

    // -------------------------------------------------------------------------
    // Spec URL runtime resolution
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_SpecUrlResolution_SetsDataUrlViaJs()
    {
        // The spec URL must be set via setAttribute so the path is resolved
        // at runtime (not baked in as a static href).
        var html = _renderer.RenderScalar("/openapi.yaml", "API");

        html.Should().Contain("setAttribute('data-url', specUrl)");
    }

    [Fact]
    public void RenderScalar_SpecUrlResolution_BasepathStripsScalarSuffix()
    {
        // The basePath regex must strip /docs/scalar... so the spec URL is
        // resolved from the API root, not the docs sub-path.
        var html = _renderer.RenderScalar("/openapi.yaml", "API");

        html.Should().Contain("replace(/\\/docs\\/scalar.*/, '')");
    }

    // -------------------------------------------------------------------------
    // Config delivery mechanism — HTML attribute, not JS literal
    // -------------------------------------------------------------------------

    [Fact]
    public void RenderScalar_ConfigDelivery_PlacedInHtmlAttributeNotJsLiteral()
    {
        var html = _renderer.RenderScalar("/spec.json", "API");

        // The data-configuration value must be in the HTML attribute on the
        // <script> element, not as a JS assignment inside a <script> block.
        // Check: the JSON opening brace appears HTML-encoded as an attribute.
        html.Should().Contain("data-configuration=\"{");
    }
}
