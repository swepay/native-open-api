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
}
