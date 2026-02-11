namespace Native.OpenApi;

/// <summary>
/// Contract for auto-generated OpenAPI specifications produced by the NativeLambdaRouter Source Generator.
/// Each consuming project generates its own implementation with a unique namespace.
/// </summary>
public interface IGeneratedOpenApiSpec
{
    /// <summary>
    /// Gets the OpenAPI YAML specification content.
    /// </summary>
    string Yaml { get; }

    /// <summary>
    /// Gets the number of discovered endpoints.
    /// </summary>
    int EndpointCount { get; }

    /// <summary>
    /// Gets the discovered endpoint methods and paths.
    /// </summary>
    (string Method, string Path)[] Endpoints { get; }
}
