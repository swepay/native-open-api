namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

public sealed class OpenApiYamlGeneratorTests
{
    [Fact]
    public void Generate_WithEmptyEndpoints_ReturnsMinimalSpec()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>();

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("openapi: \"3.1.0\"");
        yaml.Should().Contain("title: \"TestApi\"");
        yaml.Should().Contain("version: \"1.0.0\"");
        yaml.Should().Contain("paths:");
        yaml.Should().Contain("components:");
    }

    [Fact]
    public void Generate_WithSingleGetEndpoint_GeneratesCorrectYaml()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items",
                CommandTypeName = "TestApp.GetItemsCommand",
                ResponseTypeName = "TestApp.GetItemsResponse",
                CommandSimpleName = "GetItemsCommand",
                ResponseSimpleName = "GetItemsResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("/v1/items:");
        yaml.Should().Contain("get:");
        yaml.Should().Contain("operationId:");
        yaml.Should().Contain("GetItemsCommand");
        yaml.Should().Contain("GetItemsResponse");
    }

    [Fact]
    public void Generate_WithPostEndpoint_IncludesRequestBody()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "POST",
                Path = "/v1/items",
                CommandTypeName = "TestApp.CreateItemCommand",
                ResponseTypeName = "TestApp.CreateItemResponse",
                CommandSimpleName = "CreateItemCommand",
                ResponseSimpleName = "CreateItemResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("post:");
        yaml.Should().Contain("requestBody:");
        yaml.Should().Contain("required: true");
        yaml.Should().Contain("application/json:");
        yaml.Should().Contain("$ref: \"#/components/schemas/CreateItemCommand\"");
    }

    [Fact]
    public void Generate_WithPutEndpoint_IncludesRequestBody()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "PUT",
                Path = "/v1/items/{id}",
                CommandTypeName = "TestApp.UpdateItemCommand",
                ResponseTypeName = "TestApp.UpdateItemResponse",
                CommandSimpleName = "UpdateItemCommand",
                ResponseSimpleName = "UpdateItemResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("put:");
        yaml.Should().Contain("requestBody:");
        yaml.Should().Contain("$ref: \"#/components/schemas/UpdateItemCommand\"");
    }

    [Fact]
    public void Generate_WithPatchEndpoint_IncludesRequestBody()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "PATCH",
                Path = "/v1/items/{id}",
                CommandTypeName = "TestApp.PatchItemCommand",
                ResponseTypeName = "TestApp.PatchItemResponse",
                CommandSimpleName = "PatchItemCommand",
                ResponseSimpleName = "PatchItemResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("patch:");
        yaml.Should().Contain("requestBody:");
    }

    [Fact]
    public void Generate_WithDeleteEndpoint_DoesNotIncludeRequestBody()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "DELETE",
                Path = "/v1/items/{id}",
                CommandTypeName = "TestApp.DeleteItemCommand",
                ResponseTypeName = "TestApp.DeleteItemResponse",
                CommandSimpleName = "DeleteItemCommand",
                ResponseSimpleName = "DeleteItemResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("delete:");
        yaml.Should().NotContain("requestBody:");
    }

    [Fact]
    public void Generate_WithPathParameter_IncludesParameterDefinition()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items/{id}",
                CommandTypeName = "TestApp.GetItemCommand",
                ResponseTypeName = "TestApp.GetItemResponse",
                CommandSimpleName = "GetItemCommand",
                ResponseSimpleName = "GetItemResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("parameters:");
        yaml.Should().Contain("- name: id");
        yaml.Should().Contain("in: path");
        yaml.Should().Contain("required: true");
    }

    [Fact]
    public void Generate_WithMultiplePathParameters_IncludesAllParameters()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/users/{userId}/items/{itemId}",
                CommandTypeName = "TestApp.GetUserItemCommand",
                ResponseTypeName = "TestApp.GetUserItemResponse",
                CommandSimpleName = "GetUserItemCommand",
                ResponseSimpleName = "GetUserItemResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("- name: userId");
        yaml.Should().Contain("- name: itemId");
    }

    [Fact]
    public void Generate_WithRequiresAuth_IncludesSecurity()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items",
                CommandTypeName = "TestApp.GetItemsCommand",
                ResponseTypeName = "TestApp.GetItemsResponse",
                CommandSimpleName = "GetItemsCommand",
                ResponseSimpleName = "GetItemsResponse",
                RequiresAuth = true
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("security:");
        yaml.Should().Contain("- JwtBearer: []");
    }

    [Fact]
    public void Generate_WithoutRequiresAuth_DoesNotIncludeSecurity()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/public",
                CommandTypeName = "TestApp.GetPublicCommand",
                ResponseTypeName = "TestApp.GetPublicResponse",
                CommandSimpleName = "GetPublicCommand",
                ResponseSimpleName = "GetPublicResponse",
                RequiresAuth = false
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert – anonymous endpoints emit security: [] per OpenAPI 3.1
        yaml.Should().Contain("security: []");
        yaml.Should().NotContain("JwtBearer");
    }

    [Fact]
    public void Generate_IncludesStandardErrorResponses()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items",
                CommandTypeName = "TestApp.GetItemsCommand",
                ResponseTypeName = "TestApp.GetItemsResponse",
                CommandSimpleName = "GetItemsCommand",
                ResponseSimpleName = "GetItemsResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("\"400\":");
        yaml.Should().Contain("$ref: \"#/components/responses/BadRequest\"");
        yaml.Should().Contain("\"401\":");
        yaml.Should().Contain("$ref: \"#/components/responses/Unauthorized\"");
        yaml.Should().Contain("\"500\":");
        yaml.Should().Contain("$ref: \"#/components/responses/InternalServerError\"");
    }

    [Fact]
    public void Generate_WithMultipleEndpointsOnSamePath_GroupsThem()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items",
                CommandTypeName = "TestApp.GetItemsCommand",
                ResponseTypeName = "TestApp.GetItemsResponse",
                CommandSimpleName = "GetItemsCommand",
                ResponseSimpleName = "GetItemsResponse"
            },
            new()
            {
                Method = "POST",
                Path = "/v1/items",
                CommandTypeName = "TestApp.CreateItemCommand",
                ResponseTypeName = "TestApp.CreateItemResponse",
                CommandSimpleName = "CreateItemCommand",
                ResponseSimpleName = "CreateItemResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        // Path should appear only once
        var pathCount = yaml.Split("/v1/items:").Length - 1;
        pathCount.Should().Be(1);

        yaml.Should().Contain("get:");
        yaml.Should().Contain("post:");
    }

    [Fact]
    public void Generate_IncludesSchemaDefinitions()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items",
                CommandTypeName = "TestApp.GetItemsCommand",
                ResponseTypeName = "TestApp.GetItemsResponse",
                CommandSimpleName = "GetItemsCommand",
                ResponseSimpleName = "GetItemsResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("components:");
        yaml.Should().Contain("schemas:");
        yaml.Should().Contain("GetItemsCommand:");
        yaml.Should().Contain("GetItemsResponse:");
        yaml.Should().Contain("type: object");
    }

    [Fact]
    public void Generate_WithCustomContentType_UsesCustomContentType()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/export",
                CommandTypeName = "TestApp.ExportCommand",
                ResponseTypeName = "TestApp.ExportResponse",
                CommandSimpleName = "ExportCommand",
                ResponseSimpleName = "ExportResponse",
                ProducesContentType = "application/xml"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("application/xml:");
    }

    [Fact]
    public void Generate_WithDefaultContentType_UsesApplicationJson()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items",
                CommandTypeName = "TestApp.GetItemsCommand",
                ResponseTypeName = "TestApp.GetItemsResponse",
                CommandSimpleName = "GetItemsCommand",
                ResponseSimpleName = "GetItemsResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("application/json:");
    }

    [Fact]
    public void Generate_SortsPathsAlphabetically()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/zebras",
                CommandTypeName = "TestApp.GetZebrasCommand",
                ResponseTypeName = "TestApp.GetZebrasResponse",
                CommandSimpleName = "GetZebrasCommand",
                ResponseSimpleName = "GetZebrasResponse"
            },
            new()
            {
                Method = "GET",
                Path = "/v1/apples",
                CommandTypeName = "TestApp.GetApplesCommand",
                ResponseTypeName = "TestApp.GetApplesResponse",
                CommandSimpleName = "GetApplesCommand",
                ResponseSimpleName = "GetApplesResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        var applesIndex = yaml.IndexOf("/v1/apples:");
        var zebrasIndex = yaml.IndexOf("/v1/zebras:");
        applesIndex.Should().BeLessThan(zebrasIndex);
    }

    [Fact]
    public void Generate_IncludesTags()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items",
                CommandTypeName = "TestApp.GetItemsCommand",
                ResponseTypeName = "TestApp.GetItemsResponse",
                CommandSimpleName = "GetItemsCommand",
                ResponseSimpleName = "GetItemsResponse"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("tags:");
    }
}
