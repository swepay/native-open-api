using NativeMediator;
using Functions.Admin.Responses;
using NativeLambdaRouter.OpenApi.Attributes;
using Native.OpenApi.Attributes;

namespace Functions.Admin.Commands;

[EndpointName("ListUsersFromAttribute")]
[EndpointSummary("Lista usuários administrativos")]
[EndpointDescription("Retorna usuários administrativos com paginação simplificada")]
[Tags("Admin", "Users", "FromAttribute")]
[ErrorCatalog(typeof(SwepayErrors))]
public sealed class ListUsersCommand : IRequest<ListUsersResponse> { }

// RFC § F09 — named request/response examples (happy path + conflict).
// RFC § F12 — error codes sliced from SwepayErrors against the declared
// 409 and 422 responses in Function.cs.
[ApiExample(
    name: "happy-path",
    summary: "Novo usuário com role existente",
    RequestJson = "examples/admin/create-user/happy.json",
    ResponseStatus = 201,
    ResponseJson = "examples/admin/create-user/happy-response.json")]
[ApiExample(
    name: "duplicate-email",
    summary: "E-mail já registrado",
    ResponseStatus = 409,
    ResponseJson = "examples/admin/create-user/duplicate-email.json")]
[ErrorCatalog(typeof(SwepayErrors))]
public sealed class CreateUserCommand : IRequest<CreateUserResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
}

[ErrorCatalog(typeof(SwepayErrors))]
public sealed class DeleteUserCommand : IRequest<DeleteUserResponse>
{
    public string Id { get; init; }
    public DeleteUserCommand(string id) => Id = id;
}

// RFC § F03 — the legacy PUT endpoint is deprecated; consumers should adopt
// PatchUserRoleCommand at PATCH /v1/admin/users/{id}/role. The Redoc banner
// reads the sunset date, alternative and reason from this attribute.
[Deprecated(
    sunset: "2026-09-30",
    alternative: "PATCH /v1/admin/users/{id}/role",
    reason: "PUT overwrote the full user; PATCH is scoped to the role field only.")]
[ErrorCatalog(typeof(SwepayErrors))]
public sealed class UpdateUserRoleCommand : IRequest<UpdateUserRoleResponse>
{
    public string Id { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

// Current, non-deprecated PATCH variant. Same response, distinct command so
// the Wave 1 [Deprecated] marker does not leak onto the PATCH operation.
[ErrorCatalog(typeof(SwepayErrors))]
public sealed class PatchUserRoleCommand : IRequest<UpdateUserRoleResponse>
{
    public string Id { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
