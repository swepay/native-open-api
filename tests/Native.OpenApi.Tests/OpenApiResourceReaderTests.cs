using System.Reflection;

namespace Native.OpenApi.Tests;

public class OpenApiResourceReaderTests
{
    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenAssemblyIsNull()
    {
        // Act
        var act = () => new OpenApiResourceReader(null!, "Namespace.");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("assembly");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenBaseNamespaceIsNull()
    {
        // Act
        var act = () => new OpenApiResourceReader(Assembly.GetExecutingAssembly(), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("baseNamespace");
    }

    [Fact]
    public void ReadText_ShouldThrowInvalidOperationException_WhenResourceNotFound()
    {
        // Arrange
        var reader = new OpenApiResourceReader(Assembly.GetExecutingAssembly(), "Native.OpenApi.Tests.");

        // Act
        var act = () => reader.ReadText("nonexistent/resource.yaml");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void ListResources_ShouldReturnListOfResourceNames()
    {
        // Arrange
        var reader = new OpenApiResourceReader(Assembly.GetExecutingAssembly(), "Native.OpenApi.Tests.");

        // Act
        var resources = reader.ListResources();

        // Assert
        resources.Should().NotBeNull();
        resources.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void ReadText_ShouldConvertPathSeparators()
    {
        // Arrange
        var reader = new OpenApiResourceReader(Assembly.GetExecutingAssembly(), "Native.OpenApi.Tests.");

        // Act - Should normalize path separators even if resource doesn't exist
        var act = () => reader.ReadText("path/to/resource.yaml");
        var act2 = () => reader.ReadText("path\\to\\resource.yaml");

        // Assert - Both should throw with the same normalized resource name
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Native.OpenApi.Tests.path.to.resource.yaml*");
        act2.Should().Throw<InvalidOperationException>()
            .WithMessage("*Native.OpenApi.Tests.path.to.resource.yaml*");
    }
}
