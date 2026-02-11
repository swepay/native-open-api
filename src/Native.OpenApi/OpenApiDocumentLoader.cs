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
    /// Lazy-initialized YAML deserializer (uses static context for AOT, falls back to dynamic).
    /// </summary>
    private static readonly Lazy<IDeserializer> LazyYamlDeserializer = new(CreateDeserializer);

    /// <summary>
    /// Lazy-initialized YAML-to-JSON serializer (uses static context for AOT, falls back to dynamic).
    /// </summary>
    private static readonly Lazy<ISerializer> LazyYamlToJsonSerializer = new(CreateJsonSerializer);

    private static IDeserializer YamlDeserializer => LazyYamlDeserializer.Value;
    private static ISerializer YamlToJsonSerializer => LazyYamlToJsonSerializer.Value;

    private static IDeserializer CreateDeserializer()
    {
        try
        {
            return new StaticDeserializerBuilder(new OpenApiYamlStaticContext()).Build();
        }
        catch (NotImplementedException)
        {
            // Fallback to dynamic deserializer (non-AOT environments such as tests)
#pragma warning disable IL3050, IL2026
            return new DeserializerBuilder().Build();
#pragma warning restore IL3050, IL2026
        }
    }

    private static ISerializer CreateJsonSerializer()
    {
        try
        {
            return new StaticSerializerBuilder(new OpenApiYamlStaticContext()).JsonCompatible().Build();
        }
        catch (NotImplementedException)
        {
            // Fallback to dynamic serializer (non-AOT environments such as tests)
#pragma warning disable IL3050, IL2026
            return new SerializerBuilder().JsonCompatible().Build();
#pragma warning restore IL3050, IL2026
        }
    }

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

    /// <summary>
    /// Creates an <see cref="OpenApiDocumentPart"/> from an <see cref="IGeneratedOpenApiSpec"/>
    /// produced by the NativeLambdaRouter Source Generator.
    /// </summary>
    /// <param name="name">A logical name for the part (e.g., 'admin', 'identity').</param>
    /// <param name="spec">The generated OpenAPI spec instance.</param>
    /// <returns>The loaded document part.</returns>
    protected static OpenApiDocumentPart LoadFromGeneratedSpec(string name, IGeneratedOpenApiSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var raw = spec.Yaml;
        var yamlObject = YamlDeserializer.Deserialize<object>(raw);
        var json = YamlToJsonSerializer.Serialize(yamlObject);
        var root = JsonNode.Parse(json, null, new JsonDocumentOptions { AllowTrailingCommas = true })
                   ?? throw new InvalidOperationException($"Generated OpenAPI spec '{name}' produced empty content.");
        return new OpenApiDocumentPart(name, $"generated:{name}", root, raw);
    }

    /// <summary>
    /// Creates an <see cref="OpenApiDocumentPart"/> from raw YAML content.
    /// This is useful when you have the YAML string directly (e.g., from a generated constant).
    /// </summary>
    /// <param name="name">A logical name for the part (e.g., 'admin', 'identity').</param>
    /// <param name="yaml">The raw OpenAPI YAML content.</param>
    /// <returns>The loaded document part.</returns>
    protected static OpenApiDocumentPart LoadFromYaml(string name, string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        var yamlObject = YamlDeserializer.Deserialize<object>(yaml);
        var json = YamlToJsonSerializer.Serialize(yamlObject);
        var root = JsonNode.Parse(json, null, new JsonDocumentOptions { AllowTrailingCommas = true })
                   ?? throw new InvalidOperationException($"YAML spec '{name}' produced empty content.");
        return new OpenApiDocumentPart(name, $"yaml:{name}", root, yaml);
    }
}
