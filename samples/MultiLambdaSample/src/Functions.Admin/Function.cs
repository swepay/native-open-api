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
            "/v1/admin/users", ctx => new ListUsersCommand())
            .WithName("ListAdminUsers")
            .WithSummary("Lista usuários administrativos")
            .WithDescription("Retorna usuários administrativos e demonstra precedência de fluent metadata sobre atributos")
            .WithTags("Admin", "Users")
            .ProducesProblem(400);

        routes.MapPost<CreateUserCommand, CreateUserResponse>(
            "/v1/admin/users", ctx => JsonSerializer.Deserialize(ctx.Body!, AdminJsonContext.Default.CreateUserCommand)!)
            .WithName("CreateAdminUser")
            .WithSummary("Cria usuário administrativo")
            .WithDescription("Cria um usuário com payload JSON e inclui resposta adicional tipada")
            .WithTags("Admin", "Users")
            .Produces<UserAlreadyExistsError>(409)
            .ProducesProblem(422);

        routes.MapPut<UpdateUserRoleCommand, UpdateUserRoleResponse>(
            "/v1/admin/users/{id}",
            ctx =>
            {
                var request = JsonSerializer.Deserialize(ctx.Body!, AdminJsonContext.Default.UpdateUserRoleCommand)!;
                return new UpdateUserRoleCommand
                {
                    Id = ctx.PathParameters["id"],
                    Role = request.Role
                };
            })
            .WithName("ReplaceAdminUserRole")
            .WithSummary("Substitui role do usuário")
            .WithDescription("Demonstra suporte ao MapPut com request tipado")
            .WithTags("Admin", "Users")
            .Produces<UserNotFoundError>(404);

        routes.MapPatch<UpdateUserRoleCommand, UpdateUserRoleResponse>(
            "/v1/admin/users/{id}/role",
            ctx =>
            {
                var request = JsonSerializer.Deserialize(ctx.Body!, AdminJsonContext.Default.UpdateUserRoleCommand)!;
                return new UpdateUserRoleCommand
                {
                    Id = ctx.PathParameters["id"],
                    Role = request.Role
                };
            })
            .WithName("UpdateAdminUserRole")
            .WithSummary("Atualiza role do usuário")
            .WithDescription("Demonstra suporte ao MapPatch com schema de request/response")
            .WithTags("Admin", "Users")
            .Produces<UserNotFoundError>(404)
            .ProducesProblem(409);

        routes.MapDelete<DeleteUserCommand, DeleteUserResponse>(
            "/v1/admin/users/{id}", ctx => new DeleteUserCommand(ctx.PathParameters["id"]))
            .WithName("DeleteAdminUser")
            .WithSummary("Remove usuário")
            .WithDescription("Remove um usuário administrativo pelo id")
            .WithTags("Admin", "Users")
            .Produces<UserNotFoundError>(404);
    }

    protected override async Task<object> ExecuteCommandAsync(RouteMatch match, RouteContext context, IMediator mediator)
    {
        var command = match.Route.CommandFactory(context);
        return command switch
        {
            ListUsersCommand cmd => await mediator.Send(cmd),
            CreateUserCommand cmd => await mediator.Send(cmd),
            UpdateUserRoleCommand cmd => await mediator.Send(cmd),
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
            UpdateUserRoleResponse r => JsonSerializer.Serialize(r, AdminJsonContext.Default.UpdateUserRoleResponse),
            DeleteUserResponse r => JsonSerializer.Serialize(r, AdminJsonContext.Default.DeleteUserResponse),
            UserAlreadyExistsError r => JsonSerializer.Serialize(r, AdminJsonContext.Default.UserAlreadyExistsError),
            UserNotFoundError r => JsonSerializer.Serialize(r, AdminJsonContext.Default.UserNotFoundError),
            NativeLambdaRouter.ErrorResponse r => JsonSerializer.Serialize(r, AdminJsonContext.Default.ErrorResponse),
            RouteNotFoundResponse r => JsonSerializer.Serialize(r, AdminJsonContext.Default.RouteNotFoundResponse),
            _ => throw new NotSupportedException($"No serializer for {response.GetType().Name}")
        };
    }
}
