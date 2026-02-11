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

Durante o build, o Source Generator cria automaticamente a classe `GeneratedOpenApiSpec` no namespace `{AssemblyName}.Generated`:

```csharp
namespace SampleApiFunction.Generated;

public sealed class GeneratedOpenApiSpec : Native.OpenApi.IGeneratedOpenApiSpec
{
    public static readonly GeneratedOpenApiSpec Instance = new();
    public const string YamlContent = @"..."; // OpenAPI 3.1 YAML completo
    public const int EndpointCount = 6;
    public static readonly (string Method, string Path)[] EndpointList = ...;
}
```

### Visualizar arquivos gerados (opcional, apenas para debug)

O Source Generator injeta o código diretamente na compilação em memória — **não é necessário nenhuma configuração adicional** no `.csproj` para funcionar.

Se quiser **inspecionar** os arquivos `.cs` gerados fisicamente em disco, adicione no `.csproj`:

```xml
<PropertyGroup>
  <!-- Opcional: salva cópia dos arquivos gerados em disco para inspeção -->
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Os arquivos ficam em:

```
obj/Debug/net10.0/generated/NativeLambdaRouter.SourceGenerator.OpenApi/
    NativeLambdaRouter.SourceGenerator.OpenApi.OpenApiSourceGenerator/
        GeneratedOpenApiSpec.g.cs
```

> ⚠️ **Isso NÃO é necessário para usar o generator.** A classe `GeneratedOpenApiSpec` já está disponível na compilação automaticamente.

## Usando a Spec Gerada

```csharp
using SampleApiFunction.Generated;

var yaml = GeneratedOpenApiSpec.YamlContent;
var count = GeneratedOpenApiSpec.EndpointCount; // 6

foreach (var (method, path) in GeneratedOpenApiSpec.EndpointList)
{
    Console.WriteLine($"{method} {path}");
}

// Como IGeneratedOpenApiSpec (quando NativeOpenApi está referenciado)
IGeneratedOpenApiSpec spec = GeneratedOpenApiSpec.Instance;
```

## Diagnósticos do Source Generator

| ID | Severidade | Descrição |
|----|-----------|-----------|
| `NLOAPI001` | Info | Endpoint encontrado |
| `NLOAPI002` | Warning | Nenhum endpoint detectado |
| `NLOAPI003` | Info | Invocação de Map* encontrada |
