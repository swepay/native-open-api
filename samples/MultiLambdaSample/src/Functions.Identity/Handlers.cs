using NativeMediator;
using Functions.Identity.Commands;
using Functions.Identity.Responses;

namespace Functions.Identity.Handlers;

public sealed class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    public ValueTask<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return new ValueTask<LoginResponse>(
            new LoginResponse("access-token-sample", "refresh-token-sample", 3600));
    }
}

public sealed class RegisterHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    public ValueTask<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return new ValueTask<RegisterResponse>(
            new RegisterResponse(Guid.NewGuid().ToString(), $"User '{request.Email}' registered"));
    }
}

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public ValueTask<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return new ValueTask<RefreshTokenResponse>(
            new RefreshTokenResponse("new-access-token", "new-refresh-token", 3600));
    }
}
