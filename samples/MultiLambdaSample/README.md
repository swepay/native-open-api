# MultiLambdaSample

This sample demonstrates how to use **NativeLambdaRouter.SourceGenerator.OpenApi** and **NativeOpenApi** in a multi-Lambda architecture where **all projects share `AssemblyName=bootstrap`** (required by AWS Lambda custom runtime).

## Architecture

```
┌───────────────────────────────────────────────────────────────┐
│                    Multi-Lambda Platform                       │
├──────────────────┬──────────────────┬─────────────────────────┤
│  Functions.Admin │ Functions.Identity│    Functions.OpenId      │
│  (producer)      │ (producer)       │    (producer)            │
│                  │                  │                          │
│  GET  /v1/admin/ │ POST /v1/auth/   │ GET /.well-known/       │
│       users      │      login       │     openid-configuration │
│  POST /v1/admin/ │ POST /v1/auth/   │ GET /.well-known/       │
│       users      │      register    │     jwks.json            │
│  DEL  /v1/admin/ │ POST /v1/auth/   │ POST /v1/openid/token   │
│       users/{id} │      refresh     │                          │
├──────────────────┴──────────────────┴─────────────────────────┤
│                      Functions.OpenApi                         │
│                       (consumer)                               │
│                                                                │
│  GET /docs/openapi.json  → Merged OpenAPI JSON                 │
│  GET /docs/redoc         → Redoc HTML viewer                   │
│  GET /docs/scalar        → Scalar HTML viewer                  │
│                                                                │
│  Merges all 3 producer specs + common schemas/responses/       │
│  security into a single consolidated OpenAPI 3.1.0 document    │
└───────────────────────────────────────────────────────────────┘
```

## The `AssemblyName=bootstrap` Problem

AWS Lambda custom runtime requires the entry point to be named `bootstrap`. When multiple Lambda projects in the same solution all set `AssemblyName=bootstrap`, two problems arise:

### Problem 1: Source Generator Namespace Collision

The Source Generator uses `AssemblyName` to generate the namespace `{AssemblyName}.Generated`. With all projects named `bootstrap`, they'd all generate `bootstrap.Generated.GeneratedOpenApiSpec`.

**Solution:** Use the `OpenApiSpecName` MSBuild property:

```xml
<OpenApiSpecName>Functions.Admin</OpenApiSpecName>
<OpenApiSpecTitle>Admin API</OpenApiSpecTitle>
```

This generates `Functions.Admin.Generated.GeneratedOpenApiSpec` instead.

### Problem 2: NuGet Restore Ambiguity

When one project references another via `<ProjectReference>` and both have `AssemblyName=bootstrap`, NuGet restore fails with:

```
error: Ambiguous project name 'bootstrap'
```

**Solution:** The consumer project (`Functions.OpenApi`) does **not** reference the producer projects directly. Instead, it embeds the YAML partial specs as resources:

1. Each producer project builds independently and generates its OpenAPI YAML
2. The generated YAML is committed to `Functions.OpenApi/openapi/partials/`
3. The consumer loads them as embedded resources via `OpenApiDocumentLoaderBase.Load()`

## Project Structure

```
MultiLambdaSample/
├── MultiLambdaSample.slnx
├── README.md
└── src/
    ├── Functions.Admin/            # Producer: Admin endpoints
    │   ├── Functions.Admin.csproj  # AssemblyName=bootstrap, OpenApiSpecName=Functions.Admin
    │   ├── Function.cs
    │   ├── Commands.cs
    │   ├── Responses.cs
    │   ├── Handlers.cs
    │   └── AdminJsonContext.cs
    ├── Functions.Identity/         # Producer: Auth endpoints
    │   ├── Functions.Identity.csproj
    │   ├── Function.cs
    │   ├── Commands.cs
    │   ├── Responses.cs
    │   ├── Handlers.cs
    │   └── IdentityJsonContext.cs
    ├── Functions.OpenId/           # Producer: OIDC endpoints
    │   ├── Functions.OpenId.csproj
    │   ├── Function.cs
    │   ├── Commands.cs
    │   ├── Responses.cs
    │   ├── Handlers.cs
    │   └── OpenIdJsonContext.cs
    └── Functions.OpenApi/          # Consumer: Merges all specs
        ├── Functions.OpenApi.csproj
        ├── Function.cs
        ├── Commands.cs
        ├── Responses.cs
        ├── MultiLambdaDocumentLoader.cs
        ├── MultiLambdaDocumentMerger.cs
        ├── OpenApiJsonSerializerContext.cs
        └── openapi/
            ├── common/
            │   ├── schemas.yaml
            │   ├── responses.yaml
            │   └── security.yaml
            └── partials/
                ├── admin.yaml
                ├── identity.yaml
                └── openid.yaml
```

## Building

```bash
# Build all projects
dotnet build

# Build a specific producer
dotnet build src/Functions.Admin/Functions.Admin.csproj

# Build the consumer
dotnet build src/Functions.OpenApi/Functions.OpenApi.csproj
```

## Key MSBuild Properties

| Property | Description | Example |
|----------|-------------|---------|
| `OpenApiSpecName` | Overrides the namespace for the generated class | `Functions.Admin` |
| `OpenApiSpecTitle` | Sets the title in the generated OpenAPI YAML | `Admin API` |
| `AssemblyName` | Must be `bootstrap` for AWS Lambda custom runtime | `bootstrap` |
| `RootNamespace` | Keeps the original namespace for C# code | `Functions.Admin` |

## How the Consumer Works

`Functions.OpenApi` uses `Native.OpenApi` library to:

1. **Load common parts** — schemas, responses, security definitions (3 YAML files)
2. **Load partial specs** — one per producer Lambda (3 YAML files)
3. **Merge** all parts into a single OpenAPI 3.1.0 document
4. **Lint** the merged document for consistency
5. **Serve** the consolidated spec via Redoc and Scalar viewers
