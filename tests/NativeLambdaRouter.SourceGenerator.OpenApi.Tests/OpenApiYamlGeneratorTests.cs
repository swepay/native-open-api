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

    [Fact]
    public void Generate_WithOperationName_UsesCustomOperationId()
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
                OperationName = "ListAllItems"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("operationId: ListAllItems");
    }

    [Fact]
    public void Generate_WithSummary_UsesCustomSummary()
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
                Summary = "Retrieve all items from inventory"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("summary: \"Retrieve all items from inventory\"");
    }

    [Fact]
    public void Generate_WithDescription_IncludesDescription()
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
                Description = "Returns a paginated list of items"
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("description: \"Returns a paginated list of items\"");
    }

    [Fact]
    public void Generate_WithCustomTags_UsesThoseTags()
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
                Tags = new List<string> { "Inventory", "Products" }
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert — tags are now emitted quoted (MELHORIA-5 fix)
        yaml.Should().Contain("- \"Inventory\"");
        yaml.Should().Contain("- \"Products\"");
    }

    [Fact]
    public void Generate_WithAdditionalProduces_EmitsExtraResponses()
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
                ResponseSimpleName = "GetItemResponse",
                AdditionalProduces = new List<ProducesInfo>
                {
                    new() { StatusCode = 404, ResponseTypeSimpleName = "NotFoundError", ContentType = "application/json" }
                }
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("\"404\":");
        yaml.Should().Contain("NotFoundError");
    }

    [Fact]
    public void Generate_WithProducesProblem_EmitsProblemJsonResponse()
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
                ResponseSimpleName = "GetItemResponse",
                AdditionalProduces = new List<ProducesInfo>
                {
                    new() { StatusCode = 422, ContentType = "application/problem+json" }
                }
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("\"422\":");
        yaml.Should().Contain("application/problem+json");
        yaml.Should().Contain("Unprocessable Entity");
    }

    [Fact]
    public void Generate_ProducesProblemOverridesDefaultError()
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
                ResponseSimpleName = "GetItemResponse",
                AdditionalProduces = new List<ProducesInfo>
                {
                    new() { StatusCode = 400, ContentType = "application/problem+json" }
                }
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert — The default $ref for 400 should not appear since ProducesProblem covers it
        yaml.Should().Contain("\"400\":");
        yaml.Should().Contain("application/problem+json");
        yaml.Should().NotContain("$ref: \"#/components/responses/BadRequest\"");
    }

    [Fact]
    public void Generate_WithoutDescription_OmitsDescriptionField()
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
                // Description is null by default
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert — No description field on the operation level.
        // When endpoint.Description is null the generator must NOT emit "description:" between
        // "summary:" and "tags:" in the operation block. We verify this by checking that the
        // string "summary:" is never immediately followed (within 200 chars) by "description:".
        // This is simpler than line-counting since SwepayProblemDetails and components/responses
        // also emit "description:" lines which are unrelated to operation-level metadata.
        var summaryIndex = yaml.IndexOf("summary:", StringComparison.Ordinal);
        summaryIndex.Should().BeGreaterThan(-1, "the generated YAML must contain a summary field");
        var afterSummary = yaml.Substring(summaryIndex, System.Math.Min(200, yaml.Length - summaryIndex));
        afterSummary.Should().NotContain("\n      description:", "endpoint.Description is null so no operation description should follow summary");
    }

    // ── Accepts / Form-encoded ──────────────────────────────────────

    [Fact]
    public void Generate_WithFormUrlEncodedAccepts_UsesFormContentType()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "POST",
                Path = "/v1/token",
                CommandTypeName = "TestApp.TokenCommand",
                ResponseTypeName = "TestApp.TokenResponse",
                CommandSimpleName = "TokenCommand",
                ResponseSimpleName = "TokenResponse",
                AcceptsContentType = "application/x-www-form-urlencoded",
                RequiresAuth = false,
                CommandPropertiesResolved = true,
                CommandProperties = new List<SchemaPropertyInfo>
                {
                    new() { Name = "ClientId", JsonName = "clientId", OpenApiType = "string", IsRequired = true },
                    new() { Name = "GrantType", JsonName = "grantType", OpenApiType = "string", IsRequired = true },
                    new() { Name = "Scope", JsonName = "scope", OpenApiType = "string", IsRequired = false }
                }
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("application/x-www-form-urlencoded:");
        yaml.Should().Contain("clientId:");
        yaml.Should().Contain("grantType:");
        yaml.Should().Contain("scope:");
        yaml.Should().Contain("type: object");
        // Required fields
        yaml.Should().Contain("- clientId");
        yaml.Should().Contain("- grantType");
        // scope is not required
        yaml.Should().NotContain("- scope");
        // No $ref for the command in requestBody
        yaml.Should().NotContain("$ref: \"#/components/schemas/TokenCommand\"");
    }

    [Fact]
    public void Generate_WithFormUrlEncodedUnresolved_EmitsDescriptionFallback()
    {
        // Arrange
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "POST",
                Path = "/v1/token",
                CommandTypeName = "TestApp.TokenCommand",
                ResponseTypeName = "TestApp.TokenResponse",
                CommandSimpleName = "TokenCommand",
                ResponseSimpleName = "TokenResponse",
                AcceptsContentType = "application/x-www-form-urlencoded",
                RequiresAuth = false,
                CommandPropertiesResolved = false
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert — fallback when properties can't be resolved
        yaml.Should().Contain("application/x-www-form-urlencoded:");
        yaml.Should().Contain("type: object");
        yaml.Should().Contain("TokenCommand form fields");
    }

    [Fact]
    public void Generate_WithoutAccepts_DefaultsToJsonRef()
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

        // Assert — default JSON with $ref
        yaml.Should().Contain("application/json:");
        yaml.Should().Contain("$ref: \"#/components/schemas/CreateItemCommand\"");
    }

    // ── AdditionalProduces grouping (swepay/native-open-api#58) ──────

    [Fact]
    public void Generate_AdditionalProducesWithSameStatusTwoContentTypes_MergesIntoOneResponseObject()
    {
        // Arrange — two ProducesInfo entries for the same status code (one typed, one
        // ProducesProblem) must render as ONE "401": response with two `content` entries,
        // not two separate (invalid) "401": mapping keys.
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/userinfo",
                CommandTypeName = "TestApp.UserInfoCommand",
                ResponseTypeName = "TestApp.UserInfoResponse",
                CommandSimpleName = "UserInfoCommand",
                ResponseSimpleName = "UserInfoResponse",
                AdditionalProduces =
                [
                    new ProducesInfo { StatusCode = 401, ResponseTypeSimpleName = "OAuthErrorResponse", ContentType = "application/json" },
                    new ProducesInfo { StatusCode = 401, ResponseTypeSimpleName = "SwepayProblemDetails", ContentType = "application/problem+json" }
                ]
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        CountOccurrences(yaml, "\"401\":").Should().Be(1);
        yaml.Should().Contain("application/json:");
        yaml.Should().Contain("application/problem+json:");
        yaml.Should().Contain("$ref: \"#/components/schemas/OAuthErrorResponse\"");
        yaml.Should().Contain("$ref: \"#/components/schemas/SwepayProblemDetails\"");
    }

    [Fact]
    public void Generate_AdditionalProducesWithSameStatusSameContentTypeTwice_DoesNotDuplicateContentEntry()
    {
        // Arrange — a status/content-type pair declared twice (e.g. a duplicate
        // .ProducesProblem(401) call) must not render the same media type key twice under
        // the same `content:` map.
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items/{id}",
                CommandTypeName = "TestApp.GetItemCommand",
                ResponseTypeName = "TestApp.GetItemResponse",
                CommandSimpleName = "GetItemCommand",
                ResponseSimpleName = "GetItemResponse",
                AdditionalProduces =
                [
                    new ProducesInfo { StatusCode = 401, ResponseTypeSimpleName = "SwepayProblemDetails", ContentType = "application/problem+json" },
                    new ProducesInfo { StatusCode = 401, ResponseTypeSimpleName = "SwepayProblemDetails", ContentType = "application/problem+json" }
                ]
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");
        var responseBlock = ExtractOperationResponseBlock(yaml, 401);

        // Assert
        CountOccurrences(yaml, "\"401\":").Should().Be(1);
        CountOccurrences(responseBlock, "application/problem+json:").Should().Be(1);
    }

    [Fact]
    public void Generate_ProducesProblem_NonProblemContentType_StillEmitsSwepayProblemDetailsContent()
    {
        // Arrange — regression for swepay/native-open-api#58 item 2: a ProducesInfo entry
        // from .ProducesProblem(status, contentType) must always carry
        // ResponseTypeSimpleName = "SwepayProblemDetails", regardless of the content-type
        // label, and must therefore still get a `content` block.
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items/{id}",
                CommandTypeName = "TestApp.GetItemCommand",
                ResponseTypeName = "TestApp.GetItemResponse",
                CommandSimpleName = "GetItemCommand",
                ResponseSimpleName = "GetItemResponse",
                AdditionalProduces =
                [
                    new ProducesInfo { StatusCode = 400, ResponseTypeSimpleName = "SwepayProblemDetails", ContentType = "application/json" }
                ]
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        yaml.Should().Contain("\"400\":");
        yaml.Should().Contain("content:");
        yaml.Should().Contain("$ref: \"#/components/schemas/SwepayProblemDetails\"");
    }

    [Fact]
    public void Generate_AdditionalProducesWithNoBody_EmitsNoContentMarkerAndNoContentBlock()
    {
        // Arrange — .Produces(statusCode) (no type argument): explicit "no body" response.
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/userinfo",
                CommandTypeName = "TestApp.UserInfoCommand",
                ResponseTypeName = "TestApp.UserInfoResponse",
                CommandSimpleName = "UserInfoCommand",
                ResponseSimpleName = "UserInfoResponse",
                AdditionalProduces =
                [
                    new ProducesInfo { StatusCode = 401, ResponseTypeSimpleName = null, NoBody = true }
                ],
                ResponseHeaders =
                [
                    new ResponseHeaderInfo { StatusCode = 401, Name = "WWW-Authenticate", SchemaType = "string" }
                ]
            }
        };

        // Act
        var yaml = OpenApiYamlGenerator.Generate(endpoints, "TestApi", "1.0.0");

        // Assert
        var responseBlock = ExtractOperationResponseBlock(yaml, 401);
        responseBlock.Should().Contain("x-no-content: true");
        responseBlock.Should().Contain("WWW-Authenticate");
        // Checks for the `content:` key specifically (not merely the "content:" substring,
        // which "x-no-content: true" itself also contains).
        responseBlock.Should().NotContain("          content:");
    }

    /// <summary>
    /// Extracts the single <c>responses.{statusCode}</c> block text for the (only) operation
    /// in the generated YAML, stopping right before the next status-code key at the same
    /// indentation (8 spaces) — or at the end of the string if this is the last one. Response
    /// status keys are the only 8-space-indented quoted keys in the document, so this never
    /// runs into <c>components/responses</c> (4-space indented) or any other section.
    /// </summary>
    private static string ExtractOperationResponseBlock(string yaml, int statusCode)
    {
        const string nextKeyMarker = "\n        \"";
        var marker = $"        \"{statusCode}\":";
        var start = yaml.IndexOf(marker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"the generated YAML must contain a \"{statusCode}\": response key");

        var nextKeyIndex = yaml.IndexOf(nextKeyMarker, start + marker.Length, StringComparison.Ordinal);
        var end = nextKeyIndex >= 0 ? nextKeyIndex : yaml.Length;
        return yaml.Substring(start, end - start);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
