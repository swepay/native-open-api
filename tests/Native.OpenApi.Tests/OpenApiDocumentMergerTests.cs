using System.Text.Json.Nodes;

namespace Native.OpenApi.Tests;

public class OpenApiDocumentMergerTests
{
    private readonly TestOpenApiDocumentMerger _merger = new();

    [Fact]
    public void Merge_ShouldCreateDocumentWithOpenApiVersion()
    {
        // Arrange
        var schemas = CreateEmptyPart("schemas");
        var responses = CreateEmptyPart("responses");
        var security = CreateEmptyPart("security");

        // Act
        var result = _merger.Merge(schemas, responses, security, []);

        // Assert
        result["openapi"]!.GetValue<string>().Should().Be("3.1.0");
    }

    [Fact]
    public void Merge_ShouldIncludeInfoSection()
    {
        // Arrange
        var schemas = CreateEmptyPart("schemas");
        var responses = CreateEmptyPart("responses");
        var security = CreateEmptyPart("security");

        // Act
        var result = _merger.Merge(schemas, responses, security, []);

        // Assert
        result["info"].Should().NotBeNull();
        result["info"]!["title"]!.GetValue<string>().Should().Be("Test API");
        result["info"]!["description"]!.GetValue<string>().Should().Be("Test API Description");
    }

    [Fact]
    public void Merge_ShouldIncludeServerUrl()
    {
        // Arrange
        var schemas = CreateEmptyPart("schemas");
        var responses = CreateEmptyPart("responses");
        var security = CreateEmptyPart("security");

        // Act
        var result = _merger.Merge(schemas, responses, security, []);

        // Assert
        result["servers"].Should().NotBeNull();
        var servers = result["servers"] as JsonArray;
        servers.Should().NotBeNull();
        servers![0]!["url"]!.GetValue<string>().Should().Be("https://test.api.com");
    }

    [Fact]
    public void Merge_ShouldMergePathsFromPartials()
    {
        // Arrange
        var schemas = CreateEmptyPart("schemas");
        var responses = CreateEmptyPart("responses");
        var security = CreateEmptyPart("security");
        var partials = new[]
        {
            CreatePartWithPath("users", "/v1/users"),
            CreatePartWithPath("products", "/v1/products")
        };

        // Act
        var result = _merger.Merge(schemas, responses, security, partials);

        // Assert
        var paths = result["paths"] as JsonObject;
        paths.Should().NotBeNull();
        paths!.ContainsKey("/v1/users").Should().BeTrue();
        paths.ContainsKey("/v1/products").Should().BeTrue();
    }

    [Fact]
    public void Merge_ShouldThrowOnDuplicatePath()
    {
        // Arrange
        var schemas = CreateEmptyPart("schemas");
        var responses = CreateEmptyPart("responses");
        var security = CreateEmptyPart("security");
        var partials = new[]
        {
            CreatePartWithPath("users1", "/v1/users"),
            CreatePartWithPath("users2", "/v1/users")
        };

        // Act
        var act = () => _merger.Merge(schemas, responses, security, partials);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate path*");
    }

    [Fact]
    public void Merge_ShouldMergeComponentsFromSchemas()
    {
        // Arrange
        var schemas = CreatePartWithComponent("schemas", "schemas", "UserSchema");
        var responses = CreateEmptyPart("responses");
        var security = CreateEmptyPart("security");

        // Act
        var result = _merger.Merge(schemas, responses, security, []);

        // Assert
        var components = result["components"] as JsonObject;
        components.Should().NotBeNull();
        var schemasSection = components!["schemas"] as JsonObject;
        schemasSection.Should().NotBeNull();
        schemasSection!.ContainsKey("UserSchema").Should().BeTrue();
    }

    [Fact]
    public void Merge_ShouldThrowOnDuplicateComponent()
    {
        // Arrange
        var schemas = CreateEmptyPart("schemas");
        var responses = CreateEmptyPart("responses");
        var security = CreateEmptyPart("security");
        // Both partials define the same component schema (and have paths so they get processed)
        var partial1 = CreatePartWithPathAndComponent("partial1", "/v1/users", "schemas", "DuplicateSchema");
        var partial2 = CreatePartWithPathAndComponent("partial2", "/v1/products", "schemas", "DuplicateSchema");
        var partials = new[] { partial1, partial2 };

        // Act
        var act = () => _merger.Merge(schemas, responses, security, partials);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate component*");
    }

    private static OpenApiDocumentPart CreatePartWithPathAndComponent(string name, string path, string componentType, string componentName)
    {
        var json = $$"""
        {
            "paths": {
                "{{path}}": {
                    "get": {}
                }
            },
            "components": {
                "{{componentType}}": {
                    "{{componentName}}": {
                        "type": "object"
                    }
                }
            }
        }
        """;
        var root = JsonNode.Parse(json)!;
        return new OpenApiDocumentPart(name, $"{name}.yaml", root, json);
    }

    private static OpenApiDocumentPart CreateEmptyPart(string name)
    {
        var root = JsonNode.Parse("{}")!;
        return new OpenApiDocumentPart(name, $"{name}.yaml", root, "{}");
    }

    private static OpenApiDocumentPart CreatePartWithPath(string name, string path)
    {
        var json = $$"""
        {
            "paths": {
                "{{path}}": {
                    "get": {}
                }
            }
        }
        """;
        var root = JsonNode.Parse(json)!;
        return new OpenApiDocumentPart(name, $"{name}.yaml", root, json);
    }

    private static OpenApiDocumentPart CreatePartWithComponent(string name, string componentType, string componentName)
    {
        var json = $$"""
        {
            "components": {
                "{{componentType}}": {
                    "{{componentName}}": {
                        "type": "object"
                    }
                }
            }
        }
        """;
        var root = JsonNode.Parse(json)!;
        return new OpenApiDocumentPart(name, $"{name}.yaml", root, json);
    }

    private sealed class TestOpenApiDocumentMerger : OpenApiDocumentMerger
    {
        protected override string GetServerUrl() => "https://test.api.com";
        protected override string GetApiTitle() => "Test API";
        protected override string GetApiDescription() => "Test API Description";
    }
}
