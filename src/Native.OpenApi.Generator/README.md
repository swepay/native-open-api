# NativeOpenApiGenerator

[![NuGet](https://img.shields.io/nuget/v/NativeOpenApiGenerator.svg)](https://www.nuget.org/packages/NativeOpenApiGenerator)
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
dotnet add package NativeOpenApiGenerator
```

## Quick Start

### 1. Define your routes

The generator automatically discovers endpoints in your `ConfigureRoutes` method:

```csharp
public class MyRouter : LambdaRouter
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

```csharp
using Native.OpenApi.Generated;

// Get the full OpenAPI YAML specification
string yaml = GeneratedOpenApiSpec.Yaml;

// Get endpoint count
int count = GeneratedOpenApiSpec.EndpointCount;

// Get list of all endpoints
var endpoints = GeneratedOpenApiSpec.Endpoints;
foreach (var (method, path) in endpoints)
{
    Console.WriteLine($"{method} {path}");
}
```

## Generated Output

The generator creates a `GeneratedOpenApiSpec` class in the `Native.OpenApi.Generated` namespace:

```csharp
namespace Native.OpenApi.Generated;

public static class GeneratedOpenApiSpec
{
    public const string Yaml = @"
openapi: ""3.1.0""
info:
  title: ""MyApi""
  version: ""1.0.0""
paths:
  /v1/items:
    get:
      operationId: getV1Items
      summary: ""Get GetItems""
      tags:
        - items
      security:
        - JwtBearer: []
      responses:
        ""200"":
          description: ""Successful response""
          content:
            application/json:
              schema:
                $ref: ""#/components/schemas/GetItemsResponse""
        ""400"":
          $ref: ""#/components/responses/BadRequest""
        ""401"":
          $ref: ""#/components/responses/Unauthorized""
        ""500"":
          $ref: ""#/components/responses/InternalServerError""
    post:
      operationId: postV1Items
      summary: ""Create CreateItem""
      tags:
        - items
      security:
        - JwtBearer: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: ""#/components/schemas/CreateItemCommand""
      responses:
        ...
";

    public const int EndpointCount = 5;
    
    public static readonly (string Method, string Path)[] Endpoints = new[]
    {
        ("DELETE", "/v1/items/{id}"),
        ("GET", "/v1/items"),
        ("GET", "/v1/items/{id}"),
        ("POST", "/v1/items"),
        ("PUT", "/v1/items/{id}"),
    };
}
```

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
using Native.OpenApi.Generated;

// Use the generated spec as part of your document loading
public class MyOpenApiDocumentLoader : OpenApiDocumentLoaderBase
{
    public override IReadOnlyList<OpenApiDocumentPart> LoadPartials()
    {
        return new List<OpenApiDocumentPart>
        {
            // Load from generated spec
            LoadFromString("generated", GeneratedOpenApiSpec.Yaml),
            // Load additional partials
            Load("schemas", "openapi/schemas.yaml")
        };
    }
}
```

## Requirements

- .NET 6.0 or later (for Source Generator support)
- NativeLambdaRouter for endpoint definitions
- C# 9.0 or later

## How It Works

1. **Syntax Analysis**: The generator scans your code for `Map*` method invocations
2. **Semantic Analysis**: Validates that calls are on `IRouteBuilder` and extracts type information
3. **Code Generation**: Creates the `GeneratedOpenApiSpec` class with the OpenAPI YAML
4. **Build Integration**: The generated file is automatically included in compilation

## Related Packages

- [NativeOpenApi](../Native.OpenApi/) - OpenAPI document loading, linting, merging, and rendering

## License

MIT
