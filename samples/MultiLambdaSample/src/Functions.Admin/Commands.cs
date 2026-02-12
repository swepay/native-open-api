using NativeMediator;
using Functions.Admin.Responses;

namespace Functions.Admin.Commands;

public sealed class ListUsersCommand : IRequest<ListUsersResponse> { }

public sealed class CreateUserCommand : IRequest<CreateUserResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public sealed class DeleteUserCommand : IRequest<DeleteUserResponse>
{
    public string Id { get; init; }
    public DeleteUserCommand(string id) => Id = id;
}
