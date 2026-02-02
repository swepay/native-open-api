using YamlDotNet.Serialization;

namespace Native.OpenApi;

/// <summary>
/// Static YAML context for AOT-compatible serialization.
/// This context enables YamlDotNet to work with Native AOT by providing
/// static type information instead of using reflection.
/// </summary>
[YamlStaticContext]
public partial class OpenApiYamlStaticContext : StaticContext
{
}
