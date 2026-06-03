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
}
