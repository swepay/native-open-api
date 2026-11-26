namespace Native.OpenApi.Tests;

/// <summary>
/// swepay/native-open-api#60: `x-no-content: true` emitted by the generator reaches the linter
/// as the JSON string "true" once the partial is loaded from YAML.
/// </summary>
public sealed class NoContentMarkerFromYamlTests
{
    private const string USERINFO_YAML = """
        openapi: "3.1.0"
        info:
          title: "Identity API"
          version: "1.0.0"
        paths:
          /v1/userinfo:
            get:
              security:
                - JwtBearer: []
              responses:
                '200':
                  $ref: '#/components/responses/Success'
                '401':
                  description: Unauthorized
                  x-no-content: {0}
        """;

    [Theory]
    [InlineData("true")]
    [InlineData("'true'")]
    public void Lint_ShouldAcceptNoContentMarker_WhenDocumentIsLoadedFromYaml(string marker)
    {
        // Arrange
        var part = new YamlLoader(USERINFO_YAML.Replace("{0}", marker)).LoadPartials()[0];
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);

        // Act
        var errors = linter.Lint(part.Name, part.Root);

        // Assert
        errors.Should().NotContain(e => e.Contains("content required"));
    }

    [Fact]
    public void Lint_ShouldRejectResponseWithoutContent_WhenMarkerIsFalseInYaml()
    {
        // Arrange
        var part = new YamlLoader(USERINFO_YAML.Replace("{0}", "false")).LoadPartials()[0];
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);

        // Act
        var errors = linter.Lint(part.Name, part.Root);

        // Assert
        errors.Should().Contain(e => e.Contains("content required") && e.Contains("401"));
    }

    private sealed class YamlLoader(string yaml)
        : OpenApiDocumentLoaderBase(new OpenApiResourceReader(typeof(YamlLoader).Assembly, "Native.OpenApi.Tests."))
    {
        public override IReadOnlyList<OpenApiDocumentPart> LoadCommon() => [];

        public override IReadOnlyList<OpenApiDocumentPart> LoadPartials() => [LoadFromYaml("identity", yaml)];
    }
}
