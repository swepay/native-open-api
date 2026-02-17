# Changelog

All notable changes to this project will be documented in this file.

## [1.4.0] - 2026-02-12

### Added
- **Source Generator**: Real OpenAPI schema property generation from C# types via Roslyn introspection.
  Previously, all schemas were emitted as placeholder stubs (`type: object`, `description: "Request type - properties to be documented"`).
  Now the Source Generator extracts actual properties from record and class types, including:
  - Property names (converted to camelCase for JSON)
  - OpenAPI types and formats (`string`, `integer/int32`, `number/double`, `boolean`, `string/date-time`, `string/uuid`, etc.)
  - `required` array based on nullability annotations (nullable properties excluded)
  - Array/List properties rendered as `type: array` with proper `items`
  - Enum properties rendered with `enum:` values list
  - Complex nested types referenced via `$ref: "#/components/schemas/TypeName"`
  - Dictionary types rendered as `type: object`
- **Source Generator**: New `TypePropertyExtractor` class for mapping Roslyn `ITypeSymbol`/`IPropertySymbol` to OpenAPI schema properties.
  Uses `SpecialType` and type `Name` for reliable type identification across different compilation contexts.
- **Source Generator**: New `SchemaPropertyInfo` and `SchemaTypeInfo` data classes for property metadata.
- **Source Generator**: `EndpointInfo` extended with `CommandProperties`, `ResponseProperties`, `CommandPropertiesResolved`, `ResponsePropertiesResolved`.
- **Tests**: 11 new tests covering schema property generation: record properties, nullable exclusion from required,
  integer/boolean/DateTime/Guid types, List/array properties, enum properties, class properties, empty class fallback,
  camelCase conversion, and complex real-world `CreateRoleRequest` scenario (139 total tests).

### Changed
- **Source Generator**: `OpenApiYamlGenerator` now renders real `properties:` and `required:` sections
  instead of placeholder descriptions when type properties are resolved.
- **Source Generator**: `OpenApiYamlGenerator` internal `TypeInfo` class replaced by `SchemaTypeInfo`
  with `BuildSchemaTypes()` and `MergeSchema()` for deduplication.
- **Test Helper**: Added `System.Collections` assembly reference to `GeneratorTestHelper` for proper
  `List<T>` resolution in test compilations.

### Fixed
- **Source Generator**: Schemas for request/response types now include actual property definitions,
  fixing the issue where importing the generated spec showed empty schemas.
- **Source Generator**: Optional/nullable parameters (e.g., `string? Description`, `List<string>? PermissionIds`)
  are no longer included in the `required` array.

## [1.3.3] - 2026-02-11

### Added
- **MultiLambdaSample**: Automated YAML extraction via `Directory.Build.targets` with inline
  MSBuild task (`ExtractOpenApiYaml`). Producer projects now automatically export their generated
  OpenAPI partial specs to the consumer project's `openapi/partials/` directory on every build.
  No manual YAML copy is required.
- **MultiLambdaSample**: `Directory.Build.props` that declares `CompilerVisibleProperty` for
  `OpenApiSpecName` and `OpenApiSpecTitle`, enabling MSBuild→Roslyn property bridging when the
  Source Generator is referenced via `ProjectReference` instead of NuGet `PackageReference`.
- **MultiLambdaSample**: New MSBuild properties per producer `.csproj`:
  - `EmitCompilerGeneratedFiles=true` — persists `.g.cs` to disk for extraction
  - `OpenApiPartialName` — controls the output filename in `openapi/partials/`

### Changed
- **MultiLambdaSample**: Updated `Functions.OpenApi.csproj` comments to document the automated
  extraction approach (replaces manual copy / build-specs.ps1 instructions).
- **Documentation**: Updated `MultiLambdaSample/README.md` with full documentation of the automated
  YAML extraction workflow, required files, and MSBuild properties.

## [1.3.2] - 2026-02-11

### Fixed
- **Source Generator**: Fluent chain calls `.AllowAnonymous()` and `.Produces("contentType")` are now
  detected by walking up the Roslyn syntax tree from the `MapGet/MapPost/...` invocation node.
  Previously, endpoints with `.AllowAnonymous()` were always emitted with `security: [JwtBearer: []]`
  in the generated YAML, and `.Produces(...)` content types were ignored (always `application/json`).

### Added
- **Source Generator**: New `ApplyFluentChainOptions` method that inspects parent
  `InvocationExpressionSyntax` nodes for chained method calls on `IRouteEndpointBuilder`.
- **Tests**: 5 new tests covering fluent chain detection: `AllowAnonymous`, `Produces`,
  combined chains, mixed auth/anonymous endpoints, and reversed chain order (128 total).
- **Test Helper**: `CreateRouteBuilderSource()` now includes `IRouteEndpointBuilder` interface with
  `AllowAnonymous()`, `Produces(string)`, and `WithHeader(string, string)` methods, matching the
  NativeLambdaRouter 2.x fluent API.

## [1.3.1] - 2026-02-11

### Added
- **Source Generator**: New MSBuild properties `OpenApiSpecName` and `OpenApiSpecTitle` for customizing
  the generated namespace and API title.
  - `OpenApiSpecName` overrides the assembly name used as namespace base (`{value}.Generated`).
  - `OpenApiSpecTitle` overrides the API title in the generated YAML `info.title`.
  - Both properties are optional; the generator falls back to `AssemblyName` when not set.
- **Source Generator**: Bundled `.props` file auto-imported via NuGet that exposes
  `OpenApiSpecName` and `OpenApiSpecTitle` as `CompilerVisibleProperty` to the Roslyn analyzer.
- **Tests**: 6 new tests covering `OpenApiSpecName`/`OpenApiSpecTitle` customization,
  fallback behavior, and multi-bootstrap-project scenarios (123 total).

### Fixed
- **Source Generator**: AWS Lambda custom runtime projects using `AssemblyName=bootstrap`
  can now produce unique namespaces per function project via `OpenApiSpecName`.

## [1.3.0] - 2026-02-11

### ⚠️ Breaking Changes
- **Source Generator**: Generated class namespace changed from `Native.OpenApi.Generated` to `{AssemblyName}.Generated`.
  This enables multi-project architectures where each project gets its own unique namespace, avoiding conflicts.
- **Source Generator**: Generated class changed from `static` to `sealed` with a `public static readonly Instance` singleton.
- **Source Generator**: `Yaml` constant renamed to `YamlContent`.
- **Source Generator**: `Endpoints` field renamed to `EndpointList`.

### Added
- **Native.OpenApi**: New `IGeneratedOpenApiSpec` interface for polymorphic access to generated specs.
- **Source Generator**: When `NativeOpenApi` package is referenced, the generated class implements `IGeneratedOpenApiSpec` automatically.
  When not referenced, the class is generated standalone (no interface dependency).
- **Native.OpenApi**: New `LoadFromGeneratedSpec(name, spec)` method on `OpenApiDocumentLoaderBase`
  for loading generated specs as `OpenApiDocumentPart` for merging.
- **Native.OpenApi**: New `LoadFromYaml(name, yaml)` method on `OpenApiDocumentLoaderBase`
  for loading raw YAML strings as `OpenApiDocumentPart`.
- **Native.OpenApi**: YAML deserializer now uses lazy initialization with automatic fallback from
  static (AOT) to dynamic (reflection) mode for better test compatibility.

### Changed
- **Documentation**: Updated all READMEs with multi-project architecture examples showing how to
  consolidate specs from Admin, Identity, OpenId into a single OpenAPI document.

## [1.2.7] - 2026-02-11

### Fixed
- **Documentation**: Clarified that `EmitCompilerGeneratedFiles` is **not required** for the Source Generator
  to work. It only saves a physical copy of generated files to disk for debug/inspection purposes.
  The generated `GeneratedOpenApiSpec` class is injected directly into compilation in memory.

### Changed
- **Sample README**: Added explicit "optional, only for debug" callout around `EmitCompilerGeneratedFiles`
  with full explanation of the generated file path.
- **Generator README**: Added note and collapsible section explaining the difference between
  the generator working (automatic) vs. inspecting generated files on disk (optional).

## [1.2.6] - 2025-07-16

### Fixed
- **Source Generator**: Fixed endpoint detection fallback when `IMethodSymbol` cannot be resolved from semantic model.
  Added `CandidateSymbols` fallback and syntax-based extraction for cases where NuGet package methods
  are not fully resolved during incremental generation.
- **Source Generator**: Removed `IncludeBuildOutput=false` from csproj to fix `ProjectReference` with
  `OutputItemType="Analyzer"` not loading the generator DLL.

### Added
- **Sample Project**: Complete working sample `SampleApiFunction` demonstrating:
  - `RoutedApiGatewayFunction` (NativeLambdaRouter 2.0.2) with 6 REST endpoints
  - `IRequestHandler<TCommand, TResponse>` handlers (NativeMediator 1.0.4)
  - `JsonSerializerContext` for Native AOT serialization
  - Source Generator producing OpenAPI 3.1 spec at compile-time

### Removed
- Deleted `TROUBLESHOOTING.md` (issues resolved)
- Deleted `TempInspect` temporary inspection project
- Deleted stale `Models.cs` from sample (caused duplicate type definitions without `IRequest<T>`)

## [1.2.5] - 2025-07-15

### Fixed
- **Source Generator**: Enhanced endpoint detection to support NativeLambdaRouter 2.x API.
  Now checks receiver type interfaces and extension method patterns.

## [1.2.0] - 2025-07-14

### Added
- **NativeLambdaRouter.SourceGenerator.OpenApi**: Roslyn Source Generator that discovers
  `MapGet<TCommand, TResponse>`, `MapPost`, `MapPut`, `MapDelete`, `MapPatch` invocations
  on `IRouteBuilder` and generates OpenAPI 3.1 YAML specifications at compile-time.

## [1.1.0] - 2025-07-13

### Added
- YAML file loading support via `OpenApiDocumentLoader` with AOT-compatible parsing.

## [1.0.0] - 2025-07-12

### Added
- Initial release of `Native.OpenApi` library.
- `OpenApiDocument`, `OpenApiDocumentLoader`, `OpenApiDocumentMerger`.
- `OpenApiLinter` with configurable validation rules.
- `OpenApiHtmlRenderer` for documentation generation.
- `OpenApiResourceReader` for embedded resource loading.
- Full Native AOT compatibility (zero reflection).
