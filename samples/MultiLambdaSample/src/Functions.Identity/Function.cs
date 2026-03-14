using Amazon.Lambda.Core;
using NativeMediator;
using NativeLambdaRouter;
using Functions.Identity.Commands;
using Functions.Identity.Responses;
using System.Text.Json;
using System.Net;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Functions.Identity;

public sealed class Function : RoutedApiGatewayFunction
{
    public Function(IMediator mediator) : base(mediator) { }

    protected override void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapPost<LoginCommand, LoginResponse>(
            "/v1/auth/login",
            ctx => JsonSerializer.Deserialize(ctx.Body!, IdentityJsonContext.Default.LoginCommand)!)
            .WithName("LoginWithPasswordFluent")
            .WithSummary("Realiza login com senha")
            .WithDescription("Demonstra precedência de fluent metadata sobre atributos")
            .WithTags("Identity", "Auth", "Fluent")
            .Produces<InvalidCredentialsError>(401)
            .ProducesProblem(429)
            .AllowAnonymous();

        routes.MapPost<RegisterCommand, RegisterResponse>(
            "/v1/auth/register",
            ctx => JsonSerializer.Deserialize(ctx.Body!, IdentityJsonContext.Default.RegisterCommand)!)
            .WithName("RegisterUser")
            .WithDescription("Cria um novo usuário no domínio de identidade")
            .ProducesProblem(400)
            .AllowAnonymous();

        routes.MapPost<RefreshTokenCommand, RefreshTokenResponse>(
            "/v1/auth/refresh",
            ctx => ParseRefreshTokenCommand(ctx.Body!))
            .WithName("RefreshToken")
            .Produces<ValidationProblemError>(422)
            .AllowAnonymous();

        routes.Map<LoginOptionsCommand, LoginOptionsResponse>(
            "OPTIONS",
            "/v1/auth/login",
            ctx => new LoginOptionsCommand())
            .WithName("LoginOptions")
            .WithSummary("Metadados de métodos de autenticação")
            .WithDescription("Demonstra suporte ao Map<TCommand, TResponse> com método HTTP customizado")
            .WithTags("Identity", "Metadata")
            .AllowAnonymous();
    }

    private static RefreshTokenCommand ParseRefreshTokenCommand(string body)
    {
        var values = ParseFormBody(body);

        return new RefreshTokenCommand
        {
            ClientId = values.TryGetValue("client_id", out var clientId) ? clientId : string.Empty,
            RefreshToken = values.TryGetValue("refresh_token", out var refreshToken) ? refreshToken : string.Empty,
            Scope = values.TryGetValue("scope", out var scope) ? scope : null
        };
    }

    private static Dictionary<string, string> ParseFormBody(string body)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(body))
        {
            return values;
        }

        var pairs = body.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            var key = WebUtility.UrlDecode(parts[0]);
            var value = parts.Length > 1 ? WebUtility.UrlDecode(parts[1]) : string.Empty;
            values[key] = value;
        }

        return values;
    }

    protected override async Task<object> ExecuteCommandAsync(RouteMatch match, RouteContext context, IMediator mediator)
    {
        var command = match.Route.CommandFactory(context);
        return command switch
        {
            LoginCommand cmd => await mediator.Send(cmd),
            RegisterCommand cmd => await mediator.Send(cmd),
            RefreshTokenCommand cmd => await mediator.Send(cmd),
            LoginOptionsCommand cmd => await mediator.Send(cmd),
            _ => throw new InvalidOperationException($"Unknown command: {command.GetType().Name}")
        };
    }

    protected override string SerializeResponse(object response)
    {
        return response switch
        {
            LoginResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.LoginResponse),
            RegisterResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.RegisterResponse),
            RefreshTokenResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.RefreshTokenResponse),
            InvalidCredentialsError r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.InvalidCredentialsError),
            ValidationProblemError r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.ValidationProblemError),
            LoginOptionsResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.LoginOptionsResponse),
            ErrorResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.ErrorResponse),
            RouteNotFoundResponse r => JsonSerializer.Serialize(r, IdentityJsonContext.Default.RouteNotFoundResponse),
            _ => throw new NotSupportedException($"No serializer for {response.GetType().Name}")
        };
    }
}
