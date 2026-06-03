# Native.OpenApi

[![Build Status](https://github.com/swepay/native-open-api/actions/workflows/dotnet.yml/badge.svg)](https://github.com/swepay/native-open-api/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Compile-time OpenAPI 3.1 for Native AOT .NET 10 APIs. Zero runtime reflection.
Ships two NuGet packages + a Roslyn Source Generator.

- **Current version:** `1.8.3`
- **Target:** `net10.0` (library) / `netstandard2.0` (generator)
- **AOT:** `PublishAot=true`, `IsTrimmable=true`, no runtime reflection
- **OpenAPI:** 3.1-only
- **Canonical docs:** [src/Native.OpenApi/README.md](src/Native.OpenApi/README.md) · [src/NativeLambdaRouter.SourceGenerator.OpenApi/README.md](src/NativeLambdaRouter.SourceGenerator.OpenApi/README.md)
- **Changelog:** [docs/CHANGELOG.md](docs/CHANGELOG.md) · **Troubleshooting:** [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) · **UX RFC:** [docs/RFC-DOCUMENTACAO-UX.md](docs/RFC-DOCUMENTACAO-UX.md)

---

## Agent quick-reference

> If you are an agent reading this file to plan a change, **start here**.

### Packages

| Package | Where it runs | Install | Docs |
|---|---|---|---|
| `NativeOpenApi` | Runtime + build-time (library + source references) | `dotnet add package NativeOpenApi` | [README](src/Native.OpenApi/README.md) |
| `NativeLambdaRouter.SourceGenerator.OpenApi` | Build-time only (Roslyn analyzer) | `dotnet add package NativeLambdaRouter.SourceGenerator.OpenApi` | [README](src/NativeLambdaRouter.SourceGenerator.OpenApi/README.md) |

### What each package is for

- **`NativeOpenApi`** ships: attributes (`[HideFromDocs]`, `[Deprecated]`, `[ApiExample]`, `[ErrorCatalog]`, `[ErrorDefinition]`, `[ApiResponse]`; v1.8.0 adds `[TagMetadata]`, `[TagGroup]`, `[OpenApiExternalDocs]`, `[EndpointExternalDocs]`, `[CodeSample]`, `[OperationBadge]`, `[ScalarStability]`, `[OpenApiProperty]`, `[OpenApiEnumMember]`, `[OpenApiDiscriminator]`, `[OpenApiSubType]`, `[OpenApiInfo]`, `[OpenApiServer]`, `[QueryParameter]`, `[HeaderParameter]`, `[ResponseHeader]`, `[ResponseLink]`, `[Callback]`, `[Webhook]`), models (`SwepayProblemDetails`), document primitives (`OpenApiDocument`, `OpenApiDocumentLoader`, `OpenApiDocumentMerger`, `OpenApiDocumentProvider`, `OpenApiLinter`), and renderer (`OpenApiHtmlRenderer` + `OpenApiRendererOptions` for branding/footer/Mermaid + `OpenApiScalarViewerOptions` for Scalar-specific knobs).
- **`NativeLambdaRouter.SourceGenerator.OpenApi`** reads `MapGet/MapPost/MapPut/MapPatch/MapDelete/Map` calls on `IRouteBuilder`, plus the attributes above, and emits a `GeneratedOpenApiSpec : IGeneratedOpenApiSpec` singleton with the full YAML at compile time.

### Decision tree

| You want to… | Do this | Files to edit |
|---|---|---|
| Hide an endpoint from docs | Add `[HideFromDocs]` on `TCommand` **or** `.ExcludeFromDocs()` on the mapping | your `Commands.cs` / `Function.cs` |
| Mark an endpoint deprecated | `[Deprecated(sunset, alternative, reason)]` on `TCommand` | your `Commands.cs` |
| Add named request/response examples | `[ApiExample(name, summary) { RequestJson, ResponseStatus, ResponseJson }]` on `TCommand` (multi-use); extend with `RequestValue`/`ResponseValue` for inline values | your `Commands.cs` |
| Centralise error codes | Create a `static class SwepayErrors` with `[ErrorDefinition]` consts, annotate commands with `[ErrorCatalog(typeof(SwepayErrors))]` | new `SwepayErrors.cs` + your `Commands.cs` |
| Use the canonical `problem+json` schema | Advertise a response with no typed body: `.ProducesProblem(400)` or `[ApiResponse(422, null, "application/problem+json")]` | your `Function.cs` / handler |
| Brand the Redoc/Scalar page | Instantiate `OpenApiRendererOptions` with `Branding`, `Footer`, `EnableMermaid`; pass to `OpenApiHtmlRenderer.Render*(spec, title, options)` | the project that hosts `/docs/*` |
| Configure the Scalar viewer (theme, dark mode, layout, hide panels) | Set `OpenApiRendererOptions.ScalarViewer` to a new `OpenApiScalarViewerOptions { Theme, DarkMode, Layout, ... }` | the project that hosts `/docs/scalar` |
| Draw a diagram inside a description | Put a fenced ` ```mermaid ` block in the `description` text and enable `options.EnableMermaid` | the `[EndpointDescription]` or `.WithDescription(...)` |
| Override the generated namespace | Set MSBuild property `OpenApiSpecName` in the producer `.csproj` | `*.csproj` |
| Group tags in the sidebar | `[assembly: TagGroup("Group Name", new[] { "Tag1", "Tag2" })]` | your `ApiDocumentation.cs` |
| Enrich a tag with description and display name | `[assembly: TagMetadata("Tag", Description = "...", DisplayName = "...")]` | your `ApiDocumentation.cs` |
| Add code samples to an operation | `[CodeSample(lang: "curl", source: "...")]` on `TCommand` (multi-use) | your `Commands.cs` |
| Set Scalar stability indicator | `[ScalarStability(Stability.Experimental)]` on `TCommand` | your `Commands.cs` |
| Annotate a property with constraints | `[property: OpenApiProperty(Description = "...", MinLength = 1, MaxLength = 100)]` | your request/response types |
| Document polymorphism | `[OpenApiDiscriminator("kind")]` + `[OpenApiSubType(typeof(T), "value")]` on the abstract base class | your domain types |
| Declare query / header parameters | `[QueryParameter("page", typeof(int))]` / `[HeaderParameter("X-Tenant-Id", typeof(string), Required = true)]` on `TCommand` | your `Commands.cs` |
| Document response headers | `[ResponseHeader(201, "Location", typeof(string), Required = true)]` on `TCommand` | your `Commands.cs` |
| Declare a webhook | `[assembly: Webhook("orderCreated", typeof(OrderCreatedEvent))]` | your `ApiDocumentation.cs` |
| Serve the spec from rich servers | `[assembly: OpenApiServer("https://api.example.com", Description = "Production")]` | your `ApiDocumentation.cs` |

### Feature matrix — v1.7.0 Wave 1 (RFC: [docs/RFC-DOCUMENTACAO-UX.md](docs/RFC-DOCUMENTACAO-UX.md))

| RFC id | Surface | Emits in YAML | Visible in Redoc/Scalar |
|---|---|---|---|
| F01 | `[HideFromDocs]` · `.ExcludeFromDocs()` | operation omitted | gone |
| F03 | `[Deprecated(sunset, alternative, reason)]` | `deprecated: true` + `x-sunset` + `x-swepay-alternative` + `x-swepay-deprecation-reason` | native deprecated flag |
| F09 | `[ApiExample]` (multi-use) | `examples` sub-node (request + per-status response), `externalValue` reference | named example picker |
| F12 | `[ErrorCatalog]` + `[ErrorDefinition]` | `x-swepay-errors` per op + `x-swepay-error-catalog` at root | raw extensions (table UI deferred) |
| F13 | `SwepayProblemDetails` record | `components.schemas.SwepayProblemDetails` auto-injected whenever any `application/problem+json` lacks a typed body | schema page |
| F15 | `OpenApiBrandingOptions` | — (renderer only) | primary/accent/logo/favicon/font |
| F16 | `OpenApiFooterOptions` | — (renderer only) | Status · Support · Changelog · SLA · Terms |
| F17 | `OpenApiRendererOptions.EnableMermaid` | — (renderer only) | Mermaid.js injected; fenced ` ```mermaid ` blocks render as SVG |

> **Wave 2 / Wave 3** (audiences, stability tiers, flow, state machine, rate-limit, idempotency, etc.) are RFC-tracked but not yet emitted by the generator.

### Feature matrix — v1.8.0 — OpenAPI 3.1 documentation expansion

Policy: **Scalar-first** on any Scalar/Redoc divergence. Extensions prefixed `x-scalar-*` and `x-enum-*` are not dual-emitted for Redoc. All additions are opt-in; no existing API breaks.

#### Navigation

| Attribute | Target | Emits in YAML | Scalar rendering |
|---|---|---|---|
| `[assembly: TagMetadata(name)]` + optional `Description`, `DisplayName`, `ExternalDocsUrl` / `ExternalDocsDescription` | assembly | root `tags[]` with `description`, `x-displayName`, `externalDocs` | enriched sidebar tag header |
| `[assembly: TagGroup(name, tags[])]` (multi-use) | assembly | root `x-tagGroups` | grouped sidebar sections |
| `[assembly: OpenApiExternalDocs(url)]` + optional `Description` | assembly | root `externalDocs` | header link |
| `[EndpointExternalDocs(url)]` + optional `Description` | class/struct | operation `externalDocs` | per-operation external link |

#### Operation richness

| Attribute | Target | Emits in YAML | Scalar rendering |
|---|---|---|---|
| `[CodeSample(lang, source)]` + optional `Label` (multi-use) | class/struct | `x-codeSamples[]` | syntax-highlighted language tabs |
| `[OperationBadge(name)]` + optional `Position`, `Color` (multi-use) | class/struct | `x-badges[]` | coloured pills next to operation title |
| `[ScalarStability(Stability.X)]` | class/struct | `x-scalar-stability: stable\|experimental\|deprecated` | stability badge (Scalar-first, no Redoc equivalent) |

#### Schema richness

| Attribute | Target | Emits in YAML |
|---|---|---|
| `[property: OpenApiProperty(...)]` | property | `description`, `example`, `default`, string/numeric/array constraints, `x-order`, `x-additionalPropertiesName` |
| `[OpenApiEnumMember(Description = ..., DisplayName = ...)]` (on enum fields) | field | parallel `x-enum-descriptions` + `x-enum-varnames` arrays (Scalar-first) |
| DataAnnotations: `[Required]`, `[StringLength]`, `[MinLength]`, `[MaxLength]`, `[Range]`, `[RegularExpression]` | property | matching OpenAPI constraints (read automatically, no extra attribute needed) |

#### Polymorphism

| Attribute | Target | Emits in YAML |
|---|---|---|
| `[OpenApiDiscriminator(propertyName)]` on abstract base class | class | `discriminator: { propertyName, mapping }` |
| `[OpenApiSubType(typeof(T), discriminatorValue)]` (multi-use) on base class | class | `oneOf: [$ref Sub1, $ref Sub2]`; sub-types emit `allOf: [$ref Base__Core, {own props}]` |
| C# inheritance (no attribute required) | — | `allOf` on subclass schemas |

#### Document-level

| Attribute | Target | Emits in YAML |
|---|---|---|
| `[assembly: OpenApiInfo(...)]` with `Description`, `Summary`, `TermsOfService`, `ContactName/Url/Email`, `LicenseName/LicenseUrl` | assembly | rich `info` object |
| `[assembly: OpenApiServer(url)]` + optional `Description` (multi-use) | assembly | `servers[]` |
| `[ApiExample]` with `RequestValue` / `ResponseValue` | class/struct | inline `value:` examples (in addition to existing `externalValue`) |
| auto-emitted (fixed in v1.8.0) | — | `components/responses` (BadRequest, Unauthorized, InternalServerError) and `components/securitySchemes` (JwtBearer) |

#### Structural (OpenAPI 3.1)

| Attribute | Target | Emits in YAML |
|---|---|---|
| `[QueryParameter(name, parameterType?)]` + optional `Required`, `Description` (multi-use) | class/struct | `parameters: [{ in: query }]` |
| `[HeaderParameter(name, parameterType?)]` + optional `Required`, `Description` (multi-use) | class/struct | `parameters: [{ in: header }]` |
| `[ResponseHeader(statusCode, name, headerType?)]` + optional `Required`, `Description` (multi-use) | class/struct | `responses.{status}.headers` |
| `[assembly: Webhook(name, typeof(Payload))]` + optional `Method`, `Summary`, `Description` (multi-use) | assembly | top-level `webhooks:` (payload schema registered in `components/schemas`) |
| `[ResponseLink(statusCode, linkId)]` + optional `OperationId`, `Parameters`, `Description` (multi-use) | class/struct | `responses.{status}.links` |
| `[Callback(name)]` + optional `Expression`, `Method`, `Summary`, `PayloadType` (multi-use) | class/struct | operation `callbacks` |

#### Scalar viewer options — `OpenApiScalarViewerOptions`

Set via `OpenApiRendererOptions.ScalarViewer = new OpenApiScalarViewerOptions { ... }`.

| Property | Type | Default | Purpose |
|---|---|---|---|
| `Theme` | `string` | `"default"` | built-in colour theme (`"purple"`, `"blue"`, `"moon"`, `"midnight"`, etc.) |
| `DarkMode` | `bool` | `false` | start in dark mode |
| `Layout` | `string` | `"sidebar"` | `"sidebar"` (three-panel) or `"classic"` (single-column) |
| `HideModels` | `bool` | `false` | hide schemas section from sidebar and content area |
| `HideDownloadButton` | `bool` | `false` | hide the spec download button |
| `HideSidebar` | `bool` | `false` | hide the navigation sidebar on load |
| `HideTestRequestButton` | `bool` | `false` | collapse the "Try it out" panel |
| `DefaultHttpClientTargetKey` | `string?` | `null` | default HTTP client language (`"Shell"`, `"Node"`, `"Python"`, `"Go"`, ...) |
| `DefaultHttpClientClientKey` | `string?` | `null` | client library within the target (e.g. `"curl"`, `"wget"` for Shell) |
| `LocalAssetPath` | `string?` | `null` | local JS bundle path for air-gapped deployments |

### MSBuild properties exposed to the generator

Declared in [src/NativeLambdaRouter.SourceGenerator.OpenApi/build/NativeLambdaRouter.SourceGenerator.OpenApi.props](src/NativeLambdaRouter.SourceGenerator.OpenApi/build/NativeLambdaRouter.SourceGenerator.OpenApi.props).

| Property | Read by | Purpose |
|---|---|---|
| `OpenApiSpecName` | generator | overrides namespace base (`{value}.Generated`) |
| `OpenApiSpecTitle` | generator | overrides YAML `info.title` |
| `OpenApiBrandPrimaryColor`, `OpenApiBrandAccentColor`, `OpenApiBrandLogoUrl`, `OpenApiBrandFavicon`, `OpenApiBrandFontFamily`, `OpenApiBrandThemeJson` | renderer host | F15 branding |
| `OpenApiFooterStatusUrl`, `OpenApiFooterSupportUrl`, `OpenApiFooterChangelogUrl`, `OpenApiFooterSlaUrl`, `OpenApiFooterTermsUrl` | renderer host | F16 footer |
| `OpenApiEnableMermaid`, `OpenApiInlineAssets` | renderer host | F17 diagrams + air-gap |
| `OpenApiServerProduction`, `OpenApiServerSandbox`, `OpenApiDefaultAudience` | reserved | Wave 2/3 (backlog) |

> Renderer-side properties are **reserved** for `IConfiguration`/env binding by the consumer host. They are MSBuild-visible so a future generator version can bake them into the generated spec.

---

## End-to-end walkthrough (5 minutes)

### 1. Install

```xml
<ItemGroup>
  <PackageReference Include="NativeOpenApi" Version="1.8.3" />
  <PackageReference Include="NativeLambdaRouter.SourceGenerator.OpenApi" Version="1.8.3"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

### 2. Annotate commands

```csharp
using Native.OpenApi.Attributes;
using NativeMediator;

[HideFromDocs("ops-only")]                          // F01 — hidden
public sealed class HealthCheckCommand : IRequest<HealthResponse> { }

[Deprecated(                                        // F03 — deprecation banner
    sunset: "2026-12-31",
    alternative: "POST /v2/orders",
    reason: "v1 doesn't support split payments.")]
[ApiExample(                                        // F09 — named examples
    name: "happy-path",
    summary: "Simple order",
    RequestJson = "examples/create-order/happy.json",
    ResponseStatus = 201,
    ResponseJson = "examples/create-order/happy-response.json")]
[ErrorCatalog(typeof(SwepayErrors))]                // F12 — wire error codes
public sealed record CreateOrderCommand(string CustomerId, decimal Amount)
    : IRequest<CreateOrderResponse>;
```

### 3. Declare the error catalog once

```csharp
using Native.OpenApi.Attributes;

public static class SwepayErrors
{
    [ErrorDefinition(
        code: "ORDER_INSUFFICIENT_FUNDS",
        httpStatus: 402,
        userMessage: "Saldo insuficiente no método de pagamento.",
        recovery: "Tente outro método de pagamento ou adicione saldo.",
        DocUrl = "https://docs.swepay.com.br/errors/ORDER_INSUFFICIENT_FUNDS")]
    public const string OrderInsufficientFunds = "ORDER_INSUFFICIENT_FUNDS";
}
```

### 4. Serve docs with branding

```csharp
using Native.OpenApi;
using Native.OpenApi.Rendering;

var options = new OpenApiRendererOptions
{
    Branding = new OpenApiBrandingOptions
    {
        PrimaryColor = "#0A2540",
        AccentColor = "#00D4AA",
        LogoUrl = "https://cdn.swepay.com.br/brand/logo-dark.svg",
        FaviconUrl = "https://cdn.swepay.com.br/brand/favicon.ico"
    },
    Footer = new OpenApiFooterOptions
    {
        StatusUrl = "https://status.swepay.com.br",
        SupportUrl = "https://docs.swepay.com.br/support",
        ChangelogUrl = "https://docs.swepay.com.br/changelog"
    },
    EnableMermaid = true
};

var renderer = new OpenApiHtmlRenderer();
string redocHtml  = renderer.RenderRedoc("/docs/openapi.json",  "My API", options);
string scalarHtml = renderer.RenderScalar("/docs/openapi.json", "My API", options);
```

Legacy two-arg overloads (`RenderRedoc(spec, title)`) stay available — Wave 1 is fully opt-in (RFC principle **O5**).

### 5. Inspect the generated YAML

```csharp
using MyProject.Generated;  // assembly name + ".Generated"

string yaml = GeneratedOpenApiSpec.YamlContent;
int count   = GeneratedOpenApiSpec.EndpointCount;
```

> The generator **does not** emit hidden endpoints into `YamlContent`, but keeps them in the `EndpointList` array so runtime router wiring still works.

---

## Samples

| Sample | Purpose | Demonstrates |
|---|---|---|
| [samples/SampleApiFunction](samples/SampleApiFunction/) | single-Lambda baseline | F01, F03, F09, F12, F13, `.ExcludeFromDocs()`, `[ApiResponse]`; v1.8.0: `ApiDocumentation.cs` with info/servers/tag metadata/tag groups/external docs/webhooks, code samples, badges, stability, schema richness, polymorphic `PaymentMethod` hierarchy, inline examples, query/header params, response headers, links |
| [samples/MultiLambdaSample](samples/MultiLambdaSample/) | multi-Lambda merge + branded Redoc/Scalar | everything above + F15/F16/F17 renderer wiring, multi-partial merge, `AssemblyName=bootstrap` handling |

For the complete reference (humans + agents), start at [samples/MultiLambdaSample/README.md](samples/MultiLambdaSample/README.md).

---

## Repository layout

```
native-open-api/
├── src/
│   ├── Native.OpenApi/                                # library (attributes, models, renderer, linter, loader)
│   │   ├── Attributes/                                # Wave 1 + v1.8.0 attributes (24 files)
│   │   ├── Extensions/OpenApiRouteExtensions.cs       # .ExcludeFromDocs() fluent marker
│   │   ├── Models/SwepayProblemDetails.cs             # canonical problem+json payload (F13)
│   │   ├── Rendering/                                 # OpenApiRendererOptions, branding, footer, OpenApiScalarViewerOptions
│   │   └── OpenApiHtmlRenderer.cs                     # Redoc + Scalar HTML
│   └── NativeLambdaRouter.SourceGenerator.OpenApi/    # Roslyn generator
│       ├── OpenApiSourceGenerator.cs                  # endpoint discovery + attribute wiring
│       ├── OpenApiYamlGenerator.cs                    # YAML emission (F03/F09/F12/F13 + v1.8.0 all features)
│       └── build/*.props                              # CompilerVisibleProperty bindings
├── tests/                                             # xUnit suites (459 tests as of v1.8.0)
├── samples/
│   ├── SampleApiFunction/                             # v1.8.0: ApiDocumentation.cs + full v1.8.0 coverage
│   └── MultiLambdaSample/
└── docs/
    ├── CHANGELOG.md
    ├── TROUBLESHOOTING.md
    └── RFC-DOCUMENTACAO-UX.md
```

---

## Build & test

```bash
git clone https://github.com/swepay/native-open-api.git
cd native-open-api
dotnet build
dotnet test
```

Multi-Lambda sample builds in a specific order because all producers share `AssemblyName=bootstrap`:

```bash
dotnet build samples/MultiLambdaSample/src/Functions.Admin
dotnet build samples/MultiLambdaSample/src/Functions.Identity
dotnet build samples/MultiLambdaSample/src/Functions.OpenId
dotnet build samples/MultiLambdaSample/src/Functions.OpenApi
```

Each producer emits its partial YAML into `Functions.OpenApi/openapi/partials/` via the inline `ExtractOpenApiYaml` task (see [Directory.Build.targets](samples/MultiLambdaSample/Directory.Build.targets)).

---

## Requirements

- .NET 10.0 SDK
- C# 12+

## License

MIT
