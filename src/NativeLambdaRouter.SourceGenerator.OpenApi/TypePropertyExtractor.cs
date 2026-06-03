using Microsoft.CodeAnalysis;

namespace NativeLambdaRouter.SourceGenerator.OpenApi;

/// <summary>
/// Extracts property information from Roslyn type symbols for OpenAPI schema generation.
/// </summary>
internal static class TypePropertyExtractor
{
    // ── Polymorphism-aware schema discovery ───────────────────────────

    /// <summary>
    /// Recursively discovers all <see cref="SchemaTypeInfo"/> objects reachable from
    /// <paramref name="rootType"/>, including polymorphism metadata
    /// (<c>allOf</c> base, <c>oneOf</c> sub-types, discriminator).
    /// </summary>
    /// <remarks>
    /// The returned list is de-duplicated by schema name and ordered deterministically.
    /// The root type's schema is the first element.
    /// Complex property references ($ref) and array item references are followed transitively.
    /// </remarks>
    public static List<SchemaTypeInfo> DiscoverSchemas(ITypeSymbol rootType)
    {
        var visited = new HashSet<string>(System.StringComparer.Ordinal);
        var result = new List<SchemaTypeInfo>();
        DiscoverRecursive(rootType, visited, result);
        return result;
    }

    private static void DiscoverRecursive(
        ITypeSymbol typeSymbol,
        HashSet<string> visited,
        List<SchemaTypeInfo> result)
    {
        // Skip primitives, enums, collections, and well-known types
        if (typeSymbol.TypeKind == TypeKind.Enum) return;
        if (typeSymbol.TypeKind == TypeKind.Interface) return;
        if (typeSymbol.SpecialType != SpecialType.None) return;
        if (IsPrimitiveName(typeSymbol.Name)) return;
        if (IsCollectionOrDictionarySymbol(typeSymbol)) return;

        var schemaName = typeSymbol.Name;
        if (!visited.Add(schemaName)) return;

        // Detect base class first so we know whether to use ownDeclaredOnly.
        string? baseSchemaName = null;
        if (typeSymbol is INamedTypeSymbol namedSelfCheck
            && namedSelfCheck.BaseType != null
            && namedSelfCheck.BaseType.SpecialType == SpecialType.None
            && namedSelfCheck.BaseType.TypeKind == TypeKind.Class
            && namedSelfCheck.BaseType.Name != "Object"
            && !IsPrimitiveName(namedSelfCheck.BaseType.Name))
        {
            baseSchemaName = namedSelfCheck.BaseType.Name;
        }

        // For derived types, emit only own-declared properties in the allOf block;
        // base properties are covered by the $ref.
        var ownDeclaredOnly = baseSchemaName != null;
        var properties = Extract(typeSymbol, ownDeclaredOnly);

        var schema = new SchemaTypeInfo
        {
            TypeName = schemaName,
            TypeKind = "Object",
            Properties = properties,
            IsResolved = properties.Count > 0 || HasAnyPublicProperty(typeSymbol),
            BaseSchemaName = baseSchemaName
        };

        // ── oneOf + discriminator: read [OpenApiSubType] / [OpenApiDiscriminator] ─
        if (typeSymbol is INamedTypeSymbol namedForAttrs)
        {
            ReadPolymorphismAttributes(namedForAttrs, schema);
        }

        result.Add(schema);

        // ── Follow base type recursively ──────────────────────────────
        if (schema.BaseSchemaName != null && typeSymbol is INamedTypeSymbol namedBase)
        {
            DiscoverRecursive(namedBase.BaseType!, visited, result);
        }

        // ── Follow sub-types recursively ──────────────────────────────
        if (typeSymbol is INamedTypeSymbol namedForSubs)
        {
            foreach (var sub in schema.SubTypes)
            {
                // Find the sub-type symbol from the declaring type's assembly
                var subSymbol = FindSubTypeSymbol(namedForSubs, sub.SchemaName);
                if (subSymbol != null)
                    DiscoverRecursive(subSymbol, visited, result);
            }
        }

        // ── Follow complex property $refs recursively ─────────────────
        foreach (var prop in properties)
        {
            if (prop.RefSchemaName != null)
            {
                var refSymbol = FindTypeByName(typeSymbol, prop.RefSchemaName);
                if (refSymbol != null)
                    DiscoverRecursive(refSymbol, visited, result);
            }
            if (prop.ArrayItemRefSchemaName != null)
            {
                var refSymbol = FindTypeByName(typeSymbol, prop.ArrayItemRefSchemaName);
                if (refSymbol != null)
                    DiscoverRecursive(refSymbol, visited, result);
            }
        }
    }

    /// <summary>
    /// Reads <c>[OpenApiSubType]</c> and <c>[OpenApiDiscriminator]</c> from a named type symbol.
    /// </summary>
    private static void ReadPolymorphismAttributes(INamedTypeSymbol typeSymbol, SchemaTypeInfo schema)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == null) continue;

            switch (attrName)
            {
                case "OpenApiSubTypeAttribute":
                    if (attr.ConstructorArguments.Length >= 2
                        && attr.ConstructorArguments[0].Value is INamedTypeSymbol subTypeSymbol
                        && attr.ConstructorArguments[1].Value is string discriminatorValue)
                    {
                        schema.SubTypes.Add(new SubTypeEntry
                        {
                            SchemaName = subTypeSymbol.Name,
                            DiscriminatorValue = discriminatorValue
                        });
                    }
                    break;

                case "OpenApiDiscriminatorAttribute":
                    if (attr.ConstructorArguments.Length >= 1
                        && attr.ConstructorArguments[0].Value is string propName)
                    {
                        schema.DiscriminatorPropertyName = propName;
                    }
                    break;
            }
        }

        // Sort sub-types by schema name for determinism.
        if (schema.SubTypes.Count > 1)
        {
            schema.SubTypes.Sort(static (a, b) =>
                System.StringComparer.Ordinal.Compare(a.SchemaName, b.SchemaName));
        }
    }

    private static bool HasAnyPublicProperty(ITypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.DeclaredAccessibility == Accessibility.Public
                      && !p.IsStatic
                      && !p.IsIndexer
                      && p.Name != "EqualityContract");
    }

    private static bool IsCollectionOrDictionarySymbol(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol) return true;

        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var name = namedType.ConstructedFrom.Name;
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
                case "Dictionary":
                case "IDictionary":
                case "IReadOnlyDictionary":
                    return true;
            }
        }
        return false;
    }

    private static bool IsPrimitiveName(string name)
    {
        switch (name)
        {
            case "String":
            case "Int32":
            case "Int64":
            case "Int16":
            case "Byte":
            case "Single":
            case "Double":
            case "Decimal":
            case "Boolean":
            case "DateTime":
            case "DateTimeOffset":
            case "DateOnly":
            case "TimeOnly":
            case "TimeSpan":
            case "Guid":
            case "Uri":
            case "Object":
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Tries to find a named type in the same assembly / compilation as <paramref name="context"/>
    /// by simple name. Returns null when not found.
    /// </summary>
    private static INamedTypeSymbol? FindTypeByName(ITypeSymbol context, string simpleName)
    {
        return FindTypeByNameInNamespace(
            context.ContainingNamespace as INamespaceSymbol,
            simpleName,
            context.ContainingAssembly);
    }

    private static INamedTypeSymbol? FindTypeByNameInNamespace(
        INamespaceSymbol? ns,
        string simpleName,
        IAssemblySymbol? assembly)
    {
        if (assembly == null) return null;

        // Walk the whole global namespace of the assembly looking for a type with that name.
        var stack = new Stack<INamespaceOrTypeSymbol>();
        stack.Push(assembly.GlobalNamespace);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is INamedTypeSymbol t)
            {
                if (t.Name == simpleName) return t;
                foreach (var nested in t.GetTypeMembers())
                    stack.Push(nested);
            }
            else if (current is INamespaceSymbol nsym)
            {
                foreach (var member in nsym.GetMembers())
                    stack.Push(member);
            }
        }
        return null;
    }

    /// <summary>
    /// Looks for a sub-type symbol whose simple name matches <paramref name="schemaName"/>
    /// within the same assembly as <paramref name="baseType"/>.
    /// </summary>
    private static INamedTypeSymbol? FindSubTypeSymbol(INamedTypeSymbol baseType, string schemaName)
        => FindTypeByName(baseType, schemaName);

    /// <summary>
    /// Extracts all public properties from a type symbol and maps them to OpenAPI schema properties.
    /// Handles records (primary constructor parameters) and classes/structs with public properties.
    /// </summary>
    /// <param name="typeSymbol">The type to extract properties from.</param>
    /// <param name="ownDeclaredOnly">
    /// When <c>true</c>, only properties declared directly on this type are returned
    /// (properties inherited from a base class are excluded). Use <c>true</c> when
    /// generating the <c>allOf</c> own-properties block for a derived type.
    /// When <c>false</c> (default), all public properties including inherited ones are returned.
    /// </param>
    public static List<SchemaPropertyInfo> Extract(ITypeSymbol typeSymbol, bool ownDeclaredOnly = false)
    {
        var properties = new List<SchemaPropertyInfo>();

        // Get public properties. When ownDeclaredOnly, restrict to members declared directly
        // on this type (ContainingType equals typeSymbol) so we don't repeat base class props.
        var members = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public
                        && !p.IsStatic
                        && !p.IsIndexer
                        && p.Name != "EqualityContract" // Exclude record internal property
                        && (!ownDeclaredOnly || SymbolEqualityComparer.Default.Equals(p.ContainingType, typeSymbol)));

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

        // Apply [OpenApiProperty] attribute (our own, from Native.OpenApi.Attributes).
        ApplyOpenApiPropertyAttribute(property, info);

        // Apply DataAnnotations attributes for constraint interop.
        ApplyDataAnnotationsAttributes(property, info);

        return info;
    }

    // ── [OpenApiProperty] reader ──────────────────────────────────────

    private static void ApplyOpenApiPropertyAttribute(IPropertySymbol property, SchemaPropertyInfo info)
    {
        foreach (var attr in property.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "OpenApiPropertyAttribute") continue;

            foreach (var named in attr.NamedArguments)
            {
                switch (named.Key)
                {
                    case "Description" when named.Value.Value is string desc:
                        info.Description = desc;
                        break;
                    case "Example" when named.Value.Value is string ex:
                        info.Example = ex;
                        break;
                    case "Default" when named.Value.Value is string def:
                        info.Default = def;
                        break;
                    case "MinLength" when named.Value.Value is int minLen && minLen >= 0:
                        info.MinLength = minLen;
                        break;
                    case "MaxLength" when named.Value.Value is int maxLen && maxLen >= 0:
                        info.MaxLength = maxLen;
                        break;
                    case "Pattern" when named.Value.Value is string pat:
                        info.Pattern = pat;
                        break;
                    case "Minimum" when named.Value.Value is double min:
                        info.Minimum = min;
                        break;
                    case "Maximum" when named.Value.Value is double max:
                        info.Maximum = max;
                        break;
                    case "ExclusiveMinimum" when named.Value.Value is double exMin:
                        info.ExclusiveMinimum = exMin;
                        break;
                    case "ExclusiveMaximum" when named.Value.Value is double exMax:
                        info.ExclusiveMaximum = exMax;
                        break;
                    case "MultipleOf" when named.Value.Value is double mul:
                        info.MultipleOf = mul;
                        break;
                    case "MinItems" when named.Value.Value is int minIt && minIt >= 0:
                        info.MinItems = minIt;
                        break;
                    case "MaxItems" when named.Value.Value is int maxIt && maxIt >= 0:
                        info.MaxItems = maxIt;
                        break;
                    case "UniqueItems" when named.Value.Value is bool uniq:
                        info.UniqueItems = uniq;
                        break;
                    case "Order" when named.Value.Value is int order && order >= 0:
                        info.Order = order;
                        break;
                    case "AdditionalPropertiesName" when named.Value.Value is string apn:
                        info.AdditionalPropertiesName = apn;
                        break;
                }
            }

            // Only one [OpenApiProperty] per property.
            break;
        }
    }

    // ── DataAnnotations interop ───────────────────────────────────────

    /// <summary>
    /// Reads well-known DataAnnotations attributes on a property and sets schema
    /// constraints on <paramref name="info"/>. [OpenApiProperty] values take precedence
    /// (they are applied first and this method only fills gaps).
    /// </summary>
    private static void ApplyDataAnnotationsAttributes(IPropertySymbol property, SchemaPropertyInfo info)
    {
        foreach (var attr in property.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == null) continue;

            switch (attrName)
            {
                // [Required] — mark as required regardless of nullability inference
                case "RequiredAttribute":
                    info.IsRequired = true;
                    break;

                // [StringLength(maxLength, MinimumLength = n)]
                case "StringLengthAttribute":
                    if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int slMax)
                    {
                        if (info.MaxLength < 0) info.MaxLength = slMax;
                    }
                    foreach (var named in attr.NamedArguments)
                    {
                        if (named.Key == "MinimumLength" && named.Value.Value is int slMin && info.MinLength < 0)
                            info.MinLength = slMin;
                    }
                    break;

                // [MinLength(n)]
                case "MinLengthAttribute":
                    if (attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is int mlMin
                        && info.MinLength < 0)
                    {
                        info.MinLength = mlMin;
                    }
                    break;

                // [MaxLength(n)]
                case "MaxLengthAttribute":
                    if (attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is int mlMax
                        && info.MaxLength < 0)
                    {
                        info.MaxLength = mlMax;
                    }
                    break;

                // [Range(minimum, maximum)] — both double and int overloads
                case "RangeAttribute":
                    if (attr.ConstructorArguments.Length >= 2)
                    {
                        var minArg = attr.ConstructorArguments[0];
                        var maxArg = attr.ConstructorArguments[1];

                        double? minVal = minArg.Value switch
                        {
                            int i => i,
                            double d => d,
                            _ => null
                        };
                        double? maxVal = maxArg.Value switch
                        {
                            int i => i,
                            double d => d,
                            _ => null
                        };

                        if (minVal.HasValue && double.IsNaN(info.Minimum)) info.Minimum = minVal.Value;
                        if (maxVal.HasValue && double.IsNaN(info.Maximum)) info.Maximum = maxVal.Value;
                    }
                    break;

                // [RegularExpression(pattern)]
                case "RegularExpressionAttribute":
                    if (attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is string pattern
                        && info.Pattern == null)
                    {
                        info.Pattern = pattern;
                    }
                    break;
            }
        }
    }

    // ── Type mapping ──────────────────────────────────────────────────

    private static void MapType(ITypeSymbol typeSymbol, SchemaPropertyInfo info)
    {
        // Handle enum types
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            info.OpenApiType = "string";
            info.IsEnum = true;
            MapEnumValues(typeSymbol, info);
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
            info.IsDictionary = true;
            info.OpenApiFormat = null;
            return;
        }

        // Complex object type — reference another schema
        info.OpenApiType = "object";
        info.RefSchemaName = typeSymbol.Name;
    }

    /// <summary>
    /// Maps enum field names and reads [OpenApiEnumMember] attributes for
    /// x-enum-descriptions and x-enum-varnames (Scalar-first).
    /// </summary>
    private static void MapEnumValues(ITypeSymbol typeSymbol, SchemaPropertyInfo info)
    {
        var fields = typeSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.IsConst)
            .ToList();

        info.EnumValues = fields.Select(f => f.Name).ToList();

        // Read per-member [OpenApiEnumMember] annotations
        var descriptions = new List<string?>();
        var varNames = new List<string?>();
        var hasAnyDescription = false;
        var hasAnyVarName = false;

        foreach (var field in fields)
        {
            string? desc = null;
            string? varName = null;

            foreach (var attr in field.GetAttributes())
            {
                if (attr.AttributeClass?.Name != "OpenApiEnumMemberAttribute") continue;

                foreach (var named in attr.NamedArguments)
                {
                    switch (named.Key)
                    {
                        case "Description" when named.Value.Value is string d:
                            desc = d;
                            hasAnyDescription = true;
                            break;
                        case "DisplayName" when named.Value.Value is string v:
                            varName = v;
                            hasAnyVarName = true;
                            break;
                    }
                }
                break; // only one [OpenApiEnumMember] per field
            }

            descriptions.Add(desc);
            varNames.Add(varName);
        }

        // Only set when there is something to emit
        if (hasAnyDescription) info.EnumDescriptions = descriptions;
        if (hasAnyVarName) info.EnumVarNames = varNames;
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
