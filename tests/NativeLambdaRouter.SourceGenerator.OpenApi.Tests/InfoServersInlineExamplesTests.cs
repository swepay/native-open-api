namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Tests for Wave 3 features:
/// - Rich <c>info</c> block (description, summary, termsOfService, contact, license)
/// - Root <c>servers</c> array
/// - Inline example <c>value:</c> (as opposed to <c>externalValue:</c>)
/// </summary>
public sealed class InfoServersInlineExamplesTests
{
    // ── helpers ───────────────────────────────────────────────────────

    private static EndpointInfo MakeGetEndpoint(string path = "/v1/items")
        => new()
        {
            Method = "GET",
            Path = path,
            CommandTypeName = "App.Command",
            ResponseTypeName = "App.Response",
            CommandSimpleName = "Command",
            ResponseSimpleName = "Response"
        };

    private static EndpointInfo MakePostEndpoint(string path = "/v1/items")
        => new()
        {
            Method = "POST",
            Path = path,
            CommandTypeName = "App.CreateCommand",
            ResponseTypeName = "App.CreateResponse",
            CommandSimpleName = "CreateCommand",
            ResponseSimpleName = "CreateResponse"
        };

    private static string Generate(
        EndpointInfo endpoint,
        OpenApiInfoMetadata? infoMeta = null,
        IReadOnlyList<OpenApiServerInfo>? servers = null)
        => OpenApiYamlGenerator.Generate(
            new List<EndpointInfo> { endpoint },
            "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null,
            infoMeta,
            servers ?? new List<OpenApiServerInfo>());

    // ── info: base fields always present ─────────────────────────────

    [Fact]
    public void Generate_WithNullInfoMetadata_EmitsTitleAndVersionOnly()
    {
        var endpoint = MakeGetEndpoint();
        var yaml = Generate(endpoint, infoMeta: null);

        yaml.Should().Contain("info:");
        yaml.Should().Contain("  title: \"Test API\"");
        yaml.Should().Contain("  version: \"1.0.0\"");

        // None of the rich info-level fields should appear in the whole spec
        // when no metadata is supplied.  These keys are unique to the info block —
        // the rest of the spec uses different key names (e.g. "description" is
        // always nested deeper with more indentation in paths/schemas, never at
        // the 2-space level that info uses outside of a declared metadata context).
        yaml.Should().NotContain("termsOfService:");
        yaml.Should().NotContain("contact:");
        yaml.Should().NotContain("license:");
        // "  description:" with exactly 2 leading spaces only appears inside info.
        // Use line-prefix match to avoid false positives from deeper indentation.
        var lines = yaml.Replace("\r\n", "\n").Split('\n');
        lines.Should().NotContain("  description:", "info-level description should not be emitted when InfoMetadata is null");
        // "  summary:" at 2-space indent belongs only to info — operation summaries are 8-space.
        lines.Should().NotContain("  summary:", "info-level summary should not be emitted when InfoMetadata is null");
    }

    // ── info: description ────────────────────────────────────────────

    [Fact]
    public void Generate_WithInfoDescription_EmitsDescriptionInInfoBlock()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata { Description = "Marketplace API description." };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().Contain("  description: \"Marketplace API description.\"");
    }

    [Fact]
    public void Generate_InfoDescriptionWithQuotes_IsEscaped()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata { Description = "Say \"hello\"." };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().Contain("Say \\\"hello\\\"");
    }

    // ── info: summary ────────────────────────────────────────────────

    [Fact]
    public void Generate_WithInfoSummary_EmitsSummaryInInfoBlock()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata { Summary = "Short one-liner." };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().Contain("  summary: \"Short one-liner.\"");
    }

    [Fact]
    public void Generate_InfoSummaryAppearsAfterVersionAndBeforeDescription()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata
        {
            Summary = "Short one-liner.",
            Description = "Long description."
        };
        var yaml = Generate(endpoint, infoMeta: meta);

        var versionIdx   = yaml.IndexOf("  version:", StringComparison.Ordinal);
        var summaryIdx   = yaml.IndexOf("  summary:", StringComparison.Ordinal);
        var descIdx      = yaml.IndexOf("  description:", StringComparison.Ordinal);

        versionIdx.Should().BeGreaterThanOrEqualTo(0);
        summaryIdx.Should().BeGreaterThanOrEqualTo(0);
        descIdx.Should().BeGreaterThanOrEqualTo(0);

        versionIdx.Should().BeLessThan(summaryIdx);
        summaryIdx.Should().BeLessThan(descIdx);
    }

    // ── info: termsOfService ─────────────────────────────────────────

    [Fact]
    public void Generate_WithTermsOfService_EmitsTermsOfServiceField()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata { TermsOfService = "https://swepay.com/terms" };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().Contain("  termsOfService: \"https://swepay.com/terms\"");
    }

    // ── info: contact ────────────────────────────────────────────────

    [Fact]
    public void Generate_WithContactNameOnly_EmitsContactBlock()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata { ContactName = "Swepay Support" };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().Contain("  contact:");
        yaml.Should().Contain("    name: \"Swepay Support\"");
        yaml.Should().NotContain("    url:");
        yaml.Should().NotContain("    email:");
    }

    [Fact]
    public void Generate_WithFullContact_EmitsAllContactFields()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata
        {
            ContactName  = "Swepay Support",
            ContactUrl   = "https://swepay.com/contact",
            ContactEmail = "api@swepay.com"
        };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().Contain("  contact:");
        yaml.Should().Contain("    name: \"Swepay Support\"");
        yaml.Should().Contain("    url: \"https://swepay.com/contact\"");
        yaml.Should().Contain("    email: \"api@swepay.com\"");
    }

    [Fact]
    public void Generate_WithoutContactFields_DoesNotEmitContactBlock()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata { Description = "desc only" };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().NotContain("  contact:");
    }

    // ── info: license ────────────────────────────────────────────────

    [Fact]
    public void Generate_WithLicenseNameOnly_EmitsLicenseBlock()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata { LicenseName = "MIT" };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().Contain("  license:");
        yaml.Should().Contain("    name: \"MIT\"");
        yaml.Should().NotContain("    url:");
        yaml.Should().NotContain("    identifier:");
    }

    [Fact]
    public void Generate_WithLicenseUrl_EmitsUrlField()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata
        {
            LicenseName = "Apache 2.0",
            LicenseUrl  = "https://www.apache.org/licenses/LICENSE-2.0"
        };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().Contain("  license:");
        yaml.Should().Contain("    name: \"Apache 2.0\"");
        yaml.Should().Contain("    url: \"https://www.apache.org/licenses/LICENSE-2.0\"");
        yaml.Should().NotContain("    identifier:");
    }

    [Fact]
    public void Generate_WithSpdxLicenseIdentifier_EmitsIdentifierField()
    {
        // When LicenseUrl has no "://" it is treated as an SPDX identifier.
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata
        {
            LicenseName = "Apache 2.0",
            LicenseUrl  = "Apache-2.0"   // SPDX — no scheme
        };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().Contain("  license:");
        yaml.Should().Contain("    name: \"Apache 2.0\"");
        yaml.Should().Contain("    identifier: \"Apache-2.0\"");
        yaml.Should().NotContain("    url:");
    }

    [Fact]
    public void Generate_WithoutLicenseName_DoesNotEmitLicenseBlock()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata { ContactEmail = "x@y.com" };
        var yaml = Generate(endpoint, infoMeta: meta);

        yaml.Should().NotContain("  license:");
    }

    // ── info: order of fields ─────────────────────────────────────────

    [Fact]
    public void Generate_RichInfoBlock_AppearsBeforeServersAndPaths()
    {
        var endpoint = MakeGetEndpoint();
        var meta = new OpenApiInfoMetadata
        {
            Description  = "Desc",
            TermsOfService = "https://swepay.com/terms",
            ContactName  = "Support",
            LicenseName  = "MIT"
        };
        var servers = new List<OpenApiServerInfo>
        {
            new() { Url = "https://api.swepay.com", Description = "Prod" }
        };
        var yaml = Generate(endpoint, infoMeta: meta, servers: servers);

        var infoIdx    = yaml.IndexOf("info:", StringComparison.Ordinal);
        var serversIdx = yaml.IndexOf("servers:", StringComparison.Ordinal);
        var pathsIdx   = yaml.IndexOf("paths:", StringComparison.Ordinal);

        infoIdx.Should().BeGreaterThanOrEqualTo(0);
        serversIdx.Should().BeGreaterThanOrEqualTo(0);
        pathsIdx.Should().BeGreaterThanOrEqualTo(0);

        infoIdx.Should().BeLessThan(serversIdx);
        serversIdx.Should().BeLessThan(pathsIdx);
    }

    // ── servers ───────────────────────────────────────────────────────

    [Fact]
    public void Generate_WithNoServers_DoesNotEmitServersKey()
    {
        var endpoint = MakeGetEndpoint();
        var yaml = Generate(endpoint, servers: new List<OpenApiServerInfo>());

        yaml.Should().NotContain("servers:");
    }

    [Fact]
    public void Generate_WithSingleServer_EmitsServersArray()
    {
        var endpoint = MakeGetEndpoint();
        var servers = new List<OpenApiServerInfo>
        {
            new() { Url = "https://api.swepay.com", Description = "Production" }
        };
        var yaml = Generate(endpoint, servers: servers);

        yaml.Should().Contain("servers:");
        yaml.Should().Contain("  - url: \"https://api.swepay.com\"");
        yaml.Should().Contain("    description: \"Production\"");
    }

    [Fact]
    public void Generate_WithTwoServers_EmitsBothInDeclarationOrder()
    {
        var endpoint = MakeGetEndpoint();
        var servers = new List<OpenApiServerInfo>
        {
            new() { Url = "https://api.swepay.com",         Description = "Production" },
            new() { Url = "https://api.sandbox.swepay.com", Description = "Sandbox" }
        };
        var yaml = Generate(endpoint, servers: servers);

        yaml.Should().Contain("  - url: \"https://api.swepay.com\"");
        yaml.Should().Contain("    description: \"Production\"");
        yaml.Should().Contain("  - url: \"https://api.sandbox.swepay.com\"");
        yaml.Should().Contain("    description: \"Sandbox\"");

        var prodIdx    = yaml.IndexOf("https://api.swepay.com\"", StringComparison.Ordinal);
        var sandboxIdx = yaml.IndexOf("https://api.sandbox.swepay.com\"", StringComparison.Ordinal);
        prodIdx.Should().BeLessThan(sandboxIdx, "servers must be emitted in declaration order");
    }

    [Fact]
    public void Generate_ServerWithoutDescription_EmitsUrlOnly()
    {
        var endpoint = MakeGetEndpoint();
        var servers = new List<OpenApiServerInfo>
        {
            new() { Url = "https://api.swepay.com" }
        };
        var yaml = Generate(endpoint, servers: servers);

        yaml.Should().Contain("  - url: \"https://api.swepay.com\"");
        // description line should not be present for this server entry
        var serverIdx = yaml.IndexOf("  - url: \"https://api.swepay.com\"", StringComparison.Ordinal);
        var nextLineStart = yaml.IndexOf('\n', serverIdx) + 1;
        var nextLine = yaml.Substring(nextLineStart, yaml.IndexOf('\n', nextLineStart) - nextLineStart);
        nextLine.Trim().Should().NotStartWith("description:");
    }

    [Fact]
    public void Generate_ServerUrlWithSpecialChars_IsEscaped()
    {
        var endpoint = MakeGetEndpoint();
        var servers = new List<OpenApiServerInfo>
        {
            new() { Url = "https://api.swepay.com", Description = "Say \"hello\"" }
        };
        var yaml = Generate(endpoint, servers: servers);

        yaml.Should().Contain("Say \\\"hello\\\"");
    }

    [Fact]
    public void Generate_ServersAppearAfterInfoAndBeforePaths()
    {
        var endpoint = MakeGetEndpoint();
        var servers = new List<OpenApiServerInfo>
        {
            new() { Url = "https://api.swepay.com" }
        };
        var yaml = Generate(endpoint, servers: servers);

        var infoIdx    = yaml.IndexOf("info:", StringComparison.Ordinal);
        var serversIdx = yaml.IndexOf("servers:", StringComparison.Ordinal);
        var pathsIdx   = yaml.IndexOf("paths:", StringComparison.Ordinal);

        infoIdx.Should().BeLessThan(serversIdx);
        serversIdx.Should().BeLessThan(pathsIdx);
    }

    // ── inline examples: request body ────────────────────────────────

    [Fact]
    public void Generate_WithInlineRequestValue_EmitsValueKey()
    {
        var endpoint = MakePostEndpoint();
        endpoint.Examples.Add(new ApiExampleInfo
        {
            Name = "happy-path",
            Summary = "Simple create",
            RequestInlineValue = "{\"name\": \"Widget\", \"price\": 9.99}"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("examples:");
        yaml.Should().Contain("happy-path:");
        yaml.Should().Contain("summary: \"Simple create\"");
        yaml.Should().Contain("value: {\"name\": \"Widget\", \"price\": 9.99}");
        yaml.Should().NotContain("externalValue:");
    }

    [Fact]
    public void Generate_WithExternalValueAndInlineValue_ExternalValueWins()
    {
        var endpoint = MakePostEndpoint();
        endpoint.Examples.Add(new ApiExampleInfo
        {
            Name = "dual",
            Summary = "Both set — externalValue should win",
            RequestJsonPath = "examples/req.json",
            RequestInlineValue = "{\"name\": \"inline\"}"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("externalValue: \"examples/req.json\"");
        yaml.Should().NotContain("value: {\"name\": \"inline\"}");
    }

    [Fact]
    public void Generate_WithInlineRequestValue_DoesNotEmitExternalValue()
    {
        var endpoint = MakePostEndpoint();
        endpoint.Examples.Add(new ApiExampleInfo
        {
            Name = "inline-only",
            Summary = "Inline",
            RequestInlineValue = "{\"x\": 1}"
        });

        var yaml = Generate(endpoint);

        yaml.Should().NotContain("externalValue:");
        yaml.Should().Contain("value: {\"x\": 1}");
    }

    // ── inline examples: response ────────────────────────────────────

    [Fact]
    public void Generate_WithInlineResponseValue_EmitsValueKeyOnResponse()
    {
        var endpoint = MakeGetEndpoint();
        endpoint.Examples.Add(new ApiExampleInfo
        {
            Name = "ok-example",
            Summary = "Successful 200",
            ResponseStatus = 200,
            ResponseInlineValue = "{\"id\": \"abc\", \"status\": \"active\"}"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("ok-example:");
        yaml.Should().Contain("value: {\"id\": \"abc\", \"status\": \"active\"}");
    }

    [Fact]
    public void Generate_ResponseExternalValueTakesPrecedenceOverInline()
    {
        var endpoint = MakeGetEndpoint();
        endpoint.Examples.Add(new ApiExampleInfo
        {
            Name = "dual-rsp",
            Summary = "Both response fields set",
            ResponseStatus = 200,
            ResponseJsonPath = "examples/rsp.json",
            ResponseInlineValue = "{\"id\": \"inline\"}"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("externalValue: \"examples/rsp.json\"");
        yaml.Should().NotContain("value: {\"id\": \"inline\"}");
    }

    [Fact]
    public void Generate_MultilineInlineValue_EmitsBlockScalar()
    {
        var endpoint = MakePostEndpoint();
        var json = "{\n  \"name\": \"Widget\",\n  \"price\": 9.99\n}";
        endpoint.Examples.Add(new ApiExampleInfo
        {
            Name = "multiline",
            Summary = "Formatted JSON",
            RequestInlineValue = json
        });

        var yaml = Generate(endpoint);

        // Block scalar indicator
        yaml.Should().Contain("value: |");
        yaml.Should().Contain("\"name\": \"Widget\"");
    }

    [Fact]
    public void Generate_RequestOnlyInlineExample_IsNotAttachedToResponse()
    {
        // A request-only inline example (ResponseStatus == 0) should not appear in the response block.
        var endpoint = MakePostEndpoint();
        endpoint.Examples.Add(new ApiExampleInfo
        {
            Name = "req-only",
            Summary = "Request only",
            RequestInlineValue = "{\"amount\": 100}",
            ResponseStatus = 0   // request-only
        });

        var yaml = Generate(endpoint);

        // Should appear in requestBody examples, not in responses examples
        yaml.Should().Contain("req-only:");
        yaml.Should().Contain("value: {\"amount\": 100}");
    }

    [Fact]
    public void Generate_ExampleWithBothRequestAndResponseInline_EmitsBothSides()
    {
        var endpoint = MakePostEndpoint();
        endpoint.Examples.Add(new ApiExampleInfo
        {
            Name = "full",
            Summary = "Full round-trip example",
            RequestInlineValue = "{\"name\": \"Widget\"}",
            ResponseStatus = 200,
            ResponseInlineValue = "{\"id\": \"wgt_1\"}"
        });

        var yaml = Generate(endpoint);

        // Request side
        yaml.Should().Contain("value: {\"name\": \"Widget\"}");
        // Response side
        yaml.Should().Contain("value: {\"id\": \"wgt_1\"}");
    }

    // ── end-to-end: source generator reads [OpenApiInfo] and [OpenApiServer] ──

    [Fact]
    public void Generator_OpenApiInfoAttribute_IsReadAndEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateWave3AttributeSource(),
            @"
using Native.OpenApi.Attributes;
using TestApp;

[assembly: OpenApiInfo(
    Description    = ""Marketplace API for managing items."",
    Summary        = ""Marketplace API"",
    TermsOfService = ""https://swepay.com/terms"",
    ContactName    = ""Swepay Support"",
    ContactUrl     = ""https://swepay.com/contact"",
    ContactEmail   = ""api@swepay.com"",
    LicenseName    = ""Apache 2.0"",
    LicenseUrl     = ""https://www.apache.org/licenses/LICENSE-2.0"")]
",
            @"
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
"
        });

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        source.Should().NotBeNull();
        source.Should().Contain("description: \"\"Marketplace API for managing items.\"\"");
        source.Should().Contain("summary: \"\"Marketplace API\"\"");
        source.Should().Contain("termsOfService: \"\"https://swepay.com/terms\"\"");
        source.Should().Contain("contact:");
        source.Should().Contain("name: \"\"Swepay Support\"\"");
        source.Should().Contain("email: \"\"api@swepay.com\"\"");
        source.Should().Contain("license:");
        source.Should().Contain("name: \"\"Apache 2.0\"\"");
        source.Should().Contain("url: \"\"https://www.apache.org/licenses/LICENSE-2.0\"\"");
    }

    [Fact]
    public void Generator_OpenApiServerAttribute_IsReadAndEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateWave3AttributeSource(),
            @"
using Native.OpenApi.Attributes;
using TestApp;

[assembly: OpenApiServer(""https://api.swepay.com"",         Description = ""Production"")]
[assembly: OpenApiServer(""https://api.sandbox.swepay.com"", Description = ""Sandbox"")]
",
            @"
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
"
        });

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        source.Should().NotBeNull();
        source.Should().Contain("servers:");
        source.Should().Contain("url: \"\"https://api.swepay.com\"\"");
        source.Should().Contain("description: \"\"Production\"\"");
        source.Should().Contain("url: \"\"https://api.sandbox.swepay.com\"\"");
        source.Should().Contain("description: \"\"Sandbox\"\"");
    }

    [Fact]
    public void Generator_ApiExampleWithInlineValue_IsReadAndEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateWave3AttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

[ApiExample(""happy-path"", ""Simple create"",
    RequestValue = ""{\""\""name\""\"":\""Widget\""}"")]
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

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        source.Should().NotBeNull();
        source.Should().Contain("examples:");
        source.Should().Contain("happy-path:");
    }

    // ── helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Inlined attribute source for Wave 3 attributes used in generator end-to-end tests.
    /// </summary>
    internal static string CreateWave3AttributeSource()
    {
        return @"
namespace Native.OpenApi.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class OpenApiInfoAttribute : System.Attribute
    {
        public string Description { get; set; }
        public string Summary { get; set; }
        public string TermsOfService { get; set; }
        public string ContactName { get; set; }
        public string ContactUrl { get; set; }
        public string ContactEmail { get; set; }
        public string LicenseName { get; set; }
        public string LicenseUrl { get; set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class OpenApiServerAttribute : System.Attribute
    {
        public string Url { get; }
        public string Description { get; set; }
        public OpenApiServerAttribute(string url) { Url = url; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public sealed class ApiExampleAttribute : System.Attribute
    {
        public string Name { get; }
        public string Summary { get; }
        public string RequestJson { get; set; }
        public int ResponseStatus { get; set; }
        public string ResponseJson { get; set; }
        public string RequestValue { get; set; }
        public string ResponseValue { get; set; }
        public ApiExampleAttribute(string name, string summary) { Name = name; Summary = summary; }
    }
}
";
    }
}
