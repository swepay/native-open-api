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

- [NativeOpenApiGenerator](../Native.OpenApi.Generator/) - Source Generator for automatic OpenAPI spec generation from NativeLambdaRouter endpoints

## License

MIT
