using Microsoft.CodeAnalysis;

namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Tests for OpenAPI metadata support: fluent chain methods (.WithName, .WithDescription,
/// .WithSummary, .WithTags, .ProducesProblem, .Accepts) and attribute-based metadata
/// ([EndpointName], [EndpointSummary], [EndpointDescription], [Tags], [Accepts]).
/// </summary>
public sealed class OpenApiMetadataTests
{
    // ── WithName ────────────────────────────────────────────────────

    [Fact]
    public void Generator_WithName_UsesCustomOperationId()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand())
            .WithName(""ListAllClients"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId: ListAllClients");
    }

    // ── WithSummary ─────────────────────────────────────────────────

    [Fact]
    public void Generator_WithSummary_UsesCustomSummary()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetUsersCommand { }
public class GetUsersResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetUsersCommand, GetUsersResponse>(""/v1/users"", ctx => new GetUsersCommand())
            .WithSummary(""Retrieve all users"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("summary: \"\"Retrieve all users\"\"");
    }

    // ── WithDescription ─────────────────────────────────────────────

    [Fact]
    public void Generator_WithDescription_IncludesDescription()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetUsersCommand { }
public class GetUsersResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetUsersCommand, GetUsersResponse>(""/v1/users"", ctx => new GetUsersCommand())
            .WithDescription(""This endpoint returns all registered users"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("description: \"\"This endpoint returns all registered users\"\"");
    }

    // ── WithTags ────────────────────────────────────────────────────

    [Fact]
    public void Generator_WithTags_UsesCustomTags()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand())
            .WithTags(""Clients"", ""Admin"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — tags are now emitted quoted (MELHORIA-5 fix); in the verbatim
        // C# string they appear as - ""Clients"" and - ""Admin""
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("- \"\"Clients\"\"");
        generatedSource.Should().Contain("- \"\"Admin\"\"");
    }

    [Fact]
    public void Generator_WithSingleTag_UsesThatTag()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemsCommand { }
public class GetItemsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemsCommand, GetItemsResponse>(""/v1/items"", ctx => new GetItemsCommand())
            .WithTags(""Inventory"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — tags are now emitted quoted (MELHORIA-5 fix); in the verbatim
        // C# string they appear as - ""Inventory""
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("- \"\"Inventory\"\"");
        // The auto-generated tag (V1 or Items) should NOT appear
        generatedSource.Should().NotContain("- \"\"V1\"\"");
    }

    // ── ProducesProblem ─────────────────────────────────────────────

    [Fact]
    public void Generator_ProducesProblem_EmitsProblemJsonResponse()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemCommand { }
public class GetItemResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemCommand, GetItemResponse>(""/v1/items/{id}"", ctx => new GetItemCommand())
            .ProducesProblem(404);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("\"\"404\"\":");
        generatedSource.Should().Contain("application/problem+json");
        generatedSource.Should().Contain("Not Found");
    }

    // ── Attribute-based metadata ────────────────────────────────────

    [Fact]
    public void Generator_EndpointNameAttribute_UsesAttributeOperationId()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[EndpointName(""ListAllClients"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId: ListAllClients");
    }

    [Fact]
    public void Generator_TagsAttribute_UsesAttributeTags()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[Tags(""Clients"", ""Management"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — tags are now emitted quoted (MELHORIA-5 fix)
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("- \"\"Clients\"\"");
        generatedSource.Should().Contain("- \"\"Management\"\"");
    }

    [Fact]
    public void Generator_EndpointDescriptionAttribute_IncludesDescription()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[EndpointDescription(""Returns a paginated list of clients"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("description: \"\"Returns a paginated list of clients\"\"");
    }

    [Fact]
    public void Generator_EndpointSummaryAttribute_UsesSummary()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[EndpointSummary(""Get clients"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("summary: \"\"Get clients\"\"");
    }

    // ── Fluent chain takes precedence over attributes ───────────────

    [Fact]
    public void Generator_FluentChainOverridesAttributes()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[EndpointName(""FromAttribute"")]
[Tags(""AttributeTag"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand())
            .WithName(""FromFluentChain"")
            .WithTags(""FluentTag"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — Fluent chain values should win over attributes
        // Tags are now emitted quoted (MELHORIA-5 fix); in verbatim C# string: - ""FluentTag""
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId: FromFluentChain");
        generatedSource.Should().NotContain("FromAttribute");
        generatedSource.Should().Contain("- \"\"FluentTag\"\"");
        generatedSource.Should().NotContain("- \"\"AttributeTag\"\"");
    }

    // ── Multiple attributes combined ────────────────────────────────

    [Fact]
    public void Generator_MultipleAttributes_AllApplied()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[EndpointName(""ListClients"")]
[EndpointSummary(""List all clients"")]
[EndpointDescription(""Returns paginated clients for the realm"")]
[Tags(""Clients"")]
public class ListClientsCommand { }
public class ListClientsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListClientsCommand, ListClientsResponse>(""/v1/clients"", ctx => new ListClientsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId: ListClients");
        generatedSource.Should().Contain("summary: \"\"List all clients\"\"");
        generatedSource.Should().Contain("description: \"\"Returns paginated clients for the realm\"\"");
        // Tags are now emitted quoted (MELHORIA-5 fix); in verbatim C# string: - ""Clients""
        generatedSource.Should().Contain("- \"\"Clients\"\"");
    }

    // ── Without metadata, auto-generation still works ───────────────

    [Fact]
    public void Generator_WithoutMetadata_AutoGeneratesOperationIdAndSummary()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemsCommand { }
public class GetItemsResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemsCommand, GetItemsResponse>(""/v1/items"", ctx => new GetItemsCommand());
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — operationId and summary should be auto-generated
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId:");
        generatedSource.Should().Contain("summary:");
        // Auto-generated tag from path
        generatedSource.Should().Contain("tags:");
    }

    // ── ProducesProblem overrides default standard error ─────────────

    [Fact]
    public void Generator_ProducesProblem_OverridesDefaultErrorResponse()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemCommand { }
public class GetItemResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemCommand, GetItemResponse>(""/v1/items/{id}"", ctx => new GetItemCommand())
            .ProducesProblem(400);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — ProducesProblem(400) should replace the default $ref for 400
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("\"\"400\"\":");
        generatedSource.Should().Contain("application/problem+json");
        // The default $ref: "#/components/responses/BadRequest" should NOT appear
        generatedSource.Should().NotContain("$ref: \"\"#/components/responses/BadRequest\"\"");
    }

    // ── Accepts (fluent chain) ──────────────────────────────────────

    [Fact]
    public void Generator_AcceptsFormUrlEncoded_UsesFormContentType()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public sealed record RefreshTokenCommand(
    string RealmId,
    string ClientId,
    string RefreshToken,
    string? ClientSecret);

public sealed record TokenResponse(string AccessToken, string TokenType);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<RefreshTokenCommand, TokenResponse>(
            ""/v1/realms/{realm}/token"", ctx => new RefreshTokenCommand("""", """", """", null))
            .Accepts(""application/x-www-form-urlencoded"")
            .AllowAnonymous();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("application/x-www-form-urlencoded");
        // Form-encoded should have inline properties, not $ref to command schema
        generatedSource.Should().Contain("realmId");
        generatedSource.Should().Contain("clientId");
        generatedSource.Should().Contain("refreshToken");
        generatedSource.Should().Contain("clientSecret");
        // All form fields should be type: string
        generatedSource.Should().Contain("type: string");
    }

    [Fact]
    public void Generator_AcceptsFormUrlEncoded_EmitsRequiredFields()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public sealed record TokenCommand(
    string ClientId,
    string GrantType,
    string? Scope);

public sealed record TokenResponse(string AccessToken);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<TokenCommand, TokenResponse>(
            ""/v1/token"", ctx => new TokenCommand("""", """", null))
            .Accepts(""application/x-www-form-urlencoded"")
            .AllowAnonymous();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — required array should only include non-nullable fields
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("application/x-www-form-urlencoded");
        generatedSource.Should().Contain("- clientId");
        generatedSource.Should().Contain("- grantType");
        // Scope is nullable, should NOT be required
        generatedSource.Should().NotContain("- scope");
    }

    [Fact]
    public void Generator_AcceptsFormUrlEncoded_NoRefToCommandSchema()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public sealed record LoginCommand(string Username, string Password);
public sealed record LoginResponse(string Token);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<LoginCommand, LoginResponse>(
            ""/v1/login"", ctx => new LoginCommand("""", """"))
            .Accepts(""application/x-www-form-urlencoded"")
            .AllowAnonymous();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — requestBody should NOT use $ref for the command schema
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("application/x-www-form-urlencoded");
        // Inline properties instead of $ref
        generatedSource.Should().Contain("username");
        generatedSource.Should().Contain("password");
        // The requestBody should not reference LoginCommand via $ref
        generatedSource.Should().NotContain("$ref: \"\"#/components/schemas/LoginCommand\"\"");
    }

    [Fact]
    public void Generator_AcceptsFormUrlEncoded_WithFullFluentChain()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public sealed record RefreshTokenCommand(
    string RealmId,
    string ClientId,
    string RefreshToken,
    string? ClientSecret,
    string? Scope);

public sealed record TokenResponse(string AccessToken, string TokenType);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<RefreshTokenCommand, TokenResponse>(
            ""/v1/realms/{realm}/protocol/openid-connect/refresh"",
            ctx => new RefreshTokenCommand("""", """", """", null, null))
            .WithName(""RefreshToken"")
            .WithSummary(""Refresh token endpoint"")
            .WithDescription(""Exchanges a valid refresh token for a new access token."")
            .WithTags(""OAuth2"")
            .Accepts(""application/x-www-form-urlencoded"")

            .ProducesProblem(400)
            .ProducesProblem(401)
            .AllowAnonymous();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("operationId: RefreshToken");
        generatedSource.Should().Contain("summary: \"\"Refresh token endpoint\"\"");
        generatedSource.Should().Contain("description: \"\"Exchanges a valid refresh token for a new access token.\"\"");
        // Tags are now emitted quoted (MELHORIA-5 fix); in verbatim C# string: - ""OAuth2""
        generatedSource.Should().Contain("- \"\"OAuth2\"\"");
        generatedSource.Should().Contain("application/x-www-form-urlencoded");
        generatedSource.Should().Contain("security: []");
        // Form fields from the command type
        generatedSource.Should().Contain("realmId");
        generatedSource.Should().Contain("clientId");
        generatedSource.Should().Contain("refreshToken");
        // Nullable fields still present as properties but not required
        generatedSource.Should().Contain("clientSecret");
        generatedSource.Should().Contain("scope");
    }

    [Fact]
    public void Generator_WithoutAccepts_DefaultsToApplicationJson()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public sealed record CreateItemCommand(string Name, string Description);
public sealed record CreateItemResponse(string Id);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateItemCommand, CreateItemResponse>(
            ""/v1/items"", ctx => new CreateItemCommand("""", """"));
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — default should be application/json with $ref
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("application/json:");
        generatedSource.Should().Contain("$ref: \"\"#/components/schemas/CreateItemCommand\"\"");
    }

    // ── Accepts<TBody> (declared request body type, swepay/native-guard-backend#97) ──

    [Fact]
    public void Generator_AcceptsGeneric_UsesBodyTypeSchema_InsteadOfCommand()
    {
        // Arrange — EnsureExternalIdpUserCommand mixes path/header/claim-derived fields with
        // the few fields that actually come from the JSON body (AdminEnsureExternalIdpUserRequest).
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public sealed record EnsureExternalIdpUserCommand(
    string RealmId,
    string AssertionToken,
    string CallerClientId,
    string CallerIpAddress,
    string ExternalIdpSource);

public sealed record AdminEnsureExternalIdpUserRequest(string ExternalIdpSource);

public sealed record EnsureExternalIdpUserResponse(string UserId);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<EnsureExternalIdpUserCommand, EnsureExternalIdpUserResponse>(
            ""/admin/v1/realms/{realmId}/external-idp-users/ensure"",
            ctx => new EnsureExternalIdpUserCommand("""", """", """", """", """"))
            .Accepts<AdminEnsureExternalIdpUserRequest>();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — requestBody references the declared body type, not the command
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("$ref: \"\"#/components/schemas/AdminEnsureExternalIdpUserRequest\"\"");
        generatedSource.Should().NotContain("$ref: \"\"#/components/schemas/EnsureExternalIdpUserCommand\"\"");
    }

    [Fact]
    public void Generator_AcceptsGeneric_RegistersBodyTypeInComponentsSchemas_WithItsOwnProperties()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public sealed record EnsureExternalIdpUserCommand(
    string RealmId,
    string AssertionToken,
    string ExternalIdpSource);

public sealed record AdminEnsureExternalIdpUserRequest(string ExternalIdpSource, string? DisplayName);

public sealed record EnsureExternalIdpUserResponse(string UserId);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<EnsureExternalIdpUserCommand, EnsureExternalIdpUserResponse>(
            ""/admin/v1/realms/{realmId}/external-idp-users/ensure"",
            ctx => new EnsureExternalIdpUserCommand("""", """", """"))
            .Accepts<AdminEnsureExternalIdpUserRequest>();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — AdminEnsureExternalIdpUserRequest is registered in components/schemas with
        // its own properties (not an empty/unresolved stub).
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("AdminEnsureExternalIdpUserRequest:");
        generatedSource.Should().Contain("externalIdpSource:");
        generatedSource.Should().Contain("displayName:");
    }

    [Fact]
    public void Generator_AcceptsGeneric_ServerDerivedCommandFieldsNeverAppearInRequestBody()
    {
        // Arrange — realmId/assertionToken/callerClientId/callerIpAddress are populated from
        // path/header/claims; they must not leak into the requestBody as (wrongly) required.
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public sealed record EnsureExternalIdpUserCommand(
    string RealmId,
    string AssertionToken,
    string CallerClientId,
    string CallerIpAddress,
    string ExternalIdpSource);

public sealed record AdminEnsureExternalIdpUserRequest(string ExternalIdpSource);

public sealed record EnsureExternalIdpUserResponse(string UserId);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        // No {realmId} path segment here on purpose: this test isolates requestBody/schema
        // leakage from path-parameter extraction, which is a separate, already-correct concern.
        routes.MapPost<EnsureExternalIdpUserCommand, EnsureExternalIdpUserResponse>(
            ""/admin/v1/external-idp-users/ensure"",
            ctx => new EnsureExternalIdpUserCommand("""", """", """", """", """"))
            .Accepts<AdminEnsureExternalIdpUserRequest>();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — none of the command's server-derived fields leak anywhere in the spec, and
        // the command schema itself is not registered (Accepts<TBody>() replaces it entirely).
        generatedSource.Should().NotBeNull();
        generatedSource.Should().NotContain("realmId");
        generatedSource.Should().NotContain("assertionToken");
        generatedSource.Should().NotContain("callerClientId");
        generatedSource.Should().NotContain("callerIpAddress");
        generatedSource.Should().NotContain("EnsureExternalIdpUserCommand:");
    }

    [Fact]
    public void Generator_WithoutAcceptsGeneric_RequestBodySchemaStaysOnCommand_Unchanged()
    {
        // Arrange — regression guard: a route that never calls .Accepts<TBody>() keeps
        // documenting the command type as the request body, exactly as before this feature.
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public sealed record CreateItemCommand(string Name, string Description);
public sealed record CreateItemResponse(string Id);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<CreateItemCommand, CreateItemResponse>(
            ""/v1/items"", ctx => new CreateItemCommand("""", """"));
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("$ref: \"\"#/components/schemas/CreateItemCommand\"\"");
    }

    [Fact]
    public void Generator_AcceptsGenericWithFormUrlEncodedContentType_UsesBodyTypeFields()
    {
        // Arrange — Accepts<TBody>() and the content-type Accepts(string) overload are
        // independent and chainable together.
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public sealed record TokenCommand(string RealmId, string ClientId, string GrantType);
public sealed record TokenBody(string ClientId, string GrantType);
public sealed record TokenResponse(string AccessToken);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        // No {realmId} path segment on purpose -- see the sibling test above for why.
        routes.MapPost<TokenCommand, TokenResponse>(
            ""/v1/token"", ctx => new TokenCommand("""", """", """"))
            .Accepts<TokenBody>()
            .Accepts(""application/x-www-form-urlencoded"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — form fields come from TokenBody (no realmId), not from TokenCommand
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("application/x-www-form-urlencoded");
        generatedSource.Should().Contain("clientId");
        generatedSource.Should().Contain("grantType");
        generatedSource.Should().NotContain("realmId");
    }

    // ── [JsonPropertyName] honored by schema extraction (swepay/native-open-api#58) ─

    [Fact]
    public void Generator_AcceptsGeneric_RecordWithPropertyTargetedJsonPropertyName_UsesSnakeCaseFormField()
    {
        // Arrange — regression for swepay/native-open-api#58 item 5: TypePropertyExtractor
        // used to always camelCase the C# property name (ToCamelCase(property.Name)), so an
        // OAuth2 token body's grant_type was rendered as grantType. [property: JsonPropertyName]
        // on a record primary-constructor parameter must win.
        // Declared as a plain top-level class next to the route builder stub (not under its
        // own `namespace System.Text.Json.Serialization;`): a second file-scoped namespace
        // declaration appended after the one `CreateRouteBuilderSource()` already opened
        // nests under it instead of opening an independent top-level namespace, which breaks
        // a fully-qualified reference to the attribute from inside `TestApp`. The generator
        // matches `[JsonPropertyName]` by simple type name only (see
        // TypePropertyExtractor.ApplyJsonPropertyNameAttribute), so the real namespace is
        // irrelevant to this test.
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

[System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
public sealed class JsonPropertyNameAttribute : System.Attribute
{
    public string Name { get; }
    public JsonPropertyNameAttribute(string name) => Name = name;
}

public sealed record TokenCommand(string RealmId, string ClientId, string GrantType);

public sealed record TokenBody(
    string ClientId,
    [property: JsonPropertyName(""grant_type"")] string GrantType);

public sealed record TokenResponse(string AccessToken);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<TokenCommand, TokenResponse>(
            ""/v1/token"", ctx => new TokenCommand("""", """", """"))
            .Accepts<TokenBody>()
            .Accepts(""application/x-www-form-urlencoded"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("grant_type");
        generatedSource.Should().NotContain("grantType");
    }

    [Fact]
    public void Generator_RegisteredSchema_ClassWithJsonPropertyName_UsesDeclaredWireName()
    {
        // Arrange — same override, but on a plain class property (not a record parameter),
        // registered via the typed Produces<T>(statusCode) path.
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

[System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
public sealed class JsonPropertyNameAttribute : System.Attribute
{
    public string Name { get; }
    public JsonPropertyNameAttribute(string name) => Name = name;
}

public class LoginCommand { }
public class LoginResponse { }

public class OAuthErrorResponse
{
    public string Error { get; set; } = """";

    [JsonPropertyName(""error_description"")]
    public string ErrorDescription { get; set; } = """";
}

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<LoginCommand, LoginResponse>(""/v1/token"", ctx => new LoginCommand())
            .Produces<OAuthErrorResponse>(400, ""application/json"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("error_description:");
        generatedSource.Should().NotContain("errorDescription");
    }

    // ── Accepts (attribute-based) ───────────────────────────────────

    [Fact]
    public void Generator_AcceptsAttribute_UsesFormContentType()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[Accepts(""application/x-www-form-urlencoded"")]
public sealed record LoginCommand(string Username, string Password);
public sealed record LoginResponse(string Token);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<LoginCommand, LoginResponse>(""/v1/login"", ctx => new LoginCommand("""", """"))
            .AllowAnonymous();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("application/x-www-form-urlencoded");
        generatedSource.Should().Contain("username");
        generatedSource.Should().Contain("password");
    }

    [Fact]
    public void Generator_AcceptsFluentChain_OverridesAttribute()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var attributeSource = GeneratorTestHelper.CreateAttributeSource();
        var sourceCode = routeBuilderSource + attributeSource + @"

using NativeLambdaRouter.OpenApi.Attributes;

namespace TestApp;

[Accepts(""application/json"")]
public sealed record LoginCommand(string Username, string Password);
public sealed record LoginResponse(string Token);

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<LoginCommand, LoginResponse>(""/v1/login"", ctx => new LoginCommand("""", """"))
            .Accepts(""application/x-www-form-urlencoded"")
            .AllowAnonymous();
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — fluent chain should win
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("application/x-www-form-urlencoded");
    }

    // ── Produces<T>(statusCode[, contentType]) — swepay/native-open-api#58 ───

    [Fact]
    public void Generator_ProducesTyped_Status400_EmitsTypedResponse()
    {
        // Arrange — before #58, .Produces<T>(statusCode) was silently ignored for any
        // status other than the implicit 200 (ApplyProduces only read the non-generic
        // .Produces(string contentType) overload).
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class LoginCommand { }
public class LoginResponse { }
public class OAuthErrorResponse { public string Error { get; set; } = """"; public string ErrorDescription { get; set; } = """"; }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<LoginCommand, LoginResponse>(""/v1/token"", ctx => new LoginCommand())
            .Produces<OAuthErrorResponse>(400, ""application/json"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("\"\"400\"\":");
        generatedSource.Should().Contain("application/json");
        generatedSource.Should().Contain("$ref: \"\"#/components/schemas/OAuthErrorResponse\"\"");
        // The typed schema must be registered, not just $ref'd (no dangling $ref).
        generatedSource.Should().Contain("    OAuthErrorResponse:");
        generatedSource.Should().Contain("error:");
        generatedSource.Should().Contain("errorDescription:");
    }

    [Fact]
    public void Generator_ProducesTyped_Status401_WithoutContentType_DefaultsToApplicationJson()
    {
        // Arrange
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetUserCommand { }
public class GetUserResponse { }
public class AuthErrorResponse { public string Error { get; set; } = """"; }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetUserCommand, GetUserResponse>(""/v1/users/me"", ctx => new GetUserCommand())
            .Produces<AuthErrorResponse>(401);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("\"\"401\"\":");
        generatedSource.Should().Contain("$ref: \"\"#/components/schemas/AuthErrorResponse\"\"");
    }

    [Fact]
    public void Generator_ProducesTyped_SameStatusTwoContentTypes_MergeIntoOneResponse()
    {
        // Arrange — 401 declared both as a typed OAuth JSON error AND as an RFC 9470
        // problem+json challenge must be ONE "401": response object with two `content`
        // entries, not two separate (invalid/ambiguous) "401": mapping keys.
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class UserInfoCommand { }
public class UserInfoResponse { }
public class OAuthErrorResponse { public string Error { get; set; } = """"; }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<UserInfoCommand, UserInfoResponse>(""/v1/userinfo"", ctx => new UserInfoCommand())
            .Produces<OAuthErrorResponse>(401, ""application/json"")
            .ProducesProblem(401);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        // Exactly one "401": mapping key.
        CountOccurrences(generatedSource!, "\"\"401\"\":").Should().Be(1);
        generatedSource.Should().Contain("$ref: \"\"#/components/schemas/OAuthErrorResponse\"\"");
        generatedSource.Should().Contain("$ref: \"\"#/components/schemas/SwepayProblemDetails\"\"");
        generatedSource.Should().Contain("application/json");
        generatedSource.Should().Contain("application/problem+json");
    }

    [Fact]
    public void Generator_ProducesProblem_NonProblemContentType_StillEmitsContent()
    {
        // Arrange — regression for the bug reported in swepay/native-open-api#58 item 2:
        // .ProducesProblem(status, contentType) with a contentType other than the literal
        // "application/problem+json" used to leave ResponseTypeSimpleName null, which the
        // YAML generator rendered with NO `content` at all (rejected by
        // OpenApiLinter.ValidateContentSchemas).
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemCommand { }
public class GetItemResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetItemCommand, GetItemResponse>(""/v1/items/{id}"", ctx => new GetItemCommand())
            .ProducesProblem(400, ""application/json"");
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert — still gets a content block pointing at SwepayProblemDetails, now labeled
        // with the caller's content type instead of silently losing its body.
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("\"\"400\"\":");
        generatedSource.Should().Contain("$ref: \"\"#/components/schemas/SwepayProblemDetails\"\"");
    }

    // ── Produces(statusCode) — explicit no-body response (swepay/native-open-api#58) ─

    [Fact]
    public void Generator_ProducesNoBody_EmitsNoContentMarker()
    {
        // Arrange — /userinfo-style 401 documented only by a WWW-Authenticate header.
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class UserInfoCommand { }
public class UserInfoResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<UserInfoCommand, UserInfoResponse>(""/v1/userinfo"", ctx => new UserInfoCommand())
            .Produces(401);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var generatedSource = GeneratorTestHelper.GetGeneratedSource(result, "GeneratedOpenApiSpec.g.cs");

        // Assert
        generatedSource.Should().NotBeNull();
        generatedSource.Should().Contain("\"\"401\"\":");
        generatedSource.Should().Contain("x-no-content: true");
    }

    // ── NLOAPI004 diagnostic — call recognized by name but not statically resolvable ─

    [Fact]
    public void Generator_ProducesTyped_NonLiteralStatusCode_EmitsNLOAPI004Diagnostic()
    {
        // Arrange — the status code is a local variable, not an integer literal, so the
        // generator cannot read it at compile time. Previously this was a silent no-op.
        var routeBuilderSource = GeneratorTestHelper.CreateRouteBuilderSource();
        var sourceCode = routeBuilderSource + @"

namespace TestApp;

public class GetItemCommand { }
public class GetItemResponse { }
public class ErrorResponse { }

public class MyRouter
{
    protected void ConfigureRoutes(IRouteBuilder routes)
    {
        int status = 400;
        routes.MapGet<GetItemCommand, GetItemResponse>(""/v1/items/{id}"", ctx => new GetItemCommand())
            .Produces<ErrorResponse>(status);
    }
}
";

        // Act
        var result = GeneratorTestHelper.RunGenerator(sourceCode);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "NLOAPI004" && d.Severity == DiagnosticSeverity.Warning);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
