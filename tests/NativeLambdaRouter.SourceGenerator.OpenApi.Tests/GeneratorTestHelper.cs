using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Helper class for running source generator tests.
/// </summary>
internal static class GeneratorTestHelper
{
    /// <summary>
    /// Runs the OpenApiSourceGenerator on the given source code and returns the compilation result.
    /// </summary>
    public static GeneratorDriverRunResult RunGenerator(string sourceCode, string assemblyName = "TestAssembly", params MetadataReference[] additionalReferences)
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

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

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
