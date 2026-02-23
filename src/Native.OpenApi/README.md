# NativeOpenApi

[![NuGet](https://img.shields.io/nuget/v/NativeOpenApi.svg)](https://www.nuget.org/packages/NativeOpenApi)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

OpenAPI 3.1 document loading, linting, merging, and rendering abstractions for .NET 10 Native AOT applications.

## Features

- **OpenAPI Document Loading**: Load OpenAPI specs from embedded resources (JSON and YAML supported)
- **YAML Support**: Full support for YAML format with AOT-compatible parsing
- **Document Merging**: Merge multiple partial specs into a consolidated document
- **Linting**: Validate OpenAPI specs against configurable rules
- **HTML Rendering**: Generate Redoc and Scalar documentation pages
- **Native AOT Compatible**: Fully optimized for ahead-of-time compilation

## Installation

```bash
dotnet add package NativeOpenApi
```

## Quick Start

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

## ApiResponse Attribute

The `ApiResponseAttribute` allows you to document HTTP responses directly on handler methods. This is particularly useful when using the `NativeLambdaRouter.SourceGenerator.OpenApi` package to automatically generate OpenAPI specifications.

### Usage

Apply the `[ApiResponse]` attribute to your handler methods to specify possible HTTP responses:

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
        // ... handler implementation
    }
}
```

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `statusCode` | `int` | Yes | - | HTTP status code (e.g., 200, 404, 500) |
| `responseType` | `Type?` | No | `null` | Type of the response body. Null for no body (204, 404) |
| `contentType` | `string` | No | `"application/json"` | Content type of the response |

### Multiple Response Types

The attribute can be applied multiple times to document various response scenarios:

```csharp
[ApiResponse(200, typeof(Product))]              // Success with JSON
[ApiResponse(201, typeof(Product))]              // Created with JSON
[ApiResponse(204)]                                // No Content
[ApiResponse(400, typeof(ValidationProblem), "application/problem+json")]
[ApiResponse(401)]                                // Unauthorized (no body)
[ApiResponse(403, typeof(ProblemDetails), "application/problem+json")]
[ApiResponse(404, typeof(ProblemDetails), "application/problem+json")]
[ApiResponse(500, typeof(ProblemDetails), "application/problem+json")]
```

### How It Works

When used with the `NativeLambdaRouter.SourceGenerator.OpenApi`:

1. The Source Generator scans your code for handler methods (implementing `IRequestHandler<TCommand, TResponse>`)
2. It reads `[ApiResponse]` attributes from the handler's `Handle` method
3. It automatically includes these responses in the generated OpenAPI specification
4. Each response is properly typed and includes the correct content type

### Benefits

- **Type-safe**: Response types are checked at compile time
- **Single Source of Truth**: Documentation lives next to the implementation
- **Auto-generated**: No manual YAML/JSON editing required
- **AOT-compatible**: Works perfectly with Native AOT compilation

### Example: Complete Handler

```csharp
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    [ApiResponse(201, typeof(CreateOrderResponse), "application/json")]
    [ApiResponse(400, typeof(ValidationProblemDetails), "application/problem+json")]
    [ApiResponse(409, typeof(ConflictProblemDetails), "application/problem+json")]
    [ApiResponse(500, typeof(ProblemDetails), "application/problem+json")]
    public async ValueTask<CreateOrderResponse> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrEmpty(request.CustomerId))
            throw new ValidationException("CustomerId is required");

        // Create order
        var order = await _orderService.CreateAsync(request, cancellationToken);
        
        return new CreateOrderResponse(order.Id, order.Total);
    }
}
```

The generated OpenAPI spec will include all four response types with proper schemas and content types.

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

## API Reference

### Core Classes

| Class | Description |
|-------|-------------|
| `OpenApiDocumentLoaderBase` | Base class for loading OpenAPI document parts |
| `OpenApiDocumentMerger` | Merges multiple OpenAPI document parts into one |
| `OpenApiDocumentProvider` | Orchestrates loading, merging, and linting |
| `OpenApiLinter` | Validates OpenAPI documents against rules |
| `OpenApiHtmlRenderer` | Generates HTML documentation pages |
| `OpenApiResourceReader` | Reads embedded resources from assemblies |

### Document Types

| Type | Description |
|------|-------------|
| `OpenApiDocument` | Represents a complete OpenAPI document with JSON/YAML output |
| `OpenApiDocumentPart` | Represents a partial OpenAPI spec to be merged |
| `OpenApiLintOptions` | Configuration options for linting rules |

## Native AOT Compatibility

This library is fully compatible with .NET Native AOT compilation:

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

All JSON/YAML parsing uses source-generated serialization contexts for optimal performance and trimming support.

## Related Packages

- [NativeLambdaRouter.SourceGenerator.OpenApi](../NativeLambdaRouter.SourceGenerator.OpenApi/) - Source Generator for automatic OpenAPI spec generation from NativeLambdaRouter endpoints

## License

MIT
