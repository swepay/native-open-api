using Functions.Admin.Commands;
using Functions.Admin.Responses;
using NativeLambdaRouter;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Functions.Admin;

[JsonSerializable(typeof(ListUsersCommand))]
[JsonSerializable(typeof(ListUsersResponse))]
[JsonSerializable(typeof(CreateUserCommand))]
[JsonSerializable(typeof(CreateUserResponse))]
[JsonSerializable(typeof(UpdateUserRoleCommand))]
[JsonSerializable(typeof(UpdateUserRoleResponse))]
[JsonSerializable(typeof(DeleteUserCommand))]
[JsonSerializable(typeof(DeleteUserResponse))]
[JsonSerializable(typeof(UserAlreadyExistsError))]
[JsonSerializable(typeof(UserNotFoundError))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(List<UserDto>))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(RouteNotFoundResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AdminJsonContext : JsonSerializerContext
{
}
