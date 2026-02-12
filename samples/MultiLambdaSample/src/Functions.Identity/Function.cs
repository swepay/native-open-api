using Amazon.Lambda.Core;
using NativeMediator;
using NativeLambdaRouter;
using Functions.Identity.Commands;
using Functions.Identity.Responses;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Functions.Identity;

public sealed class Function : RoutedApiGatewayFunction
{
    public Function(IMediator mediator) : base(mediator) { }

    protected override void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<LoginCommand, LoginResponse>(
            "/v1/auth/login",
            ctx => JsonSerializer.Deserialize(ctx.Body!, IdentityJsonContext.Default.LoginCommand)!)
            .AllowAnonymous();

        routes.MapPost<RegisterCommand, RegisterResponse>(
            "/v1/auth/register",
            ctx => JsonSerializer.Deserialize(ctx.Body!, IdentityJsonContext.Default.RegisterCommand)!)
            .AllowAnonymous();

        routes.MapPost<RefreshTokenCommand, RefreshTokenResponse>(
            "/v1/auth/refresh",
            ctx => JsonSerializer.Deserialize(ctx.Body!, IdentityJsonContext.Default.RefreshTokenCommand)!)
            .AllowAnonymous();
    }

    protected override async Task<object> ExecuteCommandAsync(RouteMatch match, RouteContext context, IMediator mediator)
    {
        var command = match.Route.CommandFactory(context);
        return command switch
        {
            LoginCommand cmd => await mediator.Send(cmd),
            RegisterCommand cmd => await mediator.Send(cmd),
            RefreshTokenCommand cmd => await mediator.Send(cmd),
            _ => throw new InvalidOperationException($"Unknown command: {command.GetType().Name}")
        };
    }

    protected override string SerializeResponse(object response)
    {
        return response switch
        {
            LoginResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.LoginResponse),
            RegisterResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.RegisterResponse),
            RefreshTokenResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.RefreshTokenResponse),
            ErrorResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.ErrorResponse),
            RouteNotFoundResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.RouteNotFoundResponse),
            _ => throw new NotSupportedException($"No serializer for {response.GetType().Name}")
        };
    }
}
