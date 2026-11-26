using System.Text.Json.Nodes;

namespace Native.OpenApi.Tests;

public class OpenApiLinterTests
{
    [Fact]
    public void Lint_ShouldReturnError_WhenOpenApiVersionMissing()
    {
        // Arrange
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("{\"info\":{\"title\":\"Test\"}}")!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().Contain(e => e.Contains("missing 'openapi' version field"));
    }

    [Fact]
    public void Lint_ShouldReturnError_WhenVersionIsNot3_1_0()
    {
        // Arrange
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("{\"openapi\":\"3.0.0\"}")!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().Contain(e => e.Contains("OpenAPI version must be 3.1.0"));
    }

    [Fact]
    public void Lint_ShouldReturnError_WhenPathsEmpty_AndRequirePathsIsTrue()
    {
        // Arrange
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("{\"openapi\":\"3.1.0\",\"paths\":{}}")!;

        // Act
        var errors = linter.Lint("test.yaml", root, requirePaths: true);

        // Assert
        errors.Should().Contain(e => e.Contains("at least one path is required"));
    }

    [Fact]
    public void Lint_ShouldNotReturnError_WhenPathsEmpty_AndRequirePathsIsFalse()
    {
        // Arrange
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("{\"openapi\":\"3.1.0\",\"paths\":{}}")!;

        // Act
        var errors = linter.Lint("test.yaml", root, requirePaths: false);

        // Assert
        errors.Should().NotContain(e => e.Contains("at least one path is required"));
    }

    [Fact]
    public void Lint_ShouldReturnError_WhenPathMissingVersionSegment()
    {
        // Arrange
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/users": {
                    "get": {
                        "security": [{"JwtBearer": []}],
                        "responses": {"200": {"$ref": "#/components/responses/Success"}}
                    }
                }
            }
        }
        """)!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().Contain(e => e.Contains("must include version"));
    }

    [Fact]
    public void Lint_ShouldNotReturnVersionError_ForWellKnownPaths()
    {
        // RFC 8615 well-known URIs (OIDC discovery, JWKS) are standardized at a
        // fixed location and cannot carry a version segment.
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/.well-known/openid-configuration": {
                    "get": {
                        "security": [],
                        "responses": {"200": {"$ref": "#/components/responses/Success"}}
                    }
                }
            }
        }
        """)!;

        var errors = linter.Lint("openid.yaml", root);

        errors.Should().NotContain(e => e.Contains("must include version"));
    }

    [Fact]
    public void Lint_ShouldNotReturnError_WhenSecurityIsEmptyArray_AnonymousEndpoint()
    {
        // Arrange
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/v1/auth/login": {
                    "post": {
                        "security": [],
                        "responses": {"200": {"$ref": "#/components/responses/Success"}}
                    }
                }
            }
        }
        """)!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert - security: [] means anonymous, should not require JwtBearer
        errors.Should().NotContain(e => e.Contains("security required"));
        errors.Should().NotContain(e => e.Contains("JwtBearer or OAuth2 required"));
    }

    [Fact]
    public void Lint_ShouldReturnError_WhenSecurityMissing()
    {
        // Arrange
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/v1/users": {
                    "get": {
                        "responses": {"200": {"$ref": "#/components/responses/Success"}}
                    }
                }
            }
        }
        """)!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().Contain(e => e.Contains("security required"));
    }

    [Fact]
    public void Lint_ShouldNotReturnSecurityError_WhenJwtBearerPresent()
    {
        // Arrange
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/v1/users": {
                    "get": {
                        "security": [{"JwtBearer": []}],
                        "responses": {"200": {"$ref": "#/components/responses/Success"}}
                    }
                }
            }
        }
        """)!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().NotContain(e => e.Contains("security required"));
        errors.Should().NotContain(e => e.Contains("JwtBearer or OAuth2 required"));
    }

    [Fact]
    public void Lint_ShouldReturnError_WhenRequiredResponseMissing()
    {
        // Arrange
        var options = new OpenApiLintOptions(["400", "500"], [], []);
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/v1/users": {
                    "get": {
                        "security": [{"JwtBearer": []}],
                        "responses": {"200": {"$ref": "#/components/responses/Success"}}
                    }
                }
            }
        }
        """)!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().Contain(e => e.Contains("response 400 is required"));
        errors.Should().Contain(e => e.Contains("response 500 is required"));
    }

    [Fact]
    public void Lint_ShouldNotReturnError_WhenAllRequiredResponsesPresent()
    {
        // Arrange
        var options = new OpenApiLintOptions(["400", "500"], [], []);
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/v1/users": {
                    "get": {
                        "security": [{"JwtBearer": []}],
                        "responses": {
                            "200": {"$ref": "#/components/responses/Success"},
                            "400": {"$ref": "#/components/responses/BadRequest"},
                            "500": {"$ref": "#/components/responses/ServerError"}
                        }
                    }
                }
            }
        }
        """)!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().NotContain(e => e.Contains("response 400 is required"));
        errors.Should().NotContain(e => e.Contains("response 500 is required"));
    }

    [Fact]
    public void Lint_ShouldReturnNoErrors_ForValidDocument()
    {
        // Arrange
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/v1/users": {
                    "get": {
                        "security": [{"JwtBearer": []}],
                        "responses": {"200": {"$ref": "#/components/responses/Success"}}
                    }
                }
            }
        }
        """)!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Act
        var act = () => new OpenApiLinter(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // ── x-no-content (swepay/native-open-api#58) ──────────────────────

    [Fact]
    public void Lint_ShouldReturnError_WhenResponseHasNoContentAndNoExplicitNoContentMarker()
    {
        // Arrange — a response with neither `$ref`, `content`, nor the generator's
        // `x-no-content: true` marker is indistinguishable from a dropped
        // .Produces<T>()/.ProducesProblem() call, so it must still be rejected.
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/v1/userinfo": {
                    "get": {
                        "security": [{"JwtBearer": []}],
                        "responses": {
                            "200": {"$ref": "#/components/responses/Success"},
                            "401": {"description": "Unauthorized"}
                        }
                    }
                }
            }
        }
        """)!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().Contain(e => e.Contains("content required") && e.Contains("401"));
    }

    [Fact]
    public void Lint_ShouldNotReturnError_WhenResponseDeclaresExplicitNoContentMarker()
    {
        // Arrange — .Produces(401) (no type argument) renders exactly this shape: a
        // response with no `content` but an explicit `x-no-content: true` marker, e.g. a
        // 401 documented only by a WWW-Authenticate response header.
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/v1/userinfo": {
                    "get": {
                        "security": [{"JwtBearer": []}],
                        "responses": {
                            "200": {"$ref": "#/components/responses/Success"},
                            "401": {"description": "Unauthorized", "x-no-content": true}
                        }
                    }
                }
            }
        }
        """)!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().NotContain(e => e.Contains("content required"));
    }

    [Fact]
    public void Lint_ShouldReturnError_WhenRequestBodyHasNoContentAndNoExplicitNoContentMarker()
    {
        // Arrange — the same ValidateContentSchemas rule also covers requestBody; the
        // x-no-content marker is never emitted there by the generator, so an empty
        // requestBody must still be rejected.
        var options = OpenApiLintOptions.Empty;
        var linter = new OpenApiLinter(options);
        var root = JsonNode.Parse("""
        {
            "openapi": "3.1.0",
            "paths": {
                "/v1/users": {
                    "post": {
                        "security": [{"JwtBearer": []}],
                        "requestBody": {"description": "no schema"},
                        "responses": {"200": {"$ref": "#/components/responses/Success"}}
                    }
                }
            }
        }
        """)!;

        // Act
        var errors = linter.Lint("test.yaml", root);

        // Assert
        errors.Should().Contain(e => e.Contains("content required"));
    }
}
