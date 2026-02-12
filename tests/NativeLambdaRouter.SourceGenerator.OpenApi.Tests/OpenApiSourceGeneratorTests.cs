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

    [Fact]
    public void Generator_WithOpenApiSpecName_UsesCustomNamespace()
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
        var globalOptions = new Dictionary<string, string>
        {
            ["build_property.OpenApiSpecName"] = "NativeGuardBackend.Functions.Admin"
        };

        // Act — AssemblyName is "bootstrap" but OpenApiSpecName overrides it
        var result = GeneratorTestHelper.RunGenerator(sourceCode, "bootstrap", globalOptions);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("namespace NativeGuardBackend.Functions.Admin.Generated;");
        generatedSource.Should().NotContain("namespace bootstrap.Generated;");
    }

    [Fact]
    public void Generator_WithOpenApiSpecTitle_UsesCustomTitle()
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
        var globalOptions = new Dictionary<string, string>
        {
            ["build_property.OpenApiSpecName"] = "NativeGuardBackend.Functions.Admin",
            ["build_property.OpenApiSpecTitle"] = "Admin API"
        };

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode, "bootstrap", globalOptions);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — the YAML title should use the custom value
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("title: \"\"Admin API\"\"");
        generatedSource.Should().NotContain("title: \"\"bootstrap\"\"");
    }

    [Fact]
    public void Generator_WithoutOpenApiSpecName_FallsBackToAssemblyName()
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

        // Act — No global options, should use assembly name
        var result = GeneratorTestHelper.RunGenerator(sourceCode, "MyService.Api");
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("namespace MyService.Api.Generated;");
    }

    [Fact]
    public void Generator_WithEmptyOpenApiSpecName_FallsBackToAssemblyName()
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
        var globalOptions = new Dictionary<string, string>
        {
            ["build_property.OpenApiSpecName"] = ""
        };

        // Act — Empty spec name should fallback
        var result = GeneratorTestHelper.RunGenerator(sourceCode, "FallbackAssembly", globalOptions);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("namespace FallbackAssembly.Generated;");
    }

    [Fact]
    public void Generator_WithOpenApiSpecNameOnly_TitleDerivedFromSpecName()
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
        var globalOptions = new Dictionary<string, string>
        {
            ["build_property.OpenApiSpecName"] = "NativeGuard.Functions.Identity"
        };

        // Act — Only OpenApiSpecName set, title should be derived by replacing dots with spaces
        var result = GeneratorTestHelper.RunGenerator(sourceCode, "bootstrap", globalOptions);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("title: \"\"NativeGuard Functions Identity\"\"");
    }

    [Fact]
    public void Generator_MultipleBootstrapProjects_WithDifferentSpecNames_ProduceDifferentNamespaces()
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

        // Act — Both assemblies are named "bootstrap" but have different OpenApiSpecName
        var adminResult = GeneratorTestHelper.RunGenerator(
            sourceCode, "bootstrap",
            new Dictionary<string, string> { ["build_property.OpenApiSpecName"] = "Functions.Admin" });
        var identityResult = GeneratorTestHelper.RunGenerator(
            sourceCode, "bootstrap",
            new Dictionary<string, string> { ["build_property.OpenApiSpecName"] = "Functions.Identity" });

        var adminSource = GeneratorTestHelper.GetGeneratedSource(adminResult, "GeneratedOpenApiSpec.g.cs");
        var identitySource = GeneratorTestHelper.GetGeneratedSource(identityResult, "GeneratedOpenApiSpec.g.cs");

        // Assert — Even though both are "bootstrap", they produce different namespaces
        adminSource.Should().Contain("namespace Functions.Admin.Generated;");
        identitySource.Should().Contain("namespace Functions.Identity.Generated;");
    }

    [Fact]
    public void Generator_WithAllowAnonymous_OmitsSecurityBlock()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetPublicCommand { }
public class GetPublicResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetPublicCommand, GetPublicResponse>(""/v1/public"", ctx => new GetPublicCommand())
            .AllowAnonymous();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — The YAML should NOT contain a security block for this endpoint
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("/v1/public");
        generatedSource.Should().NotContain("security:");
        generatedSource.Should().NotContain("JwtBearer");
    }

    [Fact]
    public void Generator_WithProduces_UsesCustomContentType()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetDocsCommand { }
public class GetDocsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetDocsCommand, GetDocsResponse>(""/v1/docs"", ctx => new GetDocsCommand())
            .Produces(""text/html"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — The YAML should use text/html instead of application/json
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("text/html");
        generatedSource.Should().NotContain("application/json");
    }

    [Fact]
    public void Generator_WithAllowAnonymousAndProduces_AppliesBoth()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetOpenApiCommand { }
public class GetOpenApiResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetOpenApiCommand, GetOpenApiResponse>(""/v1/openapi"", ctx => new GetOpenApiCommand())
            .AllowAnonymous()
            .Produces(""text/html"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — No security block AND custom content type
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("/v1/openapi");
        generatedSource.Should().NotContain("security:");
        generatedSource.Should().NotContain("JwtBearer");
        generatedSource.Should().Contain("text/html");
        generatedSource.Should().NotContain("application/json");
    }

    [Fact]
    public void Generator_MixedEndpoints_AuthAndAnonymous_GeneratesCorrectSecurity()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemsCommand { }
public class GetItemsResponse { }
public class GetPublicCommand { }
public class GetPublicResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemsCommand, GetItemsResponse>(""/v1/items"", ctx => new GetItemsCommand());
        routes.MapGet<GetPublicCommand, GetPublicResponse>(""/v1/public"", ctx => new GetPublicCommand())
            .AllowAnonymous();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — /v1/items should have security, /v1/public should not
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("/v1/items");
        generatedSource.Should().Contain("/v1/public");
        // The authenticated endpoint still has security
        generatedSource.Should().Contain("security:");
        generatedSource.Should().Contain("JwtBearer");
    }

    [Fact]
    public void Generator_WithProducesReversedOrder_AppliesBoth()
    {
        // Arrange — Produces before AllowAnonymous in the chain
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetDocsCommand { }
public class GetDocsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetDocsCommand, GetDocsResponse>(""/v1/docs"", ctx => new GetDocsCommand())
            .Produces(""text/html"")
            .AllowAnonymous();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — Both should be applied regardless of order
        generatedSource.Should().NotBeNull();
        generatedSource.Should().NotContain("security:");
        generatedSource.Should().Contain("text/html");
    }
}
