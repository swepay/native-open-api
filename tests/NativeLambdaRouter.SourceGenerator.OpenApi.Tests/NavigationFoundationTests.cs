using Microsoft.CodeAnalysis;

namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Tests for the Navigation Foundation features:
/// - tags root object (name, description, x-displayName, externalDocs per tag)
/// - x-tagGroups (sidebar grouping)
/// - Document-level externalDocs
/// - Per-operation externalDocs (from [EndpointExternalDocs] on TCommand)
/// </summary>
public sealed class NavigationFoundationTests
{
    // ── tags root section ─────────────────────────────────────────────

    [Fact]
    public void Generate_WithSingleEndpoint_EmitsTagsRootSection()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse",
                Tags = new List<string> { "Orders" }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        yaml.Should().Contain("tags:");
        yaml.Should().Contain("  - name: \"Orders\"");
    }

    [Fact]
    public void Generate_WithTagMetadata_EmitsDescriptionAndDisplayName()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse",
                Tags = new List<string> { "Orders" }
            }
        };

        var tagMetadata = new List<TagMetadataInfo>
        {
            new()
            {
                Name = "Orders",
                Description = "Operations related to order lifecycle.",
                DisplayName = "Order Management"
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            tagMetadata,
            new List<TagGroupInfo>(),
            null);

        yaml.Should().Contain("  - name: \"Orders\"");
        yaml.Should().Contain("    description: \"Operations related to order lifecycle.\"");
        yaml.Should().Contain("    x-displayName: \"Order Management\"");
    }

    [Fact]
    public void Generate_WithTagMetadataExternalDocs_EmitsExternalDocsUnderTag()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse",
                Tags = new List<string> { "Orders" }
            }
        };

        var tagMetadata = new List<TagMetadataInfo>
        {
            new()
            {
                Name = "Orders",
                ExternalDocs = new ExternalDocsInfo
                {
                    Url = "https://docs.example.com/orders",
                    Description = "Order domain guide"
                }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            tagMetadata,
            new List<TagGroupInfo>(),
            null);

        yaml.Should().Contain("    externalDocs:");
        yaml.Should().Contain("      description: \"Order domain guide\"");
        yaml.Should().Contain("      url: \"https://docs.example.com/orders\"");
    }

    [Fact]
    public void Generate_WithTagMetadataOnlyUrl_EmitsExternalDocsWithoutDescription()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/items",
                CommandTypeName = "App.GetItemsCommand",
                ResponseTypeName = "App.GetItemsResponse",
                CommandSimpleName = "GetItemsCommand",
                ResponseSimpleName = "GetItemsResponse",
                Tags = new List<string> { "Items" }
            }
        };

        var tagMetadata = new List<TagMetadataInfo>
        {
            new()
            {
                Name = "Items",
                ExternalDocs = new ExternalDocsInfo { Url = "https://docs.example.com/items" }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            tagMetadata,
            new List<TagGroupInfo>(),
            null);

        yaml.Should().Contain("    externalDocs:");
        yaml.Should().Contain("      url: \"https://docs.example.com/items\"");
        // No description line since it was not set
        yaml.Should().NotContain("      description: \"\"");
    }

    [Fact]
    public void Generate_WithMultipleTags_EmitsAllTagsAlphabeticallySorted()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/zebras",
                CommandTypeName = "App.GetZebrasCommand",
                ResponseTypeName = "App.GetZebrasResponse",
                CommandSimpleName = "GetZebrasCommand",
                ResponseSimpleName = "GetZebrasResponse",
                Tags = new List<string> { "Zebras" }
            },
            new()
            {
                Method = "GET",
                Path = "/v1/apples",
                CommandTypeName = "App.GetApplesCommand",
                ResponseTypeName = "App.GetApplesResponse",
                CommandSimpleName = "GetApplesCommand",
                ResponseSimpleName = "GetApplesResponse",
                Tags = new List<string> { "Apples" }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        var applesIndex = yaml.IndexOf("  - name: \"Apples\"", StringComparison.Ordinal);
        var zebrasIndex = yaml.IndexOf("  - name: \"Zebras\"", StringComparison.Ordinal);
        applesIndex.Should().BeGreaterThanOrEqualTo(0);
        zebrasIndex.Should().BeGreaterThanOrEqualTo(0);
        applesIndex.Should().BeLessThan(zebrasIndex, "tags root section should be sorted alphabetically");
    }

    [Fact]
    public void Generate_TagsRootListIsDeduplicatedAcrossEndpoints()
    {
        // Two endpoints with the same tag — the root tags list should contain it only once.
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse",
                Tags = new List<string> { "Orders" }
            },
            new()
            {
                Method = "POST",
                Path = "/v1/orders",
                CommandTypeName = "App.CreateOrderCommand",
                ResponseTypeName = "App.CreateOrderResponse",
                CommandSimpleName = "CreateOrderCommand",
                ResponseSimpleName = "CreateOrderResponse",
                Tags = new List<string> { "Orders" }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        // "  - name: \"Orders\"" should appear exactly once in the tags root.
        var count = CountOccurrences(yaml, "  - name: \"Orders\"");
        count.Should().Be(1, "deduplication is required in the root tags list");
    }

    [Fact]
    public void Generate_WithNoTagMetadata_StillEmitsTagsRootForAutoGeneratedTags()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/products",
                CommandTypeName = "App.GetProductsCommand",
                ResponseTypeName = "App.GetProductsResponse",
                CommandSimpleName = "GetProductsCommand",
                ResponseSimpleName = "GetProductsResponse"
                // No Tags set — auto-generated from path: first non-empty segment "v1" → "V1"
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        // Auto-tag is derived from the first path segment: "v1" → "V1"
        yaml.Should().Contain("  - name: \"V1\"");
        yaml.Should().Contain("tags:");
    }

    // ── x-tagGroups ───────────────────────────────────────────────────

    [Fact]
    public void Generate_WithTagGroups_EmitsXTagGroupsAtRoot()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse",
                Tags = new List<string> { "Orders" }
            }
        };

        var tagGroups = new List<TagGroupInfo>
        {
            new() { Name = "Core Operations", Tags = new List<string> { "Orders", "Products" } }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            tagGroups,
            null);

        yaml.Should().Contain("x-tagGroups:");
        yaml.Should().Contain("  - name: \"Core Operations\"");
        yaml.Should().Contain("    tags:");
        yaml.Should().Contain("      - \"Orders\"");
        yaml.Should().Contain("      - \"Products\"");
    }

    [Fact]
    public void Generate_WithMultipleTagGroups_EmitsAllInDeclarationOrder()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse",
                Tags = new List<string> { "Orders" }
            }
        };

        var tagGroups = new List<TagGroupInfo>
        {
            new() { Name = "Core Operations", Tags = new List<string> { "Orders", "Products" } },
            new() { Name = "Administration",  Tags = new List<string> { "Users", "Roles" } }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            tagGroups,
            null);

        var coreIdx = yaml.IndexOf("  - name: \"Core Operations\"", StringComparison.Ordinal);
        var adminIdx = yaml.IndexOf("  - name: \"Administration\"", StringComparison.Ordinal);
        coreIdx.Should().BeGreaterThanOrEqualTo(0);
        adminIdx.Should().BeGreaterThanOrEqualTo(0);
        coreIdx.Should().BeLessThan(adminIdx, "tag groups should preserve declaration order");
    }

    [Fact]
    public void Generate_WithoutTagGroups_DoesNotEmitXTagGroups()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse"
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        yaml.Should().NotContain("x-tagGroups:");
    }

    // ── Document-level externalDocs ───────────────────────────────────

    [Fact]
    public void Generate_WithRootExternalDocs_EmitsExternalDocsAtDocumentRoot()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse"
            }
        };

        var rootExternalDocs = new ExternalDocsInfo
        {
            Url = "https://docs.swepay.com",
            Description = "Full developer guide"
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            rootExternalDocs);

        yaml.Should().Contain("externalDocs:");
        yaml.Should().Contain("  description: \"Full developer guide\"");
        yaml.Should().Contain("  url: \"https://docs.swepay.com\"");
    }

    [Fact]
    public void Generate_WithRootExternalDocsNoDescription_EmitsOnlyUrl()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse"
            }
        };

        var rootExternalDocs = new ExternalDocsInfo { Url = "https://docs.swepay.com" };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            rootExternalDocs);

        yaml.Should().Contain("externalDocs:");
        yaml.Should().Contain("  url: \"https://docs.swepay.com\"");
    }

    [Fact]
    public void Generate_WithoutRootExternalDocs_DoesNotEmitExternalDocsKey()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse"
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        // No root-level externalDocs should be emitted.
        // Note: per-tag externalDocs is indented; this checks for the root-level key.
        yaml.Should().NotContain("\nexternalDocs:");
    }

    [Fact]
    public void Generate_RootExternalDocsAppearsBeforePaths()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse"
            }
        };

        var rootExternalDocs = new ExternalDocsInfo
        {
            Url = "https://docs.swepay.com",
            Description = "Developer guide"
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            rootExternalDocs);

        var extDocsIdx = yaml.IndexOf("externalDocs:", StringComparison.Ordinal);
        var pathsIdx = yaml.IndexOf("paths:", StringComparison.Ordinal);
        extDocsIdx.Should().BeGreaterThanOrEqualTo(0);
        pathsIdx.Should().BeGreaterThanOrEqualTo(0);
        extDocsIdx.Should().BeLessThan(pathsIdx, "root externalDocs must appear before the paths section");
    }

    // ── Per-operation externalDocs ─────────────────────────────────────

    [Fact]
    public void Generate_WithEndpointExternalDocs_EmitsExternalDocsInOperation()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "POST",
                Path = "/v1/orders",
                CommandTypeName = "App.CreateOrderCommand",
                ResponseTypeName = "App.CreateOrderResponse",
                CommandSimpleName = "CreateOrderCommand",
                ResponseSimpleName = "CreateOrderResponse",
                ExternalDocs = new ExternalDocsInfo
                {
                    Url = "https://docs.swepay.com/orders/create",
                    Description = "Order creation flow diagram"
                }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        yaml.Should().Contain("      externalDocs:");
        yaml.Should().Contain("        description: \"Order creation flow diagram\"");
        yaml.Should().Contain("        url: \"https://docs.swepay.com/orders/create\"");
    }

    [Fact]
    public void Generate_WithEndpointExternalDocsNoDescription_EmitsOnlyUrl()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse",
                ExternalDocs = new ExternalDocsInfo { Url = "https://docs.swepay.com/orders" }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        yaml.Should().Contain("      externalDocs:");
        yaml.Should().Contain("        url: \"https://docs.swepay.com/orders\"");
    }

    [Fact]
    public void Generate_WithoutEndpointExternalDocs_DoesNotEmitOperationExternalDocs()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse"
                // ExternalDocs is null by default
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        // Operation-level externalDocs (indented 6 spaces) should not be present
        yaml.Should().NotContain("      externalDocs:");
    }

    [Fact]
    public void Generate_EndpointExternalDocsAppearsInsideOperationBlock()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse",
                ExternalDocs = new ExternalDocsInfo
                {
                    Url = "https://docs.swepay.com/orders"
                }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        // The operation-level externalDocs must appear AFTER the operationId line
        // and BEFORE the tags/security lines in the operation block.
        var operationIdIdx = yaml.IndexOf("      operationId:", StringComparison.Ordinal);
        var extDocsIdx = yaml.IndexOf("      externalDocs:", StringComparison.Ordinal);
        var responsesIdx = yaml.IndexOf("      responses:", StringComparison.Ordinal);

        operationIdIdx.Should().BeGreaterThanOrEqualTo(0);
        extDocsIdx.Should().BeGreaterThanOrEqualTo(0);
        responsesIdx.Should().BeGreaterThanOrEqualTo(0);

        operationIdIdx.Should().BeLessThan(extDocsIdx);
        extDocsIdx.Should().BeLessThan(responsesIdx);
    }

    // ── YAML escaping ─────────────────────────────────────────────────

    [Fact]
    public void Generate_TagNameWithSpecialChars_IsEscapedInRootTagsList()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse",
                Tags = new List<string> { "Order \"Management\"" }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        // Quotes must be escaped
        yaml.Should().Contain("\\\"Management\\\"");
    }

    [Fact]
    public void Generate_ExternalDocsUrlWithSpecialChars_IsEscaped()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "GET",
                Path = "/v1/orders",
                CommandTypeName = "App.GetOrdersCommand",
                ResponseTypeName = "App.GetOrdersResponse",
                CommandSimpleName = "GetOrdersCommand",
                ResponseSimpleName = "GetOrdersResponse",
                ExternalDocs = new ExternalDocsInfo
                {
                    Url = "https://docs.example.com/path?q=1&r=2",
                    Description = "Guide: \"Advanced\" features"
                }
            }
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

        yaml.Should().Contain("https://docs.example.com/path?q=1&r=2");
        yaml.Should().Contain("\\\"Advanced\\\"");
    }

    // ── Combined scenario (all four features together) ────────────────

    [Fact]
    public void Generate_AllNavigationFeaturesCombined_ProducesValidOutput()
    {
        var endpoints = new List<EndpointInfo>
        {
            new()
            {
                Method = "POST",
                Path = "/v1/orders",
                CommandTypeName = "App.CreateOrderCommand",
                ResponseTypeName = "App.CreateOrderResponse",
                CommandSimpleName = "CreateOrderCommand",
                ResponseSimpleName = "CreateOrderResponse",
                Tags = new List<string> { "Orders" },
                ExternalDocs = new ExternalDocsInfo
                {
                    Url = "https://docs.swepay.com/orders/create",
                    Description = "Creation guide"
                }
            },
            new()
            {
                Method = "GET",
                Path = "/v1/products",
                CommandTypeName = "App.GetProductsCommand",
                ResponseTypeName = "App.GetProductsResponse",
                CommandSimpleName = "GetProductsCommand",
                ResponseSimpleName = "GetProductsResponse",
                Tags = new List<string> { "Products" }
            }
        };

        var tagMetadata = new List<TagMetadataInfo>
        {
            new()
            {
                Name = "Orders",
                Description = "Order lifecycle operations.",
                DisplayName = "Order Management",
                ExternalDocs = new ExternalDocsInfo
                {
                    Url = "https://docs.swepay.com/orders",
                    Description = "Order domain guide"
                }
            },
            new()
            {
                Name = "Products",
                Description = "Product catalog operations.",
                DisplayName = "Product Catalog"
            }
        };

        var tagGroups = new List<TagGroupInfo>
        {
            new() { Name = "Commerce", Tags = new List<string> { "Orders", "Products" } }
        };

        var rootExternalDocs = new ExternalDocsInfo
        {
            Url = "https://docs.swepay.com",
            Description = "Full developer guide"
        };

        var yaml = OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            tagMetadata,
            tagGroups,
            rootExternalDocs);

        // Root externalDocs
        yaml.Should().Contain("externalDocs:");
        yaml.Should().Contain("  description: \"Full developer guide\"");
        yaml.Should().Contain("  url: \"https://docs.swepay.com\"");

        // x-tagGroups
        yaml.Should().Contain("x-tagGroups:");
        yaml.Should().Contain("  - name: \"Commerce\"");
        yaml.Should().Contain("      - \"Orders\"");
        yaml.Should().Contain("      - \"Products\"");

        // Root tags section with metadata
        yaml.Should().Contain("  - name: \"Orders\"");
        yaml.Should().Contain("    description: \"Order lifecycle operations.\"");
        yaml.Should().Contain("    x-displayName: \"Order Management\"");
        yaml.Should().Contain("    externalDocs:");
        yaml.Should().Contain("      description: \"Order domain guide\"");
        yaml.Should().Contain("      url: \"https://docs.swepay.com/orders\"");
        yaml.Should().Contain("  - name: \"Products\"");
        yaml.Should().Contain("    description: \"Product catalog operations.\"");
        yaml.Should().Contain("    x-displayName: \"Product Catalog\"");

        // Per-operation externalDocs (indented)
        yaml.Should().Contain("      externalDocs:");
        yaml.Should().Contain("        description: \"Creation guide\"");
        yaml.Should().Contain("        url: \"https://docs.swepay.com/orders/create\"");

        // Structural correctness: root externalDocs before x-tagGroups before tags before paths
        var rootExtDocsIdx = yaml.IndexOf("\nexternalDocs:", StringComparison.Ordinal);
        var tagGroupsIdx = yaml.IndexOf("x-tagGroups:", StringComparison.Ordinal);
        var tagsIdx = yaml.IndexOf("\ntags:", StringComparison.Ordinal);
        var pathsIdx = yaml.IndexOf("paths:", StringComparison.Ordinal);

        rootExtDocsIdx.Should().BeLessThan(tagGroupsIdx);
        tagGroupsIdx.Should().BeLessThan(tagsIdx);
        tagsIdx.Should().BeLessThan(pathsIdx);
    }

    // ── Source generator end-to-end (assembly-level attribute reading) ─

    [Fact]
    public void Generator_AssemblyTagMetadataAttribute_IsReadAndEmitted()
    {
        // Using multi-source RunGenerator to avoid multiple file-scoped namespace declarations
        // in a single source file (which is illegal C#).
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            GeneratorTestHelper.CreateNavigationAttributeSource(),
            @"
using Native.OpenApi.Attributes;
using TestApp;

[assembly: TagMetadata(""Orders"", Description = ""Order lifecycle operations."", DisplayName = ""Order Management"")]
",
            @"
namespace TestApp;

public class GetOrdersCommand { }
public class GetOrdersResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetOrdersCommand, GetOrdersResponse>(""/v1/orders"", ctx => new GetOrdersCommand())
            .WithTags(""Orders"");
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("description: \"\"Order lifecycle operations.\"\"");
        generatedSource.Should().Contain("x-displayName: \"\"Order Management\"\"");
    }

    [Fact]
    public void Generator_AssemblyTagGroupAttribute_IsReadAndEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            GeneratorTestHelper.CreateNavigationAttributeSource(),
            @"
using Native.OpenApi.Attributes;
using TestApp;

[assembly: TagGroup(""Commerce"", new[] { ""Orders"", ""Products"" })]
",
            @"
namespace TestApp;

public class GetOrdersCommand { }
public class GetOrdersResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetOrdersCommand, GetOrdersResponse>(""/v1/orders"", ctx => new GetOrdersCommand())
            .WithTags(""Orders"");
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("x-tagGroups:");
        generatedSource.Should().Contain("- name: \"\"Commerce\"\"");
    }

    [Fact]
    public void Generator_AssemblyOpenApiExternalDocsAttribute_IsReadAndEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            GeneratorTestHelper.CreateNavigationAttributeSource(),
            @"
using Native.OpenApi.Attributes;
using TestApp;

[assembly: OpenApiExternalDocs(""https://docs.swepay.com"", Description = ""Full developer guide"")]
",
            @"
namespace TestApp;

public class GetOrdersCommand { }
public class GetOrdersResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetOrdersCommand, GetOrdersResponse>(""/v1/orders"", ctx => new GetOrdersCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("externalDocs:");
        generatedSource.Should().Contain("url: \"\"https://docs.swepay.com\"\"");
        generatedSource.Should().Contain("description: \"\"Full developer guide\"\"");
    }

    [Fact]
    public void Generator_EndpointExternalDocsAttribute_IsReadAndEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            GeneratorTestHelper.CreateNavigationAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

[EndpointExternalDocs(""https://docs.swepay.com/orders/create"", Description = ""Order creation flow"")]
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
        generatedSource.Should().Contain("externalDocs:");
        generatedSource.Should().Contain("url: \"\"https://docs.swepay.com/orders/create\"\"");
        generatedSource.Should().Contain("description: \"\"Order creation flow\"\"");
    }

    // ── helpers ───────────────────────────────────────────────────────

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
}
