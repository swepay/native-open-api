using Amazon.Lambda.Core;
using NativeMediator;
using NativeLambdaRouter;
using Functions.OpenId.Commands;
using Functions.OpenId.Responses;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Functions.OpenId;

public sealed class Function : RoutedApiGatewayFunction
{
    public Function(IMediator mediator) : base(mediator) { }

    protected override void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<OpenIdConfigurationCommand, OpenIdConfigurationResponse>(
            "/.well-known/openid-configuration",
            ctx => new OpenIdConfigurationCommand())
            .AllowAnonymous();

        routes.MapGet<JwksCommand, JwksResponse>(
            "/.well-known/jwks.json",
            ctx => new JwksCommand())
            .AllowAnonymous();

        routes.MapPost<TokenCommand, TokenResponse>(
            "/v1/openid/token",
            ctx => JsonSerializer.Deserialize(ctx.Body!, OpenIdJsonContext.Default.TokenCommand)!)
            .AllowAnonymous();
    }

    protected override async Task<object> ExecuteCommandAsync(RouteMatch match, RouteContext context, IMediator mediator)
    {
        var command = match.Route.CommandFactory(context);
        return command switch
        {
            OpenIdConfigurationCommand cmd => await mediator.Send(cmd),
            JwksCommand cmd => await mediator.Send(cmd),
            TokenCommand cmd => await mediator.Send(cmd),
            _ => throw new InvalidOperationException($"Unknown command: {command.GetType().Name}")
        };
    }

    protected override string SerializeResponse(object response)
    {
        return response switch
        {
            OpenIdConfigurationResponse r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.OpenIdConfigurationResponse),
            JwksResponse r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.JwksResponse),
            TokenResponse r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.TokenResponse),
            ErrorResponse r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.ErrorResponse),
            RouteNotFoundResponse r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.RouteNotFoundResponse),
            _ => throw new NotSupportedException($"No serializer for {response.GetType().Name}")
        };
    }
}
