using Native.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// --- Bootstrap OpenAPI document ---
var loader = new LocalDocumentLoader();
var merger = new LocalDocumentMerger();
var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
var provider = new OpenApiDocumentProvider(loader, merger, linter);

provider.WarmUp();

var renderer = new OpenApiHtmlRenderer();
var document = provider.Document;

Console.WriteLine($"[OpenAPI] Loaded {document.Stats.PathCount} paths from {document.Stats.ResourceCount} resources in {document.Stats.LoadDuration.TotalMilliseconds:F0}ms");

// --- Endpoints ---

app.MapGet("/", () => Results.Redirect("/docs/redoc"));

app.MapGet("/docs/openapi.json", () => Results.Text(document.Json, "application/json"));

app.MapGet("/docs/redoc", () => Results.Text(
    renderer.RenderRedoc("/docs/openapi.json", "Multi-Lambda Platform API"), "text/html"));

app.MapGet("/docs/scalar", () => Results.Text(
    renderer.RenderScalar("/docs/openapi.json", "Multi-Lambda Platform API"), "text/html"));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    version = document.Version,
    paths = document.Stats.PathCount,
    resources = document.Stats.ResourceCount,
    loadedAt = document.LoadedAt
}));

app.Run();

// --- Local implementations ---

/// <summary>
/// Loads the same embedded YAML resources linked from Functions.OpenApi.
/// Uses this assembly's namespace for resource resolution.
/// </summary>
sealed class LocalDocumentLoader : OpenApiDocumentLoaderBase
{
    public LocalDocumentLoader()
        : base(new OpenApiResourceReader(
            typeof(LocalDocumentLoader).Assembly,
            "Functions.OpenApi.LocalRunner."))
    {
    }

    public override IReadOnlyList<OpenApiDocumentPart> LoadCommon() =>
    [
        Load("schemas", "openapi/common/schemas.yaml"),
        Load("responses", "openapi/common/responses.yaml"),
        Load("security", "openapi/common/security.yaml")
    ];

    public override IReadOnlyList<OpenApiDocumentPart> LoadPartials() =>
    [
        Load("admin", "openapi/partials/admin.yaml"),
        Load("identity", "openapi/partials/identity.yaml"),
        Load("openid", "openapi/partials/openid.yaml")
    ];
}

/// <summary>
/// Provides project-specific metadata for the merged document.
/// </summary>
sealed class LocalDocumentMerger : OpenApiDocumentMerger
{
    protected override string GetServerUrl() => "http://localhost:8080";
    protected override string GetApiTitle() => "Multi-Lambda Platform API";
    protected override string GetApiDescription() =>
        "Consolidated OpenAPI specification from Admin, Identity, and OpenID Connect Lambda functions.";
}
