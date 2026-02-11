using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Source Generator that discovers NativeLambdaRouter endpoints and generates OpenAPI specifications.
/// </summary>
[Generator]
public sealed class OpenApiSourceGenerator : IIncrementalGenerator
{
    // Diagnostic descriptors for debugging
    private static readonly DiagnosticDescriptor DebugEndpointFound = new(
        "NLOAPI001",
        "Endpoint Found",
        "Found endpoint: {0} {1}",
        "Debug",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DebugNoEndpoints = new(
        "NLOAPI002",
        "No Endpoints Found",
        "No NativeLambdaRouter endpoints were discovered. Ensure you are using MapGet<TCommand, TResponse>, MapPost<TCommand, TResponse>, etc. on IRouteBuilder.",
        "Debug",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DebugMethodFound = new(
        "NLOAPI003",
        "Map Method Invocation",
        "Found Map* invocation: {0}, ContainingType: {1}",
        "Debug",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// Initializes the incremental source generator.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all invocations of Map* methods
        var endpointInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsMapMethodInvocation(node),
                transform: static (ctx, _) => TransformToEndpointInfo(ctx))
            .Where(static endpoint => endpoint != null)
            .Select(static (endpoint, _) => endpoint!);

        // Collect all endpoints
        var collectedEndpoints = endpointInvocations.Collect();

        // Combine with compilation to get assembly name
        var compilationAndEndpoints = context.CompilationProvider.Combine(collectedEndpoints);

        // Generate OpenAPI spec
        context.RegisterSourceOutput(compilationAndEndpoints, static (spc, source) =>
        {
            var (compilation, endpoints) = source;
            Execute(spc, compilation, endpoints);
        });
    }

    private static bool IsMapMethodInvocation(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        var methodName = GetMethodName(invocation);
        if (methodName == null)
            return false;

        return methodName is "MapGet" or "MapPost" or "MapPut" or "MapDelete" or "MapPatch" or "Map";
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name switch
            {
                GenericNameSyntax genericName => genericName.Identifier.Text,
                IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
                _ => null
            },
            GenericNameSyntax genericName => genericName.Identifier.Text,
            _ => null
        };
    }

    private static EndpointInfo? TransformToEndpointInfo(GeneratorSyntaxContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return null;

        // Check if the method is from IRouteBuilder, RouteBuilder, or extension methods on these types
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return null;

        var typeName = containingType.ToDisplayString();
        
        // Also check if it's an extension method by looking at the first parameter type
        var isRouteBuilderMethod = IsRouteBuilderType(typeName);
        
        if (!isRouteBuilderMethod && methodSymbol.IsExtensionMethod && methodSymbol.Parameters.Length > 0)
        {
            var firstParamType = methodSymbol.Parameters[0].Type.ToDisplayString();
            isRouteBuilderMethod = IsRouteBuilderType(firstParamType);
        }
        
        // Also check the receiver type for instance methods
        if (!isRouteBuilderMethod && invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var receiverTypeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
            if (receiverTypeInfo.Type != null)
            {
                var receiverTypeName = receiverTypeInfo.Type.ToDisplayString();
                isRouteBuilderMethod = IsRouteBuilderType(receiverTypeName);
                
                // Also check interfaces implemented by the receiver
                if (!isRouteBuilderMethod)
                {
                    foreach (var iface in receiverTypeInfo.Type.AllInterfaces)
                    {
                        if (IsRouteBuilderType(iface.ToDisplayString()))
                        {
                            isRouteBuilderMethod = true;
                            break;
                        }
                    }
                }
            }
        }

        if (!isRouteBuilderMethod)
            return null;

        // Get type arguments (TCommand, TResponse)
        if (methodSymbol.TypeArguments.Length < 2)
            return null;

        var commandType = methodSymbol.TypeArguments[0];
        var responseType = methodSymbol.TypeArguments[1];

        // Get HTTP method
        var httpMethod = GetHttpMethod(methodSymbol.Name, invocation);
        if (httpMethod == null)
            return null;

        // Get path
        var path = GetPathArgument(invocation, semanticModel, methodSymbol.Name);
        if (path == null)
            return null;

        // Get source location
        var location = invocation.GetLocation();
        var lineSpan = location.GetLineSpan();

        return new EndpointInfo
        {
            Method = httpMethod,
            Path = path,
            CommandTypeName = commandType.ToDisplayString(),
            ResponseTypeName = responseType.ToDisplayString(),
            CommandSimpleName = commandType.Name,
            ResponseSimpleName = responseType.Name,
            SourceFile = lineSpan.Path,
            LineNumber = lineSpan.StartLinePosition.Line + 1
        };
    }

    private static bool IsRouteBuilderType(string typeName)
    {
        return typeName.Contains("IRouteBuilder") 
            || typeName.Contains("RouteBuilder")
            || typeName.Contains("NativeLambdaRouter");
    }

    private static string? GetHttpMethod(string methodName, InvocationExpressionSyntax invocation)
    {
        return methodName switch
        {
            "MapGet" => "GET",
            "MapPost" => "POST",
            "MapPut" => "PUT",
            "MapDelete" => "DELETE",
            "MapPatch" => "PATCH",
            "Map" => GetMethodFromFirstArgument(invocation),
            _ => null
        };
    }

    private static string? GetMethodFromFirstArgument(InvocationExpressionSyntax invocation)
    {
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0)
            return null;

        var firstArg = arguments[0].Expression;
        if (firstArg is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
        {
            return literal.Token.ValueText.ToUpperInvariant();
        }

        // Handle HttpMethod.GET etc.
        if (firstArg is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.Text.ToUpperInvariant();
        }

        return null;
    }

    private static string? GetPathArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0)
            return null;

        // For MapGet/MapPost etc., path is the first argument
        // For Map, path is the second argument
        var pathIndex = methodName == "Map" ? 1 : 0;

        if (arguments.Count <= pathIndex)
            return null;

        var pathArg = arguments[pathIndex].Expression;

        // Handle string literal
        if (pathArg is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
        {
            return literal.Token.ValueText;
        }

        // Handle constant expressions
        var constantValue = semanticModel.GetConstantValue(pathArg);
        if (constantValue.HasValue && constantValue.Value is string stringValue)
        {
            return stringValue;
        }

        return null;
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<EndpointInfo> endpoints)
    {
        if (endpoints.IsDefaultOrEmpty)
        {
            // Emit warning when no endpoints found
            context.ReportDiagnostic(Diagnostic.Create(DebugNoEndpoints, Location.None));
            return;
        }

        // Emit info diagnostics for each endpoint found
        foreach (var endpoint in endpoints)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DebugEndpointFound, 
                Location.None, 
                endpoint.Method, 
                endpoint.Path));
        }

        var assemblyName = compilation.AssemblyName ?? "API";
        var apiTitle = assemblyName.Replace(".", " ");

        // Generate OpenAPI YAML
        var yaml = OpenApiYamlGenerator.Generate(endpoints.ToList(), apiTitle, "1.0.0");

        // Generate a C# class that embeds the OpenAPI spec
        var source = GenerateOpenApiProviderClass(assemblyName, yaml, endpoints);

        context.AddSource("GeneratedOpenApiSpec.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateOpenApiProviderClass(
        string assemblyName,
        string yaml,
        ImmutableArray<EndpointInfo> endpoints)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Native.OpenApi.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated OpenAPI specification from NativeLambdaRouter endpoints.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class GeneratedOpenApiSpec");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// The generated OpenAPI YAML specification.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public const string Yaml = @\"");

        // Escape the YAML for C# string literal
        var escapedYaml = yaml.Replace("\"", "\"\"");
        sb.Append(escapedYaml);

        sb.AppendLine("\";");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// The number of endpoints discovered.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public const int EndpointCount = {endpoints.Length};");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets the endpoint paths and methods.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static readonly (string Method, string Path)[] Endpoints = new[]");
        sb.AppendLine("    {");

        foreach (var endpoint in endpoints.OrderBy(e => e.Path).ThenBy(e => e.Method))
        {
            sb.AppendLine($"        (\"{endpoint.Method}\", \"{endpoint.Path}\"),");
        }

        sb.AppendLine("    };");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
