using System.Text.Json.Nodes;

namespace Native.OpenApi;

/// <summary>
/// Represents a fully loaded and merged OpenAPI document.
/// </summary>
/// <param name="Root">The JSON root node of the merged document.</param>
/// <param name="Json">The serialized JSON representation.</param>
/// <param name="Yaml">The serialized YAML representation.</param>
/// <param name="Version">The OpenAPI specification version (e.g., 3.1.0).</param>
/// <param name="LoadedAt">The timestamp when the document was loaded.</param>
/// <param name="Stats">Statistics about the loading process.</param>
public sealed record OpenApiDocument(
    JsonNode Root,
    string Json,
    string Yaml,
    string Version,
    DateTimeOffset LoadedAt,
    OpenApiLoadStats Stats);

/// <summary>
/// Statistics about the OpenAPI document loading process.
/// </summary>
/// <param name="LoadDuration">Time taken to load and merge the document.</param>
/// <param name="ResourceCount">Number of source files loaded.</param>
/// <param name="PathCount">Number of API paths in the merged document.</param>
public sealed record OpenApiLoadStats(
    TimeSpan LoadDuration,
    int ResourceCount,
    int PathCount);
