# Native.OpenApi

[![Build Status](https://github.com/swepay/native-open-api/actions/workflows/dotnet.yml/badge.svg)](https://github.com/swepay/native-open-api/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Bibliotecas .NET 10 para gerenciamento de documentos OpenAPI 3.1, otimizadas para Native AOT.

Este repositório contém dois pacotes NuGet:

| Package | NuGet | Descrição |
|---------|-------|-----------|
| **NativeOpenApi** | [![NuGet](https://img.shields.io/nuget/v/NativeOpenApi.svg)](https://www.nuget.org/packages/NativeOpenApi) | Loading, linting, merging e rendering de documentos OpenAPI |
| **NativeLambdaRouter.SourceGenerator.OpenApi** | [![NuGet](https://img.shields.io/nuget/v/NativeLambdaRouter.SourceGenerator.OpenApi.svg)](https://www.nuget.org/packages/NativeLambdaRouter.SourceGenerator.OpenApi) | Source Generator que gera specs OpenAPI em compile-time a partir de endpoints NativeLambdaRouter |

---

## NativeOpenApi

Biblioteca core para carregar, validar, mesclar e renderizar documentos OpenAPI 3.1 sem reflection.

- Carregamento de specs a partir de embedded resources (JSON e YAML)
- Merge de múltiplas specs parciais em um documento consolidado
- Linting configurável com regras de validação
- Geração de páginas Redoc e Scalar
- Interface `IGeneratedOpenApiSpec` para integração com o Source Generator
- 100% Native AOT, zero reflection

```bash
dotnet add package NativeOpenApi
```

> **[Documentação completa →](src/Native.OpenApi/README.md)**

---

## NativeLambdaRouter.SourceGenerator.OpenApi

Roslyn Source Generator que descobre endpoints `MapGet`, `MapPost`, `MapPut`, `MapDelete`, `MapPatch` do NativeLambdaRouter e gera a spec OpenAPI 3.1 em compile-time.

- Geração em compile-time (zero overhead em runtime)
- Descoberta automática de endpoints via análise sintática/semântica
- Operation IDs, tags, security e path parameters gerados automaticamente
- Metadata customizável via fluent chain (`.WithName()`, `.WithSummary()`, `.WithDescription()`, `.WithTags()`, `.Accepts()`) e atributos (`[EndpointName]`, `[EndpointSummary]`, `[EndpointDescription]`, `[Tags]`, `[Accepts]`)
- Respostas adicionais via `.Produces<T>(statusCode)` e `.ProducesProblem(statusCode)`
- Suporte a `application/x-www-form-urlencoded` com schema inline para endpoints OAuth2/formulários
- Introspecção real de propriedades de tipos C# via Roslyn (records, classes, enums, arrays, nullables)
- Implementa `IGeneratedOpenApiSpec` quando NativeOpenApi está referenciado
- Namespace customizável via `OpenApiSpecName` (resolve `AssemblyName=bootstrap` em AWS Lambda)

```bash
dotnet add package NativeLambdaRouter.SourceGenerator.OpenApi
```

> **[Documentação completa →](src/NativeLambdaRouter.SourceGenerator.OpenApi/README.md)**

---

## Quando usar cada pacote?

| Cenário | Pacotes |
|---------|---------|
| Carregar/mesclar specs escritas manualmente | `NativeOpenApi` |
| Gerar specs automaticamente a partir de endpoints | `NativeLambdaRouter.SourceGenerator.OpenApi` |
| Gerar + mesclar com schemas comuns + servir documentação | `NativeOpenApi` + `NativeLambdaRouter.SourceGenerator.OpenApi` |

---

## Estrutura do Repositório

```
native-open-api/
├── src/
│   ├── Native.OpenApi/                                # Biblioteca core
│   └── NativeLambdaRouter.SourceGenerator.OpenApi/    # Source Generator
├── tests/
│   ├── Native.OpenApi.Tests/
│   └── NativeLambdaRouter.SourceGenerator.OpenApi.Tests/
├── samples/
│   └── SampleApiFunction/                             # Exemplo completo com AWS Lambda
└── docs/
    └── CHANGELOG.md
```

## Requisitos

- .NET 10.0+
- C# 12.0+

## Build

```bash
git clone https://github.com/swepay/native-open-api.git
cd native-open-api
dotnet build
dotnet test
```

## License

MIT
