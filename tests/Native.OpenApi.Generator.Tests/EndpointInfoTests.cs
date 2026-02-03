namespace Native.OpenApi.Generator.Tests;

public sealed class EndpointInfoTests
{
    [Fact]
    public void EndpointInfo_DefaultValues_AreCorrect()
    {
        // Act
        var endpoint = new EndpointInfo();

        // Assert
        endpoint.Method.Should().Be("");
        endpoint.Path.Should().Be("");
        endpoint.CommandTypeName.Should().Be("");
        endpoint.ResponseTypeName.Should().Be("");
        endpoint.CommandSimpleName.Should().Be("");
        endpoint.ResponseSimpleName.Should().Be("");
        endpoint.RequiresAuth.Should().BeTrue();
        endpoint.ProducesContentType.Should().BeNull();
        endpoint.SourceFile.Should().BeNull();
        endpoint.LineNumber.Should().Be(0);
    }

    [Fact]
    public void EndpointInfo_CanSetAllProperties()
    {
        // Arrange & Act
        var endpoint = new EndpointInfo
        {
            Method = "GET",
            Path = "/v1/items/{id}",
            CommandTypeName = "MyApp.Commands.GetItemCommand",
            ResponseTypeName = "MyApp.Responses.GetItemResponse",
            CommandSimpleName = "GetItemCommand",
            ResponseSimpleName = "GetItemResponse",
            RequiresAuth = false,
            ProducesContentType = "application/xml",
            SourceFile = "MyRouter.cs",
            LineNumber = 42
        };

        // Assert
        endpoint.Method.Should().Be("GET");
        endpoint.Path.Should().Be("/v1/items/{id}");
        endpoint.CommandTypeName.Should().Be("MyApp.Commands.GetItemCommand");
        endpoint.ResponseTypeName.Should().Be("MyApp.Responses.GetItemResponse");
        endpoint.CommandSimpleName.Should().Be("GetItemCommand");
        endpoint.ResponseSimpleName.Should().Be("GetItemResponse");
        endpoint.RequiresAuth.Should().BeFalse();
        endpoint.ProducesContentType.Should().Be("application/xml");
        endpoint.SourceFile.Should().Be("MyRouter.cs");
        endpoint.LineNumber.Should().Be(42);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public void EndpointInfo_SupportsAllHttpMethods(string method)
    {
        // Arrange & Act
        var endpoint = new EndpointInfo { Method = method };

        // Assert
        endpoint.Method.Should().Be(method);
    }

    [Theory]
    [InlineData("/v1/items")]
    [InlineData("/v1/items/{id}")]
    [InlineData("/v1/users/{userId}/items/{itemId}")]
    [InlineData("/api/health")]
    public void EndpointInfo_SupportsVariousPathFormats(string path)
    {
        // Arrange & Act
        var endpoint = new EndpointInfo { Path = path };

        // Assert
        endpoint.Path.Should().Be(path);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/xml")]
    [InlineData("text/plain")]
    [InlineData("application/octet-stream")]
    public void EndpointInfo_SupportsVariousContentTypes(string contentType)
    {
        // Arrange & Act
        var endpoint = new EndpointInfo { ProducesContentType = contentType };

        // Assert
        endpoint.ProducesContentType.Should().Be(contentType);
    }
}
