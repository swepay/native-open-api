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
- **Metadata & Extensions**: Fluent chain methods (`.WithName()`, `.WithSummary()`, `.Produces<T>()`, etc.) and attribute-based metadata
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

        routes.MapGet<GetItemByIdCommand, GetItemByIdResponse>("/v1/items/{id}", ctx => new GetItemByIdCommand(ctx.PathParameters["id"]))
            .Produces<NotFoundError>(404);

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
- **Responses**: Success response with schema + standard error responses + additional responses via `.Produces<T>()` / `.ProducesProblem()`

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
    .Produces<NotFoundError>(404)            // typed additional response
    .ProducesProblem(422);                   // problem+json error response
```

| Method | Effect |
|--------|--------|
| `.WithName("id")` | Sets `operationId` in the YAML |
| `.WithSummary("text")` | Sets `summary` in the YAML |
| `.WithDescription("text")` | Adds `description` field to the operation |
| `.WithTags("A", "B")` | Overrides auto-generated `tags` |
| `.Produces<T>(statusCode)` | Adds a typed response with `$ref` to schema |
| `.ProducesProblem(statusCode)` | Adds a `application/problem+json` error response |

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

### Additional Responses

Use `.Produces<T>(statusCode)` and `.ProducesProblem(statusCode)` to define additional responses beyond the default 200/400/401/500:

```csharp
routes.MapGet<GetItemCommand, GetItemResponse>("/v1/items/{id}", ctx => new GetItemCommand(ctx.PathParameters["id"]))
    .Produces<NotFoundError>(404)    // typed response → $ref schema
    .ProducesProblem(422);           // problem+json → generic error
```

Generates:

```yaml
responses:
  "200":
    description: OK
    content:
      application/json:
        schema:
          $ref: "#/components/schemas/GetItemResponse"
  "404":
    description: Not Found
    content:
      application/json:
        schema:
          $ref: "#/components/schemas/NotFoundError"
  "422":
    description: Unprocessable Entity
    content:
      application/problem+json:
        schema:
          type: object
  "400":
    $ref: "#/components/responses/BadRequest"
  "401":
    $ref: "#/components/responses/Unauthorized"
  "500":
    $ref: "#/components/responses/InternalError"
```

> **Note:** When `.ProducesProblem(statusCode)` overlaps with a default error response (400, 401, 500), the custom `application/problem+json` response **replaces** the default `$ref` — no duplicates.

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
