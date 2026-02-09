using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Syntax receiver that collects all route mapping invocations.
/// </summary>
internal sealed class RouteMappingSyntaxReceiver : ISyntaxContextReceiver
{
    private static readonly HashSet<string> MapMethods = new(StringComparer.Ordinal)
    {
        "MapGet",
        "MapPost",
        "MapPut",
        "MapDelete",
        "MapPatch",
        "Map"
    };

    public List<EndpointInfo> Endpoints { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;

        // Check if it's a method invocation like routes.MapGet<...>(...) or chained .MapGet<...>(...)
        var methodName = GetMethodName(invocation);
        if (methodName == null || !MapMethods.Contains(methodName))
            return;

        // Get semantic model to resolve types
        var semanticModel = context.SemanticModel;
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        // Check if the method is from IRouteBuilder or RouteBuilder
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return;

        var typeName = containingType.ToDisplayString();
        if (!typeName.Contains("IRouteBuilder") && !typeName.Contains("RouteBuilder"))
            return;

        // Extract endpoint information
        var endpoint = ExtractEndpointInfo(invocation, methodSymbol, semanticModel);
        if (endpoint != null)
        {
            Endpoints.Add(endpoint);
        }
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
            IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
            _ => null
        };
    }

    private static EndpointInfo? ExtractEndpointInfo(
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol,
        SemanticModel semanticModel)
    {
        // Get type arguments (TCommand, TResponse)
        if (methodSymbol.TypeArguments.Length < 2)
            return null;

        var commandType = methodSymbol.TypeArguments[0];
        var responseType = methodSymbol.TypeArguments[1];

        // Get HTTP method from method name or first argument
        var httpMethod = GetHttpMethod(methodSymbol.Name, invocation);
        if (httpMethod == null)
            return null;

        // Get path from arguments
        var path = GetPathArgument(invocation, semanticModel);
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

        return null;
    }

    private static string? GetPathArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0)
            return null;

        // For MapGet/MapPost etc., path is the first argument
        // For Map, path is the second argument
        var methodName = GetMethodName(invocation);
        var pathIndex = methodName == "Map" ? 1 : 0;

        if (arguments.Count <= pathIndex)
            return null;

        var pathArg = arguments[pathIndex].Expression;

        // Handle string literal
        if (pathArg is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
        {
            return literal.Token.ValueText;
        }

        // Handle interpolated strings or other expressions
        var constantValue = semanticModel.GetConstantValue(pathArg);
        if (constantValue.HasValue && constantValue.Value is string stringValue)
        {
            return stringValue;
        }

        return null;
    }
}
