using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Native.OpenApi;

/// <summary>
/// Provides a cached OpenAPI document with warm-up capability.
/// </summary>
public class OpenApiDocumentProvider
{
    private readonly IOpenApiDocumentLoader _loader;
    private readonly OpenApiDocumentMerger _merger;
    private readonly OpenApiLinter _linter;
    private OpenApiDocument? _document;
    private int _loadCount;

    /// <summary>
    /// Initializes the provider with required dependencies.
    /// </summary>
    public OpenApiDocumentProvider(IOpenApiDocumentLoader loader, OpenApiDocumentMerger merger, OpenApiLinter linter)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _merger = merger ?? throw new ArgumentNullException(nameof(merger));
        _linter = linter ?? throw new ArgumentNullException(nameof(linter));
    }

    /// <summary>
    /// Gets the loaded document. Throws if WarmUp has not been called.
    /// </summary>
    public OpenApiDocument Document => _document ?? throw new InvalidOperationException("OpenAPI document not initialized. Call WarmUp() first.");

    /// <summary>
    /// Gets the number of times the document has been loaded.
    /// </summary>
    public int LoadCount => _loadCount;

    /// <summary>
    /// Loads, merges, and validates the OpenAPI document.
    /// </summary>
    /// <exception cref="OpenApiValidationException">Thrown when validation fails.</exception>
    public void WarmUp()
    {
        if (_document is not null)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        var commonParts = _loader.LoadCommon();
        var partials = _loader.LoadPartials();

        var allErrors = new List<string>();

        foreach (var part in commonParts.Concat(partials))
        {
            var requirePaths = !part.SourcePath.Contains("/common/", StringComparison.OrdinalIgnoreCase)
                               && !part.SourcePath.Contains("\\common\\", StringComparison.OrdinalIgnoreCase);
            allErrors.AddRange(_linter.Lint(part.SourcePath, part.Root, requirePaths));
        }

        if (commonParts.Count < 3)
        {
            throw new InvalidOperationException("LoadCommon() must return at least 3 parts: schemas, responses, and security.");
        }

        var merged = _merger.Merge(commonParts[0], commonParts[1], commonParts[2], partials);
        allErrors.AddRange(_linter.Lint("merged", merged));

        if (allErrors.Count > 0)
        {
            throw new OpenApiValidationException(allErrors);
        }

        var json = JsonSerializer.Serialize(merged, OpenApiJsonContext.Default.JsonNode);

        var yaml = json; // YAML conversion can be added if needed

        stopwatch.Stop();

        var pathCount = merged["paths"] is JsonObject paths ? paths.Count : 0;

        _document = new OpenApiDocument(
            merged,
            json,
            yaml,
            merged["openapi"]?.GetValue<string>() ?? "3.1.0",
            DateTimeOffset.UtcNow,
            new OpenApiLoadStats(stopwatch.Elapsed, commonParts.Count + partials.Count, pathCount));

        _loadCount++;
    }
}
