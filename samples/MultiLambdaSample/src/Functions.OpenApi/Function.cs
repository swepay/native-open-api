using Amazon.Lambda.Core;
using NativeMediator;
using NativeLambdaRouter;
using Functions.OpenApi.Commands;
using Functions.OpenApi.Responses;
using System.Text.Json;
using Native.OpenApi;
using Native.OpenApi.Rendering;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Functions.OpenApi;

public sealed class Function : RoutedApiGatewayFunction
{
    private readonly OpenApiDocumentProvider _provider;
    private readonly OpenApiRendererOptions _rendererOptions;

    public Function(IMediator mediator)
        : base(mediator)
    {
        var loader = new MultiLambdaDocumentLoader();
        var merger = new MultiLambdaDocumentMerger();
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
        _provider = new OpenApiDocumentProvider(loader, merger, linter);
        _provider.WarmUp();

        // RFC §§ F15 / F16 / F17 — Swepay brand palette, institutional footer
        // and Mermaid injection (consumes fenced ```mermaid blocks inside
        // operation descriptions, plus x-swepay-flows / x-swepay-state-machine
        // in the spec once Wave 2 lands).
        _rendererOptions = new OpenApiRendererOptions
        {
            Branding = new OpenApiBrandingOptions
            {
                PrimaryColor = "#0A2540",
                AccentColor = "#00D4AA",
                LogoUrl = "https://cdn.swepay.com.br/brand/logo-dark.svg",
                FaviconUrl = "https://cdn.swepay.com.br/brand/favicon.ico",
                FontFamily = "Inter, Roboto, sans-serif"
            },
            Footer = new OpenApiFooterOptions
            {
                StatusUrl = "https://status.swepay.com.br",
                SupportUrl = "https://docs.swepay.com.br/support",
                ChangelogUrl = "https://docs.swepay.com.br/changelog",
                SlaUrl = "https://docs.swepay.com.br/sla",
                TermsUrl = "https://docs.swepay.com.br/terms"
            },
            EnableMermaid = true
        };
    }

    protected override void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetOpenApiJsonCommand, GetOpenApiJsonResponse>(
            "/docs/openapi.json",
            ctx => new GetOpenApiJsonCommand())
            .WithName("GetMergedOpenApiJson")
            .WithSummary("Retorna OpenAPI consolidado")
            .WithDescription("Documento OpenAPI 3.1 consolidado das funções Admin, Identity e OpenId")
            .WithTags("Docs", "OpenApi")
            .AllowAnonymous();

        routes.MapGet<GetRedocCommand, GetRedocResponse>(
            "/docs/redoc",
            ctx => new GetRedocCommand())
            .WithName("GetRedocUi")
            .WithSummary("Retorna documentação Redoc")
            .WithTags("Docs", "Redoc")
            .Produces("text/html")
            .AllowAnonymous();

        routes.MapGet<GetScalarCommand, GetScalarResponse>(
            "/docs/scalar",
            ctx => new GetScalarCommand())
            .WithName("GetScalarUi")
            .WithSummary("Retorna documentação Scalar")
            .WithTags("Docs", "Scalar")
            .Produces("text/html")
            .AllowAnonymous();
    }

    protected override async Task<object> ExecuteCommandAsync(RouteMatch match, RouteContext context, IMediator mediator)
    {
        var command = match.Route.CommandFactory(context);
        return command switch
        {
            GetOpenApiJsonCommand => HandleGetOpenApiJson(),
            GetRedocCommand => HandleGetRedoc(),
            GetScalarCommand => HandleGetScalar(),
            _ => throw new InvalidOperationException($"Unknown command: {command.GetType().Name}")
        };
    }

    private GetOpenApiJsonResponse HandleGetOpenApiJson()
    {
        return new GetOpenApiJsonResponse(_provider.Document.Json);
    }

    private GetRedocResponse HandleGetRedoc()
    {
        var renderer = new OpenApiHtmlRenderer();
        return new GetRedocResponse(
            renderer.RenderRedoc("/docs/openapi.json", "Multi-Lambda API", _rendererOptions));
    }

    private GetScalarResponse HandleGetScalar()
    {
        var renderer = new OpenApiHtmlRenderer();
        return new GetScalarResponse(
            renderer.RenderScalar("/docs/openapi.json", "Multi-Lambda API", _rendererOptions));
    }

    protected override string SerializeResponse(object response)
    {
        return response switch
        {
            GetOpenApiJsonResponse r => r.Json,
            GetRedocResponse r => r.Html,
            GetScalarResponse r => r.Html,
            ErrorResponse r => JsonSerializer.Serialize(r, OpenApiJsonSerializerContext.Default.ErrorResponse),
            RouteNotFoundResponse r => JsonSerializer.Serialize(r, OpenApiJsonSerializerContext.Default.RouteNotFoundResponse),
            _ => throw new NotSupportedException($"No serializer for {response.GetType().Name}")
        };
    }
}
