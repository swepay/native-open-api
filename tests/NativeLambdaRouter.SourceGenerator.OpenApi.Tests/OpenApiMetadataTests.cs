namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Tests for OpenAPI metadata support: fluent chain methods (.WithName, .WithDescription,
/// .WithSummary, .WithTags, .Produces&lt;T&gt;, .ProducesProblem) and attribute-based metadata
/// ([EndpointName], [EndpointSummary], [EndpointDescription], [Tags]).
/// </summary>
public sealed class OpenApiMetadataTests
{
    // ── WithName ────────────────────────────────────────────────────

    [Fact]
    public void Generator_WithName_UsesCustomOperationId()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand())
            .WithName(""ListAllClients"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId: ListAllClients");
    }

    // ── WithSummary ─────────────────────────────────────────────────

    [Fact]
    public void Generator_WithSummary_UsesCustomSummary()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetUsersCommand { }
public class GetUsersResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetUsersCommand, GetUsersResponse>(""/v1/users"", ctx => new GetUsersCommand())
            .WithSummary(""Retrieve all users"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("summary: \"\"Retrieve all users\"\"");
    }

    // ── WithDescription ─────────────────────────────────────────────

    [Fact]
    public void Generator_WithDescription_IncludesDescription()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetUsersCommand { }
public class GetUsersResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetUsersCommand, GetUsersResponse>(""/v1/users"", ctx => new GetUsersCommand())
            .WithDescription(""This endpoint returns all registered users"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("description: \"\"This endpoint returns all registered users\"\"");
    }

    // ── WithTags ────────────────────────────────────────────────────

    [Fact]
    public void Generator_WithTags_UsesCustomTags()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand())
            .WithTags(""Clients"", ""Admin"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("- Clients");
        generatedSource.Should().Contain("- Admin");
    }

    [Fact]
    public void Generator_WithSingleTag_UsesThatTag()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemsCommand { }
public class GetItemsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemsCommand, GetItemsResponse>(""/v1/items"", ctx => new GetItemsCommand())
            .WithTags(""Inventory"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("- Inventory");
        // The auto-generated tag (V1 or Items) should NOT appear
        generatedSource.Should().NotContain("- V1");
    }

    // ── ProducesProblem ─────────────────────────────────────────────

    [Fact]
    public void Generator_ProducesProblem_EmitsProblemJsonResponse()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemCommand { }
public class GetItemResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemCommand, GetItemResponse>(""/v1/items/{id}"", ctx => new GetItemCommand())
            .ProducesProblem(404);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("\"\"404\"\":");
        generatedSource.Should().Contain("application/problem+json");
        generatedSource.Should().Contain("Not Found");
    }

    // ── Produces<T>(statusCode) ─────────────────────────────────────

    [Fact]
    public void Generator_GenericProduces_EmitsTypedResponse()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemCommand { }
public class GetItemResponse { }
public class NotFoundError { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemCommand, GetItemResponse>(""/v1/items/{id}"", ctx => new GetItemCommand())
            .Produces<NotFoundError>(404);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("\"\"404\"\":");
        generatedSource.Should().Contain("NotFoundError");
    }

    // ── Multiple Produces ───────────────────────────────────────────

    [Fact]
    public void Generator_MultipleProduces_EmitsAllResponses()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemCommand { }
public class GetItemResponse { }
public class NotFoundError { }
public class ValidationError { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemCommand, GetItemResponse>(""/v1/items/{id}"", ctx => new GetItemCommand())
            .Produces<NotFoundError>(404)
            .ProducesProblem(422);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("\"\"404\"\":");
        generatedSource.Should().Contain("NotFoundError");
        generatedSource.Should().Contain("\"\"422\"\":");
        generatedSource.Should().Contain("application/problem+json");
    }

    // ── Full chaining ───────────────────────────────────────────────

    [Fact]
    public void Generator_FullFluentChain_AllMetadataApplied()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class ListClientsCommand { }
public class ListClientsResponse { }
public class ResponseError { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(
            ""/v1/clients"", ctx => new ListClientsCommand())
            .WithName(""GetAllClients"")
            .WithDescription(""This endpoint gets all clients"")
            .WithTags(""Clients"")
            .Produces<ResponseError>(404)
            .ProducesProblem(500);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId: GetAllClients");
        generatedSource.Should().Contain("description: \"\"This endpoint gets all clients\"\"");
        generatedSource.Should().Contain("- Clients");
        generatedSource.Should().Contain("\"\"404\"\":");
        generatedSource.Should().Contain("ResponseError");
        // 500 is covered by ProducesProblem, so default $ref should not appear
        generatedSource.Should().Contain("application/problem+json");
    }

    // ── Attribute-based metadata ────────────────────────────────────

    [Fact]
    public void Generator_EndpointNameAttribute_UsesAttributeOperationId()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[EndpointName(""ListAllClients"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId: ListAllClients");
    }

    [Fact]
    public void Generator_TagsAttribute_UsesAttributeTags()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[Tags(""Clients"", ""Management"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("- Clients");
        generatedSource.Should().Contain("- Management");
    }

    [Fact]
    public void Generator_EndpointDescriptionAttribute_IncludesDescription()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[EndpointDescription(""Returns a paginated list of clients"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("description: \"\"Returns a paginated list of clients\"\"");
    }

    [Fact]
    public void Generator_EndpointSummaryAttribute_UsesSummary()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[EndpointSummary(""Get clients"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("summary: \"\"Get clients\"\"");
    }

    // ── Fluent chain takes precedence over attributes ───────────────

    [Fact]
    public void Generator_FluentChainOverridesAttributes()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[EndpointName(""FromAttribute"")]
[Tags(""AttributeTag"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand())
            .WithName(""FromFluentChain"")
            .WithTags(""FluentTag"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — Fluent chain values should win over attributes
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId: FromFluentChain");
        generatedSource.Should().NotContain("FromAttribute");
        generatedSource.Should().Contain("- FluentTag");
        generatedSource.Should().NotContain("- AttributeTag");
    }

    // ── Multiple attributes combined ────────────────────────────────

    [Fact]
    public void Generator_MultipleAttributes_AllApplied()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[EndpointName(""ListClients"")]
[EndpointSummary(""List all clients"")]
[EndpointDescription(""Returns paginated clients for the realm"")]
[Tags(""Clients"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId: ListClients");
        generatedSource.Should().Contain("summary: \"\"List all clients\"\"");
        generatedSource.Should().Contain("description: \"\"Returns paginated clients for the realm\"\"");
        generatedSource.Should().Contain("- Clients");
    }

    // ── Without metadata, auto-generation still works ───────────────

    [Fact]
    public void Generator_WithoutMetadata_AutoGeneratesOperationIdAndSummary()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemsCommand { }
public class GetItemsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemsCommand, GetItemsResponse>(""/v1/items"", ctx => new GetItemsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — operationId and summary should be auto-generated
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId:");
        generatedSource.Should().Contain("summary:");
        // Auto-generated tag from path
        generatedSource.Should().Contain("tags:");
    }

    // ── ProducesProblem overrides default standard error ─────────────

    [Fact]
    public void Generator_ProducesProblem_OverridesDefaultErrorResponse()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemCommand { }
public class GetItemResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemCommand, GetItemResponse>(""/v1/items/{id}"", ctx => new GetItemCommand())
            .ProducesProblem(400);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — ProducesProblem(400) should replace the default $ref for 400
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("\"\"400\"\":");
        generatedSource.Should().Contain("application/problem+json");
        // The default $ref: "#/components/responses/BadRequest" should NOT appear
        generatedSource.Should().NotContain("$ref: \"\"#/components/responses/BadRequest\"\"");
    }
}
