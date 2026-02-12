using NativeMediator;
using Functions.Admin.Commands;
using Functions.Admin.Responses;

namespace Functions.Admin.Handlers;

public sealed class ListUsersHandler : IRequestHandler<ListUsersCommand, ListUsersResponse>
{
    public ValueTask<ListUsersResponse> Handle(ListUsersCommand request, CancellationToken cancellationToken)
    {
        var users = new List<UserDto>
        {
            new("1", "admin@example.com", "Admin"),
            new("2", "user@example.com", "User")
        };
        return new ValueTask<ListUsersResponse>(new ListUsersResponse(users));
    }
}

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    public ValueTask<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return new ValueTask<CreateUserResponse>(new CreateUserResponse(Guid.NewGuid().ToString(), $"User '{request.Email}' created"));
    }
}

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand, DeleteUserResponse>
{
    public ValueTask<DeleteUserResponse> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        return new ValueTask<DeleteUserResponse>(new DeleteUserResponse(request.Id, $"User '{request.Id}' deleted"));
    }
}
