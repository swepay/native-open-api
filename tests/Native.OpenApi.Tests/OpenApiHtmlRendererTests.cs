namespace Native.OpenApi.Tests;

public class OpenApiHtmlRendererTests
{
    private readonly OpenApiHtmlRenderer _renderer = new();

    [Fact]
    public void RenderRedoc_ShouldReturnValidHtml()
    {
        // Act
        var html = _renderer.RenderRedoc("/openapi/v1/spec.json", "My API");

        // Assert
        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<html>");
        html.Should().Contain("</html>");
    }

    [Fact]
    public void RenderRedoc_ShouldIncludeTitle()
    {
        // Act
        var html = _renderer.RenderRedoc("/openapi/v1/spec.json", "Custom Title");

        // Assert
        html.Should().Contain("<title>Custom Title</title>");
    }

    [Fact]
    public void RenderRedoc_ShouldIncludeSpecPath()
    {
        // Act
        var html = _renderer.RenderRedoc("/api/v2/openapi.json", "API Docs");

        // Assert
        html.Should().Contain("/api/v2/openapi.json");
    }

    [Fact]
    public void RenderRedoc_ShouldIncludeRedocScript()
    {
        // Act
        var html = _renderer.RenderRedoc("/spec.json", "API");

        // Assert
        html.Should().Contain("redoc.standalone.js");
        html.Should().Contain("<redoc");
    }

    [Fact]
    public void RenderScalar_ShouldReturnValidHtml()
    {
        // Act
        var html = _renderer.RenderScalar("/openapi/v1/spec.json", "My API");

        // Assert
        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<html>");
        html.Should().Contain("</html>");
    }

    [Fact]
    public void RenderScalar_ShouldIncludeTitle()
    {
        // Act
        var html = _renderer.RenderScalar("/openapi/v1/spec.json", "Scalar Title");

        // Assert
        html.Should().Contain("<title>Scalar Title</title>");
    }

    [Fact]
    public void RenderScalar_ShouldIncludeSpecPath()
    {
        // Act
        var html = _renderer.RenderScalar("/api/v3/openapi.json", "API Docs");

        // Assert
        html.Should().Contain("/api/v3/openapi.json");
    }

    [Fact]
    public void RenderScalar_ShouldIncludeScalarScript()
    {
        // Act
        var html = _renderer.RenderScalar("/spec.json", "API");

        // Assert
        html.Should().Contain("@scalar/api-reference");
        html.Should().Contain("api-reference");
    }

    [Fact]
    public void RenderRedoc_ShouldIncludeMetaTags()
    {
        // Act
        var html = _renderer.RenderRedoc("/spec.json", "API");

        // Assert
        html.Should().Contain("charset=\"utf-8\"");
        html.Should().Contain("viewport");
    }

    [Fact]
    public void RenderScalar_ShouldIncludeMetaTags()
    {
        // Act
        var html = _renderer.RenderScalar("/spec.json", "API");

        // Assert
        html.Should().Contain("charset=\"utf-8\"");
        html.Should().Contain("viewport");
    }
}
