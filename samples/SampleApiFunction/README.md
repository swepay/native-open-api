# SampleApiFunction

Exemplo completo de uma AWS Lambda Function usando:

- **NativeLambdaRouter** 2.0.2 — Roteamento de API Gateway requests
- **NativeMediator** 1.0.4 — Mediator pattern (CQRS)
- **NativeLambdaRouter.SourceGenerator.OpenApi** — Geração automática de OpenAPI 3.1 spec em compile-time
- **Native AOT** — Publicação AOT-first

## Estrutura

```
SampleApiFunction/
├── Function.cs           # RoutedApiGatewayFunction com definição de rotas
├── Commands.cs           # Commands implementando IRequest<TResponse>
├── Responses.cs          # Records de Response
├── Handlers.cs           # IRequestHandler<TCommand, TResponse>
├── AppJsonContext.cs     # JsonSerializerContext para AOT
└── SampleApiFunction.csproj
```

## Endpoints

| Method | Path | Command | Response |
|--------|------|---------|----------|
| GET | `/v1/items` | `GetItemsCommand` | `GetItemsResponse` |
| GET | `/v1/items/{id}` | `GetItemByIdCommand` | `GetItemByIdResponse` |
| POST | `/v1/items` | `CreateItemCommand` | `CreateItemResponse` |
| PUT | `/v1/items/{id}` | `UpdateItemCommand` | `UpdateItemResponse` |
| DELETE | `/v1/items/{id}` | `DeleteItemCommand` | `DeleteItemResponse` |
| GET | `/health` | `HealthCheckCommand` | `HealthCheckResponse` |

## Build

```bash
dotnet build
```

## OpenAPI Spec Gerada

Durante o build, o Source Generator cria automaticamente a classe `GeneratedOpenApiSpec`:

```csharp
namespace Native.OpenApi.Generated;

public static class GeneratedOpenApiSpec
{
    public const string Yaml = @"..."; // OpenAPI 3.1 YAML completo
    public const int EndpointCount = 6;
    public static readonly (string Method, string Path)[] Endpoints = ...;
}
```

Para visualizar os arquivos gerados em disco, adicione no `.csproj`:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Os arquivos ficam em:

```
obj/Debug/net10.0/generated/NativeLambdaRouter.SourceGenerator.OpenApi/...
```

## Usando a Spec Gerada

```csharp
using Native.OpenApi.Generated;

var yaml = GeneratedOpenApiSpec.Yaml;
var count = GeneratedOpenApiSpec.EndpointCount; // 6

foreach (var (method, path) in GeneratedOpenApiSpec.Endpoints)
{
    Console.WriteLine($"{method} {path}");
}
```

## Diagnósticos do Source Generator

| ID | Severidade | Descrição |
|----|-----------|-----------|
| `NLOAPI001` | Info | Endpoint encontrado |
| `NLOAPI002` | Warning | Nenhum endpoint detectado |
| `NLOAPI003` | Info | Invocação de Map* encontrada |
