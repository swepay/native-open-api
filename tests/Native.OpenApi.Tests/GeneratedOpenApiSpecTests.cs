namespace Native.OpenApi.Tests;

public sealed class GeneratedOpenApiSpecTests
{
    [Fact]
    public void LoadFromGeneratedSpec_WithValidYaml_ReturnsDocumentPart()
    {
        // Arrange
        var spec = new FakeGeneratedOpenApiSpec(
            yaml: """
                   openapi: "3.1.0"
                   info:
                     title: "Admin API"
                     version: "1.0.0"
                   paths:
                     /v1/users:
                       get:
                         operationId: getUsers
                         summary: "List users"
                         responses:
                           "200":
                             description: "OK"
                   """,
            endpointCount: 1,
            endpoints: [("GET", "/v1/users")]);

        var loader = new TestLoaderWithGeneratedSpec(spec);

        // Act
        var partials = loader.LoadPartials();

        // Assert
        partials.Should().HaveCount(1);
        partials[0].Name.Should().Be("admin");
        partials[0].SourcePath.Should().Be("generated:admin");
        partials[0].Root["openapi"]?.GetValue<string>().Should().Be("3.1.0");
        partials[0].Root["paths"]?["/v1/users"].Should().NotBeNull();
    }

    [Fact]
    public void LoadFromGeneratedSpec_WithNullSpec_ThrowsArgumentNullException()
    {
        // Arrange
        var loader = new TestLoaderWithNullSpec();

        // Act
        var act = () => loader.LoadPartials();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoadFromGeneratedSpec_MultipleSpecs_AllLoadCorrectly()
    {
        // Arrange
        var adminSpec = new FakeGeneratedOpenApiSpec(
            yaml: """
                   openapi: "3.1.0"
                   info:
                     title: "Admin API"
                     version: "1.0.0"
                   paths:
                     /v1/users:
                       get:
                         operationId: getUsers
                         responses:
                           "200":
                             description: "OK"
                   """,
            endpointCount: 1,
            endpoints: [("GET", "/v1/users")]);

        var identitySpec = new FakeGeneratedOpenApiSpec(
            yaml: """
                   openapi: "3.1.0"
                   info:
                     title: "Identity API"
                     version: "1.0.0"
                   paths:
                     /v1/auth/login:
                       post:
                         operationId: postLogin
                         responses:
                           "200":
                             description: "OK"
                   """,
            endpointCount: 1,
            endpoints: [("POST", "/v1/auth/login")]);

        var loader = new TestLoaderWithMultipleSpecs(adminSpec, identitySpec);

        // Act
        var partials = loader.LoadPartials();

        // Assert
        partials.Should().HaveCount(2);
        partials[0].Name.Should().Be("admin");
        partials[0].Root["paths"]?["/v1/users"].Should().NotBeNull();
        partials[1].Name.Should().Be("identity");
        partials[1].Root["paths"]?["/v1/auth/login"].Should().NotBeNull();
    }

    [Fact]
    public void IGeneratedOpenApiSpec_CanBeUsedPolymorphically()
    {
        // Arrange
        IGeneratedOpenApiSpec spec = new FakeGeneratedOpenApiSpec(
            yaml: "openapi: \"3.1.0\"",
            endpointCount: 2,
            endpoints: [("GET", "/items"), ("POST", "/items")]);

        // Act & Assert
        spec.Yaml.Should().Contain("3.1.0");
        spec.EndpointCount.Should().Be(2);
        spec.Endpoints.Should().HaveCount(2);
        spec.Endpoints[0].Method.Should().Be("GET");
        spec.Endpoints[1].Method.Should().Be("POST");
    }

    #region Test helpers

    private sealed class FakeGeneratedOpenApiSpec : IGeneratedOpenApiSpec
    {
        public FakeGeneratedOpenApiSpec(
            string yaml,
            int endpointCount,
            (string Method, string Path)[] endpoints)
        {
            Yaml = yaml;
            EndpointCount = endpointCount;
            Endpoints = endpoints;
        }

        public string Yaml { get; }
        public int EndpointCount { get; }
        public (string Method, string Path)[] Endpoints { get; }
    }

    private sealed class TestLoaderWithGeneratedSpec : OpenApiDocumentLoaderBase
    {
        private readonly IGeneratedOpenApiSpec _spec;

        public TestLoaderWithGeneratedSpec(IGeneratedOpenApiSpec spec)
            : base(new OpenApiResourceReader(typeof(TestLoaderWithGeneratedSpec).Assembly, "Native.OpenApi.Tests."))
        {
            _spec = spec;
        }

        public override IReadOnlyList<OpenApiDocumentPart> LoadCommon() => [];

        public override IReadOnlyList<OpenApiDocumentPart> LoadPartials()
        {
            return [LoadFromGeneratedSpec("admin", _spec)];
        }
    }

    private sealed class TestLoaderWithNullSpec : OpenApiDocumentLoaderBase
    {
        public TestLoaderWithNullSpec()
            : base(new OpenApiResourceReader(typeof(TestLoaderWithNullSpec).Assembly, "Native.OpenApi.Tests."))
        {
        }

        public override IReadOnlyList<OpenApiDocumentPart> LoadCommon() => [];

        public override IReadOnlyList<OpenApiDocumentPart> LoadPartials()
        {
            return [LoadFromGeneratedSpec("test", null!)];
        }
    }

    private sealed class TestLoaderWithMultipleSpecs : OpenApiDocumentLoaderBase
    {
        private readonly IGeneratedOpenApiSpec _adminSpec;
        private readonly IGeneratedOpenApiSpec _identitySpec;

        public TestLoaderWithMultipleSpecs(
            IGeneratedOpenApiSpec adminSpec,
            IGeneratedOpenApiSpec identitySpec)
            : base(new OpenApiResourceReader(typeof(TestLoaderWithMultipleSpecs).Assembly, "Native.OpenApi.Tests."))
        {
            _adminSpec = adminSpec;
            _identitySpec = identitySpec;
        }

        public override IReadOnlyList<OpenApiDocumentPart> LoadCommon() => [];

        public override IReadOnlyList<OpenApiDocumentPart> LoadPartials()
        {
            return
            [
                LoadFromGeneratedSpec("admin", _adminSpec),
                LoadFromGeneratedSpec("identity", _identitySpec)
            ];
        }
    }

    #endregion
}
