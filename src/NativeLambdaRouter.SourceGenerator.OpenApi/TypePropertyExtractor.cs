using Microsoft.CodeAnalysis;

namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Extracts property information from Roslyn type symbols for OpenAPI schema generation.
/// </summary>
internal static class TypePropertyExtractor
{
    /// <summary>
    /// Extracts all public properties from a type symbol and maps them to OpenAPI schema properties.
    /// Handles records (primary constructor parameters) and classes/structs with public properties.
    /// </summary>
    public static List<SchemaPropertyInfo> Extract(ITypeSymbol typeSymbol)
    {
        var properties = new List<SchemaPropertyInfo>();

        // Get all public properties (includes record primary constructor parameters exposed as properties)
        var members = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public
                        && !p.IsStatic
                        && !p.IsIndexer
                        && p.Name != "EqualityContract"); // Exclude record internal property

        foreach (var prop in members)
        {
            var info = MapProperty(prop);
            if (info != null)
            {
                properties.Add(info);
            }
        }

        return properties;
    }

    private static SchemaPropertyInfo? MapProperty(IPropertySymbol property)
    {
        var propType = property.Type;
        var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated
                         || IsNullableValueType(propType);

        // Unwrap Nullable<T> for value types
        if (IsNullableValueType(propType) && propType is INamedTypeSymbol namedNullable)
        {
            propType = namedNullable.TypeArguments[0];
        }

        var info = new SchemaPropertyInfo
        {
            Name = property.Name,
            JsonName = ToCamelCase(property.Name),
            IsNullable = isNullable,
            IsRequired = !isNullable && !HasDefaultValue(property)
        };

        MapType(propType, info);

        return info;
    }

    private static void MapType(ITypeSymbol typeSymbol, SchemaPropertyInfo info)
    {
        // Handle enum types
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            info.OpenApiType = "string";
            info.IsEnum = true;
            info.EnumValues = typeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.IsConst)
                .Select(f => f.Name)
                .ToList();
            return;
        }

        // Handle array and list types
        if (TryMapArrayType(typeSymbol, info))
            return;

        // Handle primitive and well-known types (try both display forms)
        if (TryMapPrimitiveType(typeSymbol, info))
            return;

        // Handle dictionary types
        if (IsDictionaryType(typeSymbol))
        {
            info.OpenApiType = "object";
            info.OpenApiFormat = null;
            return;
        }

        // Complex object type — reference another schema
        info.OpenApiType = "object";
        info.RefSchemaName = typeSymbol.Name;
    }

    private static bool TryMapPrimitiveType(ITypeSymbol typeSymbol, SchemaPropertyInfo info)
    {
        // Check by SpecialType first (most reliable, does not depend on display string)
        switch (typeSymbol.SpecialType)
        {
            case SpecialType.System_String:
                info.OpenApiType = "string";
                return true;
            case SpecialType.System_Int32:
                info.OpenApiType = "integer";
                info.OpenApiFormat = "int32";
                return true;
            case SpecialType.System_Int64:
                info.OpenApiType = "integer";
                info.OpenApiFormat = "int64";
                return true;
            case SpecialType.System_Int16:
            case SpecialType.System_Byte:
                info.OpenApiType = "integer";
                info.OpenApiFormat = "int32";
                return true;
            case SpecialType.System_Single:
                info.OpenApiType = "number";
                info.OpenApiFormat = "float";
                return true;
            case SpecialType.System_Double:
                info.OpenApiType = "number";
                info.OpenApiFormat = "double";
                return true;
            case SpecialType.System_Decimal:
                info.OpenApiType = "number";
                info.OpenApiFormat = "double";
                return true;
            case SpecialType.System_Boolean:
                info.OpenApiType = "boolean";
                return true;
            case SpecialType.System_DateTime:
                info.OpenApiType = "string";
                info.OpenApiFormat = "date-time";
                return true;
            case SpecialType.System_Object:
                info.OpenApiType = "object";
                return true;
        }

        // Fallback: check by name for types without SpecialType enum entries
        var name = typeSymbol.Name;
        var ns = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "";

        // DateTimeOffset, DateOnly, TimeOnly, TimeSpan, Guid, Uri, DateTime (fallback)
        // Also handles cases where the type is resolved without full namespace context
        switch (name)
        {
            case "DateTime":
            case "DateTimeOffset":
                if (ns == "System" || ns == "" || ns == "<global namespace>")
                {
                    info.OpenApiType = "string";
                    info.OpenApiFormat = "date-time";
                    return true;
                }
                break;
            case "DateOnly":
                if (ns == "System" || ns == "" || ns == "<global namespace>")
                {
                    info.OpenApiType = "string";
                    info.OpenApiFormat = "date";
                    return true;
                }
                break;
            case "TimeOnly":
            case "TimeSpan":
                if (ns == "System" || ns == "" || ns == "<global namespace>")
                {
                    info.OpenApiType = "string";
                    info.OpenApiFormat = "time";
                    return true;
                }
                break;
            case "Guid":
                if (ns == "System" || ns == "" || ns == "<global namespace>")
                {
                    info.OpenApiType = "string";
                    info.OpenApiFormat = "uuid";
                    return true;
                }
                break;
            case "Uri":
                if (ns == "System" || ns == "" || ns == "<global namespace>")
                {
                    info.OpenApiType = "string";
                    info.OpenApiFormat = "uri";
                    return true;
                }
                break;
        }

        return false;
    }

    private static bool TryMapArrayType(ITypeSymbol typeSymbol, SchemaPropertyInfo info)
    {
        ITypeSymbol? elementType = null;

        // T[]
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
        }
        // List<T>, IList<T>, IReadOnlyList<T>, IEnumerable<T>, ICollection<T>, ImmutableArray<T>, etc.
        else if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.TypeArguments.Length == 1)
        {
            if (IsCollectionType(namedType))
            {
                elementType = namedType.TypeArguments[0];
            }
        }

        if (elementType == null)
            return false;

        info.OpenApiType = "array";

        // Map element type
        if (elementType.TypeKind == TypeKind.Enum)
        {
            info.ArrayItemType = "string";
        }
        else if (TryGetPrimitiveOpenApiType(elementType, out var itemType, out var itemFormat))
        {
            info.ArrayItemType = itemType;
            info.ArrayItemFormat = itemFormat;
        }
        else
        {
            // Complex type in array → $ref
            info.ArrayItemRefSchemaName = elementType.Name;
        }

        return true;
    }

    private static bool TryGetPrimitiveOpenApiType(ITypeSymbol typeSymbol, out string type, out string? format)
    {
        var tempInfo = new SchemaPropertyInfo();
        if (TryMapPrimitiveType(typeSymbol, tempInfo))
        {
            type = tempInfo.OpenApiType;
            format = tempInfo.OpenApiFormat;
            return true;
        }
        type = "";
        format = null;
        return false;
    }

    private static bool IsCollectionType(INamedTypeSymbol namedType)
    {
        var name = namedType.ConstructedFrom.Name;
        var ns = namedType.ConstructedFrom.ContainingNamespace?.ToDisplayString() ?? "";

        // Check by name — the namespace may be fully qualified, empty, or <global namespace>
        switch (name)
        {
            case "List":
            case "IList":
            case "IReadOnlyList":
            case "IEnumerable":
            case "ICollection":
            case "IReadOnlyCollection":
            case "HashSet":
            case "ImmutableArray":
            case "ImmutableList":
                // Accept if in a System.Collections namespace, empty, or global
                return ns.StartsWith("System.Collections")
                       || ns == ""
                       || ns == "<global namespace>";
        }

        return false;
    }

    private static bool IsDictionaryType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var name = namedType.ConstructedFrom.Name;
            var ns = namedType.ConstructedFrom.ContainingNamespace?.ToDisplayString() ?? "";

            if (ns == "System.Collections.Generic" || ns == "")
            {
                return name == "Dictionary" || name == "IDictionary" || name == "IReadOnlyDictionary";
            }
        }
        return false;
    }

    private static bool IsNullableValueType(ITypeSymbol typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol { IsValueType: true, OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
    }

    private static bool HasDefaultValue(IPropertySymbol property)
    {
        // For record primary constructor parameters, check if they have a default value
        // by looking at the corresponding parameter in the constructor
        var containingType = property.ContainingType;
        if (containingType == null)
            return false;

        // Look for a primary constructor parameter with same name
        foreach (var ctor in containingType.Constructors)
        {
            if (ctor.IsImplicitlyDeclared || ctor.Parameters.Length == 0)
                continue;

            foreach (var param in ctor.Parameters)
            {
                if (string.Equals(param.Name, property.Name, System.StringComparison.OrdinalIgnoreCase)
                    && param.HasExplicitDefaultValue)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        if (name.Length == 1)
            return name.ToLowerInvariant();

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
