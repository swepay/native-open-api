# NativeLambdaRouter.SourceGenerator.OpenApi

[![NuGet](https://img.shields.io/nuget/v/NativeLambdaRouter.SourceGenerator.OpenApi.svg)](https://www.nuget.org/packages/NativeLambdaRouter.SourceGenerator.OpenApi)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A Roslyn Source Generator that automatically generates OpenAPI 3.1 specifications from NativeLambdaRouter endpoint configurations at compile time.

## Features

- **Compile-Time Generation**: OpenAPI specs are generated during build, not at runtime
- **Zero Runtime Overhead**: No reflection or dynamic code generation
- **NativeLambdaRouter Integration**: Automatic discovery of `MapGet`, `MapPost`, etc. endpoints
- **Type-Safe**: Extracts request/response types from generic parameters
- **Schema Property Introspection**: Generates real `properties` and `required` from C# record/class types
- **Nullable-Aware**: Nullable properties are excluded from `required` arrays
- **Metadata & Extensions**: Fluent chain methods (`.WithName()`, `.WithSummary()`, `.ProducesProblem()`, etc.) and attribute-based metadata, plus `[ApiResponse]` attributes on handlers
- **OpenAPI 3.1 Compliant**: Generates valid OpenAPI 3.1 YAML specifications

## Installation

```bash
dotnet add package NativeLambdaRouter.SourceGenerator.OpenApi
```

## Quick Start

### 1. Define your routes

The generator automatically discovers endpoints in your `ConfigureRoutes` method:

```csharp
public class MyRouter : RoutedApiGatewayFunction
{
    protected override void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemsCommand, GetItemsResponse>("/v1/items", ctx => new GetItemsCommand())
            .WithTags("Items")
            .WithSummary("List all items");

        routes.MapPost<CreateItemCommand, CreateItemResponse>("/v1/items", ctx => Deserialize<CreateItemCommand>(ctx.Body!))
            .WithName("CreateItem")
            .WithDescription("Creates a new item in the catalog");

        routes.MapGet<GetItemByIdCommand, GetItemByIdResponse>("/v1/items/{id}", ctx => new GetItemByIdCommand(ctx.PathParameters["id"]));

        routes.MapPut<UpdateItemCommand, UpdateItemResponse>("/v1/items/{id}", ctx => Deserialize<UpdateItemCommand>(ctx.Body!));
        routes.MapDelete<DeleteItemCommand, DeleteItemResponse>("/v1/items/{id}", ctx => new DeleteItemCommand(ctx.PathParameters["id"]));
    }
}
```

### 2. Build your project

The generator runs automatically during compilation. No additional configuration needed!

### 3. Access the generated spec

The class is generated in the `{AssemblyName}.Generated` namespace:

```csharp
using MyProject.Generated;

// Get the full OpenAPI YAML specification
string yaml = GeneratedOpenApiSpec.YamlContent;

// Get endpoint count
int count = GeneratedOpenApiSpec.EndpointCount;

// Get list of all endpoints
foreach (var (method, path) in GeneratedOpenApiSpec.EndpointList)
{
    Console.WriteLine($"{method} {path}");
}

// Use as IGeneratedOpenApiSpec (when NativeOpenApi is referenced)
IGeneratedOpenApiSpec spec = GeneratedOpenApiSpec.Instance;
```

## Generated Output

The generator creates a `GeneratedOpenApiSpec` class in the `{AssemblyName}.Generated` namespace.

When `NativeOpenApi` is referenced, it implements `IGeneratedOpenApiSpec`:

```csharp
// For assembly "MyProject" with NativeOpenApi referenced
namespace MyProject.Generated;

public sealed class GeneratedOpenApiSpec : Native.OpenApi.IGeneratedOpenApiSpec
{
    public static readonly GeneratedOpenApiSpec Instance = new();

    public const string YamlContent = @"
openapi: ""3.1.0""
info:
  title: ""MyProject""
  version: ""1.0.0""
paths:
  /v1/items:
    get:
      operationId: getV1Items
      ...
";

    public const int EndpointCount = 5;
    
    public static readonly (string Method, string Path)[] EndpointList = new[]
    {
        ("DELETE", "/v1/items/{id}"),
        ("GET", "/v1/items"),
        ("GET", "/v1/items/{id}"),
        ("POST", "/v1/items"),
        ("PUT", "/v1/items/{id}"),
    };
}
```

Without `NativeOpenApi`, the class is standalone (no interface).

## Supported Mapping Methods

The generator detects all NativeLambdaRouter mapping methods:

| Method | HTTP Verb |
|--------|-----------|
| `MapGet<TCommand, TResponse>` | GET |
| `MapPost<TCommand, TResponse>` | POST |
| `MapPut<TCommand, TResponse>` | PUT |
| `MapDelete<TCommand, TResponse>` | DELETE |
| `MapPatch<TCommand, TResponse>` | PATCH |
| `Map<TCommand, TResponse>` | Custom (first argument) |

## Generated Features

For each endpoint, the generator creates:

- **Operation ID**: Auto-generated from HTTP method and path (e.g., `getV1Items`), or customized via `.WithName()` / `[EndpointName]`
- **Summary**: Generated from command type name, or customized via `.WithSummary()` / `[EndpointSummary]`
- **Description**: Included when set via `.WithDescription()` / `[EndpointDescription]`
- **Tags**: Extracted from path segments for grouping, or customized via `.WithTags()` / `[Tags]`
- **Security**: JWT Bearer authentication by default; `security: []` for anonymous endpoints (`.AllowAnonymous()`)
- **Parameters**: Path parameters extracted from route template
- **Request Body**: For POST, PUT, PATCH methods with schema reference
- **Responses**: Success response with schema + standard error responses + additional responses via `[ApiResponse]` attributes or `.ProducesProblem()`

## Metadata & Extensions (v1.5.0+)

The generator supports two ways to customize the generated OpenAPI spec for each endpoint: **fluent chain methods** and **attributes**. Both follow ASP.NET Core Minimal APIs patterns.

### Fluent Chain Methods

Chain methods directly on `MapGet`, `MapPost`, etc.:

```csharp
routes.MapGet<GetClientsCommand, GetClientsResponse>("/v1/clients", ctx => new GetClientsCommand())
    .WithName("ListAllClients")              // custom operationId
    .WithSummary("Retrieve all clients")     // custom summary
    .WithDescription("Returns a paginated list of registered clients") // description
    .WithTags("Clients", "Admin")            // custom tags (overrides auto-generated)
    .ProducesProblem(422);                   // problem+json error response
```

| Method | Effect |
|--------|--------|
| `.WithName("id")` | Sets `operationId` in the YAML |
| `.WithSummary("text")` | Sets `summary` in the YAML |
| `.WithDescription("text")` | Adds `description` field to the operation |
| `.WithTags("A", "B")` | Overrides auto-generated `tags` |
| `.Accepts("contentType")` | Sets the request body content type (default: `application/json`) |
| `.ProducesProblem(statusCode)` | Adds a `application/problem+json` error response |

For additional typed responses, use the `[ApiResponse]` attribute on handler methods (see Handler-Based Response Attributes section below).

### Attribute-Based Metadata

Apply attributes on the **TCommand** type as an alternative to fluent methods:

```csharp
using NativeLambdaRouter.OpenApi.Attributes;

[EndpointName("ListAllClients")]
[EndpointSummary("Retrieve all clients")]
[EndpointDescription("Returns a paginated list of registered clients")]
[Tags("Clients", "Admin")]
public class GetClientsCommand { }
```

| Attribute | Equivalent Fluent Method |
|-----------|--------------------------|
| `[EndpointName("id")]` | `.WithName("id")` |
| `[EndpointSummary("text")]` | `.WithSummary("text")` |
| `[EndpointDescription("text")]` | `.WithDescription("text")` |
| `[Tags("A", "B")]` | `.WithTags("A", "B")` |
| `[Accepts("contentType")]` | `.Accepts("contentType")` |

### Precedence

When both fluent chain and attributes are set for the same endpoint, **fluent chain takes precedence**, following ASP.NET Core conventions:

```csharp
[EndpointName("FromAttribute")]     // ← ignored
[Tags("AttrTag")]                    // ← ignored
public class ListClientsCommand { }

routes.MapGet<ListClientsCommand, ListClientsResponse>("/v1/clients", ctx => new())
    .WithName("FromFluentChain")     // ← wins → operationId: FromFluentChain
    .WithTags("FluentTag");          // ← wins → tags: [FluentTag]
```

### Handler-Based Response Attributes (v1.6.0+)

Document responses directly on handler methods using the `[ApiResponse]` attribute from the `Native.OpenApi` package:

```csharp
using Native.OpenApi;
using NativeMediator;

public class GetProductHandler : IRequestHandler<GetProductCommand, GetProductResponse>
{
    [ApiResponse(200, typeof(GetProductResponse), "application/json")]
    [ApiResponse(404, typeof(ErrorResponse), "application/json")]
    [ApiResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [ApiResponse(500, typeof(ProblemDetails), "application/problem+json")]
    public ValueTask<GetProductResponse> Handle(GetProductCommand request, CancellationToken cancellationToken)
    {
        // ... implementation
    }
}
```

**Benefits:**
- **Co-located Documentation**: Response definitions live next to the handler implementation
- **Type-Safe**: Response types are verified at compile time
- **Automatic Discovery**: The Source Generator automatically finds handlers for each command and extracts `[ApiResponse]` attributes
- **Complementary**: Combines with fluent chain method `.ProducesProblem()` for problem details responses

**How it works:**
1. The Source Generator finds `IRequestHandler<TCommand, TResponse>` implementations
2. It reads `[ApiResponse]` attributes from the `Handle` method
3. Response definitions are merged into the generated OpenAPI specification

**Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `statusCode` | `int` | Yes | - | HTTP status code (200, 404, 500, etc.) |
| `responseType` | `Type?` | No | `null` | Response body type. Null for no body (204, 404) |
| `contentType` | `string` | No | `"application/json"` | Content type of the response |

**Example:**

```csharp
// Handler with multiple response types
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    [ApiResponse(201, typeof(CreateOrderResponse), "application/json")]
    [ApiResponse(400, typeof(ValidationProblem), "application/problem+json")]
    [ApiResponse(409, typeof(ConflictProblem), "application/problem+json")]
    [ApiResponse(500, typeof(ProblemDetails), "application/problem+json")]
    public async ValueTask<CreateOrderResponse> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        // ... implementation
    }
}
```

This generates:

```yaml
responses:
  "201":
    description: Created
    content:
      application/json:
        schema:
          $ref: "#/components/schemas/CreateOrderResponse"
  "400":
    description: Bad Request
    content:
      application/problem+json:
        schema:
          $ref: "#/components/schemas/ValidationProblem"
  "409":
    description: Conflict
    content:
      application/problem+json:
        schema:
          $ref: "#/components/schemas/ConflictProblem"
  "500":
    description: Internal Server Error
    content:
      application/problem+json:
        schema:
          $ref: "#/components/schemas/ProblemDetails"
```

> **Note:** When `.ProducesProblem(statusCode)` overlaps with a default error response (400, 401, 500), the custom `application/problem+json` response **replaces** the default `$ref` — no duplicates.

### Form-Encoded Request Bodies (v1.5.1+)

Use `.Accepts("application/x-www-form-urlencoded")` for endpoints that receive form data instead of JSON (e.g., OAuth2 token endpoints):

```csharp
routes.MapPost<RefreshTokenCommand, TokenResponse>(
    "/v1/realms/{realm}/protocol/openid-connect/refresh",
    ctx => new RefreshTokenCommand(...))
    .WithName("RefreshToken")
    .WithSummary("Refresh token endpoint")
    .WithTags("OAuth2")
    .Accepts("application/x-www-form-urlencoded")
    .ProducesProblem(400)
    .ProducesProblem(401)
    .AllowAnonymous();
```

Or via attribute on the TCommand:

```csharp
[Accepts("application/x-www-form-urlencoded")]
public sealed record RefreshTokenCommand(
    string RealmId,
    string ClientId,
    string RefreshToken,
    string? ClientSecret,
    string? Scope);
```

Generates an inline form schema instead of `$ref`:

```yaml
requestBody:
  required: true
  content:
    application/x-www-form-urlencoded:
      schema:
        type: object
        properties:
          realmId:
            type: string
          clientId:
            type: string
          refreshToken:
            type: string
          clientSecret:
            type: string
          scope:
            type: string
        required:
          - realmId
          - clientId
          - refreshToken
```

> **Key differences from JSON request bodies:**
> - Content type is `application/x-www-form-urlencoded` instead of `application/json`
> - Schema is emitted **inline** (not via `$ref`), since form fields are always strings
> - All properties are typed as `type: string` (HTTP forms transmit everything as text)
> - Nullable properties are excluded from the `required` array

## Schema Property Generation

The generator introspects C# types via Roslyn to produce real OpenAPI schemas with properties, types, formats, and required fields.

### Supported Type Mappings

| C# Type | OpenAPI Type | Format |
|---------|-------------|--------|
| `string` | `string` | — |
| `int` | `integer` | `int32` |
| `long` | `integer` | `int64` |
| `float` | `number` | `float` |
| `double` | `number` | `double` |
| `decimal` | `number` | `double` |
| `bool` | `boolean` | — |
| `DateTime`, `DateTimeOffset` | `string` | `date-time` |
| `DateOnly` | `string` | `date` |
| `Guid` | `string` | `uuid` |
| `Uri` | `string` | `uri` |
| `List<T>`, `T[]`, `IReadOnlyList<T>` | `array` | items: `{T}` |
| `Dictionary<K,V>` | `object` | — |
| Enum types | `string` | `enum: [values]` |
| Complex types | — | `$ref: "#/components/schemas/TypeName"` |

### Example

```csharp
public sealed record CreateRoleRequest(
    string RealmId,
    string Name,
    string? Description,        // nullable → not in required
    List<string>? PermissionIds, // nullable array → not in required
    string PerformedBy);

public sealed record CreateRoleResponse(string Id, string Name);
```

Generates:

```yaml
components:
  schemas:
    CreateRoleRequest:
      type: object
      properties:
        realmId:
          type: string
        name:
          type: string
        description:
          type: string
        permissionIds:
          type: array
          items:
            type: string
        performedBy:
          type: string
      required:
        - realmId
        - name
        - performedBy
    CreateRoleResponse:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
      required:
        - id
        - name
```

## Integration with NativeOpenApi

Combine with `NativeOpenApi` for a complete solution:

```csharp
using Native.OpenApi;
using MyProject.Generated;

// Use the generated spec as part of your document loading
public class MyOpenApiDocumentLoader : OpenApiDocumentLoaderBase
{
    public MyOpenApiDocumentLoader(OpenApiResourceReader reader) : base(reader) { }

    public override IReadOnlyList<OpenApiDocumentPart> LoadCommon() => [];

    public override IReadOnlyList<OpenApiDocumentPart> LoadPartials()
    {
        return new List<OpenApiDocumentPart>
        {
            // Load from generated spec via IGeneratedOpenApiSpec
            LoadFromGeneratedSpec("my-api", GeneratedOpenApiSpec.Instance),
            // Load additional partials
            Load("schemas", "openapi/schemas.yaml")
        };
    }
}
```

### Multi-Project Architecture

The generator uses the **assembly name** as namespace, so each project gets its own unique class:

```
Functions.Admin      → namespace Functions.Admin.Generated      → GeneratedOpenApiSpec
Functions.Identity   → namespace Functions.Identity.Generated   → GeneratedOpenApiSpec
Functions.OpenId     → namespace Functions.OpenId.Generated     → GeneratedOpenApiSpec
```

When `NativeOpenApi` is referenced, the generated class implements `IGeneratedOpenApiSpec`,
enabling the `Functions.OpenApi` project to merge all specs polymorphically:

```csharp
// In Functions.OpenApi — merges all generated specs into a single document
public class ConsolidatedOpenApiDocumentLoader : OpenApiDocumentLoaderBase
{
    public ConsolidatedOpenApiDocumentLoader(OpenApiResourceReader reader) : base(reader) { }

    public override IReadOnlyList<OpenApiDocumentPart> LoadCommon()
    {
        return [Load("schemas", "openapi/schemas.yaml"),
                Load("responses", "openapi/responses.yaml"),
                Load("security", "openapi/security.yaml")];
    }

    public override IReadOnlyList<OpenApiDocumentPart> LoadPartials()
    {
        return [LoadFromGeneratedSpec("admin", Functions.Admin.Generated.GeneratedOpenApiSpec.Instance),
                LoadFromGeneratedSpec("identity", Functions.Identity.Generated.GeneratedOpenApiSpec.Instance),
                LoadFromGeneratedSpec("openid", Functions.OpenId.Generated.GeneratedOpenApiSpec.Instance)];
    }
}
```

Each Function project only needs:

```xml
<!-- Functions.Admin.csproj, Functions.Identity.csproj, Functions.OpenId.csproj -->
<PackageReference Include="NativeLambdaRouter.SourceGenerator.OpenApi" Version="1.5.0" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
<PackageReference Include="NativeOpenApi" Version="1.5.0" />
```

And the consolidator project:

```xml
<!-- Functions.OpenApi.csproj -->
<PackageReference Include="NativeOpenApi" Version="1.5.0" />
<ProjectReference Include="..\Functions.Admin\Functions.Admin.csproj" />
<ProjectReference Include="..\Functions.Identity\Functions.Identity.csproj" />
<ProjectReference Include="..\Functions.OpenId\Functions.OpenId.csproj" />
```

### Custom Namespace with `OpenApiSpecName` (v1.3.1+)

By default the generator uses the **assembly name** as the namespace base. This works fine in most cases, but **AWS Lambda custom runtime projects** typically set `AssemblyName=bootstrap` for all functions — causing namespace collisions.

Use the `OpenApiSpecName` MSBuild property to override the namespace base:

```xml
<!-- Functions.Admin.csproj -->
<PropertyGroup>
  <AssemblyName>bootstrap</AssemblyName>
  <OpenApiSpecName>NativeGuardBackend.Functions.Admin</OpenApiSpecName>
  <OpenApiSpecTitle>Admin API</OpenApiSpecTitle> <!-- optional: overrides the API title in the YAML -->
</PropertyGroup>
```

This generates:

```csharp
namespace NativeGuardBackend.Functions.Admin.Generated;

public sealed class GeneratedOpenApiSpec : Native.OpenApi.IGeneratedOpenApiSpec
{
    // YAML title: "Admin API"
    public const string YamlContent = @"...";
}
```

**Priority:**

| Property | Fallback | Used For |
|----------|----------|----------|
| `OpenApiSpecName` | `AssemblyName` | Namespace base (`{value}.Generated`) |
| `OpenApiSpecTitle` | `OpenApiSpecName` (dots → spaces) | YAML `info.title` field |

**Example for multi-Lambda setup:**

```xml
<!-- Functions.Admin.csproj -->
<PropertyGroup>
  <AssemblyName>bootstrap</AssemblyName>
  <OpenApiSpecName>NativeGuardBackend.Functions.Admin</OpenApiSpecName>
</PropertyGroup>

<!-- Functions.Identity.csproj -->
<PropertyGroup>
  <AssemblyName>bootstrap</AssemblyName>
  <OpenApiSpecName>NativeGuardBackend.Functions.Identity</OpenApiSpecName>
</PropertyGroup>

<!-- Functions.OpenId.csproj -->
<PropertyGroup>
  <AssemblyName>bootstrap</AssemblyName>
  <OpenApiSpecName>NativeGuardBackend.Functions.OpenId</OpenApiSpecName>
</PropertyGroup>
```

Each produces a unique namespace even though all assemblies are named `bootstrap`.

### Using `ProjectReference` Instead of NuGet

When developing locally or contributing to this repository, you may reference the Source Generator via `ProjectReference` instead of `PackageReference`:

```xml
<ProjectReference Include="path\to\NativeLambdaRouter.SourceGenerator.OpenApi.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

> **Important:** The `.props` file that exposes `OpenApiSpecName` and `OpenApiSpecTitle` to the Roslyn analyzer is only auto-imported for NuGet packages. When using `ProjectReference`, you must manually declare `CompilerVisibleProperty` in your project or a `Directory.Build.props`:

```xml
<Project>
  <ItemGroup>
    <CompilerVisibleProperty Include="OpenApiSpecName" />
    <CompilerVisibleProperty Include="OpenApiSpecTitle" />
  </ItemGroup>
</Project>
```

Without this, the Source Generator cannot read MSBuild properties and will fall back to `AssemblyName` for namespace and title.

### Automated YAML Extraction (Multi-Lambda)

For multi-Lambda architectures where producers cannot be referenced by the consumer (due to `AssemblyName=bootstrap` ambiguity), you can use an MSBuild inline task to automatically extract the generated YAML on every build. See the [MultiLambdaSample](../../samples/MultiLambdaSample/) for a complete working example with `Directory.Build.targets`.

## Requirements

- .NET 6.0 or later (for Source Generator support)
- NativeLambdaRouter for endpoint definitions
- C# 9.0 or later

## How It Works

1. **Syntax Analysis**: The generator scans your code for `Map*` method invocations
2. **Semantic Analysis**: Validates that calls are on `IRouteBuilder` and extracts type information
3. **Code Generation**: Creates the `GeneratedOpenApiSpec` class with the OpenAPI YAML
4. **Build Integration**: The generated file is automatically included in compilation — no extra configuration needed

> **Note:** The generated class is injected directly into the compilation in memory. You do **not** need to add `EmitCompilerGeneratedFiles` to your `.csproj` for the generator to work. That setting only saves a physical copy of the generated `.cs` files to disk for inspection/debug purposes.

<details>
<summary>🔍 Debug: Inspecting generated files on disk</summary>

If you want to see the generated `.cs` files physically on disk, add to your `.csproj`:

```xml
<PropertyGroup>
  <!-- Optional: saves generated files to disk for inspection -->
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

The files will be saved to:

```
obj/Debug/net10.0/generated/NativeLambdaRouter.SourceGenerator.OpenApi/
    NativeLambdaRouter.SourceGenerator.OpenApi.OpenApiSourceGenerator/
        GeneratedOpenApiSpec.g.cs
```

</details>

## Related Packages

- [NativeOpenApi](../Native.OpenApi/) - OpenAPI document loading, linting, merging, and rendering

## License

MIT
