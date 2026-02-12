using Amazon.Lambda.Core;
using NativeMediator;
using NativeLambdaRouter;
using Functions.OpenApi.Commands;
using Functions.OpenApi.Responses;
using System.Text.Json;
using Native.OpenApi;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Functions.OpenApi;

public sealed class Function : RoutedApiGatewayFunction
{
    private readonly OpenApiDocumentProvider _provider;

    public Function(IMediator mediator)
        : base(mediator)
    {
        var loader = new MultiLambdaDocumentLoader();
        var merger = new MultiLambdaDocumentMerger();
        var linter = new OpenApiLinter(OpenApiLintOptions.Empty);
        _provider = new OpenApiDocumentProvider(loader, merger, linter);
        _provider.WarmUp();
    }

    protected override void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<GetOpenApiJsonCommand, GetOpenApiJsonResponse>(
            "/docs/openapi.json",
            ctx => new GetOpenApiJsonCommand())
            .AllowAnonymous();

        routes.MapGet<GetRedocCommand, GetRedocResponse>(
            "/docs/redoc",
            ctx => new GetRedocCommand())
            .Produces("text/html")
            .AllowAnonymous();

        routes.MapGet<GetScalarCommand, GetScalarResponse>(
            "/docs/scalar",
            ctx => new GetScalarCommand())
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
        return new GetRedocResponse(renderer.RenderRedoc("/docs/openapi.json", "Multi-Lambda API"));
    }

    private GetScalarResponse HandleGetScalar()
    {
        var renderer = new OpenApiHtmlRenderer();
        return new GetScalarResponse(renderer.RenderScalar("/docs/openapi.json", "Multi-Lambda API"));
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
