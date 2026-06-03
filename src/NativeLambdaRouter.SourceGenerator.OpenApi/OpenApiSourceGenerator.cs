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

        var endpoint = new EndpointInfo
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

        // Extract type properties for schema generation
        ExtractTypeProperties(commandType, responseType, endpoint);

        // Detect fluent chain options (.AllowAnonymous(), .ProducesProblem(...), .WithName(), etc.)
        ApplyFluentChainOptions(invocation, endpoint);

        // Read metadata attributes from TCommand type ([EndpointName], [Tags], etc.)
        ApplyCommandAttributes(commandType, endpoint);

        // Read ApiResponse attributes from handler method
        ApplyHandlerApiResponseAttributes(commandType, responseType, endpoint);

        return endpoint;
    }

    private static bool IsRouteBuilderType(string typeName)
    {
        return typeName.Contains("IRouteBuilder")
            || typeName.Contains("RouteBuilder")
            || typeName.Contains("NativeLambdaRouter");
    }

    /// <summary>
    /// Extracts properties from command and response type symbols for schema generation.
    /// Also runs polymorphism-aware schema discovery to populate
    /// <see cref="EndpointInfo.ReferencedSchemas"/> with all transitively referenced
    /// schemas (including sub-types, base classes, and complex property types).
    /// </summary>
    private static void ExtractTypeProperties(ITypeSymbol commandType, ITypeSymbol responseType, EndpointInfo endpoint)
    {
        var commandProps = TypePropertyExtractor.Extract(commandType);
        if (commandProps.Count > 0)
        {
            endpoint.CommandProperties = commandProps;
            endpoint.CommandPropertiesResolved = true;
        }

        var responseProps = TypePropertyExtractor.Extract(responseType);
        if (responseProps.Count > 0)
        {
            endpoint.ResponseProperties = responseProps;
            endpoint.ResponsePropertiesResolved = true;
        }

        // Polymorphism-aware deep discovery: collect all transitively referenced schemas
        // (base classes, sub-types, complex property refs) with polymorphism metadata.
        var allDiscovered = new List<SchemaTypeInfo>();
        foreach (var schema in TypePropertyExtractor.DiscoverSchemas(commandType))
            allDiscovered.Add(schema);
        foreach (var schema in TypePropertyExtractor.DiscoverSchemas(responseType))
            allDiscovered.Add(schema);
        endpoint.ReferencedSchemas = allDiscovered;
    }

    /// <summary>
    /// Walks up the syntax tree from a Map* invocation to detect fluent chain calls
    /// such as .AllowAnonymous(), .WithName(), .WithDescription(),
    /// .WithSummary(), .WithTags(), and .ProducesProblem(statusCode).
    /// </summary>
    private static void ApplyFluentChainOptions(InvocationExpressionSyntax mapInvocation, EndpointInfo endpoint)
    {
        // In the Roslyn AST, a fluent chain like:
        //   routes.MapGet<Cmd, Rsp>("/path", ctx => new Cmd()).AllowAnonymous().ProducesProblem(500)
        // is represented as nested InvocationExpressions:
        //   ProducesProblem( AllowAnonymous( MapGet(...) ) )
        //
        // The MapGet invocation is the Expression inside the MemberAccess of AllowAnonymous(),
        // and AllowAnonymous() is the Expression inside the MemberAccess of Produces().
        //
        // So we walk UP from the MapGet node through parent nodes.

        SyntaxNode current = mapInvocation;

        while (current.Parent is MemberAccessExpressionSyntax parentMemberAccess
               && parentMemberAccess.Parent is InvocationExpressionSyntax parentInvocation)
        {
            var chainedMethodName = parentMemberAccess.Name switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                GenericNameSyntax gn => gn.Identifier.Text,
                _ => null
            };

            switch (chainedMethodName)
            {
                case "AllowAnonymous":
                    endpoint.RequiresAuth = false;
                    break;

                case "ProducesProblem":
                    ApplyProducesProblem(parentInvocation, endpoint);
                    break;

                case "Produces":
                    ApplyProduces(parentInvocation, endpoint);
                    break;

                case "WithName":
                    endpoint.OperationName = ExtractFirstStringArgument(parentInvocation);
                    break;

                case "WithSummary":
                    endpoint.Summary = ExtractFirstStringArgument(parentInvocation);
                    break;

                case "WithDescription":
                    endpoint.Description = ExtractFirstStringArgument(parentInvocation);
                    break;

                case "WithTags":
                    ExtractTagsArguments(parentInvocation, endpoint);
                    break;

                case "Accepts":
                    endpoint.AcceptsContentType = ExtractFirstStringArgument(parentInvocation);
                    break;

                // RFC-DOCUMENTACAO-UX § F01: per-mapping exclusion marker.
                case "ExcludeFromDocs":
                    endpoint.ExcludedFromDocs = true;
                    break;
            }

            current = parentInvocation;
        }
    }

    /// <summary>
    /// Handles .ProducesProblem(statusCode, contentType).
    /// </summary>
    private static void ApplyProducesProblem(InvocationExpressionSyntax invocation, EndpointInfo endpoint)
    {
        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0) return;

        var statusCode = ExtractIntArgument(args[0]) ?? 500;
        var contentType = "application/problem+json";
        if (args.Count > 1)
        {
            var ct = ExtractStringArgument(args[1]);
            if (ct != null) contentType = ct;
        }

        endpoint.AdditionalProduces.Add(new ProducesInfo
        {
            StatusCode = statusCode,
            ResponseTypeSimpleName = null,
            ContentType = contentType
        });
    }

    /// <summary>
    /// Handles <c>.Produces(contentType)</c> or <c>.Produces&lt;T&gt;(contentType)</c>.
    /// Sets the content type for the default 200 response.
    /// </summary>
    private static void ApplyProduces(InvocationExpressionSyntax invocation, EndpointInfo endpoint)
    {
        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0) return;

        var contentType = ExtractStringArgument(args[0]);
        if (!string.IsNullOrEmpty(contentType))
        {
            endpoint.ProducesContentType = contentType;
        }
    }

    /// <summary>
    /// Extracts all string arguments from .WithTags("A", "B", "C").
    /// </summary>
    private static void ExtractTagsArguments(InvocationExpressionSyntax invocation, EndpointInfo endpoint)
    {
        foreach (var arg in invocation.ArgumentList.Arguments)
        {
            if (arg.Expression is LiteralExpressionSyntax literal
                && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
            {
                endpoint.Tags.Add(literal.Token.ValueText);
            }
        }
    }

    /// <summary>
    /// Extracts the first string argument from a method invocation.
    /// </summary>
    private static string? ExtractFirstStringArgument(InvocationExpressionSyntax invocation)
    {
        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0) return null;

        return ExtractStringArgument(args[0]);
    }

    /// <summary>
    /// Extracts a string value from an argument.
    /// </summary>
    private static string? ExtractStringArgument(ArgumentSyntax argument)
    {
        if (argument.Expression is LiteralExpressionSyntax literal
            && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
        {
            return literal.Token.ValueText;
        }
        return null;
    }

    /// <summary>
    /// Extracts an integer value from an argument.
    /// </summary>
    private static int? ExtractIntArgument(ArgumentSyntax argument)
    {
        if (argument.Expression is LiteralExpressionSyntax literal
            && literal.Token.IsKind(SyntaxKind.NumericLiteralToken)
            && literal.Token.Value is int intValue)
        {
            return intValue;
        }
        return null;
    }

    /// <summary>
    /// Reads metadata attributes ([EndpointName], [EndpointSummary], [EndpointDescription], [Tags])
    /// from the TCommand type and applies them to the endpoint. Fluent chain calls take precedence.
    /// </summary>
    private static void ApplyCommandAttributes(ITypeSymbol commandType, EndpointInfo endpoint)
    {
        foreach (var attr in commandType.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == null) continue;

            switch (attrName)
            {
                case "EndpointNameAttribute" when endpoint.OperationName == null:
                    if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
                        endpoint.OperationName = name;
                    break;

                case "EndpointSummaryAttribute" when endpoint.Summary == null:
                    if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string summary)
                        endpoint.Summary = summary;
                    break;

                case "EndpointDescriptionAttribute" when endpoint.Description == null:
                    if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string desc)
                        endpoint.Description = desc;
                    break;

                case "TagsAttribute" when endpoint.Tags.Count == 0:
                    if (attr.ConstructorArguments.Length > 0)
                    {
                        var tagValues = attr.ConstructorArguments[0].Values;
                        foreach (var tv in tagValues)
                        {
                            if (tv.Value is string tag)
                                endpoint.Tags.Add(tag);
                        }
                    }
                    break;

                case "AcceptsAttribute" when endpoint.AcceptsContentType == null:
                    if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string accepts)
                        endpoint.AcceptsContentType = accepts;
                    break;

                // RFC § F01 — type-level exclusion.
                case "HideFromDocsAttribute":
                    endpoint.ExcludedFromDocs = true;
                    break;

                // RFC § F03 — deprecated (sunset, alternative, reason).
                case "DeprecatedAttribute":
                    if (attr.ConstructorArguments.Length >= 3
                        && attr.ConstructorArguments[0].Value is string sunset
                        && attr.ConstructorArguments[1].Value is string alternative
                        && attr.ConstructorArguments[2].Value is string reason)
                    {
                        endpoint.Deprecation = new DeprecationInfo
                        {
                            Sunset = sunset,
                            Alternative = alternative,
                            Reason = reason
                        };
                    }
                    break;

                // RFC § F09 — repeatable named example.
                case "ApiExampleAttribute":
                    var example = ExtractApiExample(attr);
                    if (example != null) endpoint.Examples.Add(example);
                    break;

                // RFC § F12 — typed catalog reference (resolution deferred to Execute).
                case "ErrorCatalogAttribute":
                    if (attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is INamedTypeSymbol catalogSym)
                    {
                        endpoint.ErrorCatalogTypeName = catalogSym.ToDisplayString();
                    }
                    break;

                // Navigation foundation — per-operation externalDocs.
                case "EndpointExternalDocsAttribute":
                    if (attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is string extDocsUrl)
                    {
                        var extDocs = new ExternalDocsInfo { Url = extDocsUrl };
                        foreach (var named in attr.NamedArguments)
                        {
                            if (named.Key == "Description" && named.Value.Value is string extDocsDesc)
                                extDocs.Description = extDocsDesc;
                        }
                        endpoint.ExternalDocs = extDocs;
                    }
                    break;

                // Operation Richness — x-codeSamples.
                case "CodeSampleAttribute":
                    var codeSample = ExtractCodeSample(attr);
                    if (codeSample != null) endpoint.CodeSamples.Add(codeSample);
                    break;

                // Operation Richness — x-badges.
                case "OperationBadgeAttribute":
                    var badge = ExtractOperationBadge(attr);
                    if (badge != null) endpoint.Badges.Add(badge);
                    break;

                // Operation Richness — x-scalar-stability (Scalar-first).
                case "ScalarStabilityAttribute":
                    if (attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is int stabilityValue)
                    {
                        endpoint.ScalarStability = stabilityValue switch
                        {
                            0 => "stable",
                            1 => "experimental",
                            2 => "deprecated",
                            _ => null
                        };
                    }
                    break;

                // Structural Wave 5 — query parameters (in: query).
                case "QueryParameterAttribute":
                {
                    var param = ExtractExplicitParameter(attr, "query");
                    if (param != null) endpoint.QueryParameters.Add(param);
                    break;
                }

                // Structural Wave 5 — header parameters (in: header).
                case "HeaderParameterAttribute":
                {
                    var param = ExtractExplicitParameter(attr, "header");
                    if (param != null) endpoint.HeaderParameters.Add(param);
                    break;
                }

                // Structural Wave 5 — response headers.
                case "ResponseHeaderAttribute":
                {
                    var rh = ExtractResponseHeader(attr);
                    if (rh != null) endpoint.ResponseHeaders.Add(rh);
                    break;
                }

                // Structural Wave 5 — response links.
                case "ResponseLinkAttribute":
                {
                    var rl = ExtractResponseLink(attr);
                    if (rl != null) endpoint.ResponseLinks.Add(rl);
                    break;
                }

                // Structural Wave 5 — callbacks (minimal implementation).
                case "CallbackAttribute":
                {
                    var cb = ExtractCallback(attr);
                    if (cb != null) endpoint.Callbacks.Add(cb);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Maps the constructor args + named args of a <c>[QueryParameter]</c> or
    /// <c>[HeaderParameter]</c> attribute to an <see cref="ExplicitParameterInfo"/>.
    /// Returns <c>null</c> if the mandatory <c>name</c> arg is missing.
    /// </summary>
    private static ExplicitParameterInfo? ExtractExplicitParameter(AttributeData attr, string location)
    {
        if (attr.ConstructorArguments.Length < 1) return null;
        if (attr.ConstructorArguments[0].Value is not string name) return null;

        // Determine schema type from the second constructor arg (Type? parameterType).
        var (schemaType, schemaFormat) = ResolveSchemaFromTypeArg(
            attr.ConstructorArguments.Length > 1 ? attr.ConstructorArguments[1].Value as INamedTypeSymbol : null);

        var info = new ExplicitParameterInfo
        {
            Name = name,
            In = location,
            SchemaType = schemaType,
            SchemaFormat = schemaFormat
        };

        foreach (var named in attr.NamedArguments)
        {
            switch (named.Key)
            {
                case "Required" when named.Value.Value is bool req:
                    info.Required = req;
                    break;
                case "Description" when named.Value.Value is string desc:
                    info.Description = desc;
                    break;
            }
        }

        return info;
    }

    /// <summary>
    /// Maps the constructor args + named args of a <c>[ResponseHeader]</c> attribute
    /// to a <see cref="ResponseHeaderInfo"/>. Returns <c>null</c> if mandatory args are missing.
    /// </summary>
    private static ResponseHeaderInfo? ExtractResponseHeader(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 2) return null;
        if (attr.ConstructorArguments[0].Value is not int statusCode) return null;
        if (attr.ConstructorArguments[1].Value is not string name) return null;

        var (schemaType, schemaFormat) = ResolveSchemaFromTypeArg(
            attr.ConstructorArguments.Length > 2 ? attr.ConstructorArguments[2].Value as INamedTypeSymbol : null);

        var info = new ResponseHeaderInfo
        {
            StatusCode = statusCode,
            Name = name,
            SchemaType = schemaType,
            SchemaFormat = schemaFormat
        };

        foreach (var named in attr.NamedArguments)
        {
            switch (named.Key)
            {
                case "Description" when named.Value.Value is string desc:
                    info.Description = desc;
                    break;
                case "Required" when named.Value.Value is bool req:
                    info.Required = req;
                    break;
            }
        }

        return info;
    }

    /// <summary>
    /// Maps a <c>[ResponseLink]</c> attribute to a <see cref="ResponseLinkInfo"/>.
    /// Returns <c>null</c> if mandatory args are missing.
    /// </summary>
    private static ResponseLinkInfo? ExtractResponseLink(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 2) return null;
        if (attr.ConstructorArguments[0].Value is not int statusCode) return null;
        if (attr.ConstructorArguments[1].Value is not string linkId) return null;

        var info = new ResponseLinkInfo { StatusCode = statusCode, LinkId = linkId };

        foreach (var named in attr.NamedArguments)
        {
            switch (named.Key)
            {
                case "OperationId" when named.Value.Value is string opId:
                    info.OperationId = opId;
                    break;
                case "Parameters" when named.Value.Value is string parameters:
                    info.Parameters = parameters;
                    break;
                case "Description" when named.Value.Value is string desc:
                    info.Description = desc;
                    break;
            }
        }

        return info;
    }

    /// <summary>
    /// Maps a <c>[Callback]</c> attribute to a <see cref="CallbackInfo"/>.
    /// Returns <c>null</c> if mandatory args are missing.
    /// </summary>
    private static CallbackInfo? ExtractCallback(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 1) return null;
        if (attr.ConstructorArguments[0].Value is not string name) return null;

        var info = new CallbackInfo { Name = name };

        foreach (var named in attr.NamedArguments)
        {
            switch (named.Key)
            {
                case "Expression" when named.Value.Value is string expr:
                    info.Expression = expr;
                    break;
                case "Method" when named.Value.Value is string method:
                    info.Method = method.ToLowerInvariant();
                    break;
                case "Summary" when named.Value.Value is string summary:
                    info.Summary = summary;
                    break;
                case "PayloadType" when named.Value.Value is INamedTypeSymbol typeSym:
                    info.PayloadTypeName = typeSym.Name;
                    break;
            }
        }

        return info;
    }

    /// <summary>
    /// Derives an OpenAPI (schemaType, schemaFormat) pair from a CLR <see cref="INamedTypeSymbol"/>.
    /// Falls back to ("string", null) for unknown or null types.
    /// Handles Nullable&lt;T&gt; transparently.
    /// </summary>
    private static (string SchemaType, string? SchemaFormat) ResolveSchemaFromTypeArg(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null) return ("string", null);

        // Unwrap Nullable<T>
        var underlying = typeSymbol;
        if (typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && typeSymbol.TypeArguments.Length == 1)
        {
            underlying = typeSymbol.TypeArguments[0] as INamedTypeSymbol ?? typeSymbol;
        }

        return underlying.SpecialType switch
        {
            SpecialType.System_String  => ("string", null),
            SpecialType.System_Boolean => ("boolean", null),
            SpecialType.System_Int32   => ("integer", "int32"),
            SpecialType.System_Int64   => ("integer", "int64"),
            SpecialType.System_Double  => ("number", "double"),
            SpecialType.System_Decimal => ("number", "double"),
            SpecialType.System_Single  => ("number", "float"),
            _ => ResolveSchemaByName(underlying.Name)
        };
    }

    private static (string SchemaType, string? SchemaFormat) ResolveSchemaByName(string typeName)
    {
        return typeName switch
        {
            "Guid"           => ("string", "uuid"),
            "DateTime"       => ("string", "date-time"),
            "DateTimeOffset" => ("string", "date-time"),
            "DateOnly"       => ("string", "date"),
            "TimeSpan"       => ("string", null),
            "Uri"            => ("string", "uri"),
            "Byte"           => ("integer", "int32"),
            "SByte"          => ("integer", "int32"),
            "Int16"          => ("integer", "int32"),
            "UInt16"         => ("integer", "int32"),
            "UInt32"         => ("integer", "int64"),
            "UInt64"         => ("integer", null),
            _                => ("string", null)
        };
    }

    /// <summary>
    /// Maps the constructor args + named args of a <c>[CodeSample]</c> attribute
    /// to a <see cref="CodeSampleInfo"/>. Returns <c>null</c> if the mandatory
    /// <c>lang</c>/<c>source</c> positional args are missing.
    /// </summary>
    private static CodeSampleInfo? ExtractCodeSample(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 2) return null;
        if (attr.ConstructorArguments[0].Value is not string lang) return null;
        if (attr.ConstructorArguments[1].Value is not string source) return null;

        var info = new CodeSampleInfo { Lang = lang, Source = source };
        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "Label" && named.Value.Value is string label)
                info.Label = label;
        }
        return info;
    }

    /// <summary>
    /// Maps the constructor args + named args of an <c>[OperationBadge]</c> attribute
    /// to an <see cref="OperationBadgeInfo"/>. Returns <c>null</c> if the mandatory
    /// <c>name</c> positional arg is missing.
    /// </summary>
    private static OperationBadgeInfo? ExtractOperationBadge(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 1) return null;
        if (attr.ConstructorArguments[0].Value is not string name) return null;

        var info = new OperationBadgeInfo { Name = name };
        foreach (var named in attr.NamedArguments)
        {
            switch (named.Key)
            {
                case "Position" when named.Value.Value is string pos:
                    info.Position = pos;
                    break;
                case "Color" when named.Value.Value is string color:
                    info.Color = color;
                    break;
            }
        }
        return info;
    }

    /// <summary>
    /// Maps the constructor args + named args of an <c>[ApiExample]</c> attribute
    /// to an <see cref="ApiExampleInfo"/>. Returns <c>null</c> if the mandatory
    /// <c>name</c>/<c>summary</c> positional args are missing.
    /// </summary>
    private static ApiExampleInfo? ExtractApiExample(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 2) return null;
        if (attr.ConstructorArguments[0].Value is not string name) return null;
        if (attr.ConstructorArguments[1].Value is not string summary) return null;

        var info = new ApiExampleInfo { Name = name, Summary = summary };
        foreach (var named in attr.NamedArguments)
        {
            switch (named.Key)
            {
                case "RequestJson" when named.Value.Value is string rj:
                    info.RequestJsonPath = rj;
                    break;
                case "ResponseStatus" when named.Value.Value is int rs:
                    info.ResponseStatus = rs;
                    break;
                case "ResponseJson" when named.Value.Value is string rsp:
                    info.ResponseJsonPath = rsp;
                    break;
                // Wave 3: inline value support
                case "RequestValue" when named.Value.Value is string rv:
                    info.RequestInlineValue = rv;
                    break;
                case "ResponseValue" when named.Value.Value is string rrv:
                    info.ResponseInlineValue = rrv;
                    break;
            }
        }
        return info;
    }

    /// <summary>
    /// Walks the fields of a catalog type looking for
    /// <c>[ErrorDefinition(code, httpStatus, userMessage, recovery)]</c> with
    /// an optional named <c>DocUrl</c>.
    /// </summary>
    /// <remarks>
    /// Called from <see cref="Execute"/> once per distinct catalog type, so
    /// we never pay the resolution cost per endpoint.
    /// </remarks>
    private static List<ErrorCatalogEntry> ResolveCatalogEntries(INamedTypeSymbol catalogType)
    {
        var entries = new List<ErrorCatalogEntry>();
        foreach (var member in catalogType.GetMembers())
        {
            if (member is not IFieldSymbol field) continue;
            foreach (var attr in field.GetAttributes())
            {
                if (attr.AttributeClass?.Name != "ErrorDefinitionAttribute") continue;
                if (attr.ConstructorArguments.Length < 4) continue;

                if (attr.ConstructorArguments[0].Value is not string code) continue;
                if (attr.ConstructorArguments[1].Value is not int http) continue;
                if (attr.ConstructorArguments[2].Value is not string userMessage) continue;
                if (attr.ConstructorArguments[3].Value is not string recovery) continue;

                string? docUrl = null;
                foreach (var named in attr.NamedArguments)
                {
                    if (named.Key == "DocUrl" && named.Value.Value is string du) docUrl = du;
                }

                entries.Add(new ErrorCatalogEntry
                {
                    Code = code,
                    HttpStatus = http,
                    UserMessage = userMessage,
                    Recovery = recovery,
                    DocUrl = docUrl
                });
            }
        }
        return entries;
    }

    // ── Navigation foundation — assembly-level attribute readers ──────────

    /// <summary>
    /// Reads all <c>[TagMetadata]</c> assembly attributes from the compilation and returns
    /// an ordered list (sorted by tag name for determinism).
    /// </summary>
    private static List<TagMetadataInfo> ReadTagMetadata(Compilation compilation)
    {
        var result = new List<TagMetadataInfo>();

        foreach (var attr in compilation.Assembly.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "TagMetadataAttribute") continue;
            if (attr.ConstructorArguments.Length == 0) continue;
            if (attr.ConstructorArguments[0].Value is not string tagName) continue;

            var info = new TagMetadataInfo { Name = tagName };

            foreach (var named in attr.NamedArguments)
            {
                switch (named.Key)
                {
                    case "Description" when named.Value.Value is string desc:
                        info.Description = desc;
                        break;
                    case "DisplayName" when named.Value.Value is string dn:
                        info.DisplayName = dn;
                        break;
                    case "ExternalDocsDescription" when named.Value.Value is string edDesc:
                        if (info.ExternalDocs == null) info.ExternalDocs = new ExternalDocsInfo();
                        info.ExternalDocs.Description = edDesc;
                        break;
                    case "ExternalDocsUrl" when named.Value.Value is string edUrl:
                        if (info.ExternalDocs == null) info.ExternalDocs = new ExternalDocsInfo();
                        info.ExternalDocs.Url = edUrl;
                        break;
                }
            }

            // Only keep ExternalDocs when a URL is present.
            if (info.ExternalDocs != null && string.IsNullOrEmpty(info.ExternalDocs.Url))
                info.ExternalDocs = null;

            result.Add(info);
        }

        result.Sort(static (a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));
        return result;
    }

    /// <summary>
    /// Reads all <c>[TagGroup]</c> assembly attributes from the compilation and returns
    /// them in declaration order (stable within a single build).
    /// </summary>
    private static List<TagGroupInfo> ReadTagGroups(Compilation compilation)
    {
        var result = new List<TagGroupInfo>();

        foreach (var attr in compilation.Assembly.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "TagGroupAttribute") continue;
            if (attr.ConstructorArguments.Length < 2) continue;
            if (attr.ConstructorArguments[0].Value is not string groupName) continue;

            var info = new TagGroupInfo { Name = groupName };

            // Second constructor arg is params string[] tags — stored as an array constant.
            var tagsArg = attr.ConstructorArguments[1];
            foreach (var tv in tagsArg.Values)
            {
                if (tv.Value is string tag)
                    info.Tags.Add(tag);
            }

            result.Add(info);
        }

        return result;
    }

    /// <summary>
    /// Reads the (at most one) <c>[OpenApiInfo]</c> assembly attribute that provides
    /// rich metadata for the <c>info</c> object (description, summary, termsOfService,
    /// contact, license).
    /// </summary>
    private static OpenApiInfoMetadata? ReadOpenApiInfoMetadata(Compilation compilation)
    {
        foreach (var attr in compilation.Assembly.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "OpenApiInfoAttribute") continue;

            var meta = new OpenApiInfoMetadata();
            foreach (var named in attr.NamedArguments)
            {
                switch (named.Key)
                {
                    case "Description" when named.Value.Value is string v:
                        meta.Description = v;
                        break;
                    case "Summary" when named.Value.Value is string v:
                        meta.Summary = v;
                        break;
                    case "TermsOfService" when named.Value.Value is string v:
                        meta.TermsOfService = v;
                        break;
                    case "ContactName" when named.Value.Value is string v:
                        meta.ContactName = v;
                        break;
                    case "ContactUrl" when named.Value.Value is string v:
                        meta.ContactUrl = v;
                        break;
                    case "ContactEmail" when named.Value.Value is string v:
                        meta.ContactEmail = v;
                        break;
                    case "LicenseName" when named.Value.Value is string v:
                        meta.LicenseName = v;
                        break;
                    case "LicenseUrl" when named.Value.Value is string v:
                        meta.LicenseUrl = v;
                        break;
                }
            }

            // Return even if all fields are null — the presence of the attribute is intentional.
            return meta;
        }
        return null;
    }

    /// <summary>
    /// Reads all <c>[OpenApiServer]</c> assembly attributes from the compilation.
    /// Servers are returned in declaration order (stable within a single build).
    /// </summary>
    private static List<OpenApiServerInfo> ReadOpenApiServers(Compilation compilation)
    {
        var result = new List<OpenApiServerInfo>();

        foreach (var attr in compilation.Assembly.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "OpenApiServerAttribute") continue;
            if (attr.ConstructorArguments.Length == 0) continue;
            if (attr.ConstructorArguments[0].Value is not string url) continue;

            var info = new OpenApiServerInfo { Url = url };
            foreach (var named in attr.NamedArguments)
            {
                if (named.Key == "Description" && named.Value.Value is string desc)
                    info.Description = desc;
            }

            result.Add(info);
        }

        return result;
    }

    /// <summary>
    /// Reads all <c>[assembly: Webhook(...)]</c> attributes from the compilation.
    /// Webhooks are returned sorted by name for deterministic emission.
    /// The payload type is resolved to its simple name for schema registration, and
    /// its properties are extracted via <see cref="TypePropertyExtractor"/> so that the
    /// generated schema is fully documented (MELHORIA-4 fix — no more empty stubs).
    /// </summary>
    private static List<WebhookInfo> ReadWebhooks(Compilation compilation)
    {
        var result = new List<WebhookInfo>();

        foreach (var attr in compilation.Assembly.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "WebhookAttribute") continue;
            if (attr.ConstructorArguments.Length < 2) continue;
            if (attr.ConstructorArguments[0].Value is not string name) continue;

            // Second constructor arg is the payload Type (INamedTypeSymbol at compile time).
            if (attr.ConstructorArguments[1].Value is not INamedTypeSymbol payloadTypeSym) continue;

            var info = new WebhookInfo
            {
                Name = name,
                PayloadTypeName = payloadTypeSym.Name
            };

            foreach (var named in attr.NamedArguments)
            {
                switch (named.Key)
                {
                    case "Method" when named.Value.Value is string m:
                        info.Method = m.ToLowerInvariant();
                        break;
                    case "Summary" when named.Value.Value is string s:
                        info.Summary = s;
                        break;
                    case "Description" when named.Value.Value is string d:
                        info.Description = d;
                        break;
                }
            }

            // MELHORIA-4: resolve payload schema so components.schemas contains
            // real properties instead of an empty stub.
            foreach (var schema in TypePropertyExtractor.DiscoverSchemas(payloadTypeSym))
                info.ReferencedSchemas.Add(schema);

            result.Add(info);
        }

        result.Sort(static (a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));
        return result;
    }

    /// <summary>
    /// Reads the (at most one) <c>[OpenApiExternalDocs]</c> assembly attribute.
    /// </summary>
    private static ExternalDocsInfo? ReadRootExternalDocs(Compilation compilation)
    {
        foreach (var attr in compilation.Assembly.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "OpenApiExternalDocsAttribute") continue;
            if (attr.ConstructorArguments.Length == 0) continue;
            if (attr.ConstructorArguments[0].Value is not string url) continue;

            var info = new ExternalDocsInfo { Url = url };
            foreach (var named in attr.NamedArguments)
            {
                if (named.Key == "Description" && named.Value.Value is string desc)
                    info.Description = desc;
            }
            return info;
        }
        return null;
    }

    /// <summary>
    /// Finds the handler for the given command and response types, and extracts [ApiResponse] attributes
    /// from the Handle method to populate AdditionalProduces.
    /// </summary>
    private static void ApplyHandlerApiResponseAttributes(ITypeSymbol commandType, ITypeSymbol responseType, EndpointInfo endpoint)
    {
        // Find all types in the compilation
        var allTypes = GetAllTypes(commandType.ContainingAssembly);

        // Look for a type that implements IRequestHandler<TCommand, TResponse>
        foreach (var type in allTypes)
        {
            if (type.TypeKind != TypeKind.Class)
                continue;

            // Check if this type implements IRequestHandler<TCommand, TResponse>
            foreach (var iface in type.AllInterfaces)
            {
                if (iface.Name != "IRequestHandler" || iface.TypeArguments.Length != 2)
                    continue;

                var handlerCommandType = iface.TypeArguments[0];
                var handlerResponseType = iface.TypeArguments[1];

                // Check if the types match
                if (SymbolEqualityComparer.Default.Equals(handlerCommandType, commandType) &&
                    SymbolEqualityComparer.Default.Equals(handlerResponseType, responseType))
                {
                    // Found the handler! Now look for the Handle method
                    var handleMethod = type.GetMembers("Handle")
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault();

                    if (handleMethod != null)
                    {
                        ExtractApiResponseAttributes(handleMethod, endpoint);
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts [ApiResponse] attributes from a method and adds them to the endpoint's AdditionalProduces.
    /// </summary>
    private static void ExtractApiResponseAttributes(IMethodSymbol method, EndpointInfo endpoint)
    {
        foreach (var attr in method.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName is not ("ApiResponseAttribute" or "ProducesAttribute"))
                continue;

            // Extract status code (required)
            if (attr.ConstructorArguments.Length == 0)
                continue;

            if (attr.ConstructorArguments[0].Value is not int statusCode)
                continue;

            // Extract response type (optional, 2nd parameter)
            string? responseTypeName = null;
            if (attr.ConstructorArguments.Length > 1 && attr.ConstructorArguments[1].Value is INamedTypeSymbol responseTypeSymbol)
            {
                responseTypeName = responseTypeSymbol.Name;
            }

            // Extract content type (optional, 3rd parameter, defaults to "application/json")
            string contentType = "application/json";
            if (attr.ConstructorArguments.Length > 2 && attr.ConstructorArguments[2].Value is string ct)
            {
                contentType = ct;
            }

            endpoint.AdditionalProduces.Add(new ProducesInfo
            {
                StatusCode = statusCode,
                ResponseTypeSimpleName = responseTypeName,
                ContentType = contentType
            });
        }
    }

    /// <summary>
    /// Recursively gets all types from an assembly, including nested types.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> GetAllTypes(IAssemblySymbol assembly)
    {
        var stack = new Stack<INamespaceOrTypeSymbol>();
        stack.Push(assembly.GlobalNamespace);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is INamedTypeSymbol type)
            {
                yield return type;

                foreach (var nestedType in type.GetTypeMembers())
                {
                    stack.Push(nestedType);
                }
            }
            else if (current is INamespaceSymbol ns)
            {
                foreach (var member in ns.GetMembers())
                {
                    stack.Push(member);
                }
            }
        }
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

        // RFC § F01 — strip hidden endpoints before handing the list to the YAML generator.
        // Paths that lose all their operations are dropped naturally by the generator's
        // GroupBy / empty-group suppression.
        var publicEndpoints = endpoints.Where(e => !e.ExcludedFromDocs).ToList();

        // RFC § F12 — resolve every distinct ErrorCatalog type referenced by the endpoints
        // exactly once, and attach the slice of codes whose httpStatus matches a declared
        // response on each endpoint. The merged catalog (all codes, de-duped) is emitted
        // at document root as x-swepay-error-catalog.
        var catalogsByTypeName = new Dictionary<string, List<ErrorCatalogEntry>>(StringComparer.Ordinal);
        foreach (var endpoint in publicEndpoints)
        {
            if (string.IsNullOrEmpty(endpoint.ErrorCatalogTypeName)) continue;
            if (catalogsByTypeName.ContainsKey(endpoint.ErrorCatalogTypeName!)) continue;

            var catalogSymbol = compilation.GetTypeByMetadataName(endpoint.ErrorCatalogTypeName!);
            if (catalogSymbol == null) continue;

            catalogsByTypeName[endpoint.ErrorCatalogTypeName!] = ResolveCatalogEntries(catalogSymbol);
        }

        // Produce the per-endpoint matched-code slice.
        foreach (var endpoint in publicEndpoints)
        {
            if (endpoint.ErrorCatalogTypeName == null) continue;
            if (!catalogsByTypeName.TryGetValue(endpoint.ErrorCatalogTypeName, out var entries)) continue;

            var declaredStatus = new HashSet<int>(endpoint.AdditionalProduces.Select(p => p.StatusCode));
            foreach (var entry in entries)
            {
                if (declaredStatus.Contains(entry.HttpStatus))
                {
                    endpoint.MatchedErrorCodes.Add(entry.Code);
                }
            }
        }

        // De-duped, ordered, unified catalog for root emission.
        var unifiedCatalog = catalogsByTypeName.Values
            .SelectMany(list => list)
            .GroupBy(e => e.Code, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(e => e.Code, StringComparer.Ordinal)
            .ToList();

        // Navigation foundation — read assembly-level tag metadata, tag groups and root externalDocs.
        var tagMetadata = ReadTagMetadata(compilation);
        var tagGroups = ReadTagGroups(compilation);
        var rootExternalDocs = ReadRootExternalDocs(compilation);

        // Rich info + servers (Wave 3).
        var openApiInfoMeta = ReadOpenApiInfoMetadata(compilation);
        var servers = ReadOpenApiServers(compilation);

        // Structural Wave 5 — webhooks (top-level OpenAPI 3.1).
        var webhooks = ReadWebhooks(compilation);

        // Generate OpenAPI YAML
        var yaml = OpenApiYamlGenerator.Generate(
            publicEndpoints, apiTitle, "1.0.0", unifiedCatalog,
            tagMetadata, tagGroups, rootExternalDocs,
            openApiInfoMeta, servers, webhooks);

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

        var endpoint = new EndpointInfo
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

        // Extract type properties when symbols are available
        if (commandTypeInfo.Type != null && responseTypeInfo.Type != null)
        {
            ExtractTypeProperties(commandTypeInfo.Type, responseTypeInfo.Type, endpoint);
        }

        // Detect fluent chain options (.AllowAnonymous(), .ProducesProblem(), .WithName(), etc.)
        ApplyFluentChainOptions(invocation, endpoint);

        // Read metadata attributes from TCommand type ([EndpointName], [Tags], etc.)
        if (commandTypeInfo.Type != null)
        {
            ApplyCommandAttributes(commandTypeInfo.Type, endpoint);

            // Read ApiResponse attributes from handler method
            if (responseTypeInfo.Type != null)
            {
                ApplyHandlerApiResponseAttributes(commandTypeInfo.Type, responseTypeInfo.Type, endpoint);
            }
        }

        return endpoint;
    }
}
