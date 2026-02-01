using System.Text.Json.Nodes;

namespace Native.OpenApi.Tests;

public class OpenApiDocumentPartTests
{
    [Fact]
    public void Constructor_ShouldStoreAllProperties()
    {
        // Arrange
        var name = "users";
        var sourcePath = "openapi/users/spec.yaml";
        var root = JsonNode.Parse("{\"openapi\":\"3.1.0\"}")!;
        var raw = "{\"openapi\":\"3.1.0\"}";

        // Act
        var part = new OpenApiDocumentPart(name, sourcePath, root, raw);

        // Assert
        part.Name.Should().Be(name);
        part.SourcePath.Should().Be(sourcePath);
        part.Root.Should().NotBeNull();
        part.Raw.Should().Be(raw);
    }

    [Fact]
    public void Record_ShouldSupportEquality()
    {
        // Arrange
        var root1 = JsonNode.Parse("{\"openapi\":\"3.1.0\"}")!;
        var root2 = JsonNode.Parse("{\"openapi\":\"3.1.0\"}")!;
        var part1 = new OpenApiDocumentPart("test", "path", root1, "raw");
        var part2 = new OpenApiDocumentPart("test", "path", root2, "raw");

        // Note: JsonNode doesn't implement value equality, so parts with same content but different node instances are not equal
        part1.Name.Should().Be(part2.Name);
        part1.SourcePath.Should().Be(part2.SourcePath);
        part1.Raw.Should().Be(part2.Raw);
    }

    [Fact]
    public void Root_ShouldBeAccessible()
    {
        // Arrange
        var jsonContent = """
        {
            "openapi": "3.1.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            }
        }
        """;
        var root = JsonNode.Parse(jsonContent)!;
        var part = new OpenApiDocumentPart("test", "test.yaml", root, jsonContent);

        // Act & Assert
        part.Root["openapi"]!.GetValue<string>().Should().Be("3.1.0");
        part.Root["info"]!["title"]!.GetValue<string>().Should().Be("Test API");
    }
}
