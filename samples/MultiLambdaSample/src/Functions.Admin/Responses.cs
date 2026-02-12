namespace Functions.Admin.Responses;

public sealed record ListUsersResponse(List<UserDto> Users);
public sealed record CreateUserResponse(string Id, string Message);
public sealed record DeleteUserResponse(string Id, string Message);
public sealed record UserDto(string Id, string Email, string Role);
