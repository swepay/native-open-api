using System.Text.Json.Nodes;

namespace Native.OpenApi;

/// <summary>
/// Represents a single source file/part of an OpenAPI specification.
/// </summary>
/// <param name="Name">A logical name for this part (e.g., 'admin', 'identity').</param>
/// <param name="SourcePath">The original path or resource name where this was loaded from.</param>
/// <param name="Root">The parsed JSON root node.</param>
/// <param name="Raw">The raw content as loaded from the source.</param>
public sealed record OpenApiDocumentPart(
    string Name,
    string SourcePath,
    JsonNode Root,
    string Raw);
