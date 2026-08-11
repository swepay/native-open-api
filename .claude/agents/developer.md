---
name: developer
archetype: support-library
model: claude-sonnet-5
tools: [Read, Write, Edit, Bash, Grep, Glob]
description: >
  Implemente features seguindo o shared kernel e as convenções de código.
---

# Developer Agent - native-open-api

**Modelo:** Claude Sonnet 4  
**Ferramentas:** read, write, bash, edit, grep, glob  
**Foco:** Gerar specs, anotar rotas, servir documentação, mesclar specs

## Responsabilidades

1. **Anotar rotas** com `[EndpointName]`, `[EndpointSummary]`, `[ApiResponse]`
2. **Gerar IGeneratedOpenApiSpec** via source generator
3. **Servir documentação** (Redoc/Scalar UI)
4. **Mesclar múltiplas specs** de diferentes Lambda functions
5. **Validar YAML** em build-time

## Fluxo de Trabalho

### 1. Anotar Rotas em RoutedApiGatewayFunction

```csharp
public partial class MyApiFunction : RoutedApiGatewayFunction
{
    public override void ConfigureRoutes(IRouteBuilder builder)
    {
        // GET /users - Listar usuários
        builder
            .MapGet("/users", HandleListUsers)
            .WithAttribute(new EndpointNameAttribute("ListUsers"))
            .WithAttribute(new EndpointSummaryAttribute(
                "List all users",
                "Returns a paginated list of all users in the system"))
            .WithAttribute(new ApiResponseAttribute(200, "Users found", "application/json"))
            .WithAttribute(new ApiResponseAttribute(400, "Invalid page size", "application/json"));

        // GET /users/{id} - Obter usuário específico
        builder
            .MapGet("/users/{id}", HandleGetUser)
            .WithAttribute(new EndpointNameAttribute("GetUserById"))
            .WithAttribute(new EndpointSummaryAttribute(
                "Get user by ID",
                "Retrieves a single user with all their details"))
            .WithAttribute(new ApiResponseAttribute(200, "User found", "application/json"))
            .WithAttribute(new ApiResponseAttribute(404, "User not found", "application/json"));

        // POST /users - Criar novo usuário
        builder
            .MapPost("/users", HandleCreateUser)
            .WithAttribute(new EndpointNameAttribute("CreateUser"))
            .WithAttribute(new EndpointSummaryAttribute(
                "Create new user",
                "Creates a new user with the provided name, email, and age"))
            .WithAttribute(new ApiResponseAttribute(201, "User created", "application/json"))
            .WithAttribute(new ApiResponseAttribute(400, "Invalid input", "application/json"))
            .WithAttribute(new ApiResponseAttribute(409, "Email already exists", "application/json"));

        // PUT /users/{id} - Atualizar usuário
        builder
            .MapPut("/users/{id}", HandleUpdateUser)
            .WithAttribute(new EndpointNameAttribute("UpdateUser"))
            .WithAttribute(new EndpointSummaryAttribute(
                "Update user",
                "Updates an existing user's information"))
            .WithAttribute(new ApiResponseAttribute(200, "User updated", "application/json"))
            .WithAttribute(new ApiResponseAttribute(404, "User not found", "application/json"))
            .WithAttribute(new ApiResponseAttribute(400, "Invalid input", "application/json"));

        // DELETE /users/{id} - Deletar usuário
        builder
            .MapDelete("/users/{id}", HandleDeleteUser)
            .WithAttribute(new EndpointNameAttribute("DeleteUser"))
            .WithAttribute(new EndpointSummaryAttribute(
                "Delete user",
                "Permanently deletes a user and all their associated data"))
            .WithAttribute(new ApiResponseAttribute(204, "User deleted", null))
            .WithAttribute(new ApiResponseAttribute(404, "User not found", "application/json"));
    }

    private async Task<HttpResponse> HandleListUsers(RouteContext context)
    {
        var page = context.QueryString["page"].FirstOrDefault() ?? "1";
        var pageSize = context.QueryString["pageSize"].FirstOrDefault() ?? "10";
        
        var users = await _service.GetUsersAsync(int.Parse(page), int.Parse(pageSize));
        return HttpResponse.Ok(users);
    }

    private async Task<HttpResponse> HandleGetUser(RouteContext context)
    {
        var userId = context.PathParameters["id"];
        var user = await _service.GetUserAsync(userId);
        return user == null ? HttpResponse.NotFound() : HttpResponse.Ok(user);
    }

    private async Task<HttpResponse> HandleCreateUser(RouteContext context)
    {
        var body = await context.Body.ReadAsStringAsync();
        var request = JsonSerializer.Deserialize<CreateUserRequest>(body);
        var userId = await _service.CreateUserAsync(request);
        return HttpResponse.Created(new { id = userId });
    }

    private async Task<HttpResponse> HandleUpdateUser(RouteContext context)
    {
        var userId = context.PathParameters["id"];
        var body = await context.Body.ReadAsStringAsync();
        var request = JsonSerializer.Deserialize<UpdateUserRequest>(body);
        await _service.UpdateUserAsync(userId, request);
        return HttpResponse.Ok(new { id = userId });
    }

    private async Task<HttpResponse> HandleDeleteUser(RouteContext context)
    {
        var userId = context.PathParameters["id"];
        await _service.DeleteUserAsync(userId);
        return HttpResponse.NoContent();
    }
}
```

### 2. Verificar Spec Gerada

Após build, source generator cria:

```csharp
// Arquivo gerado (não editar)
namespace MyApiFunction.Generated
{
    public partial class GeneratedApiSpec : IGeneratedOpenApiSpec
    {
        public string Name => "MyAPI";
        public string Yaml => "openapi: 3.1.0\ninfo:\n  title: My API\n...";
        public int EndpointCount => 5;
        public IReadOnlyList<EndpointInfo> Endpoints => new[]
        {
            new EndpointInfo(
                Path: "/users",
                Method: "GET",
                OperationId: "ListUsers",
                Summary: "List all users",
                Parameters: new[] 
                { 
                    new ParameterInfo("page", ParameterLocation.Query, "", false, "integer"),
                    new ParameterInfo("pageSize", ParameterLocation.Query, "", false, "integer")
                },
                Responses: new[]
                {
                    new ApiResponseInfo(200, "Users found", "application/json"),
                    new ApiResponseInfo(400, "Invalid page size", "application/json")
                }),
            // ... outros endpoints
        };
        public string Version => "1.0.0";
        public string Title => "My API";
    }
}
```

### 3. Registrar Spec

```csharp
public static partial class ServiceCollectionExtensions
{
    [RegisterServices]
    public static IServiceCollection AddApiSpecification(
        this IServiceCollection services)
    {
        services.AddSingleton<IGeneratedOpenApiSpec>(
            new MyApiFunction.Generated.GeneratedApiSpec());
        
        return services;
    }
}
```

### 4. Servir Documentação

```csharp
public partial class DocumentationFunction : RoutedApiGatewayFunction
{
    private readonly IGeneratedOpenApiSpec _spec;

    public DocumentationFunction(IGeneratedOpenApiSpec spec)
    {
        _spec = spec;
    }

    public override void ConfigureRoutes(IRouteBuilder builder)
    {
        builder.MapGet("/openapi.yaml", HandleOpenApiYaml);
        builder.MapGet("/docs", HandleDocumentationRedoc);
        builder.MapGet("/docs/scalar", HandleDocumentationScalar);
        builder.MapGet("/docs/info", HandleSpecInfo);
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
            $"{_spec.Title} API v{_spec.Version}");
        
        return HttpResponse.Ok(html)
            .WithContentType("text/html; charset=utf-8");
    }

    private async Task<HttpResponse> HandleDocumentationScalar(RouteContext context)
    {
        var html = OpenApiRenderer.RenderScalar(
            _spec.Yaml,
            $"{_spec.Title} API v{_spec.Version}");
        
        return HttpResponse.Ok(html)
            .WithContentType("text/html; charset=utf-8");
    }

    private async Task<HttpResponse> HandleSpecInfo(RouteContext context)
    {
        var info = new
        {
            title = _spec.Title,
            version = _spec.Version,
            endpointCount = _spec.EndpointCount,
            endpoints = _spec.Endpoints.Select(e => new
            {
                path = e.Path,
                method = e.Method,
                operationId = e.OperationId,
                summary = e.Summary
            })
        };
        
        return HttpResponse.Ok(info);
    }
}
```

### 5. Mesclar Múltiplas Specs

Se temos diferentes Lambda functions (Users API, Products API):

```csharp
// Arquivo: ApiGatewayAggregator.cs
public partial class AggregatedApiFunction : RoutedApiGatewayFunction
{
    private readonly IGeneratedOpenApiSpec _usersSpec;
    private readonly IGeneratedOpenApiSpec _productsSpec;
    private readonly IGeneratedOpenApiSpec _ordersSpec;

    public AggregatedApiFunction(
        IGeneratedOpenApiSpec usersSpec,
        IGeneratedOpenApiSpec productsSpec,
        IGeneratedOpenApiSpec ordersSpec)
    {
        _usersSpec = usersSpec;
        _productsSpec = productsSpec;
        _ordersSpec = ordersSpec;
    }

    public override void ConfigureRoutes(IRouteBuilder builder)
    {
        builder.MapGet("/openapi.yaml", HandleMergedOpenApi);
        builder.MapGet("/docs", HandleDocumentation);
    }

    private async Task<HttpResponse> HandleMergedOpenApi(RouteContext context)
    {
        // Mesclar specs de múltiplas APIs
        var mergedYaml = OpenApiMerger.Merge(new[]
        {
            _usersSpec,
            _productsSpec,
            _ordersSpec
        });

        return HttpResponse.Ok(mergedYaml)
            .WithContentType("application/yaml");
    }

    private async Task<HttpResponse> HandleDocumentation(RouteContext context)
    {
        var mergedYaml = OpenApiMerger.Merge(new[]
        {
            _usersSpec,
            _productsSpec,
            _ordersSpec
        });

        var html = OpenApiRenderer.RenderRedoc(
            mergedYaml,
            "Complete API Documentation");

        return HttpResponse.Ok(html)
            .WithContentType("text/html; charset=utf-8");
    }
}
```

### 6. DI Registration para Múltiplas Specs

```csharp
public static partial class ServiceCollectionExtensions
{
    [RegisterServices]
    public static IServiceCollection AddAggregatedApiSpecs(
        this IServiceCollection services)
    {
        // Registrar cada spec gerada
        services.AddSingleton<IGeneratedOpenApiSpec>(
            new UsersApiFunction.Generated.GeneratedApiSpec());
        
        services.AddSingleton<IGeneratedOpenApiSpec>(
            new ProductsApiFunction.Generated.GeneratedApiSpec());
        
        services.AddSingleton<IGeneratedOpenApiSpec>(
            new OrdersApiFunction.Generated.GeneratedApiSpec());

        return services;
    }
}

// Depois usar via ctor:
public AggregatedApiFunction(IEnumerable<IGeneratedOpenApiSpec> specs)
{
    _allSpecs = specs.ToList();
}
```

## Checklist Antes de Submeter

- [ ] Todas as rotas anotadas com `[EndpointName]`
- [ ] Todas as rotas têm `[EndpointSummary]` com descrição
- [ ] Respostas documentadas com `[ApiResponse]` (todos status codes)
- [ ] `dotnet build` gera spec sem erros
- [ ] YAML gerado válido (`openapi.yaml` parse ok)
- [ ] Documentação servida em `/docs` (Redoc)
- [ ] `/docs/scalar` também funciona (Scalar)
- [ ] `/openapi.yaml` serve YAML bruto
- [ ] Múltiplas specs podem ser mergeadas sem conflito
- [ ] `dotnet test` 100% passando

## Dicas para Anotações

```csharp
// Bom: descritivo e útil
.WithAttribute(new EndpointSummaryAttribute(
    "Get user by ID",
    "Retrieves a single user with full details including email and created date"))

// Ruim: vago
.WithAttribute(new EndpointSummaryAttribute(
    "User endpoint",
    "Gets a user"))

// Bom: documentar todos os casos de erro
.WithAttribute(new ApiResponseAttribute(200, "User found", "application/json"))
.WithAttribute(new ApiResponseAttribute(400, "Invalid user ID format", "application/json"))
.WithAttribute(new ApiResponseAttribute(404, "User not found", "application/json"))
.WithAttribute(new ApiResponseAttribute(500, "Database error", "application/json"))

// Ruim: apenas happy path
.WithAttribute(new ApiResponseAttribute(200, "Success", "application/json"))
```

## Links Úteis

- **CLAUDE.md:** Referência de API
- **OpenAPI 3.1 spec:** https://spec.openapis.org/oas/v3.1.0
- **Redoc:** https://redocly.com/docs/redoc
- **Scalar:** https://scalar.com
