using NativeMediator;
using Functions.Admin.Responses;
using NativeLambdaRouter.OpenApi.Attributes;

namespace Functions.Admin.Commands;

[EndpointName("ListUsersFromAttribute")]
[EndpointSummary("Lista usuários administrativos")]
[EndpointDescription("Retorna usuários administrativos com paginação simplificada")]
[Tags("Admin", "Users", "FromAttribute")]
public sealed class ListUsersCommand : IRequest<ListUsersResponse> { }

public sealed class CreateUserCommand : IRequest<CreateUserResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
}

public sealed class DeleteUserCommand : IRequest<DeleteUserResponse>
{
    public string Id { get; init; }
    public DeleteUserCommand(string id) => Id = id;
}

public sealed class UpdateUserRoleCommand : IRequest<UpdateUserRoleResponse>
{
    public string Id { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
