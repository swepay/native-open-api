# ADR-0001: `.Produces(statusCode)` declares an explicit no-body response

**Data:** 2026-11-25
**Status:** proposed
**Repositórios afetados:** `native-open-api`, `native-guard-backend` (consumer-side declaration only)

## Contexto

[swepay/native-open-api#58](https://github.com/swepay/native-open-api/issues/58) /
[swepay/native-guard-backend#154](https://github.com/swepay/native-guard-backend/issues/154)
report that `NativeLambdaRouter.SourceGenerator.OpenApi` has no way to declare a response that
deliberately has no body — e.g. `GET /userinfo` returning `401` with only a
`WWW-Authenticate` header, never a JSON payload. Before this change, an endpoint with no
typed `.Produces<T>(...)`/`.ProducesProblem(...)` entry for a status simply emitted no
`content` for it, which is indistinguishable (in the rendered YAML) from a fluent call the
generator silently failed to resolve — exactly the defect
`OpenApiLinter.ValidateContentSchemas` exists to catch, so it correctly rejects both the same
way. There was no way to tell the linter "this one is on purpose."

## Alternativas consideradas

**A. New verb — `ProducesNoBody(int statusCode)` / `ProducesEmpty(int statusCode)`.**

- Pro: self-documenting name; does not grow the overload set of an already-overloaded
  `Produces` identifier.
- Con: a third independent fluent-chain name for consumers to learn, when the existing
  `Produces` family (`Produces(string)`, `Produces<T>(int)`, `Produces<T>(int, string)`) is
  already the home for "declare something about this operation's response." The architect
  skill's own hierarchy (`GAPS_ROADMAP`/`architect` — "menor superfície pública: preferir
  sobrecarga com defaults a adicionar novo método") favors an overload here.

**B. Overload `Produces(int statusCode)` on the existing name (adopted).**

`IRouteEndpointBuilder.Produces(string contentType)` already exists (sets the implicit 200's
content type). A sibling overload taking only an `int` is unambiguous — no call site is valid
against both signatures — and reads as the natural degenerate case of the typed family:
`Produces(int)` (no body) → `Produces<T>(int)` (typed body, default content type) →
`Produces<T>(int, string)` (typed body, explicit content type).

- Pro: smallest surface addition (one overload, not a new verb); symmetric with the typed
  family already shipped in 1.5.0; a reader scanning a route's fluent chain sees one spelling
  (`Produces`) for "this is what my responses look like," whether a type is involved or not.
- Con: three overloads under one name takes slightly more effort to read at the call site
  than a dedicated verb — mitigated by the generator's own diagnostic (NLOAPI004) catching any
  call the overload resolution still leaves ambiguous to a human (e.g. passing a variable
  instead of a literal).

## Decisão

Option B. `.Produces(int statusCode)` (no generic argument, no second parameter) declares
`statusCode` as having no response body. `NativeLambdaRouter.SourceGenerator.OpenApi`
(≥ 1.10.0) renders it as a response object with `description`, any
`[ResponseHeader(statusCode, ...)]` entries, and the vendor marker `x-no-content: true` —
deliberately **not** a `content` key, since there is no body to document. `Native.OpenApi`'s
`OpenApiLinter.ValidateContentSchemas` (≥ 1.10.0) accepts a missing `content` only when this
marker is present; every other missing-`content` case remains an error.

This is purely a consumer-declared no-op extension, matching the existing pattern for
`Produces<T>(int)`/`Produces<T>(int, string)`/`ProducesProblem(int[, string])`/`Accepts(string)`
in `native-guard-backend`'s `OpenApiEndpointExtensions.cs` (read-only reference, this PR does
not modify that repo). `native-lambda-router`'s `IRouteEndpointBuilder` is unaffected — this is
purely a new syntactic shape the generator recognizes, like `ExcludeFromDocs<TBuilder>()` in
`Native.OpenApi.Extensions.OpenApiRouteExtensions`.

## Consequências

Positivas:

- Non-breaking: routes that never call `.Produces(int)` are byte-for-byte unchanged (proven by
  the full pre-existing test suite passing unmodified).
- One name (`Produces`) covers every "declare a response" shape; `ProducesProblem` stays
  dedicated to the RFC 9457 shape.
- `x-no-content: true` makes "no body" structurally distinguishable from "the generator
  dropped this silently," which is exactly what the linter needs to keep catching real bugs.

Negativas:

- Three `Produces` overloads with meaningfully different effects (content-type-only,
  typed-body, no-body) is slightly more to hold in mind than one verb per shape — judged
  acceptable per the smaller-surface tie-breaker.
- A consumer must still declare the `Produces(int statusCode)` no-op overload locally (same as
  every other fluent marker the generator reads) — documented in the migration note for
  `native-guard-backend` (swepay/native-guard-backend#154).

Neutras:

- `[ApiResponse(statusCode)]` (no `typeof`) already documented "no response body" in its own
  XML doc comment before this ADR; this change also makes the generator honor that for the
  attribute path (`ProducesInfo.NoBody = responseTypeSymbol == null`), fixing the same
  silent-drop defect there for consistency, not as a new API surface.
