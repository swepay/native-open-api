# native-open-api

**Versão:** v1.6.0  
**Tipo:** NuGet Library - OpenAPI 3.1 Generation & Rendering  
**AOT-Safe:** Sim (PublishAot = true)  
**Linguagem:** C# 12+

## O que é

`native-open-api` carrega, valida, mescla e renderiza especificações OpenAPI 3.1 **sem reflection**. Integra-se com `native-lambda-router` para gerar specs a partir de rotas, suportando renderização via Redoc ou Scalar com zero dependencies externas (exceto YamlDotNet).

## API Pública Principal

### IGeneratedOpenApiSpec
Interface para especificações geradas.

```csharp
public interface IGeneratedOpenApiSpec
{
    string Name { get; }
    string Yaml { get; }
    int EndpointCount { get; }
    IReadOnlyList<EndpointInfo> Endpoints { get; }
    string Version { get; }
    string Title { get; }
}

public record EndpointInfo(
    string Path,
    string Method,
    string OperationId,
    string Summary,
    IReadOnlyList<ParameterInfo> Parameters,
    IReadOnlyList<ApiResponseInfo> Responses);

public record ParameterInfo(
    string Name,
    ParameterLocation Location,
    string Description,
    bool Required,
    string Schema);

public record ApiResponseInfo(
    int StatusCode,
    string Description,
    string ContentType);

public enum ParameterLocation
{
    Path,
    Query,
    Header,
    Cookie
}
```

### OpenApiMerger
Mescla múltiplas especificações em uma única.

```csharp
public class OpenApiMerger
{
    public static string Merge(
        string baseYaml,
        params string[] specs);
    
    public static string Merge(
        IEnumerable<IGeneratedOpenApiSpec> specs);
}
```

### OpenApiRenderer
Renderiza spec em HTML para visualização.

```csharp
public class OpenApiRenderer
{
    public static string RenderRedoc(
        string yamlSpec,
        string title = "API Documentation");
    
    public static string RenderScalar(
        string yamlSpec,
        string title = "API Documentation");
}
```

## Atributos de Anotação

### [EndpointName]
Define operationId da rota.

```csharp
builder
    .MapGet("/users/{id}", HandleGetUser)
    .WithAttribute(new EndpointNameAttribute("GetUserById"))
```

### [EndpointSummary]
Descrição curta do endpoint.

```csharp
builder
    .MapGet("/users/{id}", HandleGetUser)
    .WithAttribute(new EndpointSummaryAttribute(
        "Get user by ID",
        "Retrieves a single user by their unique identifier"))
```

### [ApiResponse]
Define respostas possíveis.

```csharp
builder
    .MapGet("/users/{id}", HandleGetUser)
    .WithAttribute(new ApiResponseAttribute(200, "User found", "application/json"))
    .WithAttribute(new ApiResponseAttribute(404, "User not found", "application/json"))
```

## Source Generator

A biblioteca inclui source generator que escaneia `NativeLambdaRouter` routes e gera `IGeneratedOpenApiSpec`.

### Como Funciona

1. **Build-time:** Source generator escaneia rotas registradas
2. **Gera classe:** `GeneratedApiSpec : IGeneratedOpenApiSpec`
3. **Runtime:** Injetar como singleton

```csharp
public partial class GeneratedApiSpec : IGeneratedOpenApiSpec
{
    public string Name => "MyAPI";
    public string Yaml => @"openapi: 3.1.0
info:
  title: My API
  version: 1.0.0
paths:
  /users/{id}:
    get:
      operationId: GetUserById
      parameters:
        - name: id
          in: path
          required: true
      responses:
        '200':
          description: Success";
    
    public int EndpointCount => 3;
    public IReadOnlyList<EndpointInfo> Endpoints => new[]
    {
        new EndpointInfo(
            Path: "/users/{id}",
            Method: "GET",
            OperationId: "GetUserById",
            Summary: "Get user by ID",
            Parameters: new[] { ... },
            Responses: new[] { ... })
    };
    
    public string Version => "1.0.0";
    public string Title => "My API";
}
```

## Como Usar

### 1. Anotar Rotas

```csharp
public partial class MyFunction : RoutedApiGatewayFunction
{
    public override void ConfigureRoutes(IRouteBuilder builder)
    {
        builder
            .MapGet("/users", HandleListUsers)
            .WithAttribute(new EndpointNameAttribute("ListUsers"))
            .WithAttribute(new EndpointSummaryAttribute(
                "List all users",
                "Returns a paginated list of all users"))
            .WithAttribute(new ApiResponseAttribute(200, "Users found", "application/json"))
            .WithAttribute(new ApiResponseAttribute(400, "Invalid parameters", "application/json"));

        builder
            .MapGet("/users/{id}", HandleGetUser)
            .WithAttribute(new EndpointNameAttribute("GetUserById"))
            .WithAttribute(new EndpointSummaryAttribute(
                "Get user by ID",
                "Retrieves a single user"))
            .WithAttribute(new ApiResponseAttribute(200, "User found", "application/json"))
            .WithAttribute(new ApiResponseAttribute(404, "User not found", "application/json"));

        builder
            .MapPost("/users", HandleCreateUser)
            .WithAttribute(new EndpointNameAttribute("CreateUser"))
            .WithAttribute(new EndpointSummaryAttribute(
                "Create new user",
                "Creates a new user with provided data"))
            .WithAttribute(new ApiResponseAttribute(201, "User created", "application/json"))
            .WithAttribute(new ApiResponseAttribute(400, "Invalid data", "application/json"));
    }
}
```

### 2. Registrar Spec Gerada

```csharp
services.AddSingleton<IGeneratedOpenApiSpec, GeneratedApiSpec>();
```

### 3. Servir Documentação

```csharp
public partial class ApiDocumentationFunction : RoutedApiGatewayFunction
{
    private readonly IGeneratedOpenApiSpec _spec;

    public ApiDocumentationFunction(IGeneratedOpenApiSpec spec)
    {
        _spec = spec;
    }

    public override void ConfigureRoutes(IRouteBuilder builder)
    {
        builder.MapGet("/openapi.yaml", HandleOpenApiYaml);
        builder.MapGet("/docs", HandleDocumentationRedoc);
        builder.MapGet("/docs/scalar", HandleDocumentationScalar);
    }

    private async Task<HttpResponse> HandleOpenApiYaml(RouteContext context)
    {
        return HttpResponse.Ok(_spec.Yaml)
            .WithContentType("application/yaml");
    }

    private async Task<HttpResponse> HandleDocumentationRedoc(RouteContext context)
    {
        var html = OpenApiRenderer.RenderRedoc(
            _spec.Yaml,
            $"{_spec.Title} v{_spec.Version}");
        
        return HttpResponse.Ok(html)
            .WithContentType("text/html");
    }

    private async Task<HttpResponse> HandleDocumentationScalar(RouteContext context)
    {
        var html = OpenApiRenderer.RenderScalar(
            _spec.Yaml,
            $"{_spec.Title} v{_spec.Version}");
        
        return HttpResponse.Ok(html)
            .WithContentType("text/html");
    }
}
```

### 4. Mesclar Múltiplas Specs

```csharp
// Se temos múltiplas funções Lambda com specs diferentes
var mainSpec = _mainApiSpec.Yaml;
var usersSpec = _usersApiSpec.Yaml;
var productsSpec = _productsApiSpec.Yaml;

var mergedYaml = OpenApiMerger.Merge(mainSpec, usersSpec, productsSpec);

// Ou via specs
var merged = OpenApiMerger.Merge(new[]
{
    _mainApiSpec,
    _usersApiSpec,
    _productsApiSpec
});
```

## Fluent API

```csharp
builder
    .MapGet("/products/{id}", handler)
    .WithName("GetProductById")                    // [EndpointName]
    .WithSummary("Get product by ID",             // [EndpointSummary]
                 "Retrieve a single product")
    .WithDescription("Returns product details with inventory")
    .Accepts("application/json")
    .Produces(200, "application/json")            // [ApiResponse]
    .Produces(404, "application/json")
    .ProducesProblem(400)
    .ProducesProblem(500);
```

## Namespaces & Bootstrap para Lambda

O source generator customiza namespaces para ficar isolado no Lambda bootstrap:

```csharp
// Gerado automaticamente em namespace específico
namespace MyFunction.Generated
{
    public partial class GeneratedApiSpec : IGeneratedOpenApiSpec { ... }
}

// Registrar explicitamente
services.AddSingleton<IGeneratedOpenApiSpec>(
    new MyFunction.Generated.GeneratedApiSpec());
```

## Premissas

- **Zero reflection:** Source generator (compile-time)
- **PublishAot = true:** Compilado sem trim warnings
- **Namespace:** `Native.OpenApi`
- **Target:** `net8.0`
- **YamlDotNet:** Única dependency externa (leitura YAML)
- **OpenAPI 3.1:** Versão suportada (não 3.0.x)
- **Validation:** Schema validation em build-time

## Terminologia

- **Generated Spec:** Classe gerada pelo source generator
- **Redoc:** UI de documentação (esquerda/direita layout)
- **Scalar:** UI alternativa (abas, dark mode)
- **Merge:** Combinar múltiplas specs em uma
- **YAML:** Formato de serialização (alternativa a JSON)

## Limitações

- Source generator requer `partial` class em `RoutedApiGatewayFunction`
- Atributos devem ser aplicados em build-time (não runtime)
- Merge não resolve conflitos automático (paths duplicados falham)
- Requer rebuild para atualizar spec (não é dinâmica)
