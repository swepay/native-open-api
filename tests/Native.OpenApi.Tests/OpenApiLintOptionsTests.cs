namespace Native.OpenApi.Tests;

public class OpenApiLintOptionsTests
{
    [Fact]
    public void Empty_ShouldReturnOptionsWithEmptyLists()
    {
        // Act
        var options = OpenApiLintOptions.Empty;

        // Assert
        options.RequiredErrorResponses.Should().BeEmpty();
        options.SensitiveFieldNames.Should().BeEmpty();
        options.DisallowedGenericSegments.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldStoreRequiredErrorResponses()
    {
        // Arrange
        var responses = new[] { "400", "401", "500" };

        // Act
        var options = new OpenApiLintOptions(responses, [], []);

        // Assert
        options.RequiredErrorResponses.Should().BeEquivalentTo(responses);
    }

    [Fact]
    public void Constructor_ShouldStoreSensitiveFieldNames()
    {
        // Arrange
        var fields = new[] { "password", "token", "secret" };

        // Act
        var options = new OpenApiLintOptions([], fields, []);

        // Assert
        options.SensitiveFieldNames.Should().BeEquivalentTo(fields);
    }

    [Fact]
    public void Constructor_ShouldStoreDisallowedGenericSegments()
    {
        // Arrange
        var segments = new[] { "data", "items", "list" };

        // Act
        var options = new OpenApiLintOptions([], [], segments);

        // Assert
        options.DisallowedGenericSegments.Should().BeEquivalentTo(segments);
    }
}
