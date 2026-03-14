using Amazon.Lambda.Core;
using NativeMediator;
using NativeLambdaRouter;
using Functions.OpenId.Commands;
using Functions.OpenId.Responses;
using System.Text.Json;
using System.Net;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Functions.OpenId;

public sealed class Function : RoutedApiGatewayFunction
{
    public Function(IMediator mediator) : base(mediator) { }

    protected override void ConfigureRoutes(IRouteBuilder routes)
    {
        routes.MapGet<OpenIdConfigurationCommand, OpenIdConfigurationResponse>(
            "/.well-known/openid-configuration",
            ctx => new OpenIdConfigurationCommand())
            .WithName("GetOpenIdConfiguration")
            .WithSummary("Retorna metadata OpenID Connect")
            .WithTags("OpenID", "Discovery")
            .AllowAnonymous();

        routes.MapGet<JwksCommand, JwksResponse>(
            "/.well-known/jwks.json",
            ctx => new JwksCommand())
            .WithName("GetJwks")
            .WithSummary("Retorna JWKS para validação de tokens")
            .WithTags("OpenID", "Discovery")
            .AllowAnonymous();

        routes.MapPost<TokenCommand, TokenResponse>(
            "/v1/openid/token",
            ctx => ParseTokenCommand(ctx.Body!))
            .WithName("ExchangeAuthorizationCode")
            .WithSummary("Troca authorization code por tokens")
            .WithDescription("Demonstra .Accepts(form-urlencoded) com schema inline de propriedades")
            .WithTags("OpenID", "OAuth2")
            .Accepts("application/x-www-form-urlencoded")
            .Produces<InvalidGrantError>(400)
            .ProducesProblem(401)
            .AllowAnonymous();
    }

    private static TokenCommand ParseTokenCommand(string body)
    {
        var values = ParseFormBody(body);

        return new TokenCommand
        {
            ClientId = values.TryGetValue("client_id", out var clientId) ? clientId : string.Empty,
            ClientSecret = values.TryGetValue("client_secret", out var clientSecret) ? clientSecret : null,
            GrantType = values.TryGetValue("grant_type", out var grantType) ? grantType : string.Empty,
            Code = values.TryGetValue("code", out var code) ? code : string.Empty,
            RedirectUri = values.TryGetValue("redirect_uri", out var redirectUri) ? redirectUri : string.Empty
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
            OpenIdConfigurationCommand cmd => await mediator.Send(cmd),
            JwksCommand cmd => await mediator.Send(cmd),
            TokenCommand cmd => await mediator.Send(cmd),
            _ => throw new InvalidOperationException($"Unknown command: {command.GetType().Name}")
        };
    }

    protected override string SerializeResponse(object response)
    {
        return response switch
        {
            OpenIdConfigurationResponse r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.OpenIdConfigurationResponse),
            JwksResponse r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.JwksResponse),
            TokenResponse r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.TokenResponse),
            InvalidGrantError r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.InvalidGrantError),
            ErrorResponse r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.ErrorResponse),
            RouteNotFoundResponse r => JsonSerializer.Serialize(r, OpenIdJsonContext.Default.RouteNotFoundResponse),
            _ => throw new NotSupportedException($"No serializer for {response.GetType().Name}")
        };
    }
}
