# Changelog

All notable changes to this project will be documented in this file.

## [1.8.2] - 2026-06-02 — Documentation

### Changed

- **READMEs updated for v1.8.0 features.** Root `README.md`, `src/Native.OpenApi/README.md`
  and `src/NativeLambdaRouter.SourceGenerator.OpenApi/README.md` now document the full
  v1.8.0 surface: all 20 new attributes (navigation, operation richness, schema richness,
  polymorphism, document-level, structural), `OpenApiScalarViewerOptions`, the new emitted
  OpenAPI keywords/extensions, and the `components/responses` + `securitySchemes` fixes.
  Includes v1.8.0 feature matrices and YAML output snippets. Attribute names/signatures
  verified against source. Docs-only release; no code or behavior changes.

## [1.8.1] - 2026-06-02 — Packaging & security fixes

### Fixed

- **NuGet pack (NU5128 / NU5017).** The source-generator package now ships its assembly via
  `BuildOutputTargetFolder=analyzers/dotnet/cs` (real build output, dependencies suppressed),
  producing no `lib/ref` folder. This clears both NU5128 (lib without matching dependency
  group) and NU5017 (no dependencies nor content); `dotnet pack --no-build` exits 0 and the
  generator loads correctly from the produced package.

### Security

- **YamlDotNet 16.3.0 → 18.0.0** in `Native.OpenApi`, the generator test project and the
  central `Directory.Packages.props`. Clears the vulnerable 16.3.0 and resolves the NU1605
  downgrade against `NativeOpenApi`'s transitive requirement.

## [1.8.0] - 2026-06-02 — OpenAPI 3.1 documentation feature expansion

Large opt-in expansion of OpenAPI 3.1 documentation features rendered by **Scalar**
(reference UI). Policy: **Scalar-first** on any Redoc/Scalar divergence (uses
`x-scalar-*`/`x-enum-*` extensions, no dual-emit). All additions are opt-in; no
existing API breaks. Declaration mechanism: assembly-level attributes (document
scope) and command-type / property attributes (operation / schema scope).

### Added — Native.OpenApi (attributes)

- **Navigation:** `[assembly: TagMetadata(name, Description, DisplayName, ExternalDocs*)]`,
  `[assembly: TagGroup(name, tags[])]`, `[assembly: OpenApiExternalDocs(url, Description)]`,
  `[EndpointExternalDocs(url, Description)]` — emit root `tags` (with `description` +
  `externalDocs`), `x-tagGroups`, `x-displayName`, and `externalDocs` (root + per-operation).
- **Operation richness:** `[CodeSample(lang, source, Label)]` (multi-use → `x-codeSamples`),
  `[OperationBadge(name, Position, Color)]` (multi-use → `x-badges`),
  `[ScalarStability(Stability)]` → `x-scalar-stability` (`stable`/`experimental`/`deprecated`).
- **Schema richness:** `[OpenApiProperty(...)]` on properties — `description`, `example`,
  `default`, constraints (`minLength`/`maxLength`/`pattern`/`minimum`/`maximum`/`exclusive*`/
  `multipleOf`/`minItems`/`maxItems`/`uniqueItems`), `x-order`, `x-additionalPropertiesName`.
  Also reads **DataAnnotations** (`[Required]`, `[StringLength]`, `[MinLength]`, `[MaxLength]`,
  `[Range]`, `[RegularExpression]`). `[OpenApiEnumMember(Description, DisplayName)]` on enum
  fields → `x-enum-descriptions` + `x-enum-varnames`.
- **Polymorphism:** `[OpenApiDiscriminator("prop")]` + `[OpenApiSubType(typeof(T), "value")]`
  on a base class → `oneOf` + `discriminator` (with `mapping`); `allOf` auto-detected from
  C# inheritance.
- **Document:** `[assembly: OpenApiInfo(Description, Summary, TermsOfService, Contact*, License*)]`
  → rich `info`; `[assembly: OpenApiServer(url, Description)]` (multi-use) → `servers`.
  `[ApiExample]` extended with `RequestValue`/`ResponseValue` → inline `value:` examples
  (alongside the existing `externalValue`).
- **Structural (OpenAPI 3.1):** `[QueryParameter(...)]`, `[HeaderParameter(...)]` →
  `in: query`/`in: header` parameters; `[ResponseHeader(statusCode, name, type, ...)]` →
  response `headers`; `[assembly: Webhook(name, typeof(Payload), Method, ...)]` → top-level
  `webhooks`; `[ResponseLink(statusCode, linkId, OperationId, Parameters, ...)]` →
  response `links`; `[Callback(name, Expression, Method, PayloadType)]` → operation
  `callbacks` (minimal form).

### Added — NativeLambdaRouter.SourceGenerator.OpenApi

- Roslyn consumers for all attributes above (command-type via `ApplyCommandAttributes`,
  document-scope via `compilation.Assembly.GetAttributes()`), plus YAML emission for every
  new keyword/extension. Webhook/callback payload types flow through `TypePropertyExtractor`
  so their schemas are fully resolved in `components/schemas`.
- Deterministic emission preserved (all collections sorted).

### Added — Rendering

- **`OpenApiScalarViewerOptions`** (`OpenApiRendererOptions.ScalarViewer`): `Theme`,
  `DarkMode`, `Layout`, `HideModels`, `HideDownloadButton`, `HideSidebar`,
  `HideTestRequestButton`, `DefaultHttpClientTargetKey`/`ClientKey`, `LocalAssetPath`
  (air-gap). Logo wired into Scalar's native config. Back-compat overloads preserved.

### Added — Samples

- **`SampleApiFunction`**: new `ApiDocumentation.cs` (assembly-level info/servers/tag
  metadata/tag groups/external docs/webhook); commands and responses exercising code
  samples, badges, stability, schema richness, a polymorphic `PaymentMethod` hierarchy,
  inline examples, query/header params, response headers, and links.

### Changed

- **Polymorphic base schemas** now emit pure `oneOf` + `discriminator`; shared base
  properties are factored into a synthetic `{Base}__Core` schema referenced by each
  subtype via `allOf` (strict-conformance, avoids `oneOf`+`properties` siblings that
  trip strict validators).
- **Enum descriptions** emit only the Scalar form (`x-enum-descriptions`/`x-enum-varnames`),
  no Redoc `x-enumDescriptions` dual-emit (Scalar-first policy).
- Operation `tags` are now quoted/escaped, supporting tag names with special characters.

### Fixed

- **`EscapeYamlString`** now escapes `\n`/`\r` — multi-line `description`/`summary`
  (e.g. rich `info.description`) no longer produce malformed multi-line double-quoted
  YAML scalars that strict parsers reject.
- **`components/responses`** (`BadRequest`, `Unauthorized`, `InternalServerError`) are now
  emitted — previously every endpoint `$ref`-ed them but the section was never produced,
  yielding dangling references.
- **`components/securitySchemes`** now emits the `JwtBearer` (`http`/`bearer`/`JWT`) scheme
  referenced by authenticated operations — previously referenced but never defined.
- **XSS hardening** in the Scalar renderer: config delivered via an HTML-escaped attribute
  instead of a JS string literal; `HtmlEscape` now also escapes `'`.

### Notes

- Test suite grew from 123 to **459** tests (added YamlDotNet-based conformance/parse
  regression tests). Generated spec validates as OpenAPI 3.1 and is deterministic.
- AOT compliance verification was descoped for this wave (zero-reflection runtime and
  `netstandard2.0` generator practices retained; no dedicated IL2xxx/IL3xxx pass).

## [1.7.0] - 2026-04-16 — RFC Wave 1 (UX documentation)

Implements Wave 1 of `docs/RFC-DOCUMENTACAO-UX.md`. All additions are opt-in; no
existing API breaks. Principle O5 (full retrocompat) holds.

### Added — Native.OpenApi

- **`[HideFromDocs]`** attribute (`Native.OpenApi.Attributes`) — hides a command's
  operation from the generated spec (RFC § F01).
- **`.ExcludeFromDocs()`** fluent extension (`Native.OpenApi.Extensions.OpenApiRouteExtensions`)
  — per-mapping sibling of `[HideFromDocs]`. Runtime-safe identity pass-through;
  the source generator reacts to the syntactic presence of the call.
- **`[Deprecated(sunset, alternative, reason)]`** attribute — emits
  `deprecated: true` plus `x-sunset`, `x-swepay-alternative`,
  `x-swepay-deprecation-reason` on the operation (RFC § F03).
- **`[ApiExample(name, summary) { RequestJson, ResponseStatus, ResponseJson }]`**
  attribute (multi-use) — declarative named examples (RFC § F09).
- **`[ErrorCatalog(typeof(T))]`** + **`[ErrorDefinition(code, httpStatus, userMessage, recovery) { DocUrl }]`**
  — declarative error catalog resolved by the generator across catalog classes (RFC § F12).
- **`SwepayProblemDetails`** record in `Native.OpenApi.Models` — canonical error
  payload (RFC 9457 superset with `code`, `recovery`, `requestId`); schema is
  auto-emitted whenever any endpoint serves `application/problem+json` without a
  typed body (RFC § F13).
- **`OpenApiRendererOptions`** + `OpenApiBrandingOptions` + `OpenApiFooterOptions`
  records — drive branding colour/logo/favicon/font, institutional footer links,
  and optional Mermaid.js rendering (RFC §§ F15, F16, F17).
- **`OpenApiHtmlRenderer.RenderRedoc(spec, title, options)`** and
  **`RenderScalar(spec, title, options)`** overloads — apply the new options.
  Legacy two-arg overloads preserved unchanged.

### Added — NativeLambdaRouter.SourceGenerator.OpenApi

- Attribute consumers for `[HideFromDocs]`, `[Deprecated]`, `[ApiExample]`,
  `[ErrorCatalog]`; fluent-chain recognition of `.ExcludeFromDocs()`.
- Cross-type resolution of `[ErrorDefinition]`-tagged fields in catalog classes;
  per-operation `x-swepay-errors` slice and document-level `x-swepay-error-catalog`.
- Automatic injection of `components.schemas.SwepayProblemDetails` whenever any
  operation serves `application/problem+json` without a typed body (replaces the
  former untyped `type: object` fallback).
- New `CompilerVisibleProperty` MSBuild props: `OpenApiBrandPrimaryColor`,
  `OpenApiBrandAccentColor`, `OpenApiBrandLogoUrl`, `OpenApiBrandFavicon`,
  `OpenApiBrandFontFamily`, `OpenApiBrandThemeJson`, `OpenApiFooterStatusUrl`,
  `OpenApiFooterSupportUrl`, `OpenApiFooterChangelogUrl`, `OpenApiFooterSlaUrl`,
  `OpenApiFooterTermsUrl`, `OpenApiInlineAssets`, `OpenApiEnableMermaid`,
  `OpenApiServerProduction`, `OpenApiServerSandbox`, `OpenApiDefaultAudience`.

### Added — Samples

- **`SampleApiFunction`**: new `SwepayErrors.cs` catalog; `[HideFromDocs]` on
  `HealthCheckCommand`; `[Deprecated]` + `[ErrorCatalog]` on `GetItemsCommand`;
  `[ApiExample]` + `[ErrorCatalog]` on `CreateItemCommand`; new
  `/internal/diagnostics` route demonstrating fluent `.ExcludeFromDocs()`;
  `[ApiResponse(422, null, "application/problem+json")]` on `CreateItemHandler`
  to exercise F13 auto-injection.
- **`MultiLambdaSample/Functions.Admin`**: new `SwepayErrors.cs`; `[Deprecated]`
  on the legacy `PUT /v1/admin/users/{id}` (PATCH variant now lives on a new
  `PatchUserRoleCommand` so only the PUT is marked deprecated); `[ApiExample]`
  on `CreateUserCommand`; `[ErrorCatalog]` across all command types; internal
  `.ExcludeFromDocs()` route at `/v1/admin/internal/users`.
- **`MultiLambdaSample/Functions.OpenApi`**: Redoc and Scalar routes now
  construct an `OpenApiRendererOptions` with Swepay brand colours, logo,
  favicon, footer links (Status, Support, Changelog, SLA, Terms) and
  `EnableMermaid = true`.

### Rewrote — Documentation (agent-first UX)

- Root `README.md`: decision tree, Wave 1 feature matrix, MSBuild property
  table, end-to-end walkthrough, repository map. Agent-readable sections first;
  humans get the same tables.
- `src/Native.OpenApi/README.md`: API surface tables (attributes, extensions,
  models, rendering, core classes); Wave 1 quick reference with YAML output
  snippets.
- `src/NativeLambdaRouter.SourceGenerator.OpenApi/README.md`: inspection
  surface, precedence rule, MSBuild property table, multi-project recipe for
  `AssemblyName=bootstrap`.

### Changed

- `problem+json` responses now `$ref` the shared `SwepayProblemDetails` schema
  instead of emitting inline `type: object` — only when no typed body was
  declared. Consumers serializing `ProblemDetails` into responses that declare
  a type are unaffected. When migrating, `SwepayProblemDetails` is a strict
  superset of RFC 9457 (same field set plus `code`, `recovery`, `requestId`).
- `OpenApiYamlGenerator.Generate(...)` gains an `errorCatalog` parameter.
  A backwards-compatible zero-catalog overload is kept for existing callers.

### Fixed

- XML documentation warnings on `ApplyProduces` (unescaped generic) and on
  `OpenApiRouteExtensions` class-level `paramref`. Docs now build clean.

### Known limitations / deferred to Wave 2

- `[ApiExample]` payloads are referenced via `externalValue` today; inline payload
  read from embedded resources at generation time is a Wave 2 follow-up (requires
  wiring JSON files as `AdditionalFiles` in the consumer `.csproj`).
- Mermaid rendering in Redoc is implemented via a post-mount `MutationObserver`
  (no template fork). Trade-off documented in RFC Open Question #2.
- Flow (F05) / State Machine (F06) extensions consumed by the Mermaid pre-processor
  are emitted by Wave 2 — the renderer already scans for `` ```mermaid `` blocks in
  descriptions today.
- F03 Redoc deprecation banner and F12 "Error Catalog" filterable table are
  rendered natively / raw-only for now (Redoc shows the `deprecated` badge;
  `x-swepay-error-catalog` is present in the YAML but not styled by the
  template). Banner + table UI are Wave 2 renderer follow-ups.

## [1.6.0] - 2026-02-22

### Added
- **Native.OpenApi**: New `ApiResponseAttribute` for documenting HTTP responses directly on handler methods. This attribute can be applied multiple times to specify different response types with status codes, response types, and content types.
- **Source Generator**: Automatic detection of `[ApiResponse]` attributes on handler methods (`IRequestHandler<TCommand, TResponse>.Handle`). The generator now scans all handler implementations in the assembly and extracts response documentation, merging it into the generated OpenAPI specification.
- **Source Generator**: New `ApplyHandlerApiResponseAttributes()` method that finds handlers for each command type and reads `[ApiResponse]` attributes from their `Handle` methods.
- **Source Generator**: New `ExtractApiResponseAttributes()` helper method to parse `[ApiResponse]` attribute data and convert to `ProducesInfo` entries.
- **Source Generator**: New `GetAllTypes()` recursive helper to enumerate all types in an assembly, including nested types, for handler discovery.
- **Tests**: 4 new Source Generator tests for `[ApiResponse]` attribute detection: basic multi-response scenario, different content types, multiple handlers with correct handler matching, and attribute parsing validation.
- **Tests**: New `ApiResponseAttributeTests` test suite with 10 tests covering constructor parameters, defaults, attribute usage metadata, multiple attributes, and various status code scenarios.
- **Sample**: Updated `SampleApiFunction` handlers to demonstrate `[ApiResponse]` usage with `ErrorResponse` and `ProblemDetails` types.
- **Documentation**: Added comprehensive `ApiResponse Attribute` section to `Native.OpenApi/README.md` with usage examples, parameter documentation, and complete handler examples.
- **Documentation**: Added `Handler-Based Response Attributes (v1.6.0+)` section to `NativeLambdaRouter.SourceGenerator.OpenApi/README.md` explaining the new handler-based approach and its benefits.

### Removed
- **Source Generator**: ❌ **BREAKING** - Removed support for `.Produces<T>(statusCode)` and `.Produces(statusCode, contentType)` fluent chain methods. Use `[ApiResponse]` attributes on handler methods instead for documenting typed responses. The `.ProducesProblem(statusCode)` method remains supported for problem+json error responses.
- **Source Generator**: Removed `ApplyGenericProduces()` and `ApplyNonGenericProduces()` methods.
- **Documentation**: Removed all references to `.Produces<T>()` from README files.

### Changed
- **Native.OpenApi**: Added project reference to `Native.OpenApi` in sample projects to enable `[ApiResponse]` attribute usage.
- **Sample Models**: Added `ErrorResponse` and `ProblemDetails` record types to `SampleApiFunction/Responses.cs` for demonstration purposes.
- **ProducesInfo**: Updated class documentation to reflect that responses come from `.ProducesProblem()` or `[ApiResponse]` attributes.
- **EndpointInfo**: Updated `AdditionalProduces` property documentation.

### Migration Guide

**Before (v1.5.x):**
```csharp
routes.MapGet<GetItemCommand, GetItemResponse>("/v1/items/{id}", ctx => new GetItemCommand(ctx.PathParameters["id"]))
    .Produces<NotFoundError>(404)
    .Produces<ErrorResponse>(400);
```

**After (v1.6.0):**
```csharp
// In your handler:
public class GetItemHandler : IRequestHandler<GetItemCommand, GetItemResponse>
{
    [ApiResponse(200, typeof(GetItemResponse))]
    [ApiResponse(404, typeof(NotFoundError))]
    [ApiResponse(400, typeof(ErrorResponse))]
    public ValueTask<GetItemResponse> Handle(GetItemCommand request, CancellationToken cancellationToken)
    {
        // ... implementation
    }
}
```

**Why this change?**
- Co-located documentation (responses defined next to handler logic)
- Better type safety at compile time
- Follows Swashbuckle/ASP.NET Core conventions
- Avoids naming conflict with `NativeLambdaRouter.Produces()`

### Technical Details
- The `[ApiResponse]` attribute follows Swashbuckle naming conventions for familiarity with existing .NET tooling.
- Handler discovery uses Roslyn's semantic model to find `IRequestHandler<TCommand, TResponse>` implementations and match them with routed endpoints.
- Response definitions from `[ApiResponse]` attributes are merged with fluent chain `.ProducesProblem()` calls, providing complementary documentation approaches.
- The attribute is AOT-compatible and generates no runtime overhead — all processing happens at compile time via the Source Generator.

## [Unreleased] - 2026-03-14

### Added
- **MultiLambdaSample**: Expanded producer routes to cover all mapper variants supported by the Source Generator:
  - `MapGet`, `MapPost`, `MapPut`, `MapPatch`, `MapDelete`
  - `Map<TCommand, TResponse>("OPTIONS", ... )` for custom HTTP method mapping
- **MultiLambdaSample**: Added richer metadata examples across routes using fluent chain methods:
  - `.WithName()`, `.WithSummary()`, `.WithDescription()`, `.WithTags()`
  - `.Produces<T>(statusCode)` and `.ProducesProblem(statusCode)`
  - `.Accepts("application/x-www-form-urlencoded")` and `.AllowAnonymous()`
- **MultiLambdaSample**: Added attribute-based metadata examples in command types:
  - `[EndpointName]`, `[EndpointSummary]`, `[EndpointDescription]`, `[Tags]`, `[Accepts]`
  - Includes explicit fluent-over-attribute precedence scenario in Identity login route.
- **MultiLambdaSample**: Added additional command/response models and handlers to enrich generated schemas and required/nullable behavior.

### Changed
- **Documentation**: Rewrote `samples/MultiLambdaSample/README.md` as a human + AI friendly playbook with:
  - feature coverage matrix,
  - deterministic build flow,
  - endpoint inventory,
  - operational checklist for agents.
- **Documentation**: Updated root `README.md` sample navigation to include `MultiLambdaSample` as the complete reference sample and clarified when to use each sample.

## [1.5.1] - 2026-02-19

### Added
- **Source Generator**: Support for `application/x-www-form-urlencoded` request bodies via
  `.Accepts("application/x-www-form-urlencoded")` fluent chain method and `[Accepts]` attribute.
  Previously, all POST/PUT/PATCH endpoints were generated with `application/json` and a `$ref` to
  the command schema. Now, form-encoded endpoints emit an inline `type: object` schema with
  individual string properties extracted from the TCommand type, matching the OpenAPI 3.1
  convention for HTML form submissions and OAuth2 token endpoints.
- **Source Generator**: New `AcceptsContentType` field on `EndpointInfo` to store the request
  body content type. When null, defaults to `application/json` with `$ref` schema (backward
  compatible).
- **Source Generator**: `AppendFormEncodedSchema()` helper in `OpenApiYamlGenerator` that emits
  inline form field properties with `type: string` and proper `required` arrays based on
  nullability. Falls back to a description placeholder when properties cannot be resolved.
- **Source Generator**: `[Accepts]` attribute support on TCommand types — fluent chain
  `.Accepts()` takes precedence over the attribute, following the same convention as other
  metadata methods.
- **Tests**: 7 new Source Generator tests for form-encoded request bodies: fluent chain,
  required fields, no `$ref`, full fluent chain with metadata, default JSON fallback,
  attribute-based, and fluent-over-attribute precedence.
- **Tests**: 3 new YAML generator unit tests for form-encoded content type, unresolved fallback,
  and default JSON `$ref` behavior.
- **Test Helper**: `IRouteEndpointBuilder` mock extended with `.Accepts(string contentType)`.
- **Test Helper**: `CreateAttributeSource()` extended with `AcceptsAttribute` definition.

### Changed
- **Source Generator**: `OpenApiYamlGenerator` request body section now uses
  `endpoint.AcceptsContentType` to determine the content type. For
  `application/x-www-form-urlencoded`, properties are emitted inline as string fields instead
  of using `$ref` to the command schema.
- **Source Generator**: `ApplyFluentChainOptions` extended to detect `.Accepts()` calls.
- **Source Generator**: `ApplyCommandAttributes` extended to read `[Accepts]` attribute.

## [1.5.0] - 2026-02-12

### Added
- **Source Generator**: OpenAPI metadata support via fluent chain methods following ASP.NET Core
  Minimal APIs patterns:
  - `.WithName("operationId")` — sets a custom `operationId` for the endpoint
  - `.WithSummary("text")` — sets a custom `summary` for the endpoint
  - `.WithDescription("text")` — adds a `description` field to the endpoint
  - `.WithTags("Tag1", "Tag2")` — overrides auto-generated tags with custom tag list
  - `.Produces<T>(statusCode)` — adds an additional typed response with `$ref` schema
  - `.ProducesProblem(statusCode)` — adds a `application/problem+json` error response
- **Source Generator**: Attribute-based metadata on TCommand types as an alternative to fluent
  chain methods. Supported attributes (namespace `NativeLambdaRouter.OpenApi.Attributes`):
  - `[EndpointName("operationId")]` — equivalent to `.WithName()`
  - `[EndpointSummary("text")]` — equivalent to `.WithSummary()`
  - `[EndpointDescription("text")]` — equivalent to `.WithDescription()`
  - `[Tags("Tag1", "Tag2")]` — equivalent to `.WithTags()`
- **Source Generator**: Fluent chain methods take precedence over attributes when both are
  specified on the same endpoint, following ASP.NET Core conventions.
- **Source Generator**: `ProducesProblem` responses for status codes that overlap with default
  error responses (400, 401, 500) replace the default `$ref` responses instead of duplicating.
- **Source Generator**: New `GetStatusCodeDescription()` helper maps HTTP status codes to
  standard descriptions (200→OK, 201→Created, 400→Bad Request, 404→Not Found, etc.).
- **Models**: New `ProducesInfo` class representing additional response definitions with
  `StatusCode`, `ResponseTypeName`, and `ContentType` fields.
- **Models**: `EndpointInfo` extended with `OperationName`, `Summary`, `Description`, `Tags`,
  and `AdditionalProduces` fields.
- **Tests**: 15 new Source Generator tests covering all metadata scenarios (WithName, WithSummary,
  WithDescription, WithTags, ProducesProblem, Produces<T>, full fluent chain, all attributes,
  fluent-over-attribute precedence, auto-generation fallback, ProducesProblem override).
- **Tests**: 5 new YAML generator unit tests for metadata-aware rendering (custom operationId,
  custom summary, description inclusion, custom tags, additional produces responses).

### Changed
- **Source Generator**: `OpenApiYamlGenerator` now uses metadata-provided `operationId`,
  `summary`, `description`, and `tags` when available, falling back to auto-generation.
- **Source Generator**: `ApplyFluentChainOptions` extended to parse all new fluent methods.
- **Source Generator**: Both `TransformToEndpointInfo` and `TryExtractFromSyntax` now call
  `ApplyCommandAttributes` to read metadata from TCommand type attributes via Roslyn.
- **Test Helper**: `IRouteEndpointBuilder` mock extended with `WithName`, `WithSummary`,
  `WithDescription`, `WithTags`, `Produces<T>`, `ProducesProblem` methods.
- **Test Helper**: New `CreateAttributeSource()` method providing attribute definitions
  for compile-time testing.

## [1.4.1] - 2026-02-12

### Fixed
- **Linter**: `security: []` (empty array) is now recognized as a valid anonymous endpoint marker
  per OpenAPI 3.1 specification. Previously, the linter required a non-empty `security` block on
  every operation, incorrectly rejecting endpoints marked with `.AllowAnonymous()`.
- **Merger**: Duplicate component schemas with identical definitions are now silently skipped
  instead of throwing `InvalidOperationException`. This fixes runtime errors when multiple Lambda
  partials reference the same shared types (e.g., `ErrorResponse`, `PaginatedResponse`).
  Conflicting definitions (same key, different content) still throw with a detailed error message
  showing both definitions for easier debugging.
- **Source Generator**: Anonymous endpoints (`.AllowAnonymous()`) now emit `security: []` in the
  generated YAML instead of omitting the `security` block entirely. This follows the OpenAPI 3.1
  convention where `security: []` explicitly overrides any global security requirement.

### Changed
- **Sample YAMLs**: Updated `identity.yaml` and `openid.yaml` partials in `MultiLambdaSample`
  to include `security: []` on all anonymous endpoints.

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
