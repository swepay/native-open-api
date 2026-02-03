# Native.OpenApi

[![Build Status](https://github.com/swepay/native-open-api/actions/workflows/dotnet.yml/badge.svg)](https://github.com/swepay/native-open-api/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A collection of .NET libraries for OpenAPI 3.1 document management, optimized for Native AOT applications.

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| [NativeOpenApi](src/Native.OpenApi/) | [![NuGet](https://img.shields.io/nuget/v/NativeOpenApi.svg)](https://www.nuget.org/packages/NativeOpenApi) | OpenAPI document loading, linting, merging, and HTML rendering |
| [NativeOpenApiGenerator](src/Native.OpenApi.Generator/) | [![NuGet](https://img.shields.io/nuget/v/NativeOpenApiGenerator.svg)](https://www.nuget.org/packages/NativeOpenApiGenerator) | Source Generator for automatic OpenAPI spec generation |

## Overview

### NativeOpenApi

Core library for working with OpenAPI 3.1 documents in Native AOT applications.

**Features:**
- 📄 Load OpenAPI specs from embedded resources (JSON and YAML)
- 🔀 Merge multiple partial specs into a consolidated document
- ✅ Lint and validate documents against configurable rules
- 📖 Generate Redoc and Scalar documentation pages
- ⚡ Full Native AOT compatibility

```bash
dotnet add package NativeOpenApi
```

```csharp
var resourceReader = new OpenApiResourceReader(typeof(Program).Assembly, "MyApp.");
var loader = new MyOpenApiDocumentLoader(resourceReader);
var merger = new MyOpenApiDocumentMerger();
var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
var provider = new OpenApiDocumentProvider(loader, merger, linter);

provider.WarmUp();
var json = provider.Document.Json;
```

👉 [Full documentation](src/Native.OpenApi/README.md)

---

### NativeOpenApiGenerator

Roslyn Source Generator that automatically creates OpenAPI specifications from NativeLambdaRouter endpoints at compile time.

**Features:**
- 🔧 Compile-time generation (zero runtime overhead)
- 🔍 Automatic endpoint discovery from `MapGet`, `MapPost`, etc.
- 📝 Generates OpenAPI 3.1 compliant YAML
- 🏷️ Auto-generates operation IDs, tags, and security definitions

```bash
dotnet add package NativeOpenApiGenerator
```

```csharp
// Endpoints are discovered automatically at build time
routes.MapGet<GetItemsCommand, GetItemsResponse>("/v1/items", ctx => new GetItemsCommand());
routes.MapPost<CreateItemCommand, CreateItemResponse>("/v1/items", ctx => Deserialize<CreateItemCommand>(ctx.Body!));

// Access the generated spec
string yaml = Native.OpenApi.Generated.GeneratedOpenApiSpec.Yaml;
```

👉 [Full documentation](src/Native.OpenApi.Generator/README.md)

---

## Quick Start

### Option 1: Document Loading & Merging Only

Use `NativeOpenApi` to load, merge, and serve your manually-written OpenAPI specs:

```bash
dotnet add package NativeOpenApi
```

### Option 2: Automatic Generation + Loading

Use both packages together for automatic endpoint discovery combined with document management:

```bash
dotnet add package NativeOpenApi
dotnet add package NativeOpenApiGenerator
```

```csharp
// Generated spec from your endpoints
var generatedYaml = Native.OpenApi.Generated.GeneratedOpenApiSpec.Yaml;

// Merge with common schemas and render documentation
var provider = new OpenApiDocumentProvider(loader, merger, linter);
var html = renderer.RenderRedoc("/openapi/v1/spec.json", "My API");
```

## Project Structure

```
native-open-api/
├── src/
│   ├── Native.OpenApi/           # Core library
│   │   ├── OpenApiDocumentLoader.cs
│   │   ├── OpenApiDocumentMerger.cs
│   │   ├── OpenApiDocumentProvider.cs
│   │   ├── OpenApiLinter.cs
│   │   ├── OpenApiHtmlRenderer.cs
│   │   └── README.md
│   │
│   └── Native.OpenApi.Generator/ # Source Generator
│       ├── OpenApiSourceGenerator.cs
│       ├── OpenApiYamlGenerator.cs
│       └── README.md
│
└── tests/
    ├── Native.OpenApi.Tests/
    └── Native.OpenApi.Generator.Tests/
```

## Requirements

- .NET 10.0 or later
- C# 12.0 or later

## Building from Source

```bash
git clone https://github.com/swepay/native-open-api.git
cd native-open-api
dotnet build
dotnet test
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT
