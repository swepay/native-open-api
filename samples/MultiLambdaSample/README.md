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

1. Each producer project builds and the Source Generator creates an in-memory OpenAPI YAML
2. `Directory.Build.targets` extracts the YAML from the generated `.g.cs` and writes it to `Functions.OpenApi/openapi/partials/`
3. The consumer loads them as embedded resources via `OpenApiDocumentLoaderBase.Load()`

## Automated YAML Extraction

The solution uses a `Directory.Build.targets` with an inline MSBuild task (`ExtractOpenApiYaml`) to automatically extract the generated OpenAPI YAML from each producer project on every build. No manual copy is needed.

### How It Works

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Producer Build (e.g. Functions.Admin)                                   │
│                                                                          │
│  1. Roslyn compiles the project                                          │
│  2. Source Generator produces GeneratedOpenApiSpec.g.cs (in-memory)      │
│  3. EmitCompilerGeneratedFiles=true writes the .g.cs to disk             │
│  4. ExportOpenApiPartialSpec target runs after build:                     │
│     - Reads obj/.../GeneratedOpenApiSpec.g.cs                            │
│     - Extracts YamlContent const via regex                               │
│     - Writes to Functions.OpenApi/openapi/partials/{name}.yaml           │
└─────────────────────────────────────────────────────────────────────────┘
```

### Required Files

#### `Directory.Build.props`

Exposes `OpenApiSpecName` and `OpenApiSpecTitle` MSBuild properties to the Roslyn analyzer. This is required when the Source Generator is referenced via `ProjectReference` instead of NuGet `PackageReference` (the NuGet package ships a `.props` that handles this automatically).

```xml
<Project>
  <ItemGroup>
    <CompilerVisibleProperty Include="OpenApiSpecName" />
    <CompilerVisibleProperty Include="OpenApiSpecTitle" />
  </ItemGroup>
</Project>
```

#### `Directory.Build.targets`

Defines the `ExtractOpenApiYaml` inline task and the `ExportOpenApiPartialSpec` target that runs after each producer build.

#### Producer `.csproj` Properties

Each producer project needs these properties:

```xml
<PropertyGroup>
  <!-- Customize namespace and title for the generated spec -->
  <OpenApiSpecName>Functions.Admin</OpenApiSpecName>
  <OpenApiSpecTitle>Admin API</OpenApiSpecTitle>

  <!-- Save source-generated .g.cs to disk for extraction -->
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>

  <!-- Name of the partial YAML exported to Functions.OpenApi/openapi/partials/ -->
  <OpenApiPartialName>admin</OpenApiPartialName>
</PropertyGroup>
```

| Property | Required | Description |
|----------|----------|-------------|
| `OpenApiSpecName` | Yes | Namespace override for the generated class |
| `OpenApiSpecTitle` | Yes | API title in the generated YAML `info.title` |
| `EmitCompilerGeneratedFiles` | Yes | Writes `.g.cs` to disk for extraction |
| `OpenApiPartialName` | Yes | Output filename (without `.yaml`) in `openapi/partials/` |

## Project Structure

```
MultiLambdaSample/
├── MultiLambdaSample.slnx
├── Directory.Build.props           # CompilerVisibleProperty for ProjectReference
├── Directory.Build.targets         # ExtractOpenApiYaml task + ExportOpenApiPartialSpec target
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
# Build all producers first, then the consumer.
# Each producer automatically extracts its YAML to Functions.OpenApi/openapi/partials/
dotnet build src/Functions.Admin/Functions.Admin.csproj
dotnet build src/Functions.Identity/Functions.Identity.csproj
dotnet build src/Functions.OpenId/Functions.OpenId.csproj

# Build the consumer (uses the extracted YAML partials)
dotnet build src/Functions.OpenApi/Functions.OpenApi.csproj
```

> **Note:** You cannot `dotnet build` the entire solution at once because all projects share `AssemblyName=bootstrap`, which causes NuGet restore ambiguity. Build each project individually in order.

## Key MSBuild Properties

| Property | Description | Example |
|----------|-------------|---------|
| `OpenApiSpecName` | Overrides the namespace for the generated class | `Functions.Admin` |
| `OpenApiSpecTitle` | Sets the title in the generated OpenAPI YAML | `Admin API` |
| `AssemblyName` | Must be `bootstrap` for AWS Lambda custom runtime | `bootstrap` |
| `RootNamespace` | Keeps the original namespace for C# code | `Functions.Admin` |
| `EmitCompilerGeneratedFiles` | Writes `.g.cs` to disk for YAML extraction | `true` |
| `OpenApiPartialName` | Output filename in `openapi/partials/` | `admin` |

## How the Consumer Works

`Functions.OpenApi` uses `Native.OpenApi` library to:

1. **Load common parts** — schemas, responses, security definitions (3 YAML files)
2. **Load partial specs** — one per producer Lambda (3 YAML files)
3. **Merge** all parts into a single OpenAPI 3.1.0 document
4. **Lint** the merged document for consistency
5. **Serve** the consolidated spec via Redoc and Scalar viewers
