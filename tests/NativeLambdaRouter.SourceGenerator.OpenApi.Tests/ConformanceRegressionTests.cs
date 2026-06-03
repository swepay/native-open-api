using YamlDotNet.RepresentationModel;

namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Regression tests for OpenAPI 3.1 conformance defects fixed in the audit round:
///   BUG-1  EscapeYamlString now escapes \n and \r inside double-quoted scalars.
///   BUG-2  components/responses section is always emitted so $refs are not dangling.
///   BUG-3  components/securitySchemes is emitted when at least one endpoint requires auth.
///   MELHORIA-4  Webhook payload schema is resolved (not an empty stub).
///   MELHORIA-5  Operation tags are emitted with double-quote escaping.
/// </summary>
public sealed class ConformanceRegressionTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static EndpointInfo MakeAuthEndpoint(string method = "GET", string path = "/v1/items")
        => new()
        {
            Method = method,
            Path = path,
            CommandTypeName = "App.Command",
            ResponseTypeName = "App.Response",
            CommandSimpleName = "Command",
            ResponseSimpleName = "Response",
            RequiresAuth = true
        };

    private static EndpointInfo MakeAnonEndpoint(string method = "GET", string path = "/v1/public")
        => new()
        {
            Method = method,
            Path = path,
            CommandTypeName = "App.PubCommand",
            ResponseTypeName = "App.PubResponse",
            CommandSimpleName = "PubCommand",
            ResponseSimpleName = "PubResponse",
            RequiresAuth = false
        };

    private static string Generate(IReadOnlyList<EndpointInfo> endpoints,
        IReadOnlyList<WebhookInfo>? webhooks = null)
        => OpenApiYamlGenerator.Generate(
            endpoints,
            "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null,
            null,
            new List<OpenApiServerInfo>(),
            webhooks ?? new List<WebhookInfo>());

    /// <summary>
    /// Parses YAML with YamlDotNet and throws if the YAML is syntactically invalid.
    /// </summary>
    private static void AssertValidYaml(string yaml)
    {
        var stream = new YamlStream();
        // YamlDotNet throws YamlException on parse errors.
        stream.Load(new System.IO.StringReader(yaml));
        stream.Documents.Should().NotBeEmpty("a non-empty OpenAPI spec must have at least one YAML document");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BUG-1 — EscapeYamlString: newlines in double-quoted scalars
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_SummaryWithNewline_ProducesValidYaml()
    {
        // Summaries containing \n must be escaped so the double-quoted scalar
        // stays on one line and does not break YAML structure.
        var endpoint = MakeAuthEndpoint();
        endpoint.Summary = "First line\nSecond line";

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // The escaped form must appear verbatim in the output.
        yaml.Should().Contain("\\n", because: "EscapeYamlString must escape newlines as \\n");
        // The literal newline must NOT appear inside a double-quoted scalar.
        // We verify by checking the summary line contains \\n, not a real line break.
        var summaryLine = yaml.Split('\n')
            .FirstOrDefault(l => l.TrimStart().StartsWith("summary:"));
        summaryLine.Should().NotBeNull();
        summaryLine!.Should().Contain("\\n");

        // Full YAML parse must succeed.
        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generate_DescriptionWithCarriageReturnAndNewline_ProducesValidYaml()
    {
        var endpoint = MakeAuthEndpoint();
        endpoint.Description = "Line one\r\nLine two\r\nLine three";

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("\\r\\n", because: "\\r and \\n must both be escaped");
        AssertValidYaml(yaml);
    }

    [Fact]
    public void EscapeYamlString_WithNewlines_DoesNotBreakQuotedScalar()
    {
        // Validates the escaping contract at the unit level (no YAML parse needed).
        var endpoint = MakeAuthEndpoint();
        endpoint.Summary = "Hello\nWorld";

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // There should be exactly one "summary:" line (not split across lines).
        var summaryLines = yaml.Split('\n')
            .Where(l => l.TrimStart().StartsWith("summary:"))
            .ToList();
        summaryLines.Should().HaveCount(1,
            because: "a newline inside the summary must be escaped, not emitted as a real line break");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BUG-2 — components/responses is always emitted
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_AlwaysEmitsComponentsResponses()
    {
        var endpoint = MakeAuthEndpoint();
        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("  responses:", because: "components/responses must always be emitted");
        yaml.Should().Contain("    BadRequest:", because: "BadRequest component response must be defined");
        yaml.Should().Contain("    Unauthorized:", because: "Unauthorized component response must be defined");
        yaml.Should().Contain("    InternalServerError:", because: "InternalServerError component response must be defined");
    }

    [Fact]
    public void Generate_ComponentsResponses_ReferencedByOperations()
    {
        // Every operation emits $ref to these responses, so they must be defined.
        var endpoint = MakeAuthEndpoint();
        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("$ref: \"#/components/responses/BadRequest\"");
        yaml.Should().Contain("$ref: \"#/components/responses/Unauthorized\"");
        yaml.Should().Contain("$ref: \"#/components/responses/InternalServerError\"");
    }

    [Fact]
    public void Generate_ComponentsResponses_NoDanglingRef_ParsesAsValidYaml()
    {
        // The key test: the YAML must parse without errors when $refs exist.
        // YamlDotNet parses structure but not $ref resolution; we verify structure is valid.
        var endpoint = MakeAuthEndpoint();
        var yaml = Generate(new List<EndpointInfo> { endpoint });

        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generate_BadRequest_PointsToSwepayProblemDetails()
    {
        // BadRequest response must carry a schema ref so clients know the error shape.
        var endpoint = MakeAuthEndpoint();
        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // The BadRequest response should reference SwepayProblemDetails.
        var badRequestIdx = yaml.IndexOf("    BadRequest:", StringComparison.Ordinal);
        badRequestIdx.Should().BeGreaterThan(-1);
        var afterBadRequest = yaml.Substring(badRequestIdx, 300);
        afterBadRequest.Should().Contain("SwepayProblemDetails");
    }

    [Fact]
    public void Generate_SwepayProblemDetailsSchema_AlwaysEmitted()
    {
        // SwepayProblemDetails must always be emitted because BadRequest references it.
        var endpoint = MakeAnonEndpoint(); // no problem+json produces
        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("SwepayProblemDetails:");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BUG-3 — components/securitySchemes emitted when auth required
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_WithAuthEndpoint_EmitsSecuritySchemes()
    {
        var endpoint = MakeAuthEndpoint();
        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("  securitySchemes:", because: "JwtBearer is used so securitySchemes must be defined");
        yaml.Should().Contain("    JwtBearer:");
        yaml.Should().Contain("      type: http");
        yaml.Should().Contain("      scheme: bearer");
        yaml.Should().Contain("      bearerFormat: JWT");
    }

    [Fact]
    public void Generate_WithAuthEndpoint_SecuritySchemesValidYaml()
    {
        var endpoint = MakeAuthEndpoint();
        var yaml = Generate(new List<EndpointInfo> { endpoint });
        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generate_WithOnlyAnonEndpoints_DoesNotEmitSecuritySchemes()
    {
        // When no endpoint requires auth, securitySchemes must not pollute the spec.
        var endpoint = MakeAnonEndpoint();
        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().NotContain("securitySchemes:",
            because: "no endpoint requires JwtBearer so the scheme must not be declared");
    }

    [Fact]
    public void Generate_MixedAuthAndAnon_EmitsSecuritySchemes()
    {
        // When at least one endpoint is authenticated, securitySchemes must be present.
        var endpoints = new List<EndpointInfo>
        {
            MakeAnonEndpoint(),
            MakeAuthEndpoint(path: "/v1/admin")
        };
        var yaml = Generate(endpoints);

        yaml.Should().Contain("securitySchemes:");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MELHORIA-5 — operation tags quoted with EscapeYamlString
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_TagWithSpecialChars_IsQuoted()
    {
        var endpoint = MakeAuthEndpoint();
        endpoint.Tags = new List<string> { "Order: Management", "User#Admin" };

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // Both tags must be quoted so the colon and hash do not break YAML.
        yaml.Should().Contain("- \"Order: Management\"");
        yaml.Should().Contain("- \"User#Admin\"");
        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generate_TagWithNewline_IsEscaped()
    {
        var endpoint = MakeAuthEndpoint();
        endpoint.Tags = new List<string> { "Line1\nLine2" };

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // Newline inside a tag must be escaped, not emitted as a real line break.
        yaml.Should().Contain("\\n");
        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generate_AutoGeneratedTag_IsQuoted()
    {
        // When no tags are specified, ExtractTag generates one from the path.
        // That tag must also be quoted.
        var endpoint = MakeAuthEndpoint(path: "/v1/orders");
        // Tags list is empty — auto-generated

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // Auto-generated tag "V1" must be quoted.
        yaml.Should().Contain("- \"");
        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generate_PlainTag_EmittedQuoted()
    {
        var endpoint = MakeAuthEndpoint();
        endpoint.Tags = new List<string> { "Payments" };

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("- \"Payments\"");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Combined: full spec parses as valid YAML
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_FullSpecWithAllFeatures_ParsesAsValidYaml()
    {
        var authEndpoint = MakeAuthEndpoint("POST", "/v1/orders");
        authEndpoint.Summary = "Create order\nMultiline";
        authEndpoint.Tags = new List<string> { "Orders: Core", "Admin#Only" };
        authEndpoint.AdditionalProduces.Add(new ProducesInfo
        {
            StatusCode = 201,
            ResponseTypeSimpleName = null,
            ContentType = "application/json"
        });

        var anonEndpoint = MakeAnonEndpoint("GET", "/v1/health");

        var yaml = Generate(new List<EndpointInfo> { authEndpoint, anonEndpoint });

        AssertValidYaml(yaml);
        // Structural checks
        yaml.Should().Contain("securitySchemes:");
        yaml.Should().Contain("  responses:");
        yaml.Should().Contain("SwepayProblemDetails:");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MELHORIA-4 — WebhookInfo.ReferencedSchemas fallback (unit-level)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_WebhookWithNoResolvedSchemas_EmitsStubSchema()
    {
        // When WebhookInfo.ReferencedSchemas is empty (e.g., constructed by hand in tests),
        // the generator must still emit a stub so the $ref is not dangling.
        var endpoint = MakeAnonEndpoint();
        var webhooks = new List<WebhookInfo>
        {
            new()
            {
                Name = "orderCreated",
                PayloadTypeName = "OrderCreatedPayload",
                Summary = "Fires when an order is created",
                ReferencedSchemas = new List<SchemaTypeInfo>() // empty — stub path
            }
        };

        var yaml = Generate(new List<EndpointInfo> { endpoint }, webhooks);

        yaml.Should().Contain("webhooks:");
        yaml.Should().Contain("orderCreated:");
        yaml.Should().Contain("$ref: \"#/components/schemas/OrderCreatedPayload\"");
        yaml.Should().Contain("OrderCreatedPayload:");
        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generate_WebhookWithResolvedSchemas_EmitsFullSchema()
    {
        // When WebhookInfo.ReferencedSchemas is populated, the full schema is emitted.
        var endpoint = MakeAnonEndpoint();
        var resolvedSchema = new SchemaTypeInfo
        {
            TypeName = "ShipmentCreatedPayload",
            TypeKind = "WebhookPayload",
            IsResolved = true,
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "ShipmentId", JsonName = "shipmentId", OpenApiType = "string", IsRequired = true },
                new() { Name = "Status",     JsonName = "status",     OpenApiType = "string", IsRequired = true }
            }
        };

        var webhooks = new List<WebhookInfo>
        {
            new()
            {
                Name = "shipmentCreated",
                PayloadTypeName = "ShipmentCreatedPayload",
                ReferencedSchemas = new List<SchemaTypeInfo> { resolvedSchema }
            }
        };

        var yaml = Generate(new List<EndpointInfo> { endpoint }, webhooks);

        yaml.Should().Contain("ShipmentCreatedPayload:");
        yaml.Should().Contain("shipmentId:");
        yaml.Should().Contain("status:");
        AssertValidYaml(yaml);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BUG-FIX (dangling $ref) — [ApiResponse(typeof(T))] schemas must appear in
    // components/schemas so every $ref: "#/components/schemas/X" has a definition.
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Helper: builds and generates YAML via the real source generator for the supplied
    /// source code string. Extracts the YamlContent constant from the generated .g.cs.
    /// </summary>
    private static string RunGeneratorAndGetYaml(string sourceCode)
    {
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");
        generatedSource.Should().NotBeNull("the source generator must produce a GeneratedOpenApiSpec.g.cs file");

        // The YAML is embedded as a C# verbatim string literal.
        // The generated source contains:
        //   public const string YamlContent = @"
        //   <yaml content>
        //   ";
        // Normalise line endings to LF for consistent searching.
        var normalised = generatedSource!.Replace("\r\n", "\n");

        // Find the specific constant declaration line to skip other @" occurrences.
        const string constDecl = "YamlContent = @\"\n";
        var start = normalised.IndexOf(constDecl, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1, "YamlContent constant declaration must exist in generated source");
        start += constDecl.Length;

        var end = normalised.IndexOf("\n\";", start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start, "closing \";  must follow the YamlContent open @\"");

        // Un-escape C# verbatim string double-quote escaping ("" -> ")
        return normalised.Substring(start, end - start).Replace("\"\"", "\"");
    }

    /// <summary>
    /// Parses YAML and returns all $ref values that point to #/components/schemas/.
    /// </summary>
    private static List<string> ExtractSchemaRefs(string yaml)
    {
        var refs = new List<string>();
        foreach (var line in yaml.Split('\n'))
        {
            var trimmed = line.Trim();
            const string prefix = "$ref: \"#/components/schemas/";
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var name = trimmed.Substring(prefix.Length).TrimEnd('"');
            refs.Add(name);
        }
        return refs;
    }

    /// <summary>
    /// Parses YAML and returns all schema names defined under components/schemas:.
    /// </summary>
    private static List<string> ExtractSchemaDefinitions(string yaml)
    {
        var definitions = new List<string>();
        var inComponents = false;
        var inSchemas = false;
        var schemasIndent = -1;
        var componentIndent = -1;

        foreach (var rawLine in yaml.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            var indent = line.Length - line.TrimStart().Length;
            var content = line.TrimStart();

            if (content.StartsWith("components:", StringComparison.Ordinal))
            {
                inComponents = true;
                componentIndent = indent;
                inSchemas = false;
                continue;
            }

            if (inComponents && indent <= componentIndent && !content.StartsWith("components:", StringComparison.Ordinal))
            {
                // Left the components block
                if (indent == componentIndent) inComponents = false;
            }

            if (inComponents && content.StartsWith("schemas:", StringComparison.Ordinal))
            {
                inSchemas = true;
                schemasIndent = indent;
                continue;
            }

            if (inSchemas && indent <= schemasIndent && !content.StartsWith("schemas:", StringComparison.Ordinal))
            {
                inSchemas = false;
            }

            if (inSchemas && indent == schemasIndent + 2 && content.EndsWith(":"))
            {
                definitions.Add(content.TrimEnd(':'));
            }
        }

        return definitions;
    }

    [Fact]
    public void Generator_ApiResponseWithTypedResponse_SchemaDefinedInComponents()
    {
        // Arrange — a handler with [ApiResponse(404, typeof(SomeError))] must cause SomeError
        // to appear in components/schemas, not just be $ref'd without a definition.
        var routeBuilder = GeneratorTestHelper.CreateRouteBuilderSource();

        // Use a single-string source (same pattern as the existing ApiResponse tests)
        // to ensure the generator can resolve all types correctly.
        var sourceCode = routeBuilder + @"

namespace Native.OpenApi;

[System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
public sealed class ApiResponseAttribute : System.Attribute
{
    public int StatusCode { get; }
    public System.Type? ResponseType { get; }
    public string ContentType { get; }
    public ApiResponseAttribute(int statusCode, System.Type? responseType = null, string contentType = ""application/json"")
    { StatusCode = statusCode; ResponseType = responseType; ContentType = contentType; }
}

namespace NativeMediator;

public interface IRequestHandler<TRequest, TResponse>
{
    System.Threading.Tasks.ValueTask<TResponse> Handle(TRequest request, System.Threading.CancellationToken cancellationToken);
}

namespace TestApp;

public class FindItemCommand { }
public class FindItemResponse { public string Id { get; set; } = """"; }
public class SomeError { public string Message { get; set; } = """"; public string? Details { get; set; } }

public class FindItemHandler : NativeMediator.IRequestHandler<FindItemCommand, FindItemResponse>
{
    [Native.OpenApi.ApiResponse(200, typeof(FindItemResponse), ""application/json"")]
    [Native.OpenApi.ApiResponse(404, typeof(SomeError), ""application/json"")]
    public System.Threading.Tasks.ValueTask<FindItemResponse> Handle(FindItemCommand request, System.Threading.CancellationToken cancellationToken)
    {
        return new System.Threading.Tasks.ValueTask<FindItemResponse>(new FindItemResponse());
    }
}

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<FindItemCommand, FindItemResponse>(""/v1/items/{id}"", ctx => new FindItemCommand());
    }
}
";

        // Act
        var yaml = RunGeneratorAndGetYaml(sourceCode);

        // Assert: the $ref must exist (response is wired up correctly)
        yaml.Should().Contain("$ref: \"#/components/schemas/SomeError\"",
            because: "the 404 response references SomeError so a $ref must be emitted");

        // Assert: the schema definition must also exist — no dangling ref
        yaml.Should().Contain("SomeError:",
            because: "SomeError must be defined in components/schemas, not just referenced");

        // Assert: the schema should have the expected properties
        yaml.Should().Contain("message:", because: "SomeError.Message property must be in the schema");

        // Assert: YAML is structurally valid
        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generator_ApiResponseWithProblemDetails_NoSchemaReferenceWithoutDefinition()
    {
        // This is the canonical dangling-ref scenario: ProblemDetails is used in [ApiResponse]
        // but was never registered in components/schemas.
        var routeBuilder = GeneratorTestHelper.CreateRouteBuilderSource();

        var sourceCode = routeBuilder + @"

namespace Native.OpenApi;

[System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
public sealed class ApiResponseAttribute : System.Attribute
{
    public int StatusCode { get; }
    public System.Type? ResponseType { get; }
    public string ContentType { get; }
    public ApiResponseAttribute(int statusCode, System.Type? responseType = null, string contentType = ""application/json"")
    { StatusCode = statusCode; ResponseType = responseType; ContentType = contentType; }
}

namespace NativeMediator;

public interface IRequestHandler<TRequest, TResponse>
{
    System.Threading.Tasks.ValueTask<TResponse> Handle(TRequest request, System.Threading.CancellationToken cancellationToken);
}

namespace TestApp;

public class CreateCmd { public string Name { get; set; } = """"; }
public class CreateResp { public string Id { get; set; } = """"; }

// Mirroring the real ProblemDetails type used in the sample
public class ProblemDetails
{
    public string Type { get; set; } = """";
    public string Title { get; set; } = """";
    public int Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
}

public class CreateHandler : NativeMediator.IRequestHandler<CreateCmd, CreateResp>
{
    [Native.OpenApi.ApiResponse(201, typeof(CreateResp), ""application/json"")]
    [Native.OpenApi.ApiResponse(400, typeof(ProblemDetails), ""application/problem+json"")]
    [Native.OpenApi.ApiResponse(500, typeof(ProblemDetails), ""application/problem+json"")]
    public System.Threading.Tasks.ValueTask<CreateResp> Handle(CreateCmd request, System.Threading.CancellationToken cancellationToken)
    {
        return new System.Threading.Tasks.ValueTask<CreateResp>(new CreateResp());
    }
}

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateCmd, CreateResp>(""/v1/items"", ctx => new CreateCmd());
    }
}
";

        // Act
        var yaml = RunGeneratorAndGetYaml(sourceCode);

        // Assert: ProblemDetails must be DEFINED in components/schemas
        yaml.Should().Contain("ProblemDetails:",
            because: "ProblemDetails referenced in [ApiResponse] must be defined in components/schemas");

        // Assert: no dangling $refs — every referenced schema must have a definition
        var refs = ExtractSchemaRefs(yaml);
        var definitions = ExtractSchemaDefinitions(yaml);

        var dangling = refs
            .Where(r => r != "SwepayProblemDetails") // always auto-injected
            .Distinct()
            .Where(r => !definitions.Contains(r))
            .ToList();

        dangling.Should().BeEmpty(
            because: $"every $ref in components/schemas must resolve to a definition; dangling: [{string.Join(", ", dangling)}]");

        AssertValidYaml(yaml);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // OpenApiEmitStandardComponents=false — merge-producer opt-out
    // ══════════════════════════════════════════════════════════════════════════

    private static string GenerateNoStdComponents(IReadOnlyList<EndpointInfo> endpoints)
        => OpenApiYamlGenerator.Generate(
            endpoints,
            "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null,
            null,
            new List<OpenApiServerInfo>(),
            new List<WebhookInfo>(),
            emitStandardComponents: false);

    [Fact]
    public void Generate_EmitStandardComponentsFalse_DoesNotEmitComponentsResponses()
    {
        var endpoint = MakeAuthEndpoint();
        var yaml = GenerateNoStdComponents(new List<EndpointInfo> { endpoint });

        // The three standard component responses must NOT appear in the spec.
        yaml.Should().NotContain("    BadRequest:",
            because: "emitStandardComponents=false must suppress the standard BadRequest response");
        yaml.Should().NotContain("    Unauthorized:",
            because: "emitStandardComponents=false must suppress the standard Unauthorized response");
        yaml.Should().NotContain("    InternalServerError:",
            because: "emitStandardComponents=false must suppress the standard InternalServerError response");
    }

    [Fact]
    public void Generate_EmitStandardComponentsFalse_DoesNotEmitSecuritySchemes()
    {
        // Even when an authenticated endpoint exists, securitySchemes must be absent.
        var endpoint = MakeAuthEndpoint();
        var yaml = GenerateNoStdComponents(new List<EndpointInfo> { endpoint });

        yaml.Should().NotContain("securitySchemes:",
            because: "emitStandardComponents=false must suppress the securitySchemes section");
        // The scheme definition block uses "    JwtBearer:" (4-space indent under securitySchemes).
        // Note: "- JwtBearer: []" (per-operation security entry) IS preserved and expected.
        yaml.Should().NotContain("    JwtBearer:",
            because: "JwtBearer scheme definition entry must not appear when standard components are suppressed");
    }

    [Fact]
    public void Generate_EmitStandardComponentsFalse_NoProblemJson_DoesNotEmitSwepayProblemDetailsSchema()
    {
        // When emitStandardComponents=false AND no endpoint serves application/problem+json,
        // SwepayProblemDetails is NOT referenced by any operation, so it must be omitted
        // (the common file owns the definition in a merge scenario).
        var endpoint = MakeAuthEndpoint(); // no application/problem+json produces
        var yaml = GenerateNoStdComponents(new List<EndpointInfo> { endpoint });

        yaml.Should().NotContain("SwepayProblemDetails:",
            because: "without any application/problem+json endpoint, SwepayProblemDetails is not referenced and must not be emitted when emitStandardComponents=false");
    }

    [Fact]
    public void Generate_EmitStandardComponentsFalse_WithProblemJson_EmitsSwepayProblemDetailsSchema()
    {
        // BUG FIX: when emitStandardComponents=false but an operation directly references
        // SwepayProblemDetails via application/problem+json, the schema MUST be emitted
        // or the $ref is dangling (broken in the merged spec).
        var endpoint = MakeAuthEndpoint("GET", "/v1/protected");
        endpoint.AdditionalProduces.Add(new ProducesInfo
        {
            StatusCode = 422,
            ResponseTypeSimpleName = null,       // no typed body — uses SwepayProblemDetails
            ContentType = "application/problem+json"
        });

        var yaml = GenerateNoStdComponents(new List<EndpointInfo> { endpoint });

        // The schema definition must be present so the inline $ref does not dangle.
        yaml.Should().Contain("SwepayProblemDetails:",
            because: "emitStandardComponents=false must NOT suppress SwepayProblemDetails when it is directly referenced by an application/problem+json operation");

        // But the standard components/responses and securitySchemes must still be absent.
        yaml.Should().NotContain("    BadRequest:",
            because: "emitStandardComponents=false must still suppress the standard BadRequest response definition");
        yaml.Should().NotContain("securitySchemes:",
            because: "emitStandardComponents=false must still suppress securitySchemes");

        // YAML must parse cleanly.
        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generate_EmitStandardComponentsFalse_PreservesOperationRefsToBadRequest()
    {
        // The $ref inside operations must still be emitted so the merged spec resolves them
        // via the common file.
        var endpoint = MakeAuthEndpoint();
        var yaml = GenerateNoStdComponents(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("$ref: \"#/components/responses/BadRequest\"",
            because: "operation-level $ref to BadRequest must be preserved when standard components are suppressed");
        yaml.Should().Contain("$ref: \"#/components/responses/Unauthorized\"",
            because: "operation-level $ref to Unauthorized must be preserved");
        yaml.Should().Contain("$ref: \"#/components/responses/InternalServerError\"",
            because: "operation-level $ref to InternalServerError must be preserved");
    }

    [Fact]
    public void Generate_EmitStandardComponentsFalse_PreservesSecurityPerOperation()
    {
        // Security declarations on operations must still appear.
        var endpoint = MakeAuthEndpoint();
        var yaml = GenerateNoStdComponents(new List<EndpointInfo> { endpoint });

        yaml.Should().Contain("security:");
        yaml.Should().Contain("- JwtBearer: []",
            because: "per-operation security entry must remain even when securitySchemes definition is suppressed");
    }

    [Fact]
    public void Generate_EmitStandardComponentsFalse_YamlRemainsStructurallyValid()
    {
        // Even without the standard components, the emitted YAML must be syntactically valid.
        var endpoint = MakeAuthEndpoint();
        var yaml = GenerateNoStdComponents(new List<EndpointInfo> { endpoint });

        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generate_EmitStandardComponentsTrue_DefaultBehaviourUnchanged()
    {
        // Passing emitStandardComponents=true (or the default overload) must behave exactly
        // as before — ensures no regression on the standalone path.
        var endpoint = MakeAuthEndpoint();
        var yaml = Generate(new List<EndpointInfo> { endpoint }); // default overload

        yaml.Should().Contain("  responses:", because: "default mode must still emit components/responses");
        yaml.Should().Contain("    BadRequest:");
        yaml.Should().Contain("    Unauthorized:");
        yaml.Should().Contain("    InternalServerError:");
        yaml.Should().Contain("  securitySchemes:");
        yaml.Should().Contain("    JwtBearer:");
        yaml.Should().Contain("SwepayProblemDetails:");
        AssertValidYaml(yaml);
    }

    [Fact]
    public void Generator_EmitStandardComponentsFalse_ViaAnalyzerConfigOptions_YamlLacksComponents()
    {
        // End-to-end: simulate a producer project with the MSBuild property set to false.
        // The source generator reads 'build_property.OpenApiEmitStandardComponents' via
        // AnalyzerConfigOptions and propagates it to the YAML generator.
        var routeBuilder = GeneratorTestHelper.CreateRouteBuilderSource();

        var sourceCode = routeBuilder + @"
namespace TestApp;

public class GetCmd { }
public class GetResp { public string Id { get; set; } = """"; }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetCmd, GetResp>(""/v1/items"", ctx => new GetCmd());
    }
}
";
        var globalOptions = new Dictionary<string, string>
        {
            ["build_property.OpenApiEmitStandardComponents"] = "false"
        };

        var result = GeneratorTestHelper.RunGenerator(sourceCode, "TestAssembly", globalOptions);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");
        generatedSource.Should().NotBeNull();

        var normalised = generatedSource!.Replace("\r\n", "\n");
        const string constDecl = "YamlContent = @\"\n";
        var start = normalised.IndexOf(constDecl, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1);
        start += constDecl.Length;
        var end = normalised.IndexOf("\n\";", start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start);
        var yaml = normalised.Substring(start, end - start).Replace("\"\"", "\"");

        // Standard components must be absent.
        yaml.Should().NotContain("    BadRequest:",
            because: "producer with OpenApiEmitStandardComponents=false must not emit standard responses");
        yaml.Should().NotContain("securitySchemes:",
            because: "producer with OpenApiEmitStandardComponents=false must not emit securitySchemes");
        yaml.Should().NotContain("SwepayProblemDetails:",
            because: "producer with OpenApiEmitStandardComponents=false and no application/problem+json endpoint must not emit SwepayProblemDetails (no $ref to it exists)");

        // But operation-level $refs must still be there.
        yaml.Should().Contain("$ref: \"#/components/responses/BadRequest\"");
        yaml.Should().Contain("$ref: \"#/components/responses/Unauthorized\"");
        yaml.Should().Contain("$ref: \"#/components/responses/InternalServerError\"");
    }

    [Fact]
    public void Generator_EmitStandardComponentsTrue_ExplicitlySet_YamlHasComponents()
    {
        // Explicitly setting true must behave identically to the default.
        var routeBuilder = GeneratorTestHelper.CreateRouteBuilderSource();

        var sourceCode = routeBuilder + @"
namespace TestApp;

public class GetCmd2 { }
public class GetResp2 { public string Id { get; set; } = """"; }

public class MyRouter2
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetCmd2, GetResp2>(""/v1/items2"", ctx => new GetCmd2());
    }
}
";
        var globalOptions = new Dictionary<string, string>
        {
            ["build_property.OpenApiEmitStandardComponents"] = "true"
        };

        var result = GeneratorTestHelper.RunGenerator(sourceCode, "TestAssembly", globalOptions);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");
        generatedSource.Should().NotBeNull();

        var normalised = generatedSource!.Replace("\r\n", "\n");
        const string constDecl = "YamlContent = @\"\n";
        var start = normalised.IndexOf(constDecl, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1);
        start += constDecl.Length;
        var end = normalised.IndexOf("\n\";", start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start);
        var yaml = normalised.Substring(start, end - start).Replace("\"\"", "\"");

        yaml.Should().Contain("    BadRequest:",
            because: "explicitly setting emitStandardComponents=true must preserve standard components");
        yaml.Should().Contain("SwepayProblemDetails:");
    }

    [Fact]
    public void Generate_ApiResponseRefTypes_NoDanglingRefs_UsingReferencedSchemas()
    {
        // Unit-level test using OpenApiYamlGenerator directly with pre-built EndpointInfo.
        // Simulates what DiscoverSchemas returns for a simple error type.
        var errorSchema = new SchemaTypeInfo
        {
            TypeName = "ValidationError",
            TypeKind = "Object",
            IsResolved = true,
            Properties = new List<SchemaPropertyInfo>
            {
                new() { Name = "Field", JsonName = "field", OpenApiType = "string", IsRequired = true },
                new() { Name = "Message", JsonName = "message", OpenApiType = "string", IsRequired = true }
            }
        };

        var endpoint = MakeAuthEndpoint("POST", "/v1/widgets");
        endpoint.AdditionalProduces.Add(new ProducesInfo
        {
            StatusCode = 422,
            ResponseTypeSimpleName = "ValidationError",
            ContentType = "application/json"
        });
        // Simulate the fix: DiscoverSchemas result is added to ReferencedSchemas
        endpoint.ReferencedSchemas.Add(errorSchema);

        var yaml = Generate(new List<EndpointInfo> { endpoint });

        // $ref must be emitted
        yaml.Should().Contain("$ref: \"#/components/schemas/ValidationError\"");

        // Definition must also be present
        yaml.Should().Contain("ValidationError:");
        yaml.Should().Contain("field:");
        yaml.Should().Contain("message:");

        // Full parse must succeed
        AssertValidYaml(yaml);
    }
}
