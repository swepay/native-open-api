# Changelog

All notable changes to this project will be documented in this file.

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
