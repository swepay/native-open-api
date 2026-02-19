using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Helper class for running source generator tests.
/// </summary>
internal static class GeneratorTestHelper
{
    /// <summary>
    /// Runs the OpenApiSourceGenerator on the given source code and returns the compilation result.
    /// </summary>
    public static GeneratorDriverRunResult RunGenerator(
        string sourceCode,
        string assemblyName = "TestAssembly",
        params MetadataReference[] additionalReferences)
    {
        return RunGenerator(sourceCode, assemblyName, globalOptions: null, additionalReferences);
    }

    /// <summary>
    /// Runs the OpenApiSourceGenerator on the given source code with custom MSBuild properties and returns the compilation result.
    /// </summary>
    public static GeneratorDriverRunResult RunGenerator(
        string sourceCode,
        string assemblyName,
        Dictionary<string, string>? globalOptions,
        params MetadataReference[] additionalReferences)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
        };

        // Add System.Runtime
        var runtimeAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        // Add System.Collections for List<T>, Dictionary<T,V>, etc.
        var collectionsAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Collections");
        if (collectionsAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(collectionsAssembly.Location));
        }

        references.AddRange(additionalReferences);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new OpenApiSourceGenerator();

        var optionsProvider = globalOptions != null
            ? new TestAnalyzerConfigOptionsProvider(globalOptions)
            : null;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new ISourceGenerator[] { generator.AsSourceGenerator() },
            optionsProvider: optionsProvider);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return driver.GetRunResult();
    }

    /// <summary>
    /// Gets the generated source code from the generator result.
    /// </summary>
    public static string? GetGeneratedSource(GeneratorDriverRunResult result, string hintName)
    {
        return result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains(hintName))
            ?.GetText()
            .ToString();
    }

    /// <summary>
    /// Creates a mock IRouteBuilder source for testing.
    /// </summary>
    public static string CreateRouteBuilderSource()
    {
        return @"
namespace TestApp;

public interface IRouteEndpointBuilder
{
    IRouteEndpointBuilder AllowAnonymous();
    IRouteEndpointBuilder Produces(string contentType);
    IRouteEndpointBuilder Produces(int statusCode);
    IRouteEndpointBuilder Produces(int statusCode, string contentType);
    IRouteEndpointBuilder Produces<T>(int statusCode);
    IRouteEndpointBuilder Produces<T>(int statusCode, string contentType);
    IRouteEndpointBuilder ProducesProblem(int statusCode);
    IRouteEndpointBuilder ProducesProblem(int statusCode, string contentType);
    IRouteEndpointBuilder WithName(string name);
    IRouteEndpointBuilder WithSummary(string summary);
    IRouteEndpointBuilder WithDescription(string description);
    IRouteEndpointBuilder WithTags(params string[] tags);
    IRouteEndpointBuilder Accepts(string contentType);
    IRouteEndpointBuilder WithHeader(string name, string value);
    IRouteEndpointBuilder RequireRole(params string[] roles);
}

public interface IRouteBuilder
{
    IRouteEndpointBuilder MapGet<TCommand, TResponse>(string path, Func<object, TCommand> handler);
    IRouteEndpointBuilder MapPost<TCommand, TResponse>(string path, Func<object, TCommand> handler);
    IRouteEndpointBuilder MapPut<TCommand, TResponse>(string path, Func<object, TCommand> handler);
    IRouteEndpointBuilder MapDelete<TCommand, TResponse>(string path, Func<object, TCommand> handler);
    IRouteEndpointBuilder MapPatch<TCommand, TResponse>(string path, Func<object, TCommand> handler);
    IRouteEndpointBuilder Map<TCommand, TResponse>(string method, string path, Func<object, TCommand> handler);
}
";
    }

    /// <summary>
    /// Creates attribute definitions for testing attribute-based metadata.
    /// </summary>
    public static string CreateAttributeSource()
    {
        return @"
namespace NativeLambdaRouter.OpenApi.Attributes;

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
public sealed class EndpointNameAttribute : System.Attribute
{
    public string Name { get; }
    public EndpointNameAttribute(string name) => Name = name;
}

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
public sealed class EndpointSummaryAttribute : System.Attribute
{
    public string Summary { get; }
    public EndpointSummaryAttribute(string summary) => Summary = summary;
}

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
public sealed class EndpointDescriptionAttribute : System.Attribute
{
    public string Description { get; }
    public EndpointDescriptionAttribute(string description) => Description = description;
}

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = false)]
public sealed class TagsAttribute : System.Attribute
{
    public string[] Tags { get; }
    public TagsAttribute(params string[] tags) => Tags = tags;
}

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
public sealed class AcceptsAttribute : System.Attribute
{
    public string ContentType { get; }
    public AcceptsAttribute(string contentType) => ContentType = contentType;
}
";
    }
}

/// <summary>
/// Test implementation of <see cref="AnalyzerConfigOptionsProvider"/> that exposes custom global MSBuild properties.
/// </summary>
internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly TestAnalyzerConfigOptions _globalOptions;

    public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
    {
        _globalOptions = new TestAnalyzerConfigOptions(globalOptions);
    }

    public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestAnalyzerConfigOptions.Empty;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestAnalyzerConfigOptions.Empty;
}

/// <summary>
/// Test implementation of <see cref="AnalyzerConfigOptions"/> backed by a dictionary.
/// </summary>
internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    public static readonly TestAnalyzerConfigOptions Empty = new(new Dictionary<string, string>());

    private readonly Dictionary<string, string> _options;

    public TestAnalyzerConfigOptions(Dictionary<string, string> options)
    {
        _options = options;
    }

    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        return _options.TryGetValue(key, out value);
    }
}
