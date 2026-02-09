namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

public sealed class OpenApiSourceGeneratorTests
{
    [Fact]
    public void Generator_WithNoEndpoints_GeneratesEmptySpec()
    {
        // Arrange
        var sourceCode = @"
namespace TestApp;

public class MyService
{
    public void DoSomething() { }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        result.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void Generator_WithMapGetEndpoint_GeneratesSpec()
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

        // Assert - Generator should run without errors
        result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Fact]
    public void Generator_WithMultipleEndpoints_GeneratesSpec()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemsCommand { }
public class GetItemsResponse { }
public class CreateItemCommand { }
public class CreateItemResponse { }
public class GetItemByIdCommand { }
public class GetItemByIdResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemsCommand, GetItemsResponse>(""/v1/items"", ctx => new GetItemsCommand());
        routes.MapPost<CreateItemCommand, CreateItemResponse>(""/v1/items"", ctx => new CreateItemCommand());
        routes.MapGet<GetItemByIdCommand, GetItemByIdResponse>(""/v1/items/{id}"", ctx => new GetItemByIdCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);

        // Assert - Generator should run without errors
        result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Fact]
    public void Generator_WithAllHttpMethods_GeneratesSpec()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class Command1 { }
public class Response1 { }
public class Command2 { }
public class Response2 { }
public class Command3 { }
public class Response3 { }
public class Command4 { }
public class Response4 { }
public class Command5 { }
public class Response5 { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<Command1, Response1>(""/v1/resources"", ctx => new Command1());
        routes.MapPost<Command2, Response2>(""/v1/resources"", ctx => new Command2());
        routes.MapPut<Command3, Response3>(""/v1/resources/{id}"", ctx => new Command3());
        routes.MapDelete<Command4, Response4>(""/v1/resources/{id}"", ctx => new Command4());
        routes.MapPatch<Command5, Response5>(""/v1/resources/{id}"", ctx => new Command5());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);

        // Assert - Generator should run without errors
        result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    [Fact]
    public void Generator_WithPathParameters_GeneratesSpec()
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
        routes.MapGet<GetItemCommand, GetItemResponse>(""/v1/users/{userId}/items/{itemId}"", ctx => new GetItemCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);

        // Assert - Generator should run without errors
        result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }
}
