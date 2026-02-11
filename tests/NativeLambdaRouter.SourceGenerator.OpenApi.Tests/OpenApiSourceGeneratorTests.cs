namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

public sealed class OpenApiSourceGeneratorTests
{
    [Fact]
    public void Generator_WithNoEndpoints_EmitsWarningDiagnostic()
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

        // Assert - Should emit NLOAPI002 warning when no endpoints found
        result.Diagnostics.Should().ContainSingle(d => d.Id == "NLOAPI002");
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
    public void Generator_UsesAssemblyNameAsNamespace()
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
        var result = GeneratorTestHelper.RunGenerator(sourceCode, "NativeGuardBackend.Functions.Admin");
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert - Should use assembly name as namespace
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("namespace NativeGuardBackend.Functions.Admin.Generated;");
        generatedSource.Should().NotContain("namespace Native.OpenApi.Generated;");
    }

    [Fact]
    public void Generator_DifferentAssemblies_ProduceDifferentNamespaces()
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
        var resultAdmin = GeneratorTestHelper.RunGenerator(sourceCode, "Functions.Admin");
        var resultIdentity = GeneratorTestHelper.RunGenerator(sourceCode, "Functions.Identity");

        var adminSource = GeneratorTestHelper.GetGeneratedSource(resultAdmin, "GeneratedOpenApiSpec.g.cs");
        var identitySource = GeneratorTestHelper.GetGeneratedSource(resultIdentity, "GeneratedOpenApiSpec.g.cs");

        // Assert - Each assembly should get its own namespace
        adminSource.Should().Contain("namespace Functions.Admin.Generated;");
        identitySource.Should().Contain("namespace Functions.Identity.Generated;");
    }

    [Fact]
    public void Generator_WithoutNativeOpenApiRef_DoesNotImplementInterface()
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

        // Act - No Native.OpenApi reference
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert - Should not implement IGeneratedOpenApiSpec
        generatedSource.Should().NotBeNull();
        generatedSource.Should().NotContain("IGeneratedOpenApiSpec");
        generatedSource.Should().Contain("public sealed class GeneratedOpenApiSpec");
    }

    [Fact]
    public void Generator_WithNativeOpenApiRef_ImplementsInterface()
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

        // Act - Add Native.OpenApi reference
        var nativeOpenApiRef = Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(
            typeof(Native.OpenApi.IGeneratedOpenApiSpec).Assembly.Location);
        var result = GeneratorTestHelper.RunGenerator(sourceCode, "TestAssembly", nativeOpenApiRef);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert - Should implement IGeneratedOpenApiSpec
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("Native.OpenApi.IGeneratedOpenApiSpec");
        generatedSource.Should().Contain("public sealed class GeneratedOpenApiSpec : Native.OpenApi.IGeneratedOpenApiSpec");
    }

    [Fact]
    public void Generator_GeneratesInstanceSingleton()
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

        // Assert - Should have singleton instance
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("public static readonly GeneratedOpenApiSpec Instance = new();");
    }

    [Fact]
    public void Generator_GeneratesYamlContentConstant()
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

        // Assert - Should have YamlContent constant and EndpointList
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("public const string YamlContent = @\"");
        generatedSource.Should().Contain("public static readonly (string Method, string Path)[] EndpointList");
        generatedSource.Should().Contain("openapi: \"\"3.1.0\"\"");
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
