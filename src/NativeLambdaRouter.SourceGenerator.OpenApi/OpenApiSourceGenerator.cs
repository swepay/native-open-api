using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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

        // Combine with analyzer config options to read MSBuild properties (OpenApiSpecName, OpenApiSpecTitle)
        var compilationEndpointsAndOptions = compilationAndEndpoints.Combine(context.AnalyzerConfigOptionsProvider);

        // Generate OpenAPI spec
        context.RegisterSourceOutput(compilationEndpointsAndOptions, static (spc, source) =>
        {
            var ((compilation, endpoints), optionsProvider) = source;
            Execute(spc, compilation, endpoints, optionsProvider);
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

        // Try the resolved symbol first, then fall back to candidate symbols
        IMethodSymbol? methodSymbol = symbolInfo.Symbol as IMethodSymbol;
        if (methodSymbol == null && symbolInfo.CandidateSymbols.Length > 0)
        {
            methodSymbol = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
        }

        if (methodSymbol == null)
        {
            // Last resort: try to extract from the syntax tree directly (for generic invocations)
            return TryExtractFromSyntax(invocation, semanticModel);
        }

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
        ImmutableArray<EndpointInfo> endpoints,
        AnalyzerConfigOptionsProvider optionsProvider)
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

        // Read MSBuild properties: OpenApiSpecName overrides the namespace base, OpenApiSpecTitle overrides the API title
        optionsProvider.GlobalOptions.TryGetValue("build_property.OpenApiSpecName", out var specName);
        optionsProvider.GlobalOptions.TryGetValue("build_property.OpenApiSpecTitle", out var specTitle);

        var baseName = !string.IsNullOrWhiteSpace(specName)
            ? specName!.Trim()
            : (compilation.AssemblyName ?? "API");

        var apiTitle = !string.IsNullOrWhiteSpace(specTitle)
            ? specTitle!.Trim()
            : baseName.Replace(".", " ");

        var generatedNamespace = baseName + ".Generated";

        // Generate OpenAPI YAML
        var yaml = OpenApiYamlGenerator.Generate(endpoints.ToList(), apiTitle, "1.0.0");

        // Check if Native.OpenApi is referenced (for IGeneratedOpenApiSpec support)
        var hasNativeOpenApi = compilation.ReferencedAssemblyNames
            .Any(a => string.Equals(a.Name, "Native.OpenApi", StringComparison.OrdinalIgnoreCase));

        // Generate a C# class that embeds the OpenAPI spec
        var source = GenerateOpenApiProviderClass(generatedNamespace, yaml, endpoints, hasNativeOpenApi);

        context.AddSource("GeneratedOpenApiSpec.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateOpenApiProviderClass(
        string generatedNamespace,
        string yaml,
        ImmutableArray<EndpointInfo> endpoints,
        bool implementInterface)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {generatedNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated OpenAPI specification from NativeLambdaRouter endpoints.");
        sb.AppendLine("/// </summary>");

        if (implementInterface)
        {
            sb.AppendLine("public sealed class GeneratedOpenApiSpec : Native.OpenApi.IGeneratedOpenApiSpec");
        }
        else
        {
            sb.AppendLine("public sealed class GeneratedOpenApiSpec");
        }

        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Singleton instance for interface-based access.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static readonly GeneratedOpenApiSpec Instance = new();");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// The generated OpenAPI YAML specification.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public const string YamlContent = @\"");

        // Escape the YAML for C# string literal
        var escapedYaml = yaml.Replace("\"", "\"\"");
        sb.Append(escapedYaml);

        sb.AppendLine("\";");
        sb.AppendLine();

        if (implementInterface)
        {
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine("    string Native.OpenApi.IGeneratedOpenApiSpec.Yaml => YamlContent;");
            sb.AppendLine();
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine($"    int Native.OpenApi.IGeneratedOpenApiSpec.EndpointCount => {endpoints.Length};");
            sb.AppendLine();
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine("    (string Method, string Path)[] Native.OpenApi.IGeneratedOpenApiSpec.Endpoints => EndpointList;");
            sb.AppendLine();
        }

        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// The number of endpoints discovered.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public const int EndpointCount = {endpoints.Length};");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets the endpoint paths and methods.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static readonly (string Method, string Path)[] EndpointList = new[]");
        sb.AppendLine("    {");

        foreach (var endpoint in endpoints.OrderBy(e => e.Path).ThenBy(e => e.Method))
        {
            sb.AppendLine($"        (\"{endpoint.Method}\", \"{endpoint.Path}\"),");
        }

        sb.AppendLine("    };");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Fallback extraction from syntax when the semantic model cannot resolve the method symbol.
    /// This handles cases where NuGet package types are not fully resolved during generation.
    /// </summary>
    private static EndpointInfo? TryExtractFromSyntax(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        // Check if the name is a generic name like MapGet<TCommand, TResponse>
        if (memberAccess.Name is not GenericNameSyntax genericName)
            return null;

        var methodName = genericName.Identifier.Text;
        if (methodName is not ("MapGet" or "MapPost" or "MapPut" or "MapDelete" or "MapPatch" or "Map"))
            return null;

        // Verify the receiver looks like a route builder parameter
        var receiverTypeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
        if (receiverTypeInfo.Type != null)
        {
            var receiverTypeName = receiverTypeInfo.Type.ToDisplayString();
            if (!IsRouteBuilderType(receiverTypeName))
            {
                // Check implemented interfaces
                var isRouteBuilder = false;
                foreach (var iface in receiverTypeInfo.Type.AllInterfaces)
                {
                    if (IsRouteBuilderType(iface.ToDisplayString()))
                    {
                        isRouteBuilder = true;
                        break;
                    }
                }
                if (!isRouteBuilder)
                    return null;
            }
        }

        // Get type arguments from the generic name syntax
        var typeArgs = genericName.TypeArgumentList.Arguments;
        if (typeArgs.Count < 2)
            return null;

        var commandTypeInfo = semanticModel.GetTypeInfo(typeArgs[0]);
        var responseTypeInfo = semanticModel.GetTypeInfo(typeArgs[1]);

        var commandTypeName = commandTypeInfo.Type?.ToDisplayString() ?? typeArgs[0].ToString();
        var responseTypeName = responseTypeInfo.Type?.ToDisplayString() ?? typeArgs[1].ToString();
        var commandSimpleName = commandTypeInfo.Type?.Name ?? typeArgs[0].ToString();
        var responseSimpleName = responseTypeInfo.Type?.Name ?? typeArgs[1].ToString();

        // Get HTTP method
        var httpMethod = GetHttpMethod(methodName, invocation);
        if (httpMethod == null)
            return null;

        // Get path
        var path = GetPathArgument(invocation, semanticModel, methodName);
        if (path == null)
            return null;

        // Get source location
        var location = invocation.GetLocation();
        var lineSpan = location.GetLineSpan();

        return new EndpointInfo
        {
            Method = httpMethod,
            Path = path,
            CommandTypeName = commandTypeName,
            ResponseTypeName = responseTypeName,
            CommandSimpleName = commandSimpleName,
            ResponseSimpleName = responseSimpleName,
            SourceFile = lineSpan.Path,
            LineNumber = lineSpan.StartLinePosition.Line + 1
        };
    }
}
