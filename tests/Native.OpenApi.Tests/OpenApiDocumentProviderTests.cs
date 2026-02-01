using System.Text.Json.Nodes;
using NSubstitute;

namespace Native.OpenApi.Tests;

public class OpenApiDocumentProviderTests
{
    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoaderIsNull()
    {
        // Arrange
        var merger = new OpenApiDocumentMerger();
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);

        // Act
        var act = () => new OpenApiDocumentProvider(null!, merger, linter);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("loader");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMergerIsNull()
    {
        // Arrange
        var loader = Substitute.For<IOpenApiDocumentLoader>();
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);

        // Act
        var act = () => new OpenApiDocumentProvider(loader, null!, linter);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("merger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLinterIsNull()
    {
        // Arrange
        var loader = Substitute.For<IOpenApiDocumentLoader>();
        var merger = new OpenApiDocumentMerger();

        // Act
        var act = () => new OpenApiDocumentProvider(loader, merger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("linter");
    }

    [Fact]
    public void Document_ShouldThrowInvalidOperationException_WhenWarmUpNotCalled()
    {
        // Arrange
        var loader = Substitute.For<IOpenApiDocumentLoader>();
        var merger = new OpenApiDocumentMerger();
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
        var provider = new OpenApiDocumentProvider(loader, merger, linter);

        // Act
        var act = () => _ = provider.Document;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Call WarmUp() first*");
    }

    [Fact]
    public void WarmUp_ShouldThrowInvalidOperationException_WhenLessThan3CommonParts()
    {
        // Arrange
        var loader = Substitute.For<IOpenApiDocumentLoader>();
        loader.LoadCommon().Returns(new List<OpenApiDocumentPart>
        {
            CreateValidPart("schemas")
        });
        loader.LoadPartials().Returns(new List<OpenApiDocumentPart>());

        var merger = new OpenApiDocumentMerger();
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
        var provider = new OpenApiDocumentProvider(loader, merger, linter);

        // Act
        var act = () => provider.WarmUp();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 3 parts*");
    }

    [Fact]
    public void WarmUp_ShouldIncrementLoadCount()
    {
        // Arrange
        var loader = CreateValidLoader();
        var merger = new TestMerger();
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
        var provider = new OpenApiDocumentProvider(loader, merger, linter);

        // Act
        provider.WarmUp();

        // Assert
        provider.LoadCount.Should().Be(1);
    }

    [Fact]
    public void WarmUp_ShouldNotReloadWhenCalledMultipleTimes()
    {
        // Arrange
        var loader = CreateValidLoader();
        var merger = new TestMerger();
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
        var provider = new OpenApiDocumentProvider(loader, merger, linter);

        // Act
        provider.WarmUp();
        provider.WarmUp();
        provider.WarmUp();

        // Assert
        provider.LoadCount.Should().Be(1);
    }

    [Fact]
    public void WarmUp_ShouldPopulateDocument()
    {
        // Arrange
        var loader = CreateValidLoader();
        var merger = new TestMerger();
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
        var provider = new OpenApiDocumentProvider(loader, merger, linter);

        // Act
        provider.WarmUp();

        // Assert
        provider.Document.Should().NotBeNull();
        provider.Document.Version.Should().Be("3.1.0");
        provider.Document.Json.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WarmUp_ShouldPopulateStats()
    {
        // Arrange
        var loader = CreateValidLoader();
        var merger = new TestMerger();
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
        var provider = new OpenApiDocumentProvider(loader, merger, linter);

        // Act
        provider.WarmUp();

        // Assert
        provider.Document.Stats.Should().NotBeNull();
        provider.Document.Stats.ResourceCount.Should().BeGreaterThan(0);
        provider.Document.Stats.LoadDuration.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void WarmUp_ShouldThrowOpenApiValidationException_WhenLinterFindsErrors()
    {
        // Arrange
        var loader = CreateLoaderWithInvalidSpecs();
        var merger = new TestMerger();
        var linter = new OpenApiLinter(new OpenApiLintOptions(["400", "500"], [], []));
        var provider = new OpenApiDocumentProvider(loader, merger, linter);

        // Act
        var act = () => provider.WarmUp();

        // Assert
        act.Should().Throw<OpenApiValidationException>();
    }

    private static IOpenApiDocumentLoader CreateValidLoader()
    {
        var loader = Substitute.For<IOpenApiDocumentLoader>();
        loader.LoadCommon().Returns(new List<OpenApiDocumentPart>
        {
            CreateValidPart("schemas", "/common/schemas.yaml"),
            CreateValidPart("responses", "/common/responses.yaml"),
            CreateValidPart("security", "/common/security.yaml")
        });
        loader.LoadPartials().Returns(new List<OpenApiDocumentPart>
        {
            CreateValidPartWithPath("users", "/v1/users", "users/openapi.yaml")
        });
        return loader;
    }

    private static IOpenApiDocumentLoader CreateLoaderWithInvalidSpecs()
    {
        var loader = Substitute.For<IOpenApiDocumentLoader>();
        loader.LoadCommon().Returns(new List<OpenApiDocumentPart>
        {
            CreateValidPart("schemas", "/common/schemas.yaml"),
            CreateValidPart("responses", "/common/responses.yaml"),
            CreateValidPart("security", "/common/security.yaml")
        });
        loader.LoadPartials().Returns(new List<OpenApiDocumentPart>
        {
            CreateInvalidPart("users", "users/openapi.yaml")
        });
        return loader;
    }

    private static OpenApiDocumentPart CreateValidPart(string name, string? sourcePath = null)
    {
        var json = "{\"openapi\":\"3.1.0\"}";
        var root = JsonNode.Parse(json)!;
        return new OpenApiDocumentPart(name, sourcePath ?? $"{name}.yaml", root, json);
    }

    private static OpenApiDocumentPart CreateValidPartWithPath(string name, string path, string sourcePath)
    {
        var json = $$"""
        {
            "openapi": "3.1.0",
            "paths": {
                "{{path}}": {
                    "get": {
                        "security": [{"JwtBearer": []}],
                        "responses": {
                            "200": {"$ref": "#/components/responses/Success"},
                            "400": {"$ref": "#/components/responses/BadRequest"},
                            "500": {"$ref": "#/components/responses/ServerError"}
                        }
                    }
                }
            }
        }
        """;
        var root = JsonNode.Parse(json)!;
        return new OpenApiDocumentPart(name, sourcePath, root, json);
    }

    private static OpenApiDocumentPart CreateInvalidPart(string name, string sourcePath)
    {
        // Missing required 400 and 500 responses
        var json = """
        {
            "openapi": "3.1.0",
            "paths": {
                "/v1/users": {
                    "get": {
                        "security": [{"JwtBearer": []}],
                        "responses": {
                            "200": {"$ref": "#/components/responses/Success"}
                        }
                    }
                }
            }
        }
        """;
        var root = JsonNode.Parse(json)!;
        return new OpenApiDocumentPart(name, sourcePath, root, json);
    }

    private sealed class TestMerger : OpenApiDocumentMerger
    {
        protected override string GetServerUrl() => "https://test.api.com";
        protected override string GetApiTitle() => "Test API";
        protected override string GetApiDescription() => "Test Description";
    }
}
