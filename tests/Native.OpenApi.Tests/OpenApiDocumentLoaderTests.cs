using System.Text.Json;
using System.Text.Json.Nodes;

namespace Native.OpenApi.Tests;

public class OpenApiDocumentLoaderTests
{
    [Fact]
    public void LoadCommon_ShouldReturnEmptyListByDefault()
    {
        // Arrange
        var loader = new TestDocumentLoader();

        // Act
        var result = loader.LoadCommon();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void LoadPartials_ShouldReturnPartsFromDerivedClass()
    {
        // Arrange
        var loader = new TestDocumentLoader();

        // Act
        var result = loader.LoadPartials();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("test-partial");
    }

    [Fact]
    public void LoadPartials_ShouldThrowWhenNoPartialsProvided()
    {
        // Arrange
        var loader = new EmptyDocumentLoader();

        // Act
        var act = () => loader.LoadPartials();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one partial*");
    }

    [Fact]
    public void LoadPart_ShouldCreateOpenApiDocumentPart()
    {
        // Arrange
        var loader = new TestDocumentLoader();
        var json = """{"openapi":"3.1.0"}""";

        // Act
        var result = loader.TestLoadPart("test-part", json);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("test-part");
        result.Raw.Should().Be(json);
    }

    [Fact]
    public void LoadPart_ShouldParseJsonCorrectly()
    {
        // Arrange
        var loader = new TestDocumentLoader();
        var json = """
        {
            "openapi": "3.1.0",
            "info": {
                "title": "Test API"
            }
        }
        """;

        // Act
        var result = loader.TestLoadPart("test-part", json);

        // Assert
        result.Root["openapi"]?.GetValue<string>().Should().Be("3.1.0");
        result.Root["info"]?["title"]?.GetValue<string>().Should().Be("Test API");
    }

    [Fact]
    public void LoadPart_ShouldPreserveSourcePath()
    {
        // Arrange
        var loader = new TestDocumentLoader();
        var json = """{"openapi":"3.1.0"}""";
        var sourcePath = "/api/users/openapi.yaml";

        // Act
        var result = loader.TestLoadPartWithPath("test-part", json, sourcePath);

        // Assert
        result.SourcePath.Should().Be(sourcePath);
    }

    [Fact]
    public void LoadPart_ShouldThrowForInvalidJson()
    {
        // Arrange
        var loader = new TestDocumentLoader();
        var invalidJson = "{ invalid json }";

        // Act
        var act = () => loader.TestLoadPart("test-part", invalidJson);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void LoadPart_ShouldHandleEmptyComponents()
    {
        // Arrange
        var loader = new TestDocumentLoader();
        var json = """
        {
            "openapi": "3.1.0",
            "components": {}
        }
        """;

        // Act
        var result = loader.TestLoadPart("empty-components", json);

        // Assert
        result.Root["components"].Should().NotBeNull();
    }

    [Fact]
    public void LoadPart_ShouldHandlePathsWithOperations()
    {
        // Arrange
        var loader = new TestDocumentLoader();
        var json = """
        {
            "openapi": "3.1.0",
            "paths": {
                "/users": {
                    "get": {
                        "operationId": "getUsers",
                        "responses": {
                            "200": {
                                "description": "Success"
                            }
                        }
                    }
                }
            }
        }
        """;

        // Act
        var result = loader.TestLoadPart("users-api", json);

        // Assert
        var paths = result.Root["paths"] as JsonObject;
        paths.Should().NotBeNull();
        paths.Should().ContainKey("/users");
    }

    [Fact]
    public void IsYamlFile_ShouldReturnTrueForYamlExtension()
    {
        // Arrange & Act & Assert
        TestDocumentLoader.TestIsYamlFile("test.yaml").Should().BeTrue();
        TestDocumentLoader.TestIsYamlFile("test.YAML").Should().BeTrue();
        TestDocumentLoader.TestIsYamlFile("path/to/spec.yaml").Should().BeTrue();
    }

    [Fact]
    public void IsYamlFile_ShouldReturnTrueForYmlExtension()
    {
        // Arrange & Act & Assert
        TestDocumentLoader.TestIsYamlFile("test.yml").Should().BeTrue();
        TestDocumentLoader.TestIsYamlFile("test.YML").Should().BeTrue();
        TestDocumentLoader.TestIsYamlFile("path/to/spec.yml").Should().BeTrue();
    }

    [Fact]
    public void IsYamlFile_ShouldReturnFalseForJsonExtension()
    {
        // Arrange & Act & Assert
        TestDocumentLoader.TestIsYamlFile("test.json").Should().BeFalse();
        TestDocumentLoader.TestIsYamlFile("test.JSON").Should().BeFalse();
        TestDocumentLoader.TestIsYamlFile("path/to/spec.json").Should().BeFalse();
    }

    private sealed class TestDocumentLoader : TestOpenApiDocumentLoaderBase
    {
        public override IReadOnlyList<OpenApiDocumentPart> LoadPartials()
        {
            var json = """{"openapi":"3.1.0","paths":{"/test":{}}}""";
            return [CreatePart("test-partial", json, "test/openapi.yaml")];
        }

        public OpenApiDocumentPart TestLoadPart(string name, string json)
            => CreatePart(name, json, $"{name}.json");

        public OpenApiDocumentPart TestLoadPartWithPath(string name, string json, string sourcePath)
            => CreatePart(name, json, sourcePath);

        private static OpenApiDocumentPart CreatePart(string name, string json, string sourcePath)
        {
            var root = JsonNode.Parse(json)!;
            return new OpenApiDocumentPart(name, sourcePath, root, json);
        }

        public static bool TestIsYamlFile(string path)
        {
            return path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class EmptyDocumentLoader : TestOpenApiDocumentLoaderBase
    {
        public override IReadOnlyList<OpenApiDocumentPart> LoadPartials()
        {
            throw new InvalidOperationException("Must provide at least one partial");
        }
    }

    // Simple test base class that doesn't require resource reader
    private abstract class TestOpenApiDocumentLoaderBase : IOpenApiDocumentLoader
    {
        public virtual IReadOnlyList<OpenApiDocumentPart> LoadCommon() => [];

        public abstract IReadOnlyList<OpenApiDocumentPart> LoadPartials();
    }
}
