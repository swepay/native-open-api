# NativeLambdaRouter.SourceGenerator.OpenApi

[![NuGet](https://img.shields.io/nuget/v/NativeLambdaRouter.SourceGenerator.OpenApi.svg)](https://www.nuget.org/packages/NativeLambdaRouter.SourceGenerator.OpenApi)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Roslyn Source Generator that emits OpenAPI 3.1 YAML at compile time from
`NativeLambdaRouter` endpoint maps — zero runtime overhead, zero reflection.

- **Version:** `1.7.0`
- **Target:** `netstandard2.0` (analyzer), consumed by `net10.0` projects
- **Generated artifact:** `{OpenApiSpecName ?? AssemblyName}.Generated.GeneratedOpenApiSpec`
- **Interface:** implements `Native.OpenApi.IGeneratedOpenApiSpec` when that package is referenced

---

## Agent quick-reference

### What the generator inspects

The generator walks the syntax + semantic model of each compilation looking for:

| Surface | Where |
|---|---|
| Route maps | `routes.MapGet<TCommand, TResponse>(...)`, `MapPost`, `MapPut`, `MapPatch`, `MapDelete`, `Map("METHOD", ...)` |
| Fluent chain | `.WithName`, `.WithSummary`, `.WithDescription`, `.WithTags`, `.Accepts`, `.Produces(contentType)`, `.ProducesProblem(statusCode)`, `.AllowAnonymous`, **`.ExcludeFromDocs`** |
| Command attributes | `[EndpointName]`, `[EndpointSummary]`, `[EndpointDescription]`, `[Tags]`, `[Accepts]`, **`[HideFromDocs]`**, **`[Deprecated]`**, **`[ApiExample]`**, **`[ErrorCatalog]`** |
| Catalog attributes | `[ErrorDefinition]` on `const string` fields of the type referenced by `[ErrorCatalog(typeof(T))]` |
| Handler attributes | `[ApiResponse]` on `Handle(...)` of `IRequestHandler<TCommand, TResponse>` implementations |
| Schema types | all `TCommand` / `TResponse` types; records, classes, enums, nullable reference types, arrays, dictionaries |

### What it emits

- `{assembly}.Generated.GeneratedOpenApiSpec` singleton (`public static readonly Instance`) with:
  - `YamlContent` — full OpenAPI 3.1 YAML
  - `EndpointCount` — count of **discovered** endpoints (public + hidden)
  - `EndpointList` — `(Method, Path)[]` of **discovered** endpoints
- YAML `components.schemas.SwepayProblemDetails` whenever any operation serves `application/problem+json` without a typed body (F13)
- Per-operation Wave 1 extensions (F03 deprecation fields, F09 examples map, F12 `x-swepay-errors` slice) plus the root `x-swepay-error-catalog`

### Precedence rule

Fluent chain **wins** over command attributes when both are present — matches ASP.NET Core Minimal APIs.

---

## Installation

```xml
<ItemGroup>
  <PackageReference Include="NativeLambdaRouter.SourceGenerator.OpenApi"
                    Version="1.7.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <!-- Recommended: also reference the library for attributes and the IGeneratedOpenApiSpec interface -->
  <PackageReference Include="NativeOpenApi" Version="1.7.0" />
</ItemGroup>
```

> The generator is a build-time-only analyzer. `ReferenceOutputAssembly="false"` prevents the analyzer DLL from being published with the Lambda binary.

---

## Supported map methods

| Method | HTTP verb |
|---|---|
| `MapGet<TCommand, TResponse>(path, factory)` | `GET` |
| `MapPost<TCommand, TResponse>(path, factory)` | `POST` |
| `MapPut<TCommand, TResponse>(path, factory)` | `PUT` |
| `MapPatch<TCommand, TResponse>(path, factory)` | `PATCH` |
| `MapDelete<TCommand, TResponse>(path, factory)` | `DELETE` |
| `Map<TCommand, TResponse>("METHOD", path, factory)` | arbitrary (e.g. `OPTIONS`) |

Path parameters (`{id}`) are extracted into `parameters: [{ in: path, required: true, schema: { type: string } }]`.

---

## Wave 1 (v1.7.0) — UX attributes

Full reference: [../Native.OpenApi/README.md](../Native.OpenApi/README.md) · Root: [../../README.md](../../README.md)

| Attribute / call | Generated YAML field(s) |
|---|---|
| `[HideFromDocs]` / `.ExcludeFromDocs()` | operation omitted |
| `[Deprecated(sunset, alternative, reason)]` | `deprecated: true`, `x-sunset`, `x-swepay-alternative`, `x-swepay-deprecation-reason` |
| `[ApiExample(name, summary) { RequestJson, ResponseStatus, ResponseJson }]` | `requestBody.content.*.examples.{name}` and/or `responses.{status}.content.*.examples.{name}` (via `externalValue`) |
| `[ErrorCatalog(typeof(T))]` + `[ErrorDefinition]` fields | root `x-swepay-error-catalog` + per-op `x-swepay-errors` (codes filtered by declared response statuses) |
| any `application/problem+json` response without typed body | `$ref: "#/components/schemas/SwepayProblemDetails"` + canonical schema injection |

---

## Fluent metadata — pre-Wave-1 reference

```csharp
routes.MapGet<GetClientsCommand, GetClientsResponse>("/v1/clients", ctx => new GetClientsCommand())
    .WithName("ListAllClients")
    .WithSummary("Retrieve all clients")
    .WithDescription("Returns a paginated list of registered clients.")
    .WithTags("Clients", "Admin")
    .ProducesProblem(422);
```

| Method | Effect |
|---|---|
| `.WithName("id")` | `operationId: id` |
| `.WithSummary("text")` | `summary: text` |
| `.WithDescription("text")` | `description: text` (Markdown allowed; fenced ` ```mermaid ` blocks render when the host enables F17) |
| `.WithTags("A", "B")` | overrides auto-generated tags |
| `.Accepts("contentType")` | sets request body content type (default `application/json`) |
| `.ProducesProblem(statusCode)` | adds an `application/problem+json` response — points at `SwepayProblemDetails` (F13) |
| `.AllowAnonymous()` | emits `security: []` (OpenAPI 3.1 convention) |
| `.ExcludeFromDocs()` | **F01** — omits this operation from the generated spec |

---

## Attribute-based metadata on `TCommand`

```csharp
using NativeLambdaRouter.OpenApi.Attributes;
using Native.OpenApi.Attributes;

[EndpointName("ListAllClients")]
[EndpointSummary("Retrieve all clients")]
[EndpointDescription("Returns a paginated list of registered clients.")]
[Tags("Clients", "Admin")]
[Deprecated(sunset: "2026-12-31", alternative: "GET /v2/clients", reason: "no pagination.")]
[ErrorCatalog(typeof(SwepayErrors))]
[ApiExample("happy-path", "First page of clients",
    RequestJson = "examples/list-clients/happy.json",
    ResponseStatus = 200,
    ResponseJson = "examples/list-clients/happy-response.json")]
public sealed record GetClientsCommand;
```

| Namespace | Attributes |
|---|---|
| `NativeLambdaRouter.OpenApi.Attributes` | `EndpointName`, `EndpointSummary`, `EndpointDescription`, `Tags`, `Accepts` |
| `Native.OpenApi.Attributes` | `HideFromDocs`, `Deprecated`, `ApiExample`, `ErrorCatalog`, `ErrorDefinition`, `ApiResponse` |

---

## `[ApiResponse]` on handlers (v1.6.0+)

```csharp
using Native.OpenApi;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    [ApiResponse(201, typeof(CreateOrderResponse))]
    [ApiResponse(400, typeof(ValidationProblem), "application/problem+json")]
    [ApiResponse(422, null,                      "application/problem+json")]   // → SwepayProblemDetails
    [ApiResponse(500, typeof(ProblemDetails),    "application/problem+json")]
    public async ValueTask<CreateOrderResponse> Handle(CreateOrderCommand r, CancellationToken ct) { ... }
}
```

The generator discovers `IRequestHandler<TCommand, TResponse>` via the semantic model and merges the handler's `[ApiResponse]` list with the fluent-chain `.ProducesProblem(...)` responses. When a problem+json response overlaps the default 400/401/500 responses, the declared one **replaces** the default `$ref`.

---

## Form-encoded bodies (v1.5.1+)

```csharp
[Accepts("application/x-www-form-urlencoded")]
public sealed record RefreshTokenCommand(string RealmId, string ClientId, string RefreshToken, string? Scope);
```

Or via fluent:

```csharp
routes.MapPost<RefreshTokenCommand, TokenResponse>("/v1/realms/{realm}/protocol/openid-connect/refresh", ...)
    .Accepts("application/x-www-form-urlencoded");
```

The generator emits an inline schema (all fields `type: string`, nullables excluded from `required`) instead of a `$ref` — matches the OAuth2 token endpoint convention.

---

## Schema introspection

| C# type | OpenAPI |
|---|---|
| `string` | `type: string` |
| `int`, `long`, `short`, `byte` | `type: integer` + format |
| `float`, `double`, `decimal` | `type: number` + format |
| `bool` | `type: boolean` |
| `DateTime`, `DateTimeOffset` | `type: string` + `format: date-time` |
| `DateOnly` | `type: string` + `format: date` |
| `Guid` | `type: string` + `format: uuid` |
| `Uri` | `type: string` + `format: uri` |
| `List<T>`, `T[]`, `IReadOnlyList<T>` | `type: array` + `items` |
| `Dictionary<K, V>` | `type: object` |
| `enum` | `type: string` + `enum: [...]` |
| complex types | `$ref: "#/components/schemas/{TypeName}"` |
| nullable reference / `Nullable<T>` | excluded from `required` |

---

## MSBuild properties

Shipped via `build/NativeLambdaRouter.SourceGenerator.OpenApi.props` (auto-imported when the package is added as a NuGet dependency).

| Property | Read by | Default | Purpose |
|---|---|---|---|
| `OpenApiSpecName` | generator | `AssemblyName` | namespace base → `{value}.Generated` |
| `OpenApiSpecTitle` | generator | `OpenApiSpecName` (dots → spaces) | `info.title` in YAML |
| `OpenApiBrandPrimaryColor` / `AccentColor` / `LogoUrl` / `Favicon` / `FontFamily` / `ThemeJson` | renderer host | — | F15 branding |
| `OpenApiFooterStatusUrl` / `SupportUrl` / `ChangelogUrl` / `SlaUrl` / `TermsUrl` | renderer host | — | F16 footer |
| `OpenApiEnableMermaid`, `OpenApiInlineAssets` | renderer host | `false` | F17 Mermaid + air-gap |
| `OpenApiServerProduction`, `OpenApiServerSandbox`, `OpenApiDefaultAudience` | reserved | — | Wave 2/3 |

> **Using `ProjectReference` instead of NuGet?** `.props` is not auto-imported. Add a `Directory.Build.props` with the same `<CompilerVisibleProperty Include="..." />` items. See [samples/MultiLambdaSample/Directory.Build.props](../../samples/MultiLambdaSample/Directory.Build.props).

### AWS Lambda `AssemblyName=bootstrap` recipe

Multiple Lambda projects with `AssemblyName=bootstrap` collide on the default `bootstrap.Generated` namespace. Override per producer:

```xml
<PropertyGroup>
  <AssemblyName>bootstrap</AssemblyName>
  <OpenApiSpecName>Swepay.Functions.Admin</OpenApiSpecName>
  <OpenApiSpecTitle>Admin API</OpenApiSpecTitle>
</PropertyGroup>
```

---

## Accessing the generated spec

```csharp
using MyProject.Generated;

string yaml = GeneratedOpenApiSpec.YamlContent;
int    n    = GeneratedOpenApiSpec.EndpointCount;

foreach (var (method, path) in GeneratedOpenApiSpec.EndpointList)
    Console.WriteLine($"{method} {path}");

// Polymorphic — requires the NativeOpenApi package
Native.OpenApi.IGeneratedOpenApiSpec spec = GeneratedOpenApiSpec.Instance;
```

> `EndpointList` exposes **all** discovered endpoints, including those hidden by `[HideFromDocs]` / `.ExcludeFromDocs()`. Hidden endpoints are only absent from the YAML — runtime router wiring still sees them.

---

## Multi-project architecture

Each producer project gets its own `GeneratedOpenApiSpec` under its namespace:

```
Functions.Admin      → Functions.Admin.Generated.GeneratedOpenApiSpec
Functions.Identity   → Functions.Identity.Generated.GeneratedOpenApiSpec
Functions.OpenId     → Functions.OpenId.Generated.GeneratedOpenApiSpec
```

A consolidator project merges them with `NativeOpenApi`:

```csharp
public class ConsolidatedOpenApiDocumentLoader : OpenApiDocumentLoaderBase
{
    public ConsolidatedOpenApiDocumentLoader(OpenApiResourceReader reader) : base(reader) { }

    public override IReadOnlyList<OpenApiDocumentPart> LoadCommon() => new[]
    {
        Load("schemas",   "openapi/schemas.yaml"),
        Load("responses", "openapi/responses.yaml"),
        Load("security",  "openapi/security.yaml"),
    };

    public override IReadOnlyList<OpenApiDocumentPart> LoadPartials() => new[]
    {
        LoadFromGeneratedSpec("admin",    Functions.Admin.Generated.GeneratedOpenApiSpec.Instance),
        LoadFromGeneratedSpec("identity", Functions.Identity.Generated.GeneratedOpenApiSpec.Instance),
        LoadFromGeneratedSpec("openid",   Functions.OpenId.Generated.GeneratedOpenApiSpec.Instance),
    };
}
```

Full working example: [samples/MultiLambdaSample](../../samples/MultiLambdaSample/).

---

## Debug: write generated `.cs` files to disk

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Output path:

```
obj/{Configuration}/{TargetFramework}/generated/NativeLambdaRouter.SourceGenerator.OpenApi/
    NativeLambdaRouter.SourceGenerator.OpenApi.OpenApiSourceGenerator/
        GeneratedOpenApiSpec.g.cs
```

> **Not required for the generator to work** — the generated class is injected directly into the compilation. `EmitCompilerGeneratedFiles` only produces a physical copy for inspection.

---

## Related

- [NativeOpenApi](../Native.OpenApi/) — library + renderer + attributes
- [Repository root](../../) · [Changelog](../../docs/CHANGELOG.md) · [UX RFC](../../docs/RFC-DOCUMENTACAO-UX.md)

## License

MIT
