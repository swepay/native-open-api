namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Tests for Schema Richness (Wave 3):
/// - description, example, default per property
/// - String constraints: minLength, maxLength, pattern
/// - Numeric constraints: minimum, maximum, exclusiveMinimum, exclusiveMaximum, multipleOf
/// - Array constraints: minItems, maxItems, uniqueItems
/// - x-enum-descriptions and x-enum-varnames (Scalar-first)
/// - x-additionalPropertiesName for dictionaries
/// - x-order (Scalar)
/// - DataAnnotations interop: [Required], [StringLength], [MinLength], [MaxLength], [Range], [RegularExpression]
/// </summary>
public sealed class SchemaRichnessTests
{
    // ── helpers ───────────────────────────────────────────────────────

    private static string Generate(SchemaPropertyInfo prop, string schemaName = "TestSchema")
    {
        var endpoint = MakeEndpointWithProperty(prop, schemaName);
        return OpenApiYamlGenerator.Generate(
            new List<EndpointInfo> { endpoint },
            "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null);
    }

    private static EndpointInfo MakeEndpointWithProperty(SchemaPropertyInfo prop, string schemaName)
    {
        var schema = new List<SchemaPropertyInfo> { prop };
        return new EndpointInfo
        {
            Method = "POST",
            Path = "/test",
            CommandTypeName = $"App.{schemaName}",
            ResponseTypeName = "App.TestResponse",
            CommandSimpleName = schemaName,
            ResponseSimpleName = "TestResponse",
            CommandProperties = schema,
            CommandPropertiesResolved = true,
            ResponseProperties = new List<SchemaPropertyInfo>(),
            ResponsePropertiesResolved = true
        };
    }

    private static SchemaPropertyInfo StringProp(string name = "name") => new()
    {
        Name = char.ToUpperInvariant(name[0]) + name.Substring(1),
        JsonName = name,
        OpenApiType = "string"
    };

    private static SchemaPropertyInfo IntProp(string name = "count") => new()
    {
        Name = char.ToUpperInvariant(name[0]) + name.Substring(1),
        JsonName = name,
        OpenApiType = "integer",
        OpenApiFormat = "int32"
    };

    private static SchemaPropertyInfo ArrayProp(string name = "tags") => new()
    {
        Name = char.ToUpperInvariant(name[0]) + name.Substring(1),
        JsonName = name,
        OpenApiType = "array",
        ArrayItemType = "string"
    };

    private static SchemaPropertyInfo EnumProp(string name = "status") => new()
    {
        Name = char.ToUpperInvariant(name[0]) + name.Substring(1),
        JsonName = name,
        OpenApiType = "string",
        IsEnum = true,
        EnumValues = new List<string> { "Active", "Inactive", "Suspended" }
    };

    // ── description ───────────────────────────────────────────────────

    [Fact]
    public void Generate_PropertyWithDescription_EmitsDescription()
    {
        var prop = StringProp();
        prop.Description = "The product name";

        var yaml = Generate(prop);

        yaml.Should().Contain("          description: \"The product name\"");
    }

    [Fact]
    public void Generate_PropertyWithoutDescription_DoesNotEmitDescription()
    {
        var prop = StringProp();
        // Description is null

        var yaml = Generate(prop);

        // Only operation-level or response-level descriptions should appear, not property-level
        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("description:");
    }

    [Fact]
    public void Generate_DescriptionWithSpecialChars_IsEscaped()
    {
        var prop = StringProp();
        prop.Description = "Name with \"quotes\" and backslash \\";

        var yaml = Generate(prop);

        yaml.Should().Contain("\\\"quotes\\\"");
        yaml.Should().Contain("\\\\");
    }

    // ── example ───────────────────────────────────────────────────────

    [Fact]
    public void Generate_PropertyWithStringExample_EmitsQuotedExample()
    {
        var prop = StringProp();
        prop.Example = "Widget Pro";

        var yaml = Generate(prop);

        yaml.Should().Contain("          example: \"Widget Pro\"");
    }

    [Fact]
    public void Generate_PropertyWithNumericExample_EmitsUnquotedExample()
    {
        var prop = IntProp();
        prop.Example = "42";

        var yaml = Generate(prop);

        yaml.Should().Contain("          example: 42");
        // Must NOT be quoted
        yaml.Should().NotContain("example: \"42\"");
    }

    [Fact]
    public void Generate_PropertyWithBooleanExample_EmitsUnquotedExample()
    {
        var prop = new SchemaPropertyInfo
        {
            Name = "IsActive", JsonName = "isActive", OpenApiType = "boolean",
            Example = "true"
        };

        var yaml = Generate(prop);

        yaml.Should().Contain("          example: true");
    }

    [Fact]
    public void Generate_PropertyWithoutExample_DoesNotEmitExample()
    {
        var prop = StringProp();
        // Example is null

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("example:");
    }

    // ── default ───────────────────────────────────────────────────────

    [Fact]
    public void Generate_PropertyWithStringDefault_EmitsQuotedDefault()
    {
        var prop = StringProp();
        prop.Default = "Unknown";

        var yaml = Generate(prop);

        yaml.Should().Contain("          default: \"Unknown\"");
    }

    [Fact]
    public void Generate_PropertyWithNumericDefault_EmitsUnquotedDefault()
    {
        var prop = IntProp();
        prop.Default = "0";

        var yaml = Generate(prop);

        yaml.Should().Contain("          default: 0");
        yaml.Should().NotContain("default: \"0\"");
    }

    [Fact]
    public void Generate_PropertyWithoutDefault_DoesNotEmitDefault()
    {
        var prop = StringProp();

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("default:");
    }

    // ── string constraints ────────────────────────────────────────────

    [Fact]
    public void Generate_PropertyWithMinLength_EmitsMinLength()
    {
        var prop = StringProp();
        prop.MinLength = 3;

        var yaml = Generate(prop);

        yaml.Should().Contain("          minLength: 3");
    }

    [Fact]
    public void Generate_PropertyWithMaxLength_EmitsMaxLength()
    {
        var prop = StringProp();
        prop.MaxLength = 100;

        var yaml = Generate(prop);

        yaml.Should().Contain("          maxLength: 100");
    }

    [Fact]
    public void Generate_PropertyWithPattern_EmitsPattern()
    {
        var prop = StringProp();
        prop.Pattern = @"^[A-Za-z0-9]+$";

        var yaml = Generate(prop);

        yaml.Should().Contain("          pattern: \"^[A-Za-z0-9]+$\"");
    }

    [Fact]
    public void Generate_PropertyWithPatternContainingBackslash_IsEscaped()
    {
        var prop = StringProp();
        prop.Pattern = @"^\d+$";

        var yaml = Generate(prop);

        // The backslash before d must be escaped to \\ in the YAML quoted string
        yaml.Should().Contain("^\\\\d+$");
    }

    [Fact]
    public void Generate_PropertyWithAllStringConstraints_EmitsAll()
    {
        var prop = StringProp();
        prop.MinLength = 1;
        prop.MaxLength = 50;
        prop.Pattern = @"^[a-z]+$";

        var yaml = Generate(prop);

        yaml.Should().Contain("minLength: 1");
        yaml.Should().Contain("maxLength: 50");
        yaml.Should().Contain("pattern:");
    }

    [Fact]
    public void Generate_MinLengthNotSet_DoesNotEmitMinLength()
    {
        var prop = StringProp();
        // MinLength defaults to -1 (not set)

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("minLength:");
    }

    [Fact]
    public void Generate_MaxLengthNotSet_DoesNotEmitMaxLength()
    {
        var prop = StringProp();

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("maxLength:");
    }

    // ── numeric constraints ───────────────────────────────────────────

    [Fact]
    public void Generate_PropertyWithMinimum_EmitsMinimum()
    {
        var prop = IntProp();
        prop.Minimum = 0;

        var yaml = Generate(prop);

        yaml.Should().Contain("          minimum: 0");
    }

    [Fact]
    public void Generate_PropertyWithMaximum_EmitsMaximum()
    {
        var prop = IntProp();
        prop.Maximum = 1000;

        var yaml = Generate(prop);

        yaml.Should().Contain("          maximum: 1000");
    }

    [Fact]
    public void Generate_PropertyWithFractionalMinimum_EmitsDecimal()
    {
        var prop = new SchemaPropertyInfo
        {
            Name = "Price", JsonName = "price", OpenApiType = "number", OpenApiFormat = "double",
            Minimum = 0.01
        };

        var yaml = Generate(prop);

        yaml.Should().Contain("minimum: 0.01");
    }

    [Fact]
    public void Generate_PropertyWithExclusiveMinimum_EmitsExclusiveMinimum()
    {
        var prop = IntProp();
        prop.ExclusiveMinimum = 0;

        var yaml = Generate(prop);

        yaml.Should().Contain("          exclusiveMinimum: 0");
    }

    [Fact]
    public void Generate_PropertyWithExclusiveMaximum_EmitsExclusiveMaximum()
    {
        var prop = IntProp();
        prop.ExclusiveMaximum = 100;

        var yaml = Generate(prop);

        yaml.Should().Contain("          exclusiveMaximum: 100");
    }

    [Fact]
    public void Generate_PropertyWithMultipleOf_EmitsMultipleOf()
    {
        var prop = IntProp();
        prop.MultipleOf = 5;

        var yaml = Generate(prop);

        yaml.Should().Contain("          multipleOf: 5");
    }

    [Fact]
    public void Generate_MinimumNotSet_DoesNotEmitMinimum()
    {
        var prop = IntProp();
        // Minimum defaults to NaN

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("minimum:");
    }

    [Fact]
    public void Generate_MaximumNotSet_DoesNotEmitMaximum()
    {
        var prop = IntProp();

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("maximum:");
    }

    // ── array constraints ─────────────────────────────────────────────

    [Fact]
    public void Generate_ArrayPropertyWithMinItems_EmitsMinItems()
    {
        var prop = ArrayProp();
        prop.MinItems = 1;

        var yaml = Generate(prop);

        yaml.Should().Contain("          minItems: 1");
    }

    [Fact]
    public void Generate_ArrayPropertyWithMaxItems_EmitsMaxItems()
    {
        var prop = ArrayProp();
        prop.MaxItems = 10;

        var yaml = Generate(prop);

        yaml.Should().Contain("          maxItems: 10");
    }

    [Fact]
    public void Generate_ArrayPropertyWithUniqueItems_EmitsUniqueItems()
    {
        var prop = ArrayProp();
        prop.UniqueItems = true;

        var yaml = Generate(prop);

        yaml.Should().Contain("          uniqueItems: true");
    }

    [Fact]
    public void Generate_UniqueItemsFalse_DoesNotEmitUniqueItems()
    {
        var prop = ArrayProp();
        prop.UniqueItems = false;

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("uniqueItems:");
    }

    [Fact]
    public void Generate_ArrayPropertyNoConstraints_DoesNotEmitArrayConstraints()
    {
        var prop = ArrayProp();
        // No constraints set

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("minItems:");
        schemaSection.Should().NotContain("maxItems:");
        schemaSection.Should().NotContain("uniqueItems:");
    }

    // ── x-order ──────────────────────────────────────────────────────

    [Fact]
    public void Generate_PropertyWithOrder_EmitsXOrder()
    {
        var prop = StringProp();
        prop.Order = 3;

        var yaml = Generate(prop);

        yaml.Should().Contain("          x-order: 3");
    }

    [Fact]
    public void Generate_PropertyWithOrderZero_EmitsXOrderZero()
    {
        var prop = StringProp();
        prop.Order = 0;

        var yaml = Generate(prop);

        yaml.Should().Contain("          x-order: 0");
    }

    [Fact]
    public void Generate_PropertyWithNoOrder_DoesNotEmitXOrder()
    {
        var prop = StringProp();
        // Order defaults to -1 (not set)

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("x-order:");
    }

    // ── x-additionalPropertiesName (dictionary) ───────────────────────

    [Fact]
    public void Generate_DictionaryPropertyWithAdditionalPropertiesName_EmitsExtension()
    {
        var prop = new SchemaPropertyInfo
        {
            Name = "Metadata", JsonName = "metadata",
            OpenApiType = "object", IsDictionary = true,
            AdditionalPropertiesName = "MetadataValue"
        };

        var yaml = Generate(prop);

        yaml.Should().Contain("          additionalProperties: true");
        yaml.Should().Contain("          x-additionalPropertiesName: \"MetadataValue\"");
    }

    [Fact]
    public void Generate_DictionaryPropertyWithoutAdditionalPropertiesName_EmitsOnlyAdditionalProperties()
    {
        var prop = new SchemaPropertyInfo
        {
            Name = "Metadata", JsonName = "metadata",
            OpenApiType = "object", IsDictionary = true
        };

        var yaml = Generate(prop);

        yaml.Should().Contain("          additionalProperties: true");
        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("x-additionalPropertiesName:");
    }

    [Fact]
    public void Generate_NonDictionaryObjectProperty_DoesNotEmitAdditionalProperties()
    {
        var prop = new SchemaPropertyInfo
        {
            Name = "Address", JsonName = "address",
            OpenApiType = "object", RefSchemaName = "AddressDto"
        };

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("additionalProperties:");
    }

    // ── x-enum-descriptions ───────────────────────────────────────────

    [Fact]
    public void Generate_EnumPropertyWithDescriptions_EmitsXEnumDescriptions()
    {
        var prop = EnumProp();
        prop.EnumDescriptions = new List<string?> { "Account active", "Account inactive", "Account suspended" };

        var yaml = Generate(prop);

        yaml.Should().Contain("          x-enum-descriptions:");
        yaml.Should().Contain("            - \"Account active\"");
        yaml.Should().Contain("            - \"Account inactive\"");
        yaml.Should().Contain("            - \"Account suspended\"");
    }

    [Fact]
    public void Generate_EnumPropertyWithoutDescriptions_DoesNotEmitXEnumDescriptions()
    {
        var prop = EnumProp();
        // EnumDescriptions is null

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("x-enum-descriptions:");
    }

    [Fact]
    public void Generate_EnumPropertyWithNullDescriptionForOneMember_EmitsEmptyString()
    {
        var prop = EnumProp();
        prop.EnumDescriptions = new List<string?> { "Account active", null, "Account suspended" };

        var yaml = Generate(prop);

        yaml.Should().Contain("x-enum-descriptions:");
        // The null entry should be emitted as an empty quoted string
        yaml.Should().Contain("            - \"\"");
    }

    // ── x-enum-varnames ───────────────────────────────────────────────

    [Fact]
    public void Generate_EnumPropertyWithVarNames_EmitsXEnumVarnames()
    {
        var prop = EnumProp();
        prop.EnumVarNames = new List<string?> { "ACTIVE", "INACTIVE", "SUSPENDED" };

        var yaml = Generate(prop);

        yaml.Should().Contain("          x-enum-varnames:");
        yaml.Should().Contain("            - \"ACTIVE\"");
        yaml.Should().Contain("            - \"INACTIVE\"");
        yaml.Should().Contain("            - \"SUSPENDED\"");
    }

    [Fact]
    public void Generate_EnumPropertyWithoutVarNames_DoesNotEmitXEnumVarnames()
    {
        var prop = EnumProp();

        var yaml = Generate(prop);

        var schemaSection = ExtractSchemaSection(yaml, "TestSchema");
        schemaSection.Should().NotContain("x-enum-varnames:");
    }

    [Fact]
    public void Generate_EnumWithBothDescriptionsAndVarNames_EmitsBoth()
    {
        var prop = EnumProp();
        prop.EnumDescriptions = new List<string?> { "Active desc", "Inactive desc", "Suspended desc" };
        prop.EnumVarNames = new List<string?> { "ACTIVE", "INACTIVE", "SUSPENDED" };

        var yaml = Generate(prop);

        yaml.Should().Contain("x-enum-descriptions:");
        yaml.Should().Contain("x-enum-varnames:");

        // x-enum-descriptions must appear before x-enum-varnames (declaration order)
        var descIdx = yaml.IndexOf("x-enum-descriptions:", StringComparison.Ordinal);
        var varIdx = yaml.IndexOf("x-enum-varnames:", StringComparison.Ordinal);
        descIdx.Should().BeLessThan(varIdx);
    }

    [Fact]
    public void Generate_EnumExtensions_DoNotEmitRedocEquivalents()
    {
        var prop = EnumProp();
        prop.EnumDescriptions = new List<string?> { "desc1", "desc2", "desc3" };

        var yaml = Generate(prop);

        // Scalar-first: no Redoc dual-emit
        yaml.Should().NotContain("x-enumDescriptions:");
        yaml.Should().NotContain("x-enumNames:");
    }

    // ── all enrichments together ──────────────────────────────────────

    [Fact]
    public void Generate_FullyEnrichedStringProperty_EmitsAllKeywords()
    {
        var prop = StringProp("sku");
        prop.Description = "Stock keeping unit";
        prop.Example = "SKU-001";
        prop.Default = "UNKNOWN";
        prop.MinLength = 3;
        prop.MaxLength = 20;
        prop.Pattern = @"^SKU-\d+$";
        prop.Order = 1;

        var yaml = Generate(prop);

        yaml.Should().Contain("description: \"Stock keeping unit\"");
        yaml.Should().Contain("example: \"SKU-001\"");
        yaml.Should().Contain("default: \"UNKNOWN\"");
        yaml.Should().Contain("minLength: 3");
        yaml.Should().Contain("maxLength: 20");
        yaml.Should().Contain("pattern:");
        yaml.Should().Contain("x-order: 1");
    }

    [Fact]
    public void Generate_FullyEnrichedNumericProperty_EmitsAllConstraints()
    {
        var prop = new SchemaPropertyInfo
        {
            Name = "Price", JsonName = "price",
            OpenApiType = "number", OpenApiFormat = "double",
            Minimum = 0.01,
            Maximum = 9999.99,
            MultipleOf = 0.01,
            Example = "9.99",
            Order = 2
        };

        var yaml = Generate(prop);

        yaml.Should().Contain("minimum: 0.01");
        yaml.Should().Contain("maximum: 9999.99");
        yaml.Should().Contain("multipleOf: 0.01");
        yaml.Should().Contain("example: 9.99");
        yaml.Should().Contain("x-order: 2");
    }

    [Fact]
    public void Generate_FullyEnrichedArrayProperty_EmitsAllConstraints()
    {
        var prop = ArrayProp("tags");
        prop.Description = "Tags for filtering";
        prop.MinItems = 1;
        prop.MaxItems = 5;
        prop.UniqueItems = true;
        prop.Order = 3;

        var yaml = Generate(prop);

        yaml.Should().Contain("description: \"Tags for filtering\"");
        yaml.Should().Contain("minItems: 1");
        yaml.Should().Contain("maxItems: 5");
        yaml.Should().Contain("uniqueItems: true");
        yaml.Should().Contain("x-order: 3");
    }

    // ── determinism ───────────────────────────────────────────────────

    [Fact]
    public void Generate_SameInput_ProducesDeterministicYaml()
    {
        var prop = StringProp("name");
        prop.Description = "Product name";
        prop.Example = "Widget";
        prop.MinLength = 1;
        prop.MaxLength = 100;
        prop.Order = 1;

        var yaml1 = Generate(prop);
        var yaml2 = Generate(prop);

        yaml1.Should().Be(yaml2);
    }

    // ── source generator end-to-end (Roslyn attribute reading) ────────

    [Fact]
    public void Generator_OpenApiPropertyAttribute_DescriptionAndExample_AreEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateSchemaRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

public class CreateWidgetCommand
{
    [OpenApiProperty(Description = ""The widget name"", Example = ""Fancy Widget"", Order = 1)]
    public string Name { get; set; }
}
public class CreateWidgetResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateWidgetCommand, CreateWidgetResponse>(""/v1/widgets"", ctx => new CreateWidgetCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("description: \"\"The widget name\"\"");
        generatedSource.Should().Contain("example: \"\"Fancy Widget\"\"");
        generatedSource.Should().Contain("x-order: 1");
    }

    [Fact]
    public void Generator_OpenApiPropertyAttribute_StringConstraints_AreEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateSchemaRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

public class CreateWidgetCommand
{
    [OpenApiProperty(MinLength = 2, MaxLength = 50, Pattern = ""^[A-Za-z]+$"")]
    public string Name { get; set; }
}
public class CreateWidgetResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateWidgetCommand, CreateWidgetResponse>(""/v1/widgets"", ctx => new CreateWidgetCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("minLength: 2");
        generatedSource.Should().Contain("maxLength: 50");
        generatedSource.Should().Contain("pattern:");
    }

    [Fact]
    public void Generator_OpenApiPropertyAttribute_NumericConstraints_AreEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateSchemaRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

public class CreateWidgetCommand
{
    [OpenApiProperty(Minimum = 0.0, Maximum = 1000.0)]
    public double Price { get; set; }
}
public class CreateWidgetResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateWidgetCommand, CreateWidgetResponse>(""/v1/widgets"", ctx => new CreateWidgetCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("minimum: 0");
        generatedSource.Should().Contain("maximum: 1000");
    }

    [Fact]
    public void Generator_OpenApiPropertyAttribute_ArrayConstraints_AreEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateSchemaRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;
using System.Collections.Generic;

namespace TestApp;

public class CreateWidgetCommand
{
    [OpenApiProperty(MinItems = 1, MaxItems = 5, UniqueItems = true)]
    public System.Collections.Generic.List<string> Tags { get; set; }
}
public class CreateWidgetResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateWidgetCommand, CreateWidgetResponse>(""/v1/widgets"", ctx => new CreateWidgetCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("minItems: 1");
        generatedSource.Should().Contain("maxItems: 5");
        generatedSource.Should().Contain("uniqueItems: true");
    }

    [Fact]
    public void Generator_OpenApiEnumMemberAttribute_EmitsXEnumDescriptionsAndVarnames()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateSchemaRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

public enum WidgetStatus
{
    [OpenApiEnumMember(Description = ""Widget is active"", DisplayName = ""ACTIVE"")]
    Active,
    [OpenApiEnumMember(Description = ""Widget is retired"", DisplayName = ""RETIRED"")]
    Retired
}

public class CreateWidgetCommand
{
    public WidgetStatus Status { get; set; }
}
public class CreateWidgetResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateWidgetCommand, CreateWidgetResponse>(""/v1/widgets"", ctx => new CreateWidgetCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("x-enum-descriptions:");
        generatedSource.Should().Contain("Widget is active");
        generatedSource.Should().Contain("Widget is retired");
        generatedSource.Should().Contain("x-enum-varnames:");
        generatedSource.Should().Contain("ACTIVE");
        generatedSource.Should().Contain("RETIRED");
    }

    [Fact]
    public void Generator_OpenApiEnumMemberAttribute_OnlyDescriptions_NoVarnamesEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateSchemaRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;

namespace TestApp;

public enum WidgetStatus
{
    [OpenApiEnumMember(Description = ""Widget is active"")]
    Active,
    [OpenApiEnumMember(Description = ""Widget is retired"")]
    Retired
}

public class CreateWidgetCommand
{
    public WidgetStatus Status { get; set; }
}
public class CreateWidgetResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateWidgetCommand, CreateWidgetResponse>(""/v1/widgets"", ctx => new CreateWidgetCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("x-enum-descriptions:");
        generatedSource.Should().NotContain("x-enum-varnames:");
    }

    [Fact]
    public void Generator_OpenApiPropertyAttribute_AdditionalPropertiesName_IsEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateSchemaRichnessAttributeSource(),
            @"
using Native.OpenApi.Attributes;
using System.Collections.Generic;

namespace TestApp;

public class CreateWidgetCommand
{
    [OpenApiProperty(AdditionalPropertiesName = ""MetadataValue"")]
    public System.Collections.Generic.Dictionary<string, string> Metadata { get; set; }
}
public class CreateWidgetResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateWidgetCommand, CreateWidgetResponse>(""/v1/widgets"", ctx => new CreateWidgetCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("x-additionalPropertiesName: \"\"MetadataValue\"\"");
    }

    [Fact]
    public void Generator_NoOpenApiPropertyAttribute_NoEnrichmentEmitted()
    {
        var result = GeneratorTestHelper.RunGenerator(new[]
        {
            GeneratorTestHelper.CreateRouteBuilderSource(),
            CreateSchemaRichnessAttributeSource(),
            @"
namespace TestApp;

public class CreateWidgetCommand
{
    public string Name { get; set; }
}
public class CreateWidgetResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateWidgetCommand, CreateWidgetResponse>(""/v1/widgets"", ctx => new CreateWidgetCommand());
    }
}
"
        });

        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        generatedSource.Should().NotBeNull();
        generatedSource.Should().NotContain("x-order:");
        generatedSource.Should().NotContain("x-enum-descriptions:");
        generatedSource.Should().NotContain("x-enum-varnames:");
        generatedSource.Should().NotContain("minLength:");
        generatedSource.Should().NotContain("minimum:");
    }

    // ── FormatDouble edge cases ───────────────────────────────────────

    [Fact]
    public void Generate_WholeNumberConstraint_EmitsWithoutDecimalPoint()
    {
        var prop = IntProp();
        prop.Minimum = 0.0;
        prop.Maximum = 100.0;

        var yaml = Generate(prop);

        yaml.Should().Contain("minimum: 0");
        yaml.Should().Contain("maximum: 100");
        // Must not have trailing .0
        yaml.Should().NotContain("minimum: 0.0");
        yaml.Should().NotContain("maximum: 100.0");
    }

    [Fact]
    public void Generate_FractionalConstraint_EmitsDecimalValue()
    {
        var prop = new SchemaPropertyInfo
        {
            Name = "Amount", JsonName = "amount",
            OpenApiType = "number", OpenApiFormat = "double",
            Minimum = 0.01,
            Maximum = 99999.99
        };

        var yaml = Generate(prop);

        yaml.Should().Contain("minimum: 0.01");
        yaml.Should().Contain("maximum: 99999.99");
    }

    // ── helper: attribute source for test compilation ─────────────────

    /// <summary>
    /// Inlines [OpenApiProperty] and [OpenApiEnumMember] attribute definitions for use
    /// in source generator tests. Mirrors the real attributes in Native.OpenApi.Attributes.
    /// </summary>
    public static string CreateSchemaRichnessAttributeSource()
    {
        return @"
namespace Native.OpenApi.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class OpenApiPropertyAttribute : System.Attribute
    {
        public string Description { get; set; }
        public string Example { get; set; }
        public string Default { get; set; }
        public int MinLength { get; set; } = -1;
        public int MaxLength { get; set; } = -1;
        public string Pattern { get; set; }
        public double Minimum { get; set; } = double.NaN;
        public double Maximum { get; set; } = double.NaN;
        public double ExclusiveMinimum { get; set; } = double.NaN;
        public double ExclusiveMaximum { get; set; } = double.NaN;
        public double MultipleOf { get; set; } = double.NaN;
        public int MinItems { get; set; } = -1;
        public int MaxItems { get; set; } = -1;
        public bool UniqueItems { get; set; }
        public int Order { get; set; } = -1;
        public string AdditionalPropertiesName { get; set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class OpenApiEnumMemberAttribute : System.Attribute
    {
        public string Description { get; set; }
        public string DisplayName { get; set; }
    }
}
";
    }

    // ── private helpers ───────────────────────────────────────────────

    /// <summary>
    /// Extracts the YAML block for a specific schema from the components.schemas section.
    /// Returns everything after the schema name until the next schema at the same indentation.
    /// </summary>
    private static string ExtractSchemaSection(string yaml, string schemaName)
    {
        var startMarker = $"    {schemaName}:";
        var start = yaml.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0) return "";

        // Find next schema at same indentation (next line starting with "    <Name>:")
        // or end of components block
        var nextSchema = start + startMarker.Length;
        while (nextSchema < yaml.Length)
        {
            var lineEnd = yaml.IndexOf('\n', nextSchema);
            if (lineEnd < 0) break;
            var lineStart = lineEnd + 1;
            if (lineStart >= yaml.Length) break;

            // Check if this line starts a new 4-space-indented schema entry (but not deeper)
            if (lineStart + 4 < yaml.Length
                && yaml[lineStart] == ' ' && yaml[lineStart + 1] == ' '
                && yaml[lineStart + 2] == ' ' && yaml[lineStart + 3] == ' '
                && yaml[lineStart + 4] != ' ')
            {
                // Could be a new schema: check it contains ':' after the name
                var endOfName = yaml.IndexOf(':', lineStart + 4);
                var endOfLine = yaml.IndexOf('\n', lineStart + 4);
                if (endOfName >= 0 && (endOfLine < 0 || endOfName < endOfLine))
                {
                    // New schema starts here
                    return yaml.Substring(start, lineStart - start);
                }
            }

            nextSchema = lineEnd + 1;
        }

        return yaml.Substring(start);
    }
}
