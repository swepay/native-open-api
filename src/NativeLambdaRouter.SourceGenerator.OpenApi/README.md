# NativeLambdaRouter.SourceGenerator.OpenApi

[![NuGet](https://img.shields.io/nuget/v/NativeLambdaRouter.SourceGenerator.OpenApi.svg)](https://www.nuget.org/packages/NativeLambdaRouter.SourceGenerator.OpenApi)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A Roslyn Source Generator that automatically generates OpenAPI 3.1 specifications from NativeLambdaRouter endpoint configurations at compile time.

## Features

- **Compile-Time Generation**: OpenAPI specs are generated during build, not at runtime
- **Zero Runtime Overhead**: No reflection or dynamic code generation
- **NativeLambdaRouter Integration**: Automatic discovery of `MapGet`, `MapPost`, etc. endpoints
- **Type-Safe**: Extracts request/response types from generic parameters
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
        routes.MapGet<GetItemsCommand, GetItemsResponse>("/v1/items", ctx => new GetItemsCommand());
        routes.MapPost<CreateItemCommand, CreateItemResponse>("/v1/items", ctx => Deserialize<CreateItemCommand>(ctx.Body!));
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

- **Operation ID**: Auto-generated from HTTP method and path (e.g., `getV1Items`)
- **Summary**: Generated from command type name
- **Tags**: Extracted from path segments for grouping
- **Security**: JWT Bearer authentication by default
- **Parameters**: Path parameters extracted from route template
- **Request Body**: For POST, PUT, PATCH methods with schema reference
- **Responses**: Success response with schema + standard error responses

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
<PackageReference Include="NativeLambdaRouter.SourceGenerator.OpenApi" Version="1.3.3" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
<PackageReference Include="NativeOpenApi" Version="1.3.3" />
```

And the consolidator project:

```xml
<!-- Functions.OpenApi.csproj -->
<PackageReference Include="NativeOpenApi" Version="1.3.3" />
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
