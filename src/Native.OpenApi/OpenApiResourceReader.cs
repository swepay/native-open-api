using System.Reflection;

namespace Native.OpenApi;

/// <summary>
/// Utility for reading OpenAPI resources embedded in an assembly.
/// </summary>
public sealed class OpenApiResourceReader
{
    private readonly Assembly _assembly;
    private readonly string _baseNamespace;

    /// <summary>
    /// Initializes a new resource reader.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded resources.</param>
    /// <param name="baseNamespace">The base namespace prefix for resource names.</param>
    public OpenApiResourceReader(Assembly assembly, string baseNamespace)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _baseNamespace = baseNamespace ?? throw new ArgumentNullException(nameof(baseNamespace));
    }

    /// <summary>
    /// Reads an embedded resource as text.
    /// </summary>
    /// <param name="relativePath">The relative path to the resource (using / or \ separators).</param>
    /// <returns>The resource content as a string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the resource is not found.</exception>
    public string ReadText(string relativePath)
    {
        var resourceName = _baseNamespace + relativePath.Replace('/', '.').Replace('\\', '.');
        using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"OpenAPI resource '{relativePath}' not found. Expected resource name: '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Lists all embedded resource names in the assembly.
    /// </summary>
    public IReadOnlyList<string> ListResources()
    {
        return _assembly.GetManifestResourceNames();
    }
}
