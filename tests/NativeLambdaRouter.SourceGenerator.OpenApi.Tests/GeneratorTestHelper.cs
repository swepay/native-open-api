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
        };

        // Add System.Runtime
        var runtimeAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
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

public interface IRouteBuilder
{
    void MapGet<TCommand, TResponse>(string path, Func<object, TCommand> handler);
    void MapPost<TCommand, TResponse>(string path, Func<object, TCommand> handler);
    void MapPut<TCommand, TResponse>(string path, Func<object, TCommand> handler);
    void MapDelete<TCommand, TResponse>(string path, Func<object, TCommand> handler);
    void MapPatch<TCommand, TResponse>(string path, Func<object, TCommand> handler);
    void Map<TCommand, TResponse>(string method, string path, Func<object, TCommand> handler);
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
