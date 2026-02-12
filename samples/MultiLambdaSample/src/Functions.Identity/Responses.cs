namespace Functions.Identity.Responses;

public sealed record LoginResponse(string AccessToken, string RefreshToken, long ExpiresIn);
public sealed record RegisterResponse(string UserId, string Message);
public sealed record RefreshTokenResponse(string AccessToken, string RefreshToken, long ExpiresIn);
