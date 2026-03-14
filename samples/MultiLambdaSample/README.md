# MultiLambdaSample

Exemplo completo de arquitetura **multi-Lambda** usando:

- `NativeLambdaRouter.SourceGenerator.OpenApi` (geração de OpenAPI em compile-time)
- `NativeOpenApi` (load + merge + lint + render dos documentos)

Objetivo: demonstrar **todos os recursos disponíveis** do gerador/OpenAPI em um cenário real com `AssemblyName=bootstrap`.

## TL;DR

- Três Lambdas produtoras (`Admin`, `Identity`, `OpenId`) geram specs OpenAPI parciais.
- Uma Lambda consumidora (`Functions.OpenApi`) carrega e consolida tudo em `/docs/openapi.json`.
- O fluxo usa extração automática de YAML via `Directory.Build.targets`.

## Arquitetura

- `Functions.Admin`: endpoints administrativos
- `Functions.Identity`: autenticação/registro/refresh
- `Functions.OpenId`: discovery + token endpoint OIDC
- `Functions.OpenApi`: merge de specs + UI (`/docs/redoc`, `/docs/scalar`)

## Cobertura de recursos (matriz)

| Recurso | Onde está no sample |
|---|---|
| `MapGet<TCommand,TResponse>` | `Functions.Admin`, `Functions.OpenId`, `Functions.OpenApi` |
| `MapPost<TCommand,TResponse>` | `Functions.Admin`, `Functions.Identity`, `Functions.OpenId` |
| `MapPut<TCommand,TResponse>` | `Functions.Admin` |
| `MapPatch<TCommand,TResponse>` | `Functions.Admin` |
| `MapDelete<TCommand,TResponse>` | `Functions.Admin` |
| `Map<TCommand,TResponse>("METHOD", ...)` | `Functions.Identity` (`OPTIONS /v1/auth/login`) |
| `.WithName/.WithSummary/.WithDescription/.WithTags` | Vários endpoints em todos os projetos |
| `[EndpointName]/[EndpointSummary]/[EndpointDescription]/[Tags]` | `Functions.Identity.Commands`, `Functions.Admin.Commands` |
| Precedência fluent > atributo | `Functions.Identity` (`LoginCommand` + fluent no route) |
| `.Accepts("application/x-www-form-urlencoded")` (fluent) | `Functions.OpenId` token endpoint |
| `[Accepts("application/x-www-form-urlencoded")]` (atributo) | `Functions.Identity.RefreshTokenCommand` |
| `.Produces<T>(statusCode)` | `Functions.Admin`, `Functions.Identity`, `Functions.OpenId` |
| `.ProducesProblem(statusCode)` | `Functions.Admin`, `Functions.Identity`, `Functions.OpenId` |
| `.AllowAnonymous()` (`security: []`) | `Functions.Identity`, `Functions.OpenId`, `Functions.OpenApi` |
| Introspecção de schema (nullable/required) | Commands com propriedades nullable (`Scope`, `ClientSecret`, `DisplayName`) |
| Merge + lint + render de OpenAPI | `Functions.OpenApi` |

## Problema de `AssemblyName=bootstrap`

Em AWS Lambda custom runtime, múltiplos projetos com `AssemblyName=bootstrap` causam:

1. Colisão de namespace no código gerado (`bootstrap.Generated`)
2. Ambiguidade de restore ao usar `ProjectReference` direto entre produtores

### Solução usada neste sample

- Cada produtor define:
  - `OpenApiSpecName` (namespace único da spec gerada)
  - `OpenApiSpecTitle` (título da API)
  - `OpenApiPartialName` (nome do arquivo YAML parcial)
- `Directory.Build.targets` extrai `YamlContent` do `.g.cs` e grava em:
  - `src/Functions.OpenApi/openapi/partials/*.yaml`

## Build (ordem obrigatória)

> Não faça `dotnet build` da solution inteira nesse cenário.

```bash
dotnet build src/Functions.Admin/Functions.Admin.csproj
dotnet build src/Functions.Identity/Functions.Identity.csproj
dotnet build src/Functions.OpenId/Functions.OpenId.csproj
dotnet build src/Functions.OpenApi/Functions.OpenApi.csproj
```

## Endpoints principais

### Admin

- `GET /v1/admin/users`
- `POST /v1/admin/users`
- `PUT /v1/admin/users/{id}`
- `PATCH /v1/admin/users/{id}/role`
- `DELETE /v1/admin/users/{id}`

### Identity

- `POST /v1/auth/login`
- `POST /v1/auth/register`
- `POST /v1/auth/refresh`
- `OPTIONS /v1/auth/login` (via `Map<...>("OPTIONS", ...)`)

### OpenId

- `GET /.well-known/openid-configuration`
- `GET /.well-known/jwks.json`
- `POST /v1/openid/token` (form-urlencoded)

### Docs

- `GET /docs/openapi.json`
- `GET /docs/redoc`
- `GET /docs/scalar`

## Checklist para agentes de IA

Use esta sequência para operar no sample sem quebrar o fluxo:

1. Build produtores na ordem: `Admin -> Identity -> OpenId`.
2. Verifique geração de parciais em `src/Functions.OpenApi/openapi/partials/`.
3. Build `Functions.OpenApi`.
4. Validar que `/docs/openapi.json` contém:
   - operações de todos os domínios,
   - `security: []` nos anônimos,
   - request body `application/x-www-form-urlencoded` em refresh/token,
   - respostas extras (`Produces<T>`, `ProducesProblem`).
5. Só depois validar Redoc/Scalar.

## Arquivos-chave

- `Directory.Build.props`
- `Directory.Build.targets`
- `src/Functions.Admin/Functions.Admin.csproj`
- `src/Functions.Identity/Functions.Identity.csproj`
- `src/Functions.OpenId/Functions.OpenId.csproj`
- `src/Functions.OpenApi/MultiLambdaDocumentLoader.cs`
- `src/Functions.OpenApi/MultiLambdaDocumentMerger.cs`
- `src/Functions.OpenApi/openapi/common/*.yaml`
- `src/Functions.OpenApi/openapi/partials/*.yaml`
