namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Tests for Operation Richness Wave 2 features:
/// - x-codeSamples (per operation, multi-language, deterministic order)
/// - x-badges (per operation, multi-badge, deterministic order)
/// - x-scalar-stability (Scalar-first, stable / experimental / deprecated)
/// </summary>
public sealed class OperationRichnessTests
{
    // ── helpers ───────────────────────────────────────────────────────

    private static EndpointInfo MakeEndpoint(string method = "GET", string path = "/v1/items")
        => new()
        {
            Method = method,
            Path = path,
            CommandTypeName = "App.Command",
            ResponseTypeName = "App.Response",
            CommandSimpleName = "Command",
            ResponseSimpleName = "Response"
        };

    private static string Generate(EndpointInfo endpoint)
        => OpenApiYamlGenerator.Generate(
            new List<EndpointInfo> { endpoint },
            "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

    private static int CountOccurrences(string source, string pattern)
    {
        var count = 0;
        var idx = 0;
        while ((idx = source.IndexOf(pattern, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }

    // ── x-codeSamples ────────────────────────────────────────────────

    [Fact]
    public void Generate_WithNoCodeSamples_DoesNotEmitXCodeSamples()
    {
        var endpoint = MakeEndpoint();
        // CodeSamples is empty by default

        var yaml = Generate(endpoint);

        yaml.Should().NotContain("x-codeSamples:");
    }

    [Fact]
    public void Generate_WithSingleCodeSample_EmitsXCodeSamples()
    {
        var endpoint = MakeEndpoint();
        endpoint.CodeSamples.Add(new CodeSampleInfo
        {
            Lang = "curl",
            Source = "curl -X GET https://api.example.com/v1/items"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("      x-codeSamples:");
        yaml.Should().Contain("        - lang: curl");
        yaml.Should().Contain("          source: \"curl -X GET https://api.example.com/v1/items\"");
    }

    [Fact]
    public void Generate_WithCodeSampleLabel_EmitsLabel()
    {
        var endpoint = MakeEndpoint();
        endpoint.CodeSamples.Add(new CodeSampleInfo
        {
            Lang = "csharp",
            Label = "C# (HttpClient)",
            Source = "var resp = await http.GetAsync(\"/v1/items\");"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("          label: \"C# (HttpClient)\"");
    }

    [Fact]
    public void Generate_WithCodeSampleNoLabel_DoesNotEmitLabelLine()
    {
        var endpoint = MakeEndpoint();
        endpoint.CodeSamples.Add(new CodeSampleInfo
        {
            Lang = "curl",
            Source = "curl https://api.example.com/v1/items"
        });

        var yaml = Generate(endpoint);

        yaml.Should().NotContain("          label:");
    }

    [Fact]
    public void Generate_WithMultilineCodeSample_UsesBlockScalarStyle()
    {
        var endpoint = MakeEndpoint("POST", "/v1/items");
        endpoint.CodeSamples.Add(new CodeSampleInfo
        {
            Lang = "curl",
            Source = "curl -X POST https://api.example.com/v1/items \\\n  -H \"Content-Type: application/json\""
        });

        var yaml = Generate(endpoint);

        // Block-scalar indicator
        yaml.Should().Contain("          source: |");
        // Lines indented inside the block
        yaml.Should().Contain("            curl -X POST https://api.example.com/v1/items \\");
    }

    [Fact]
    public void Generate_CodeSamplesAreSortedByLangThenLabel()
    {
        var endpoint = MakeEndpoint();
        // Add in reverse order — generator must sort
        endpoint.CodeSamples.Add(new CodeSampleInfo { Lang = "python", Source = "import requests" });
        endpoint.CodeSamples.Add(new CodeSampleInfo { Lang = "curl", Source = "curl https://api.example.com" });
        endpoint.CodeSamples.Add(new CodeSampleInfo { Lang = "csharp", Label = "C# HttpClient", Source = "var r = await http.GetAsync(...);" });

        var yaml = Generate(endpoint);

        var curlIdx = yaml.IndexOf("lang: curl", StringComparison.Ordinal);
        var csharpIdx = yaml.IndexOf("lang: csharp", StringComparison.Ordinal);
        var pythonIdx = yaml.IndexOf("lang: python", StringComparison.Ordinal);

        curlIdx.Should().BeGreaterThan(0);
        csharpIdx.Should().BeGreaterThan(0);
        pythonIdx.Should().BeGreaterThan(0);

        csharpIdx.Should().BeLessThan(curlIdx, "csharp comes before curl alphabetically");
        curlIdx.Should().BeLessThan(pythonIdx, "curl comes before python alphabetically");
    }

    [Fact]
    public void Generate_TwoCodeSamplesForSameLang_SortedByLabel()
    {
        var endpoint = MakeEndpoint();
        endpoint.CodeSamples.Add(new CodeSampleInfo { Lang = "javascript", Label = "Node.js (axios)", Source = "axios.get('/v1/items')" });
        endpoint.CodeSamples.Add(new CodeSampleInfo { Lang = "javascript", Label = "Browser (fetch)", Source = "fetch('/v1/items')" });

        var yaml = Generate(endpoint);

        var browserIdx = yaml.IndexOf("Browser (fetch)", StringComparison.Ordinal);
        var nodeIdx = yaml.IndexOf("Node.js (axios)", StringComparison.Ordinal);

        browserIdx.Should().BeLessThan(nodeIdx, "Browser sorts before Node.js case-insensitively");
    }

    [Fact]
    public void Generate_CodeSampleLangWithSpecialChars_IsNotDoubleEscaped()
    {
        // lang values are simple identifiers; validate they survive the EscapeYamlString pass
        var endpoint = MakeEndpoint();
        endpoint.CodeSamples.Add(new CodeSampleInfo
        {
            Lang = "c++",
            Source = "#include <iostream>"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("lang: c++");
    }

    [Fact]
    public void Generate_CodeSampleSourceWithQuotes_IsEscaped()
    {
        var endpoint = MakeEndpoint();
        endpoint.CodeSamples.Add(new CodeSampleInfo
        {
            Lang = "curl",
            Source = "curl -H \"Authorization: Bearer token\""
        });

        var yaml = Generate(endpoint);

        // Quotes inside quoted scalar must be escaped
        yaml.Should().Contain("\\\"Authorization: Bearer token\\\"");
    }

    [Fact]
    public void Generate_CodeSamplesAppearsInsideOperationBlock()
    {
        var endpoint = MakeEndpoint();
        endpoint.CodeSamples.Add(new CodeSampleInfo { Lang = "curl", Source = "curl https://api.example.com/v1/items" });

        var yaml = Generate(endpoint);

        var operationIdIdx = yaml.IndexOf("      operationId:", StringComparison.Ordinal);
        var codeSamplesIdx = yaml.IndexOf("      x-codeSamples:", StringComparison.Ordinal);
        var responsesIdx = yaml.IndexOf("      responses:", StringComparison.Ordinal);

        operationIdIdx.Should().BeGreaterThan(0);
        codeSamplesIdx.Should().BeGreaterThan(0);
        responsesIdx.Should().BeGreaterThan(0);

        operationIdIdx.Should().BeLessThan(codeSamplesIdx);
        codeSamplesIdx.Should().BeLessThan(responsesIdx);
    }

    [Fact]
    public void Generate_MultipleEndpoints_CodeSamplesOnlyInCorrectOperation()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items",
                CommandTypeName = "App.GetCommand",
                ResponseTypeName = "App.GetResponse",
                CommandSimpleName = "GetCommand",
                ResponseSimpleName = "GetResponse"
                // No code samples
            },
            new()
            {
                Method = "POST",
                Path = "/v1/items",
                CommandTypeName = "App.CreateCommand",
                ResponseTypeName = "App.CreateResponse",
                CommandSimpleName = "CreateCommand",
                ResponseSimpleName = "CreateResponse",
                CodeSamples = new List<CodeSampleInfo>
                {
                    new() { Lang = "curl", Source = "curl -X POST https://api.example.com/v1/items" }
                }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        // x-codeSamples should appear exactly once (only for POST)
        CountOccurrences(yaml, "x-codeSamples:").Should().Be(1);
    }

    // ── x-badges ─────────────────────────────────────────────────────

    [Fact]
    public void Generate_WithNoBadges_DoesNotEmitXBadges()
    {
        var endpoint = MakeEndpoint();

        var yaml = Generate(endpoint);

        yaml.Should().NotContain("x-badges:");
    }

    [Fact]
    public void Generate_WithSingleBadge_EmitsXBadges()
    {
        var endpoint = MakeEndpoint();
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "beta" });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("      x-badges:");
        yaml.Should().Contain("        - name: \"beta\"");
    }

    [Fact]
    public void Generate_BadgeWithPosition_EmitsPosition()
    {
        var endpoint = MakeEndpoint();
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "beta", Position = "after" });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("          position: \"after\"");
    }

    [Fact]
    public void Generate_BadgeWithColor_EmitsColor()
    {
        var endpoint = MakeEndpoint();
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "beta", Color = "#e5a505" });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("          color: \"#e5a505\"");
    }

    [Fact]
    public void Generate_BadgeWithoutPositionOrColor_OmitsThoseLines()
    {
        var endpoint = MakeEndpoint();
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "internal" });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("        - name: \"internal\"");
        yaml.Should().NotContain("          position:");
        yaml.Should().NotContain("          color:");
    }

    [Fact]
    public void Generate_MultipleBadges_AreSortedByNameAlphabetically()
    {
        var endpoint = MakeEndpoint();
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "internal" });
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "beta" });

        var yaml = Generate(endpoint);

        var betaIdx = yaml.IndexOf("\"beta\"", StringComparison.Ordinal);
        var internalIdx = yaml.IndexOf("\"internal\"", StringComparison.Ordinal);

        betaIdx.Should().BeLessThan(internalIdx, "beta sorts before internal alphabetically");
    }

    [Fact]
    public void Generate_BadgeNameWithSpecialChars_IsEscaped()
    {
        var endpoint = MakeEndpoint();
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "\"preview\"" });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("\\\"preview\\\"");
    }

    [Fact]
    public void Generate_BadgesAppearsInsideOperationBlock()
    {
        var endpoint = MakeEndpoint();
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "beta" });

        var yaml = Generate(endpoint);

        var operationIdIdx = yaml.IndexOf("      operationId:", StringComparison.Ordinal);
        var badgesIdx = yaml.IndexOf("      x-badges:", StringComparison.Ordinal);
        var responsesIdx = yaml.IndexOf("      responses:", StringComparison.Ordinal);

        operationIdIdx.Should().BeGreaterThan(0);
        badgesIdx.Should().BeGreaterThan(0);
        responsesIdx.Should().BeGreaterThan(0);

        operationIdIdx.Should().BeLessThan(badgesIdx);
        badgesIdx.Should().BeLessThan(responsesIdx);
    }

    // ── x-scalar-stability ───────────────────────────────────────────

    [Fact]
    public void Generate_WithNoScalarStability_DoesNotEmitXScalarStability()
    {
        var endpoint = MakeEndpoint();
        // ScalarStability is null by default

        var yaml = Generate(endpoint);

        yaml.Should().NotContain("x-scalar-stability:");
    }

    [Fact]
    public void Generate_WithStableStability_EmitsStable()
    {
        var endpoint = MakeEndpoint();
        endpoint.ScalarStability = "stable";

        var yaml = Generate(endpoint);

        yaml.Should().Contain("      x-scalar-stability: stable");
    }

    [Fact]
    public void Generate_WithExperimentalStability_EmitsExperimental()
    {
        var endpoint = MakeEndpoint();
        endpoint.ScalarStability = "experimental";

        var yaml = Generate(endpoint);

        yaml.Should().Contain("      x-scalar-stability: experimental");
    }

    [Fact]
    public void Generate_WithDeprecatedStability_EmitsDeprecated()
    {
        var endpoint = MakeEndpoint();
        endpoint.ScalarStability = "deprecated";

        var yaml = Generate(endpoint);

        yaml.Should().Contain("      x-scalar-stability: deprecated");
    }

    [Fact]
    public void Generate_ScalarStabilityAppearsInsideOperationBlock()
    {
        var endpoint = MakeEndpoint();
        endpoint.ScalarStability = "experimental";

        var yaml = Generate(endpoint);

        var operationIdIdx = yaml.IndexOf("      operationId:", StringComparison.Ordinal);
        var stabilityIdx = yaml.IndexOf("      x-scalar-stability:", StringComparison.Ordinal);
        var responsesIdx = yaml.IndexOf("      responses:", StringComparison.Ordinal);

        operationIdIdx.Should().BeGreaterThan(0);
        stabilityIdx.Should().BeGreaterThan(0);
        responsesIdx.Should().BeGreaterThan(0);

        operationIdIdx.Should().BeLessThan(stabilityIdx);
        stabilityIdx.Should().BeLessThan(responsesIdx);
    }

    [Fact]
    public void Generate_ScalarStabilityDoesNotEmitRedocEquivalent()
    {
        var endpoint = MakeEndpoint();
        endpoint.ScalarStability = "experimental";

        var yaml = Generate(endpoint);

        // Scalar-first: no Redoc-style stability extension should be emitted
        yaml.Should().NotContain("x-redoc-stability:");
        yaml.Should().NotContain("x-stability:");
    }

    [Fact]
    public void Generate_MultipleEndpoints_StabilityOnlyOnCorrectOperation()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items",
                CommandTypeName = "App.GetCommand",
                ResponseTypeName = "App.GetResponse",
                CommandSimpleName = "GetCommand",
                ResponseSimpleName = "GetResponse",
                ScalarStability = "stable"
            },
            new()
            {
                Method = "POST",
                Path = "/v1/items",
                CommandTypeName = "App.CreateCommand",
                ResponseTypeName = "App.CreateResponse",
                CommandSimpleName = "CreateCommand",
                ResponseSimpleName = "CreateResponse",
                ScalarStability = "experimental"
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        yaml.Should().Contain("x-scalar-stability: stable");
        yaml.Should().Contain("x-scalar-stability: experimental");
    }

    // ── Combined scenario: all three features together ────────────────

    [Fact]
    public void Generate_AllThreeFeaturesOnSameOperation_ProducesCorrectOutput()
    {
        var endpoint = MakeEndpoint("POST", "/v1/orders");
        endpoint.CodeSamples.Add(new CodeSampleInfo
        {
            Lang = "curl",
            Source = "curl -X POST https://api.example.com/v1/orders"
        });
        endpoint.CodeSamples.Add(new CodeSampleInfo
        {
            Lang = "python",
            Source = "import requests; requests.post('/v1/orders')"
        });
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "beta", Position = "after", Color = "#e5a505" });
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "internal" });
        endpoint.ScalarStability = "experimental";

        var yaml = Generate(endpoint);

        // x-codeSamples
        yaml.Should().Contain("      x-codeSamples:");
        yaml.Should().Contain("lang: curl");
        yaml.Should().Contain("lang: python");

        // x-badges (beta sorts before internal)
        yaml.Should().Contain("      x-badges:");
        yaml.Should().Contain("\"beta\"");
        yaml.Should().Contain("\"internal\"");
        yaml.Should().Contain("\"#e5a505\"");

        // x-scalar-stability
        yaml.Should().Contain("      x-scalar-stability: experimental");

        // Ordering within operation: codeSamples, then badges, then stability, then error codes, then tags
        var codeSamplesIdx = yaml.IndexOf("      x-codeSamples:", StringComparison.Ordinal);
        var badgesIdx = yaml.IndexOf("      x-badges:", StringComparison.Ordinal);
        var stabilityIdx = yaml.IndexOf("      x-scalar-stability:", StringComparison.Ordinal);
        var tagsIdx = yaml.IndexOf("      tags:", StringComparison.Ordinal);

        codeSamplesIdx.Should().BeLessThan(badgesIdx);
        badgesIdx.Should().BeLessThan(stabilityIdx);
        stabilityIdx.Should().BeLessThan(tagsIdx);
    }

    [Fact]
    public void Generate_Determinism_SameInputProducesSameYamlAcrossMultipleCalls()
    {
        var endpoint = MakeEndpoint("POST", "/v1/items");
        endpoint.CodeSamples.Add(new CodeSampleInfo { Lang = "python", Source = "import requests" });
        endpoint.CodeSamples.Add(new CodeSampleInfo { Lang = "curl", Source = "curl https://api.example.com" });
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "internal" });
        endpoint.Badges.Add(new OperationBadgeInfo { Name = "beta" });
        endpoint.ScalarStability = "stable";

        var yaml1 = Generate(endpoint);
        var yaml2 = Generate(endpoint);

        yaml1.Should().Be(yaml2, "YAML output must be deterministic");
    }

    // ── Source generator end-to-end (attribute reading via Roslyn) ───

    [Fact]
    public void Generator_CodeSampleAttribute_IsReadAndEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            GeneratorTestHelper.CreateOperationRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

[CodeSample(lang: ""curl"", source: ""curl https://api.example.com/v1/items"")]
[CodeSample(lang: ""csharp"", source: ""var r = await http.GetAsync(\""/v1/items\"");"", Label = ""C# HttpClient"")]
public class GetItemsCommand { }
public class GetItemsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemsCommand, GetItemsResponse>(""/v1/items"", ctx => new GetItemsCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("x-codeSamples:");
        generatedSource.Should().Contain("lang: curl");
        generatedSource.Should().Contain("lang: csharp");
        generatedSource.Should().Contain("C# HttpClient");
    }

    [Fact]
    public void Generator_OperationBadgeAttribute_IsReadAndEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            GeneratorTestHelper.CreateOperationRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

[OperationBadge(name: ""beta"", Position = ""after"", Color = ""#e5a505"")]
public class CreateItemCommand { }
public class CreateItemResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateItemCommand, CreateItemResponse>(""/v1/items"", ctx => new CreateItemCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("x-badges:");
        generatedSource.Should().Contain("\"\"beta\"\"");
        generatedSource.Should().Contain("\"\"#e5a505\"\"");
    }

    [Fact]
    public void Generator_ScalarStabilityAttribute_IsReadAndEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            GeneratorTestHelper.CreateOperationRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

[ScalarStability(Stability.Experimental)]
public class CreateItemCommand { }
public class CreateItemResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateItemCommand, CreateItemResponse>(""/v1/items"", ctx => new CreateItemCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("x-scalar-stability: experimental");
    }

    [Fact]
    public void Generator_ScalarStabilityStable_EmitsStable()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            GeneratorTestHelper.CreateOperationRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

[ScalarStability(Stability.Stable)]
public class GetItemsCommand { }
public class GetItemsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemsCommand, GetItemsResponse>(""/v1/items"", ctx => new GetItemsCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("x-scalar-stability: stable");
    }

    [Fact]
    public void Generator_ScalarStabilityDeprecated_EmitsDeprecated()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            GeneratorTestHelper.CreateOperationRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

[ScalarStability(Stability.Deprecated)]
public class GetItemsCommand { }
public class GetItemsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemsCommand, GetItemsResponse>(""/v1/items"", ctx => new GetItemsCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("x-scalar-stability: deprecated");
    }

    [Fact]
    public void Generator_AllThreeAttributesTogether_EmitsAllThreeExtensions()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            GeneratorTestHelper.CreateOperationRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

[CodeSample(lang: ""curl"", source: ""curl -X POST https://api.example.com/v1/orders"")]
[OperationBadge(name: ""beta"")]
[ScalarStability(Stability.Experimental)]
public class CreateOrderCommand { }
public class CreateOrderResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateOrderCommand, CreateOrderResponse>(""/v1/orders"", ctx => new CreateOrderCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("x-codeSamples:");
        generatedSource.Should().Contain("x-badges:");
        generatedSource.Should().Contain("x-scalar-stability: experimental");
    }
}
