# Native.OpenApi

[![Build Status](https://github.com/swepay/native-open-api/actions/workflows/dotnet.yml/badge.svg)](https://github.com/swepay/native-open-api/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Compile-time OpenAPI 3.1 for Native AOT .NET 10 APIs. Zero runtime reflection.
Ships two NuGet packages + a Roslyn Source Generator.

- **Current version:** `1.7.0`
- **Target:** `net10.0` (library) / `netstandard2.0` (generator)
- **AOT:** `PublishAot=true`, `IsTrimmable=true`, no runtime reflection
- **OpenAPI:** 3.1-only
- **Canonical docs:** [src/Native.OpenApi/README.md](src/Native.OpenApi/README.md) · [src/NativeLambdaRouter.SourceGenerator.OpenApi/README.md](src/NativeLambdaRouter.SourceGenerator.OpenApi/README.md)
- **Changelog:** [docs/CHANGELOG.md](docs/CHANGELOG.md) · **UX RFC:** [docs/RFC-DOCUMENTACAO-UX.md](docs/RFC-DOCUMENTACAO-UX.md)

---

## Agent quick-reference

> If you are an agent reading this file to plan a change, **start here**.

### Packages

| Package | Where it runs | Install | Docs |
|---|---|---|---|
| `NativeOpenApi` | Runtime + build-time (library + source references) | `dotnet add package NativeOpenApi` | [README](src/Native.OpenApi/README.md) |
| `NativeLambdaRouter.SourceGenerator.OpenApi` | Build-time only (Roslyn analyzer) | `dotnet add package NativeLambdaRouter.SourceGenerator.OpenApi` | [README](src/NativeLambdaRouter.SourceGenerator.OpenApi/README.md) |

### What each package is for

- **`NativeOpenApi`** ships: attributes (`[HideFromDocs]`, `[Deprecated]`, `[ApiExample]`, `[ErrorCatalog]`, `[ErrorDefinition]`, `[ApiResponse]`), models (`SwepayProblemDetails`), document primitives (`OpenApiDocument`, `OpenApiDocumentLoader`, `OpenApiDocumentMerger`, `OpenApiDocumentProvider`, `OpenApiLinter`), and renderer (`OpenApiHtmlRenderer` + `OpenApiRendererOptions` for branding/footer/Mermaid).
- **`NativeLambdaRouter.SourceGenerator.OpenApi`** reads `MapGet/MapPost/MapPut/MapPatch/MapDelete/Map` calls on `IRouteBuilder`, plus the attributes above, and emits a `GeneratedOpenApiSpec : IGeneratedOpenApiSpec` singleton with the full YAML at compile time.

### Decision tree

| You want to… | Do this | Files to edit |
|---|---|---|
| Hide an endpoint from docs | Add `[HideFromDocs]` on `TCommand` **or** `.ExcludeFromDocs()` on the mapping | your `Commands.cs` / `Function.cs` |
| Mark an endpoint deprecated | `[Deprecated(sunset, alternative, reason)]` on `TCommand` | your `Commands.cs` |
| Add named request/response examples | `[ApiExample(name, summary) { RequestJson, ResponseStatus, ResponseJson }]` on `TCommand` (multi-use) | your `Commands.cs` + embedded JSON files |
| Centralise error codes | Create a `static class SwepayErrors` with `[ErrorDefinition]` consts, annotate commands with `[ErrorCatalog(typeof(SwepayErrors))]` | new `SwepayErrors.cs` + your `Commands.cs` |
| Use the canonical `problem+json` schema | Advertise a response with no typed body: `.ProducesProblem(400)` or `[ApiResponse(422, null, "application/problem+json")]` | your `Function.cs` / handler |
| Brand the Redoc/Scalar page | Instantiate `OpenApiRendererOptions` with `Branding`, `Footer`, `EnableMermaid`; pass to `OpenApiHtmlRenderer.Render*(spec, title, options)` | the project that hosts `/docs/*` |
| Draw a diagram inside a description | Put a fenced ` ```mermaid ` block in the `description` text and enable `options.EnableMermaid` | the `[EndpointDescription]` or `.WithDescription(...)` |
| Override the generated namespace | Set MSBuild property `OpenApiSpecName` in the producer `.csproj` | `*.csproj` |

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
  <PackageReference Include="NativeOpenApi" Version="1.7.0" />
  <PackageReference Include="NativeLambdaRouter.SourceGenerator.OpenApi" Version="1.7.0"
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
| [samples/SampleApiFunction](samples/SampleApiFunction/) | single-Lambda baseline | F01, F03, F09, F12, F13, `.ExcludeFromDocs()`, `[ApiResponse]` |
| [samples/MultiLambdaSample](samples/MultiLambdaSample/) | multi-Lambda merge + branded Redoc/Scalar | everything above + F15/F16/F17 renderer wiring, multi-partial merge, `AssemblyName=bootstrap` handling |

For the complete reference (humans + agents), start at [samples/MultiLambdaSample/README.md](samples/MultiLambdaSample/README.md).

---

## Repository layout

```
native-open-api/
├── src/
│   ├── Native.OpenApi/                                # library (attributes, models, renderer, linter, loader)
│   │   ├── Attributes/                                # Wave 1 attributes
│   │   ├── Extensions/OpenApiRouteExtensions.cs       # .ExcludeFromDocs() fluent marker
│   │   ├── Models/SwepayProblemDetails.cs             # canonical problem+json payload (F13)
│   │   ├── Rendering/                                 # OpenApiRendererOptions, branding, footer
│   │   └── OpenApiHtmlRenderer.cs                     # Redoc + Scalar HTML
│   └── NativeLambdaRouter.SourceGenerator.OpenApi/    # Roslyn generator
│       ├── OpenApiSourceGenerator.cs                  # endpoint discovery + attribute wiring
│       ├── OpenApiYamlGenerator.cs                    # YAML emission (F03/F09/F12/F13)
│       └── build/*.props                              # CompilerVisibleProperty bindings
├── tests/                                             # xUnit suites (90 + 98 tests)
├── samples/
│   ├── SampleApiFunction/
│   └── MultiLambdaSample/
└── docs/
    ├── CHANGELOG.md
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
