using System.Text.Json.Nodes;

namespace Native.OpenApi.Tests;

public class OpenApiDocumentTests
{
    [Fact]
    public void Constructor_ShouldStoreAllProperties()
    {
        // Arrange
        var root = JsonNode.Parse("{\"openapi\":\"3.1.0\"}")!;
        var json = "{\"openapi\":\"3.1.0\"}";
        var yaml = "openapi: '3.1.0'";
        var version = "3.1.0";
        var loadedAt = DateTimeOffset.UtcNow;
        var stats = new OpenApiLoadStats(TimeSpan.FromMilliseconds(100), 5, 10);

        // Act
        var document = new OpenApiDocument(root, json, yaml, version, loadedAt, stats);

        // Assert
        document.Root.Should().NotBeNull();
        document.Json.Should().Be(json);
        document.Yaml.Should().Be(yaml);
        document.Version.Should().Be(version);
        document.LoadedAt.Should().Be(loadedAt);
        document.Stats.Should().Be(stats);
    }

    [Fact]
    public void Stats_ShouldContainCorrectInformation()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(250);
        var resourceCount = 8;
        var pathCount = 15;
        var stats = new OpenApiLoadStats(duration, resourceCount, pathCount);

        // Assert
        stats.LoadDuration.Should().Be(duration);
        stats.ResourceCount.Should().Be(resourceCount);
        stats.PathCount.Should().Be(pathCount);
    }

    [Fact]
    public void Record_ShouldSupportDeconstruction()
    {
        // Arrange
        var root = JsonNode.Parse("{\"openapi\":\"3.1.0\"}")!;
        var stats = new OpenApiLoadStats(TimeSpan.Zero, 1, 1);
        var document = new OpenApiDocument(root, "{}", "{}", "3.1.0", DateTimeOffset.MinValue, stats);

        // Act
        var (_, json, yaml, version, _, loadStats) = document;

        // Assert
        json.Should().Be("{}");
        yaml.Should().Be("{}");
        version.Should().Be("3.1.0");
        loadStats.Should().Be(stats);
    }

    [Fact]
    public void Stats_Record_ShouldSupportEquality()
    {
        // Arrange
        var stats1 = new OpenApiLoadStats(TimeSpan.FromSeconds(1), 5, 10);
        var stats2 = new OpenApiLoadStats(TimeSpan.FromSeconds(1), 5, 10);
        var stats3 = new OpenApiLoadStats(TimeSpan.FromSeconds(2), 5, 10);

        // Assert
        stats1.Should().Be(stats2);
        stats1.Should().NotBe(stats3);
    }
}
