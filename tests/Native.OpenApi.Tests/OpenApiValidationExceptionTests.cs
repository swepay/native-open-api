namespace Native.OpenApi.Tests;

public class OpenApiValidationExceptionTests
{
    [Fact]
    public void Constructor_ShouldStoreErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var exception = new OpenApiValidationException(errors);

        // Assert
        exception.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Constructor_ShouldCreateMessageFromErrors()
    {
        // Arrange
        var errors = new[] { "Missing field", "Invalid value" };

        // Act
        var exception = new OpenApiValidationException(errors);

        // Assert
        exception.Message.Should().Contain("Missing field");
        exception.Message.Should().Contain("Invalid value");
        exception.Message.Should().Contain("OpenAPI validation failed");
    }

    [Fact]
    public void Constructor_ShouldHandleEmptyErrors()
    {
        // Arrange
        var errors = Array.Empty<string>();

        // Act
        var exception = new OpenApiValidationException(errors);

        // Assert
        exception.Errors.Should().BeEmpty();
        exception.Message.Should().Contain("OpenAPI validation failed");
    }

    [Fact]
    public void Constructor_ShouldHandleSingleError()
    {
        // Arrange
        var errors = new[] { "Single error" };

        // Act
        var exception = new OpenApiValidationException(errors);

        // Assert
        exception.Errors.Should().HaveCount(1);
        exception.Message.Should().Contain("Single error");
    }
}
