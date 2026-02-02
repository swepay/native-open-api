using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Serialization;

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
/// Supports both JSON (.json) and YAML (.yaml, .yml) file formats.
/// </summary>
public abstract class OpenApiDocumentLoaderBase : IOpenApiDocumentLoader
{
    private readonly OpenApiResourceReader _resourceReader;

    /// <summary>
    /// Shared YAML deserializer instance (thread-safe after construction).
    /// </summary>
    private static readonly IDeserializer YamlDeserializer = new StaticDeserializerBuilder(new OpenApiYamlStaticContext())
        .Build();

    /// <summary>
    /// Shared YAML to JSON serializer instance (thread-safe after construction).
    /// </summary>
    private static readonly ISerializer YamlToJsonSerializer = new StaticSerializerBuilder(new OpenApiYamlStaticContext())
        .JsonCompatible()
        .Build();

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
    /// Automatically detects JSON or YAML format based on file extension.
    /// </summary>
    /// <param name="name">A logical name for the part.</param>
    /// <param name="path">The relative path to the resource (.json, .yaml, or .yml).</param>
    /// <returns>The loaded document part.</returns>
    protected OpenApiDocumentPart Load(string name, string path)
    {
        var raw = _resourceReader.ReadText(path);
        var json = ConvertToJson(raw, path);
        var root = JsonNode.Parse(json, null, new JsonDocumentOptions { AllowTrailingCommas = true })
                   ?? throw new InvalidOperationException($"OpenAPI resource '{path}' is empty.");
        return new OpenApiDocumentPart(name, path, root, raw);
    }

    /// <summary>
    /// Converts the raw content to JSON if it's in YAML format.
    /// </summary>
    private static string ConvertToJson(string raw, string path)
    {
        if (IsYamlFile(path))
        {
            var yamlObject = YamlDeserializer.Deserialize<object>(raw);
            return YamlToJsonSerializer.Serialize(yamlObject);
        }

        return raw;
    }

    /// <summary>
    /// Determines if the file is a YAML file based on its extension.
    /// </summary>
    private static bool IsYamlFile(string path)
    {
        return path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
    }
}
