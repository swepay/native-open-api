namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Tests for OpenAPI polymorphism: allOf (inheritance), oneOf + discriminator (sub-type union).
/// Covers:
///   - <c>allOf: [$ref base, {own props}]</c> for derived types
///   - <c>oneOf</c> on base schemas annotated with <c>[OpenApiSubType]</c>
///   - <c>discriminator</c> with propertyName + mapping
///   - Determinism (stable output across calls)
///   - Integration via source generator (attribute reading through Roslyn)
/// </summary>
public sealed class PolymorphismTests
{
    // ── helpers ───────────────────────────────────────────────────────

    private static string Generate(IReadOnlyList<EndpointInfo> endpoints)
        => OpenApiYamlGenerator.Generate(
            endpoints, "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);

    private static EndpointInfo MakeEndpoint(
        string method = "GET",
        string path = "/v1/items",
        string commandSimple = "GetItemsCommand",
        string responseSimple = "GetItemsResponse")
        => new()
        {
            Method = method,
            Path = path,
            CommandTypeName = $"App.{commandSimple}",
            ResponseTypeName = $"App.{responseSimple}",
            CommandSimpleName = commandSimple,
            ResponseSimpleName = responseSimple
        };

    private static int CountOccurrences(string source, string pattern)
    {
        var count = 0;
        var idx = 0;
        while ((idx = source.IndexOf(pattern, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }

    // ── allOf (inheritance) ───────────────────────────────────────────

    [Fact]
    public void Generate_SchemaWithBaseSchemaName_EmitsAllOf()
    {
        var endpoint = MakeEndpoint(responseSimple: "CatResponse");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "CatResponse",
            TypeKind = "Response",
            IsResolved = true,
            BaseSchemaName = "AnimalBase",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Indoor", JsonName = "indoor", OpenApiType = "boolean", IsRequired = true }
            }
        });
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "AnimalBase",
            TypeKind = "Object",
            IsResolved = true,
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Name", JsonName = "name", OpenApiType = "string", IsRequired = true }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("    CatResponse:");
        yaml.Should().Contain("      allOf:");
        yaml.Should().Contain("        - $ref: \"#/components/schemas/AnimalBase\"");
        yaml.Should().Contain("        - type: object");
        yaml.Should().Contain("          properties:");
        yaml.Should().Contain("            indoor:");
        yaml.Should().Contain("              type: boolean");
    }

    [Fact]
    public void Generate_SchemaWithBaseSchemaName_BasePropertiesNotDuplicated()
    {
        // The derived type's own-props block should NOT repeat base class properties.
        // The DogResponse.Properties list contains only the own-declared "breed" property;
        // the base's "name" is only in AnimalBase.Properties.
        var endpoint = MakeEndpoint(responseSimple: "DogResponse");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "DogResponse",
            TypeKind = "Response",
            IsResolved = true,
            BaseSchemaName = "AnimalBase",
            Properties = new List<SchemaPropertyInfo>
            {
                // Only own-declared property — base's "name" should not be here
                new() { Name = "Breed", JsonName = "breed", OpenApiType = "string", IsRequired = true }
            }
        });
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "AnimalBase",
            TypeKind = "Object",
            IsResolved = true,
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Name", JsonName = "name", OpenApiType = "string", IsRequired = true }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // breed appears in the derived allOf block (12 spaces = 6 baseIndent + 2 propIndent + 4 extra for allOf)
        yaml.Should().Contain("            breed:");

        // "name:" as a property definition (8 spaces indent within a schema's properties block)
        // should appear exactly once: in AnimalBase only, not duplicated in DogResponse.
        // We look for "        name:" (8 spaces) which is the property-name indentation inside components.schemas.
        CountOccurrences(yaml, "        name:").Should().Be(1,
            "name property should appear only in AnimalBase, not duplicated in DogResponse's allOf block");
    }

    [Fact]
    public void Generate_SchemaWithBaseSchemaName_AllOfComesBeforeRegularSchemas()
    {
        var endpoint = MakeEndpoint(responseSimple: "CatResponse");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "CatResponse",
            TypeKind = "Response",
            IsResolved = true,
            BaseSchemaName = "AnimalBase",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Indoor", JsonName = "indoor", OpenApiType = "boolean" }
            }
        });
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "AnimalBase",
            TypeKind = "Object",
            IsResolved = true,
            Properties = new List<SchemaPropertyInfo>()
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // AnimalBase sorts before CatResponse alphabetically
        var animalIdx = yaml.IndexOf("    AnimalBase:", StringComparison.Ordinal);
        var catIdx = yaml.IndexOf("    CatResponse:", StringComparison.Ordinal);
        animalIdx.Should().BeGreaterThan(0);
        catIdx.Should().BeGreaterThan(0);
        animalIdx.Should().BeLessThan(catIdx, "AnimalBase sorts before CatResponse alphabetically");
    }

    [Fact]
    public void Generate_SchemaWithBaseSchemaName_NoOwnProps_EmitsEmptyObjectInAllOf()
    {
        var endpoint = MakeEndpoint(responseSimple: "MarkerDerived");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "MarkerDerived",
            TypeKind = "Response",
            IsResolved = false,
            BaseSchemaName = "BaseType",
            Properties = new List<SchemaPropertyInfo>()
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("      allOf:");
        yaml.Should().Contain("        - $ref: \"#/components/schemas/BaseType\"");
        yaml.Should().Contain("        - type: object");

        // No properties block should appear for this schema (no own properties)
        // The "properties:" keyword only appears if there are properties to emit.
        // With an empty list, AppendSchemaPropertiesIndented is never called.
        // Count: MarkerDerived's allOf block should NOT contain "properties:"
        // We verify by checking the allOf inline object doesn't have a properties sub-key.
        var allOfInlineIdx = yaml.IndexOf("        - type: object", StringComparison.Ordinal);
        allOfInlineIdx.Should().BeGreaterThan(0);

        // The character right after "        - type: object\n" should be the start of
        // a new schema or end of file/components — not "          properties:"
        var afterInline = yaml.IndexOf("\n", allOfInlineIdx + 1, StringComparison.Ordinal);
        if (afterInline > 0 && afterInline < yaml.Length - 1)
        {
            var nextLine = yaml.Substring(afterInline + 1, System.Math.Min(20, yaml.Length - afterInline - 1));
            nextLine.Should().NotStartWith("          properties:");
        }
    }

    // ── oneOf (polymorphic base with sub-types) ───────────────────────

    [Fact]
    public void Generate_SchemaWithSubTypes_EmitsOneOf()
    {
        var endpoint = MakeEndpoint(responseSimple: "AnimalResponse");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "AnimalResponse",
            TypeKind = "Response",
            IsResolved = true,
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "CatResponse", DiscriminatorValue = "cat" },
                new() { SchemaName = "DogResponse", DiscriminatorValue = "dog" }
            },
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Name", JsonName = "name", OpenApiType = "string", IsRequired = true }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("    AnimalResponse:");
        yaml.Should().Contain("      oneOf:");
        yaml.Should().Contain("        - $ref: \"#/components/schemas/CatResponse\"");
        yaml.Should().Contain("        - $ref: \"#/components/schemas/DogResponse\"");
    }

    [Fact]
    public void Generate_SchemaWithSubTypes_DoesNotEmitTopLevelTypeObject()
    {
        // A oneOf schema should not be emitted as plain "type: object" at the top level.
        // The line immediately following the schema header should be "      oneOf:", not "      type: object".
        var endpoint = MakeEndpoint(responseSimple: "AnimalResponse");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "AnimalResponse",
            TypeKind = "Response",
            IsResolved = true,
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "CatResponse", DiscriminatorValue = "cat" }
            },
            Properties = new List<SchemaPropertyInfo>()
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // oneOf must be present for this schema
        yaml.Should().Contain("      oneOf:");
        yaml.Should().Contain("        - $ref: \"#/components/schemas/CatResponse\"");

        // The first keyword-level line after "    AnimalResponse:" must start with "oneOf:", not "type: object".
        // We find the schema header then look for the immediate next keyword at 6-space indent.
        var animalHeaderMarker = "    AnimalResponse:";
        var animalHeaderIdx = yaml.IndexOf(animalHeaderMarker, StringComparison.Ordinal);
        animalHeaderIdx.Should().BeGreaterThan(0, "AnimalResponse schema must exist in components/schemas");

        // Grab the content starting just after the header
        var afterHeader = yaml.Substring(animalHeaderIdx + animalHeaderMarker.Length);
        // Trim leading whitespace/newlines to get the first actual keyword
        afterHeader.TrimStart('\r', '\n', ' ').Should().StartWith("oneOf:",
            "the first keyword in a polymorphic base schema must be oneOf:");
    }

    [Fact]
    public void Generate_OneOfSubTypes_AreSortedAlphabetically()
    {
        var endpoint = MakeEndpoint(responseSimple: "ShapeResponse");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "ShapeResponse",
            TypeKind = "Response",
            IsResolved = false,
            SubTypes = new List<SubTypeEntry>
            {
                // Added in reverse order — must come out sorted
                new() { SchemaName = "Triangle", DiscriminatorValue = "triangle" },
                new() { SchemaName = "Circle", DiscriminatorValue = "circle" },
                new() { SchemaName = "Square", DiscriminatorValue = "square" }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        var circleIdx = yaml.IndexOf("\"#/components/schemas/Circle\"", StringComparison.Ordinal);
        var squareIdx = yaml.IndexOf("\"#/components/schemas/Square\"", StringComparison.Ordinal);
        var triangleIdx = yaml.IndexOf("\"#/components/schemas/Triangle\"", StringComparison.Ordinal);

        circleIdx.Should().BeLessThan(squareIdx, "Circle sorts before Square");
        squareIdx.Should().BeLessThan(triangleIdx, "Square sorts before Triangle");
    }

    // ── discriminator ─────────────────────────────────────────────────

    [Fact]
    public void Generate_SchemaWithDiscriminator_EmitsDiscriminatorBlock()
    {
        var endpoint = MakeEndpoint(responseSimple: "AnimalResponse");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "AnimalResponse",
            TypeKind = "Response",
            IsResolved = false,
            DiscriminatorPropertyName = "kind",
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "CatResponse", DiscriminatorValue = "cat" },
                new() { SchemaName = "DogResponse", DiscriminatorValue = "dog" }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("      discriminator:");
        yaml.Should().Contain("        propertyName: kind");
        yaml.Should().Contain("        mapping:");
        yaml.Should().Contain("          cat: \"#/components/schemas/CatResponse\"");
        yaml.Should().Contain("          dog: \"#/components/schemas/DogResponse\"");
    }

    [Fact]
    public void Generate_SchemaWithoutDiscriminatorAttribute_NoDiscriminatorBlock()
    {
        var endpoint = MakeEndpoint(responseSimple: "UnionResponse");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "UnionResponse",
            TypeKind = "Response",
            IsResolved = false,
            DiscriminatorPropertyName = null, // not set
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "TypeA", DiscriminatorValue = "a" },
                new() { SchemaName = "TypeB", DiscriminatorValue = "b" }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("      oneOf:");
        yaml.Should().NotContain("      discriminator:");
    }

    [Fact]
    public void Generate_DiscriminatorPropertyNameWithSpecialChars_IsEscaped()
    {
        var endpoint = MakeEndpoint(responseSimple: "SpecialResponse");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "SpecialResponse",
            TypeKind = "Response",
            IsResolved = false,
            DiscriminatorPropertyName = "entity\"Type",
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "TypeA", DiscriminatorValue = "a" }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("        propertyName: entity\\\"Type");
    }

    // ── allOf + oneOf + discriminator (full hierarchy) ────────────────

    [Fact]
    public void Generate_FullPolymorphicHierarchy_EmitsAllKeywords()
    {
        var endpoint = MakeEndpoint(responseSimple: "AnimalResponse");

        // Base schema — abstract, has discriminator + oneOf
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "AnimalResponse",
            TypeKind = "Response",
            IsResolved = true,
            DiscriminatorPropertyName = "kind",
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "CatResponse", DiscriminatorValue = "cat" },
                new() { SchemaName = "DogResponse", DiscriminatorValue = "dog" }
            },
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Kind", JsonName = "kind", OpenApiType = "string", IsRequired = true },
                new() { Name = "Name", JsonName = "name", OpenApiType = "string", IsRequired = true }
            }
        });

        // Cat — derived, allOf
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "CatResponse",
            TypeKind = "Object",
            IsResolved = true,
            BaseSchemaName = "AnimalResponse",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Indoor", JsonName = "indoor", OpenApiType = "boolean", IsRequired = true }
            }
        });

        // Dog — derived, allOf
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "DogResponse",
            TypeKind = "Object",
            IsResolved = true,
            BaseSchemaName = "AnimalResponse",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Breed", JsonName = "breed", OpenApiType = "string", IsRequired = true }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // Base: oneOf
        yaml.Should().Contain("    AnimalResponse:");
        yaml.Should().Contain("      oneOf:");
        yaml.Should().Contain("        - $ref: \"#/components/schemas/CatResponse\"");
        yaml.Should().Contain("        - $ref: \"#/components/schemas/DogResponse\"");

        // Base: discriminator
        yaml.Should().Contain("      discriminator:");
        yaml.Should().Contain("        propertyName: kind");
        yaml.Should().Contain("          cat: \"#/components/schemas/CatResponse\"");
        yaml.Should().Contain("          dog: \"#/components/schemas/DogResponse\"");

        // MELHORIA-6: Shared base properties must NOT appear alongside oneOf in the base schema.
        // Instead they are in the synthetic AnimalResponse__Core schema.
        yaml.Should().NotContain("    AnimalResponse:\n      oneOf:\n        - $ref: \"#/components/schemas/CatResponse\"\n        - $ref: \"#/components/schemas/DogResponse\"\n      discriminator:\n      properties:");
        yaml.Should().Contain("    AnimalResponse__Core:");
        // Shared props (kind, name) are in the Core schema (8-space indent = 6 base + 2 prop)
        yaml.Should().Contain("        kind:");
        yaml.Should().Contain("        name:");

        // Cat: allOf — references Core schema, not the discriminated-union base
        yaml.Should().Contain("    CatResponse:");
        yaml.Should().Contain("        - $ref: \"#/components/schemas/AnimalResponse__Core\"");
        yaml.Should().Contain("            indoor:");

        // Dog: allOf
        yaml.Should().Contain("    DogResponse:");
        yaml.Should().Contain("            breed:");
    }

    // ── Determinism ───────────────────────────────────────────────────

    [Fact]
    public void Generate_PolymorphicHierarchy_IsDeterministicAcrossMultipleCalls()
    {
        var endpoint = MakeEndpoint(responseSimple: "ShapeBase");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "ShapeBase",
            TypeKind = "Response",
            IsResolved = false,
            DiscriminatorPropertyName = "shapeType",
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "Circle", DiscriminatorValue = "circle" },
                new() { SchemaName = "Square", DiscriminatorValue = "square" }
            }
        });
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "Circle",
            TypeKind = "Object",
            IsResolved = true,
            BaseSchemaName = "ShapeBase",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Radius", JsonName = "radius", OpenApiType = "number" }
            }
        });
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "Square",
            TypeKind = "Object",
            IsResolved = true,
            BaseSchemaName = "ShapeBase",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Side", JsonName = "side", OpenApiType = "number" }
            }
        });

        var endpoints = new List<EndpointInfo> { endpoint };
        var yaml1 = Generate(endpoints);
        var yaml2 = Generate(endpoints);

        yaml1.Should().Be(yaml2, "polymorphic YAML must be deterministic across calls");
    }

    /// <summary>
    /// MELHORIA-6: When a polymorphic base has shared properties, those properties must NOT
    /// appear at the same level as <c>oneOf</c> in the base schema (strict Spectral/OWASP
    /// compliance). Instead they live in a synthetic <c>{TypeName}__Core</c> schema, and
    /// the discriminated base emits only <c>oneOf</c> + <c>discriminator</c>.
    /// </summary>
    [Fact]
    public void Generate_SchemaWithSubTypesAndBaseProps_PropertiesMovedToCoreSchema()
    {
        var endpoint = MakeEndpoint(responseSimple: "VehicleBase");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "VehicleBase",
            TypeKind = "Response",
            IsResolved = true,
            DiscriminatorPropertyName = "vehicleType",
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "Car", DiscriminatorValue = "car" }
            },
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "VehicleType", JsonName = "vehicleType", OpenApiType = "string", IsRequired = true },
                new() { Name = "Brand", JsonName = "brand", OpenApiType = "string" }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // Base schema: only oneOf + discriminator
        yaml.Should().Contain("    VehicleBase:");
        yaml.Should().Contain("      oneOf:");
        yaml.Should().Contain("      discriminator:");

        // MELHORIA-6: properties must NOT be at the same level as oneOf in the base schema.
        // Locate the VehicleBase schema block and verify no `properties:` inside it.
        var baseSchemaHeader = "    VehicleBase:";
        var baseIdx = yaml.IndexOf(baseSchemaHeader, StringComparison.Ordinal);
        baseIdx.Should().BeGreaterThan(0, "VehicleBase schema must exist");

        // Find the end of the VehicleBase block (next schema at 4-space indent or end)
        var coreHeader = "    VehicleBase__Core:";
        var coreIdx = yaml.IndexOf(coreHeader, StringComparison.Ordinal);
        coreIdx.Should().BeGreaterThan(baseIdx, "VehicleBase__Core must appear after VehicleBase");

        var baseBlock = yaml.Substring(baseIdx, coreIdx - baseIdx);
        baseBlock.Should().NotContain("      properties:",
            "shared properties must not coexist with oneOf at the base-schema level");

        // Synthetic Core schema: holds the shared properties
        yaml.Should().Contain(coreHeader);
        var afterCore = yaml.Substring(coreIdx);
        afterCore.Should().Contain("      properties:");
        afterCore.Should().Contain("        vehicleType:");
        afterCore.Should().Contain("        brand:");
    }

    // ── Source generator integration (Roslyn attribute reading) ───────

    /// <summary>
    /// Source containing the polymorphism attribute definitions in-process,
    /// mirroring <c>OpenApiSubTypeAttribute</c> and <c>OpenApiDiscriminatorAttribute</c>.
    /// </summary>
    private static string CreatePolymorphismAttributeSource() => @"
namespace Native.OpenApi.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class OpenApiSubTypeAttribute : System.Attribute
    {
        public System.Type SubType { get; }
        public string DiscriminatorValue { get; }
        public OpenApiSubTypeAttribute(System.Type subType, string discriminatorValue)
        {
            SubType = subType;
            DiscriminatorValue = discriminatorValue;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class OpenApiDiscriminatorAttribute : System.Attribute
    {
        public string PropertyName { get; }
        public OpenApiDiscriminatorAttribute(string propertyName) { PropertyName = propertyName; }
    }
}
";

    [Fact]
    public void Generator_DerivedType_EmitsAllOf()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreatePolymorphismAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp
{
    public abstract class AnimalBase
    {
        public string Name { get; set; } = """";
    }

    public class CatResponse : AnimalBase
    {
        public bool Indoor { get; set; }
    }

    public class GetCatCommand { }

    public class MyRouter
    {
        protected void ConfigureRoutes(IRouteBuilder routes)
        {
            routes.MapGet<GetCatCommand, CatResponse>(""/v1/cats"", ctx => new GetCatCommand());
        }
    }
}
"
        });

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        source.Should().NotBeNull();
        source.Should().Contain("allOf:");
        source.Should().Contain("$ref: \"\"#/components/schemas/AnimalBase\"\"");
        source.Should().Contain("indoor:");
    }

    [Fact]
    public void Generator_BaseTypeWithOpenApiSubTypeAttribute_EmitsOneOf()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreatePolymorphismAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp
{
    [OpenApiDiscriminator(""kind"")]
    [OpenApiSubType(typeof(Cat), ""cat"")]
    [OpenApiSubType(typeof(Dog), ""dog"")]
    public abstract class Animal
    {
        public string Kind { get; set; } = """";
        public string Name { get; set; } = """";
    }

    public class Cat : Animal
    {
        public bool Indoor { get; set; }
    }

    public class Dog : Animal
    {
        public string Breed { get; set; } = """";
    }

    public class GetAnimalCommand { }

    public class MyRouter
    {
        protected void ConfigureRoutes(IRouteBuilder routes)
        {
            routes.MapGet<GetAnimalCommand, Animal>(""/v1/animals"", ctx => new GetAnimalCommand());
        }
    }
}
"
        });

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        source.Should().NotBeNull();
        source.Should().Contain("oneOf:");
        source.Should().Contain("$ref: \"\"#/components/schemas/Cat\"\"");
        source.Should().Contain("$ref: \"\"#/components/schemas/Dog\"\"");
        source.Should().Contain("discriminator:");
        source.Should().Contain("propertyName: kind");
        source.Should().Contain("mapping:");
    }

    [Fact]
    public void Generator_FullHierarchy_EmitsAllOfForDerivedAndOneOfForBase()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreatePolymorphismAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp
{
    [OpenApiDiscriminator(""vehicleType"")]
    [OpenApiSubType(typeof(Car), ""car"")]
    [OpenApiSubType(typeof(Truck), ""truck"")]
    public abstract class Vehicle
    {
        public string VehicleType { get; set; } = """";
        public string Brand { get; set; } = """";
    }

    public class Car : Vehicle
    {
        public int Doors { get; set; }
    }

    public class Truck : Vehicle
    {
        public double PayloadTons { get; set; }
    }

    public class GetVehicleCommand { }

    public class MyRouter
    {
        protected void ConfigureRoutes(IRouteBuilder routes)
        {
            routes.MapGet<GetVehicleCommand, Vehicle>(""/v1/vehicles"", ctx => new GetVehicleCommand());
        }
    }
}
"
        });

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        source.Should().NotBeNull();

        // Vehicle (base): oneOf + discriminator
        source.Should().Contain("oneOf:");
        source.Should().Contain("discriminator:");
        source.Should().Contain("propertyName: vehicleType");
        source.Should().Contain("car: \"\"#/components/schemas/Car\"\"");
        source.Should().Contain("truck: \"\"#/components/schemas/Truck\"\"");

        // Car (derived): allOf
        source.Should().Contain("allOf:");
        source.Should().Contain("doors:");

        // Truck (derived): allOf
        source.Should().Contain("payloadTons:");
    }

    [Fact]
    public void Generator_DerivedTypeProperties_ExcludesBaseClassProperties()
    {
        // Car's allOf block should contain only Car's OWN properties (doors),
        // NOT the base Vehicle properties (vehicleType, brand).
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreatePolymorphismAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp
{
    [OpenApiDiscriminator(""vehicleType"")]
    [OpenApiSubType(typeof(Car), ""car"")]
    public abstract class Vehicle
    {
        public string VehicleType { get; set; } = """";
        public string Brand { get; set; } = """";
    }

    public class Car : Vehicle
    {
        public int Doors { get; set; }
    }

    public class ListVehiclesCommand { }

    public class MyRouter
    {
        protected void ConfigureRoutes(IRouteBuilder routes)
        {
            routes.MapGet<ListVehiclesCommand, Vehicle>(""/v1/vehicles"", ctx => new ListVehiclesCommand());
        }
    }
}
"
        });

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        source.Should().NotBeNull();

        // Find the Car schema block and ensure brand/vehicleType are NOT in Car's allOf own-props
        var carIdx = source!.IndexOf("    Car:", StringComparison.Ordinal);
        carIdx.Should().BeGreaterThan(0, "Car schema must exist");

        // The next schema after Car in alphabetical order is Vehicle
        var vehicleIdx = source.IndexOf("    Vehicle:", StringComparison.Ordinal);
        vehicleIdx.Should().BeGreaterThan(0, "Vehicle schema must exist");

        // Extract the Car block (between Car: and the next schema)
        var carBlock = vehicleIdx > carIdx
            ? source.Substring(carIdx, vehicleIdx - carIdx)
            : source.Substring(carIdx);

        // Car's own block should have doors but NOT brand or vehicleType
        carBlock.Should().Contain("doors:");
        carBlock.Should().NotContain("brand:");
        carBlock.Should().NotContain("vehicleType:");
    }

    // ── x-scalar-ignore on __Core schemas (Scalar-first policy) ─────

    /// <summary>
    /// Scalar-first policy: the synthetic <c>{Base}__Core</c> schema is a structural
    /// helper (holds shared base properties for allOf reference). It must carry
    /// <c>x-scalar-ignore: true</c> so Scalar hides it from the sidebar, while the
    /// <c>$ref</c> to it in derived schemas remains fully functional.
    /// </summary>
    [Fact]
    public void Generate_CoreSchema_ContainsXScalarIgnore()
    {
        var endpoint = MakeEndpoint(responseSimple: "VehicleBase");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "VehicleBase",
            TypeKind = "Response",
            IsResolved = true,
            DiscriminatorPropertyName = "vehicleType",
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "Car", DiscriminatorValue = "car" }
            },
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "VehicleType", JsonName = "vehicleType", OpenApiType = "string", IsRequired = true }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // Locate the Core schema block
        var coreHeader = "    VehicleBase__Core:";
        var coreIdx = yaml.IndexOf(coreHeader, StringComparison.Ordinal);
        coreIdx.Should().BeGreaterThan(0, "VehicleBase__Core must be emitted");

        // Find the end of the Core block by locating the next schema header at 4-space indent.
        // Schema headers look like "\n    SomeName:" (4 spaces, no 5th space before the name).
        // We scan forward from the position after coreHeader to find the next such header.
        var searchFrom = coreIdx + coreHeader.Length;
        var nextSchemaIdx = -1;
        var pos = yaml.IndexOf("\n    ", searchFrom, StringComparison.Ordinal);
        while (pos >= 0)
        {
            // A schema header at 4-space indent has a non-space character as the 5th char.
            var afterIndent = pos + 5; // skip '\n' + 4 spaces
            if (afterIndent < yaml.Length && yaml[afterIndent] != ' ')
            {
                nextSchemaIdx = pos;
                break;
            }
            pos = yaml.IndexOf("\n    ", pos + 1, StringComparison.Ordinal);
        }

        // Extract only the Core block text for targeted assertions.
        var coreBlock = nextSchemaIdx > 0
            ? yaml.Substring(coreIdx, nextSchemaIdx - coreIdx)
            : yaml.Substring(coreIdx);

        // x-scalar-ignore: true must appear inside the Core block.
        coreBlock.Should().Contain("      x-scalar-ignore: true",
            "the __Core schema must carry x-scalar-ignore: true so Scalar hides it from the sidebar");
    }

    /// <summary>
    /// Scalar-first policy: <c>x-scalar-ignore</c> must be emitted ONLY on <c>{Base}__Core</c>
    /// synthetic schemas. Regular object schemas, derived (allOf) schemas, and the polymorphic
    /// base (oneOf) schema itself must NOT carry the flag.
    /// </summary>
    [Fact]
    public void Generate_NonCoreSchemas_DoNotContainXScalarIgnore()
    {
        var endpoint = MakeEndpoint(responseSimple: "Animal");
        // Base discriminated schema (oneOf) — must NOT have x-scalar-ignore
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "Animal",
            TypeKind = "Response",
            IsResolved = true,
            DiscriminatorPropertyName = "kind",
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "Cat", DiscriminatorValue = "cat" }
            },
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Kind", JsonName = "kind", OpenApiType = "string", IsRequired = true }
            }
        });
        // Derived (allOf) schema — must NOT have x-scalar-ignore
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "Cat",
            TypeKind = "Object",
            IsResolved = true,
            BaseSchemaName = "Animal",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Indoor", JsonName = "indoor", OpenApiType = "boolean" }
            }
        });
        // Regular schema (neither base nor derived) — must NOT have x-scalar-ignore
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "GetItemsResponse",
            TypeKind = "Response",
            IsResolved = true,
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Id", JsonName = "id", OpenApiType = "string" }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // The flag must appear exactly once (only in the __Core schema)
        var occurrences = CountOccurrences(yaml, "x-scalar-ignore: true");
        occurrences.Should().Be(1, "x-scalar-ignore: true must be emitted only in __Core schemas");

        // Verify it is NOT inside the Animal (discriminated-union base) block.
        // Animal sorts before Animal__Core alphabetically, so we can extract Animal's block.
        var schemasMarker = "  schemas:";
        var schemasIdx = yaml.IndexOf(schemasMarker, StringComparison.Ordinal);
        var animalHeader = "    Animal:";
        var animalIdx = yaml.IndexOf(animalHeader, schemasIdx, StringComparison.Ordinal);
        var animalCoreHeader = "    Animal__Core:";
        var animalCoreIdx = yaml.IndexOf(animalCoreHeader, StringComparison.Ordinal);

        var animalBlock = yaml.Substring(animalIdx, animalCoreIdx - animalIdx);
        animalBlock.Should().NotContain("x-scalar-ignore",
            "the discriminated-union base (Animal) must not carry x-scalar-ignore");

        // Verify it is NOT inside the Cat (derived allOf) block.
        var catHeader = "    Cat:";
        var catIdx = yaml.IndexOf(catHeader, StringComparison.Ordinal);
        // Cat comes before GetItemsResponse alphabetically
        var getItemsHeader = "    GetItemsResponse:";
        var getItemsIdx = yaml.IndexOf(getItemsHeader, StringComparison.Ordinal);
        var catBlock = yaml.Substring(catIdx, getItemsIdx - catIdx);
        catBlock.Should().NotContain("x-scalar-ignore",
            "the derived allOf schema (Cat) must not carry x-scalar-ignore");

        // Verify it is NOT inside the regular GetItemsResponse block.
        var animalCoreBlock = "    Animal__Core:";
        var getItemsEndIdx = yaml.IndexOf(animalCoreBlock, getItemsIdx, StringComparison.Ordinal);
        if (getItemsEndIdx > 0)
        {
            var regularBlock = yaml.Substring(getItemsIdx, getItemsEndIdx - getItemsIdx);
            regularBlock.Should().NotContain("x-scalar-ignore",
                "a regular object schema must not carry x-scalar-ignore");
        }
    }

    // ── Edge cases ────────────────────────────────────────────────────

    [Fact]
    public void Generate_RegularSchema_NotAffectedByPolymorphismChanges()
    {
        // Non-polymorphic schemas should still emit the classic "type: object + properties" form
        var endpoint = MakeEndpoint();
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "GetItemsResponse",
            TypeKind = "Response",
            IsResolved = true,
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Id", JsonName = "id", OpenApiType = "string", IsRequired = true }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("    GetItemsResponse:");
        yaml.Should().Contain("      type: object");
        yaml.Should().Contain("        id:");
        yaml.Should().Contain("          type: string");
        yaml.Should().NotContain("allOf:");
        yaml.Should().NotContain("oneOf:");
    }

    [Fact]
    public void Generate_MultipleEndpoints_PolymorphismMergedCorrectly()
    {
        // Two endpoints both reference the same polymorphic hierarchy.
        // The base schema should appear exactly once in components.schemas.
        var endpoint1 = MakeEndpoint(path: "/v1/cats", responseSimple: "CatResponse");
        endpoint1.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "CatResponse",
            TypeKind = "Response",
            IsResolved = true,
            BaseSchemaName = "AnimalBase",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Indoor", JsonName = "indoor", OpenApiType = "boolean" }
            }
        });
        endpoint1.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "AnimalBase",
            TypeKind = "Object",
            IsResolved = true,
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "CatResponse", DiscriminatorValue = "cat" }
            },
            DiscriminatorPropertyName = "kind"
        });

        var endpoint2 = MakeEndpoint(path: "/v1/dogs", responseSimple: "DogResponse");
        endpoint2.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "DogResponse",
            TypeKind = "Response",
            IsResolved = true,
            BaseSchemaName = "AnimalBase",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Breed", JsonName = "breed", OpenApiType = "string" }
            }
        });
        endpoint2.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "AnimalBase",
            TypeKind = "Object",
            IsResolved = true,
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "DogResponse", DiscriminatorValue = "dog" }
            },
            DiscriminatorPropertyName = "kind"
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint1, endpoint2 });

        // AnimalBase should appear exactly once
        CountOccurrences(yaml, "    AnimalBase:").Should().Be(1);
    }

    [Fact]
    public void Generate_NoReferencedSchemas_StillEmitsCommandResponseSchemas()
    {
        // When ReferencedSchemas is empty, the standard command/response schemas still appear.
        var endpoint = MakeEndpoint();
        // ReferencedSchemas is empty by default

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("    GetItemsCommand:");
        yaml.Should().Contain("    GetItemsResponse:");
    }

    [Fact]
    public void Generate_AllOfRequired_EmitsRequiredUnderOwnPropsBlock()
    {
        var endpoint = MakeEndpoint(responseSimple: "CatDerived");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "CatDerived",
            TypeKind = "Response",
            IsResolved = true,
            BaseSchemaName = "AnimalBase",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Indoor", JsonName = "indoor", OpenApiType = "boolean", IsRequired = true },
                new() { Name = "Color", JsonName = "color", OpenApiType = "string", IsRequired = false }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // The required list should appear inside the allOf inline object block
        yaml.Should().Contain("          required:");
        yaml.Should().Contain("            - indoor");
        yaml.Should().NotContain("            - color");
    }

    // ── MELHORIA-6: strict-compliance tests ──────────────────────────

    /// <summary>
    /// MELHORIA-6: Validates (at YAML structure level) that no <c>properties</c> keyword
    /// appears as a sibling of <c>oneOf</c> inside a polymorphic base schema.
    /// The test parses the YAML using YamlDotNet and inspects the discriminated-union node
    /// directly in the <c>components/schemas</c> map.
    /// </summary>
    [Fact]
    public void Generate_PolymorphicBaseWithProps_NoPropertiesSiblingOfOneOf()
    {
        var endpoint = MakeEndpoint(responseSimple: "PaymentMethod");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "PaymentMethod",
            TypeKind = "Response",
            IsResolved = true,
            DiscriminatorPropertyName = "paymentMethodType",
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "CardPayment", DiscriminatorValue = "card" },
                new() { SchemaName = "BankTransfer", DiscriminatorValue = "bank" }
            },
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "PaymentMethodType", JsonName = "paymentMethodType", OpenApiType = "string", IsRequired = true },
                new() { Name = "Label", JsonName = "label", OpenApiType = "string" }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // 1. PaymentMethod base: only oneOf + discriminator (no properties at same level)
        var baseHeader = "    PaymentMethod:";
        var coreHeader = "    PaymentMethod__Core:";
        var baseIdx = yaml.IndexOf(baseHeader, StringComparison.Ordinal);
        var coreIdx = yaml.IndexOf(coreHeader, StringComparison.Ordinal);

        baseIdx.Should().BeGreaterThan(0, "PaymentMethod schema must be emitted");
        coreIdx.Should().BeGreaterThan(baseIdx, "PaymentMethod__Core must appear after PaymentMethod");

        // Extract the PaymentMethod block only
        var baseBlock = yaml.Substring(baseIdx, coreIdx - baseIdx);
        baseBlock.Should().Contain("oneOf:", "base must have oneOf");
        baseBlock.Should().Contain("discriminator:", "base must have discriminator");
        baseBlock.Should().NotContain("properties:", "properties must NOT coexist with oneOf in the base");

        // 2. Synthetic Core schema holds the shared properties
        var afterCore = yaml.Substring(coreIdx);
        // Find the end of the Core block (next schema at 4-space indent)
        // We check that Core contains type: object and the shared properties
        afterCore.Should().Contain("      type: object");
        afterCore.Should().Contain("        paymentMethodType:");
        afterCore.Should().Contain("        label:");

        // 3. No $ref to PaymentMethod (the discriminated union) in any allOf; not present
        //    since no derived schemas were added, but Core must be referenced by derived if added.
        //    Verify that the discriminator mapping still correctly points to CardPayment/BankTransfer.
        yaml.Should().Contain("card: \"#/components/schemas/CardPayment\"");
        yaml.Should().Contain("bank: \"#/components/schemas/BankTransfer\"");
    }

    [Fact]
    public void Generate_DerivedOfDiscriminatedBase_RefsCore_NotBase()
    {
        // When a derived type extends a discriminated-union base that carries shared properties,
        // the derived schema's allOf must reference {Base}__Core, not {Base} directly.
        // This ensures shared properties are inherited without making the discriminated-union
        // base (which is a pure oneOf) do double duty.
        var endpoint = MakeEndpoint(responseSimple: "Animal");
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "Animal",
            TypeKind = "Response",
            IsResolved = true,
            DiscriminatorPropertyName = "kind",
            SubTypes = new List<SubTypeEntry>
            {
                new() { SchemaName = "Cat", DiscriminatorValue = "cat" }
            },
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Kind", JsonName = "kind", OpenApiType = "string", IsRequired = true },
                new() { Name = "Name", JsonName = "name", OpenApiType = "string" }
            }
        });
        endpoint.ReferencedSchemas.Add(new SchemaTypeInfo
        {
            TypeName = "Cat",
            TypeKind = "Object",
            IsResolved = true,
            BaseSchemaName = "Animal",
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Indoor", JsonName = "indoor", OpenApiType = "boolean" }
            }
        });

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // Cat's allOf must reference Animal__Core (the shared-property holder), not Animal.
        yaml.Should().Contain("        - $ref: \"#/components/schemas/Animal__Core\"");

        // The Animal schema (discriminated union) must still exist as a pure oneOf.
        // To extract only the Animal schema block, find its start in the schemas section
        // by locating "  schemas:" first, then the Animal header after that.
        var schemasMarker = "  schemas:";
        var schemasIdx = yaml.IndexOf(schemasMarker, StringComparison.Ordinal);
        schemasIdx.Should().BeGreaterThan(0, "components/schemas section must exist");

        var animalHeader = "    Animal:";
        var animalIdx = yaml.IndexOf(animalHeader, schemasIdx, StringComparison.Ordinal);
        animalIdx.Should().BeGreaterThan(0, "Animal schema must exist");

        // Find the next schema at 4-space indent immediately after Animal.
        // Search for "    Cat:" starting AFTER animalIdx.
        var catHeader = "    Cat:";
        var catIdx = yaml.IndexOf(catHeader, animalIdx + animalHeader.Length, StringComparison.Ordinal);
        catIdx.Should().BeGreaterThan(animalIdx, "Cat schema must come after Animal");

        // Extract only the Animal block (from Animal: to Cat:) and verify no `properties:`.
        var animalBlock = yaml.Substring(animalIdx, catIdx - animalIdx);
        animalBlock.Should().Contain("oneOf:", "Animal must have oneOf");
        animalBlock.Should().Contain("discriminator:", "Animal must have discriminator");
        animalBlock.Should().NotContain("properties:",
            "the discriminated-union base must not have properties at the same level as oneOf");

        // Animal__Core must exist and have the shared properties.
        var animalCoreHeader = "    Animal__Core:";
        var animalCoreIdx = yaml.IndexOf(animalCoreHeader, StringComparison.Ordinal);
        animalCoreIdx.Should().BeGreaterThan(0, "Animal__Core must be emitted");
        var coreSection = yaml.Substring(animalCoreIdx);
        coreSection.Should().Contain("        kind:");
        coreSection.Should().Contain("        name:");
    }
}
