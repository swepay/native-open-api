# Native.OpenApi

[![Build Status](https://github.com/swepay/native-open-api/actions/workflows/dotnet.yml/badge.svg)](https://github.com/swepay/native-open-api/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/NativeOpenApi.svg)](https://www.nuget.org/packages/NativeOpenApi)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

OpenAPI 3.1 document loading, linting, merging, and rendering abstractions for .NET 10 Native AOT applications.

## Features

- **OpenAPI Document Loading**: Load OpenAPI specs from embedded resources (JSON and YAML supported)
- **YAML Support**: Full support for YAML format with AOT-compatible parsing
- **Document Merging**: Merge multiple partial specs into a consolidated document
- **Linting**: Validate OpenAPI specs against configurable rules
- **HTML Rendering**: Generate Redoc and Scalar documentation pages

## Installation

```bash
dotnet add package Native.OpenApi
```

## Usage

### 1. Create your document loader

The loader automatically detects and parses both JSON (`.json`) and YAML (`.yaml`, `.yml`) files:

```csharp
public class MyOpenApiDocumentLoader : OpenApiDocumentLoaderBase
{
    public MyOpenApiDocumentLoader(OpenApiResourceReader resourceReader) 
        : base(resourceReader) { }

    public override IReadOnlyList<OpenApiDocumentPart> LoadCommon()
    {
        return new List<OpenApiDocumentPart>
        {
            // YAML files
            Load("common-schemas", "openapi/common/schemas.yaml"),
            Load("common-responses", "openapi/common/responses.yaml"),
            Load("common-security", "openapi/common/security.yaml")
        };
    }

    public override IReadOnlyList<OpenApiDocumentPart> LoadPartials()
    {
        return new List<OpenApiDocumentPart>
        {
            // Mix of YAML and JSON files
            Load("users", "openapi/users/openapi.yaml"),
            Load("products", "openapi/products/openapi.json")
        };
    }
}
```

### 2. Create your document merger (optional)

```csharp
public class MyOpenApiDocumentMerger : OpenApiDocumentMerger
{
    protected override string GetServerUrl()
    {
        var env = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "dev";
        return env switch
        {
            "prd" => "https://api.myapp.com",
            "hml" => "https://api-staging.myapp.com",
            _ => "https://localhost:5001"
        };
    }

    protected override string GetApiTitle() => "My API";
    protected override string GetApiDescription() => "My consolidated API documentation.";
}
```

### 3. Wire up the provider

```csharp
var resourceReader = new OpenApiResourceReader(typeof(Program).Assembly, "MyApp.");
var loader = new MyOpenApiDocumentLoader(resourceReader);
var merger = new MyOpenApiDocumentMerger();
var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
var provider = new OpenApiDocumentProvider(loader, merger, linter);

provider.WarmUp();

var json = provider.Document.Json;
var yaml = provider.Document.Yaml;
```

### 4. Render documentation pages

```csharp
var renderer = new OpenApiHtmlRenderer();
var redocHtml = renderer.RenderRedoc("/openapi/v1/spec.json", "My API Docs");
var scalarHtml = renderer.RenderScalar("/openapi/v1/spec.json", "My API Docs");
```

## Linting Rules

Configure linting rules using `OpenApiLintOptions`:

```csharp
var options = new OpenApiLintOptions(
    RequiredErrorResponses: ["400", "401", "500"],
    SensitiveFieldNames: ["password", "token", "secret"],
    DisallowedGenericSegments: ["data", "items"]
);
var linter = new OpenApiLinter(options);
```

The linter validates:
- OpenAPI version is 3.1.0
- All paths include versioning (e.g., `/v1/`)
- All operations have security definitions (JwtBearer or OAuth2)
- Required error responses are present
- Sensitive fields have descriptions

## Source Generator (NativeOpenApiGenerator)

Automatically generate OpenAPI specifications from NativeLambdaRouter endpoints at compile time!

### Installation

```bash
dotnet add package NativeOpenApiGenerator
```

### How it works

The Source Generator analyzes your `ConfigureRoutes` method and extracts endpoint information:

```csharp
protected override void ConfigureRoutes(IRouteBuilder routes)
{
    // These endpoints are automatically discovered at compile time
    routes.MapGet<GetItemsCommand, GetItemsResponse>("/v1/items", ctx => new GetItemsCommand());
    routes.MapPost<CreateItemCommand, CreateItemResponse>("/v1/items", ctx => Deserialize<CreateItemCommand>(ctx.Body!));
    routes.MapGet<GetItemByIdCommand, GetItemByIdResponse>("/v1/items/{id}", ctx => new GetItemByIdCommand(ctx.PathParameters["id"]));
    routes.MapDelete<DeleteItemCommand, DeleteItemResponse>("/v1/items/{id}", ctx => new DeleteItemCommand(ctx.PathParameters["id"]));
}
```

### Generated output

The generator creates a `GeneratedOpenApiSpec` class with the YAML specification:

```csharp
// Auto-generated code
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
      ...
    post:
      operationId: postV1Items
      summary: ""Create CreateItem""
      ...
";

    public const int EndpointCount = 4;
    
    public static readonly (string Method, string Path)[] Endpoints = new[]
    {
        ("DELETE", "/v1/items/{id}"),
        ("GET", "/v1/items"),
        ("GET", "/v1/items/{id}"),
        ("POST", "/v1/items"),
    };
}
```

### Using the generated spec

```csharp
// Access the generated OpenAPI spec
var yaml = Native.OpenApi.Generated.GeneratedOpenApiSpec.Yaml;
var endpointCount = Native.OpenApi.Generated.GeneratedOpenApiSpec.EndpointCount;

// Use with Native.OpenApi to merge with common schemas
var loader = new MyOpenApiDocumentLoader(resourceReader);
// Include the generated spec as a partial...
```

### Supported mapping methods

The generator detects all NativeLambdaRouter mapping methods:
- `MapGet<TCommand, TResponse>`
- `MapPost<TCommand, TResponse>`
- `MapPut<TCommand, TResponse>`
- `MapDelete<TCommand, TResponse>`
- `MapPatch<TCommand, TResponse>`
- `Map<TCommand, TResponse>` (custom method)

## License

MIT
