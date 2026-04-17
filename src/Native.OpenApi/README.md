# NativeOpenApi

[![NuGet](https://img.shields.io/nuget/v/NativeOpenApi.svg)](https://www.nuget.org/packages/NativeOpenApi)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

OpenAPI 3.1 primitives for Native AOT .NET 10 — document loading, linting,
merging, HTML rendering, and a catalog of UX attributes consumed by
[`NativeLambdaRouter.SourceGenerator.OpenApi`](../NativeLambdaRouter.SourceGenerator.OpenApi/).

- **Version:** `1.7.0`
- **Target:** `net10.0`
- **Namespace root:** `Native.OpenApi`
- **AOT:** zero runtime reflection; serialization through source-generated contexts

---

## API surface at a glance

### Attributes — `Native.OpenApi.Attributes`

| Attribute | Target | Purpose | Wave 1 § |
|---|---|---|---|
| `[HideFromDocs(reason?)]` | class/struct | hide operation from generated YAML | F01 |
| `[Deprecated(sunset, alternative, reason)]` | class/struct | emit `deprecated: true` + `x-sunset` + `x-swepay-alternative` + `x-swepay-deprecation-reason` | F03 |
| `[ApiExample(name, summary) { RequestJson, ResponseStatus, ResponseJson }]` (multi-use) | class/struct | wire named examples into `examples` maps | F09 |
| `[ErrorCatalog(typeof(T))]` | class/struct | attach an error-code catalog to the operation | F12 |
| `[ErrorDefinition(code, httpStatus, userMessage, recovery) { DocUrl }]` | field (`const string`) | one entry inside a catalog | F12 |
| `[ApiResponse(statusCode, responseType?, contentType = "application/json")]` (multi-use) | method (handler's `Handle`) | document per-status responses without the `.Produces<T>()` fluent | v1.6.0 |

### Fluent extension — `Native.OpenApi.Extensions`

| Method | Effect |
|---|---|
| `.ExcludeFromDocs()` | route-level sibling of `[HideFromDocs]`; identity pass-through at runtime, compile-time marker for the generator |

### Models — `Native.OpenApi.Models`

| Type | Role |
|---|---|
| `SwepayProblemDetails` | RFC 9457 superset (`code`, `recovery`, `requestId`); generator auto-injects the matching `components.schemas` entry whenever any operation serves `application/problem+json` without a typed body (F13) |

### Rendering — `Native.OpenApi.Rendering`

| Type | Role |
|---|---|
| `OpenApiRendererOptions` | aggregate; `.Default` = pre-Wave-1 behaviour (no brand, no footer, no Mermaid) |
| `OpenApiBrandingOptions` | `PrimaryColor`, `AccentColor`, `LogoUrl`, `FaviconUrl`, `FontFamily`, `ThemeJsonOverride` |
| `OpenApiFooterOptions` | `StatusUrl`, `SupportUrl`, `ChangelogUrl`, `SlaUrl`, `TermsUrl` |

### Core classes

| Class | Role |
|---|---|
| `OpenApiHtmlRenderer` | renders Redoc + Scalar HTML; each method has a two-arg overload (legacy) and a three-arg `options` overload |
| `OpenApiDocumentLoaderBase` | base for embedded-resource spec loading (JSON + YAML) |
| `OpenApiDocumentMerger` | merges partial specs into a single document |
| `OpenApiDocumentProvider` | orchestrates load → merge → lint |
| `OpenApiLinter` | validates against `OpenApiLintOptions` rules |
| `OpenApiResourceReader` | reads embedded resources from an assembly |
| `IGeneratedOpenApiSpec` | polymorphic access to generator output (implemented automatically when this package is referenced) |

---

## Installation

```bash
dotnet add package NativeOpenApi
```

## Quick reference — Wave 1 (v1.7.0)

### Hide endpoints from docs — F01

```csharp
[HideFromDocs("internal ops endpoint")]
public sealed record InternalHealthCommand(string Secret);
```

Or at the route level (handy when the same command is mapped to several paths and only one should be hidden):

```csharp
using Native.OpenApi.Extensions;

routes.MapGet<ListUsersCommand, ListUsersResponse>("/v1/admin/internal/users", ...)
      .ExcludeFromDocs();
```

**Effect:** the operation never appears in `paths:`. If every operation on a path is hidden, the path itself is dropped.

### Deprecation — F03

```csharp
[Deprecated(
    sunset: "2026-12-31",
    alternative: "POST /v2/orders",
    reason: "v1 doesn't support split payments.")]
public sealed record CreateOrderV1Command(...);
```

**Emits:**

```yaml
deprecated: true
x-sunset: "2026-12-31"
x-swepay-alternative: "POST /v2/orders"
x-swepay-deprecation-reason: "v1 doesn't support split payments."
```

### Named examples — F09

```csharp
[ApiExample(
    name: "happy-path",
    summary: "Simple order with 1 item",
    RequestJson = "examples/create-order/happy.json",
    ResponseStatus = 201,
    ResponseJson = "examples/create-order/happy-response.json")]
[ApiExample(
    name: "validation-error",
    summary: "Invalid CNPJ",
    ResponseStatus = 422,
    ResponseJson = "examples/create-order/invalid-cnpj.json")]
public sealed record CreateOrderCommand(...);
```

Examples reference their JSON payload through `externalValue` (inline payload reading is a Wave 2 follow-up — see [docs/CHANGELOG.md](../../docs/CHANGELOG.md)).

### Error catalog — F12

Declare once:

```csharp
public static class SwepayErrors
{
    [ErrorDefinition(
        code: "PAYMENT_INSUFFICIENT_FUNDS",
        httpStatus: 402,
        userMessage: "Saldo insuficiente no método de pagamento.",
        recovery: "Tente outro método de pagamento ou adicione saldo.",
        DocUrl = "https://docs.swepay.com.br/errors/PAYMENT_INSUFFICIENT_FUNDS")]
    public const string PaymentInsufficientFunds = "PAYMENT_INSUFFICIENT_FUNDS";
}
```

Wire on commands:

```csharp
[ErrorCatalog(typeof(SwepayErrors))]
public sealed record CreatePaymentCommand(...);
```

**Emits at document root:**

```yaml
x-swepay-error-catalog:
  - code: "PAYMENT_INSUFFICIENT_FUNDS"
    httpStatus: 402
    userMessage: "Saldo insuficiente no método de pagamento."
    recovery: "Tente outro método de pagamento ou adicione saldo."
    docUrl: "https://docs.swepay.com.br/errors/PAYMENT_INSUFFICIENT_FUNDS"
```

**Per operation** (the generator slices only codes whose `httpStatus` matches a declared response on that operation):

```yaml
x-swepay-errors:
  - "PAYMENT_INSUFFICIENT_FUNDS"
```

### Canonical `problem+json` schema — F13

When any operation advertises `application/problem+json` **without** a typed body, the generator injects:

```yaml
components:
  schemas:
    SwepayProblemDetails:
      type: object
      properties: { type, title, status, detail, instance, code, recovery, requestId }
      required: [type, title, status, detail, code, recovery, requestId]
```

Two ways to opt in:

```csharp
// (a) fluent — no typed body
routes.MapPost<CreateOrderCommand, CreateOrderResponse>("/v1/orders", ...)
      .ProducesProblem(422);

// (b) handler attribute — null responseType
public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    [ApiResponse(422, null, "application/problem+json")]
    public ValueTask<CreateOrderResponse> Handle(...) => ...;
}
```

### Renderer options — F15 / F16 / F17

```csharp
using Native.OpenApi;
using Native.OpenApi.Rendering;

var options = new OpenApiRendererOptions
{
    Branding = new OpenApiBrandingOptions
    {
        PrimaryColor = "#0A2540",
        AccentColor  = "#00D4AA",
        LogoUrl      = "https://cdn.swepay.com.br/brand/logo-dark.svg",
        FaviconUrl   = "https://cdn.swepay.com.br/brand/favicon.ico",
        FontFamily   = "Inter, Roboto, sans-serif"
    },
    Footer = new OpenApiFooterOptions
    {
        StatusUrl     = "https://status.swepay.com.br",
        SupportUrl    = "https://docs.swepay.com.br/support",
        ChangelogUrl  = "https://docs.swepay.com.br/changelog",
        SlaUrl        = "https://docs.swepay.com.br/sla",
        TermsUrl      = "https://docs.swepay.com.br/terms"
    },
    EnableMermaid        = true,   // F17 — render fenced ```mermaid blocks as SVG
    MermaidFromLocalAsset = false  // true for air-gapped deployments (serves ./assets/mermaid.min.js)
};

var renderer = new OpenApiHtmlRenderer();
var redocHtml  = renderer.RenderRedoc ("/docs/openapi.json", "My API", options);
var scalarHtml = renderer.RenderScalar("/docs/openapi.json", "My API", options);
```

The legacy `RenderRedoc(spec, title)` / `RenderScalar(spec, title)` overloads are preserved (RFC principle **O5**).

**Mermaid in descriptions** — any fenced ` ```mermaid ` block inside `summary`/`description` text becomes inline SVG when `EnableMermaid = true`. Example description body:

````markdown
## Flow

```mermaid
flowchart LR
  A[1. Create realm] --> B[2. Register client] --> C[3. Issue token]
```
````

---

## Minimal document pipeline

### 1. Loader

```csharp
public class MyOpenApiDocumentLoader : OpenApiDocumentLoaderBase
{
    public MyOpenApiDocumentLoader(OpenApiResourceReader reader) : base(reader) { }

    public override IReadOnlyList<OpenApiDocumentPart> LoadCommon() => new[]
    {
        Load("common-schemas",   "openapi/common/schemas.yaml"),
        Load("common-responses", "openapi/common/responses.yaml"),
        Load("common-security",  "openapi/common/security.yaml"),
    };

    public override IReadOnlyList<OpenApiDocumentPart> LoadPartials() => new[]
    {
        Load("users",    "openapi/users/openapi.yaml"),
        Load("products", "openapi/products/openapi.json"),
    };
}
```

### 2. Merger (optional)

```csharp
public class MyOpenApiDocumentMerger : OpenApiDocumentMerger
{
    protected override string GetServerUrl()
        => Environment.GetEnvironmentVariable("ENVIRONMENT") switch
        {
            "prd" => "https://api.swepay.com.br",
            "hml" => "https://sandbox.api.swepay.com.br",
            _     => "https://localhost:5001"
        };

    protected override string GetApiTitle()       => "Swepay API";
    protected override string GetApiDescription() => "Consolidated OpenAPI spec for partner integrations.";
}
```

### 3. Provider

```csharp
var reader   = new OpenApiResourceReader(typeof(Program).Assembly, "MyApp.");
var loader   = new MyOpenApiDocumentLoader(reader);
var merger   = new MyOpenApiDocumentMerger();
var linter   = new OpenApiLinter(OpenApiLintOptions.Empty);
var provider = new OpenApiDocumentProvider(loader, merger, linter);

provider.WarmUp();

var json = provider.Document.Json;
var yaml = provider.Document.Yaml;
```

---

## Linting

```csharp
var options = new OpenApiLintOptions(
    RequiredErrorResponses: ["400", "401", "500"],
    SensitiveFieldNames:    ["password", "token", "secret"],
    DisallowedGenericSegments: ["data", "items"]);

var linter = new OpenApiLinter(options);
```

Checks:

- OpenAPI version is `3.1.0`
- Every path is versioned (e.g. `/v1/`)
- Every operation has a security block (or explicit `security: []` for anonymous)
- Required error responses are declared
- Sensitive fields carry a description

---

## `[ApiResponse]` on handler methods (v1.6.0+)

Co-locate per-status responses with the handler:

```csharp
using Native.OpenApi;
using NativeMediator;

public class GetProductHandler : IRequestHandler<GetProductCommand, GetProductResponse>
{
    [ApiResponse(200, typeof(GetProductResponse))]
    [ApiResponse(404, typeof(ErrorResponse))]
    [ApiResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [ApiResponse(422, null,                  "application/problem+json")]   // → SwepayProblemDetails (F13)
    public ValueTask<GetProductResponse> Handle(GetProductCommand r, CancellationToken ct) { ... }
}
```

| Parameter | Type | Default | Purpose |
|---|---|---|---|
| `statusCode` | `int` | — | HTTP status |
| `responseType` | `Type?` | `null` | typed body; when `null` + `application/problem+json`, the generator points at `SwepayProblemDetails` |
| `contentType` | `string` | `"application/json"` | content type |

---

## Native AOT

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <IsAotCompatible>true</IsAotCompatible>
  <IsTrimmable>true</IsTrimmable>
</PropertyGroup>
```

All JSON/YAML serialization flows through source-generated `JsonSerializerContext`. No runtime reflection on consumer types.

---

## Related

- [NativeLambdaRouter.SourceGenerator.OpenApi](../NativeLambdaRouter.SourceGenerator.OpenApi/) — Roslyn generator that reads the attributes above
- [Root repository](../../) · [Changelog](../../docs/CHANGELOG.md) · [UX RFC](../../docs/RFC-DOCUMENTACAO-UX.md)

## License

MIT
