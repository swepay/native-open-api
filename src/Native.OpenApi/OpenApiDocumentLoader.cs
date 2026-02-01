using System.Text.Json.Nodes;

namespace Native.OpenApi;

/// <summary>
/// Interface for loading OpenAPI document parts from embedded resources or other sources.
/// </summary>
public interface IOpenApiDocumentLoader
{
    /// <summary>
    /// Loads the common shared components (schemas, responses, security).
    /// </summary>
    /// <returns>A list of common document parts.</returns>
    IReadOnlyList<OpenApiDocumentPart> LoadCommon();

    /// <summary>
    /// Loads the partial specifications (endpoints grouped by domain).
    /// </summary>
    /// <returns>A list of partial document parts.</returns>
    IReadOnlyList<OpenApiDocumentPart> LoadPartials();
}

/// <summary>
/// Base class for loading OpenAPI document parts from embedded resources.
/// </summary>
public abstract class OpenApiDocumentLoaderBase : IOpenApiDocumentLoader
{
    private readonly OpenApiResourceReader _resourceReader;

    /// <summary>
    /// Initializes the loader with a resource reader.
    /// </summary>
    protected OpenApiDocumentLoaderBase(OpenApiResourceReader resourceReader)
    {
        _resourceReader = resourceReader ?? throw new ArgumentNullException(nameof(resourceReader));
    }

    /// <summary>
    /// Loads common document parts. Override to specify your common resources.
    /// </summary>
    public abstract IReadOnlyList<OpenApiDocumentPart> LoadCommon();

    /// <summary>
    /// Loads partial document parts. Override to specify your partial resources.
    /// </summary>
    public abstract IReadOnlyList<OpenApiDocumentPart> LoadPartials();

    /// <summary>
    /// Loads a document part from an embedded resource.
    /// </summary>
    /// <param name="name">A logical name for the part.</param>
    /// <param name="path">The relative path to the resource.</param>
    /// <returns>The loaded document part.</returns>
    protected OpenApiDocumentPart Load(string name, string path)
    {
        var raw = _resourceReader.ReadText(path);
        var root = JsonNode.Parse(raw, null, new System.Text.Json.JsonDocumentOptions { AllowTrailingCommas = true })
                   ?? throw new InvalidOperationException($"OpenAPI resource '{path}' is empty.");
        return new OpenApiDocumentPart(name, path, root, raw);
    }
}
