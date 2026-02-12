using Amazon.Lambda.Core;
using NativeMediator;
using NativeLambdaRouter;
using Functions.Admin.Commands;
using Functions.Admin.Responses;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Functions.Admin;

public sealed class Function : RoutedApiGatewayFunction
{
    public Function(IMediator mediator) : base(mediator) { }

    protected override void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<ListUsersCommand, ListUsersResponse>(
            "/v1/admin/users", ctx => new ListUsersCommand());

        routes.MapPost<CreateUserCommand, CreateUserResponse>(
            "/v1/admin/users", ctx => JsonSerializer.Deserialize(ctx.Body!, AdminJsonContext.Default.CreateUserCommand)!);

        routes.MapDelete<DeleteUserCommand, DeleteUserResponse>(
            "/v1/admin/users/{id}", ctx => new DeleteUserCommand(ctx.PathParameters["id"]));
    }

    protected override async Task<object> ExecuteCommandAsync(RouteMatch match, RouteContext context, IMediator mediator)
    {
        var command = match.Route.CommandFactory(context);
        return command switch
        {
            ListUsersCommand cmd => await mediator.Send(cmd),
            CreateUserCommand cmd => await mediator.Send(cmd),
            DeleteUserCommand cmd => await mediator.Send(cmd),
            _ => throw new InvalidOperationException($"Unknown command: {command.GetType().Name}")
        };
    }

    protected override string SerializeResponse(object response)
    {
        return response switch
        {
            ListUsersResponse r => JsonSerializer.Serialize(r, AdminJsonContext.Default.ListUsersResponse),
            CreateUserResponse r => JsonSerializer.Serialize(r, AdminJsonContext.Default.CreateUserResponse),
            DeleteUserResponse r => JsonSerializer.Serialize(r, AdminJsonContext.Default.DeleteUserResponse),
            NativeLambdaRouter.ErrorResponse r => JsonSerializer.Serialize(r, AdminJsonContext.Default.ErrorResponse),
            RouteNotFoundResponse r => JsonSerializer.Serialize(r, AdminJsonContext.Default.RouteNotFoundResponse),
            _ => throw new NotSupportedException($"No serializer for {response.GetType().Name}")
        };
    }
}
