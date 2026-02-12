using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using NativeMediator;
using Functions.Identity;
using Functions.Identity.Commands;
using Functions.Identity.Responses;
using Functions.Identity.Handlers;

var services = new ServiceCollection();
services.AddNativeMediator(options =>
{
    options.MediatorLifetime = ServiceLifetime.Singleton;
    options.AddHandler<LoginCommand, LoginResponse, LoginHandler>();
    options.AddHandler<RegisterCommand, RegisterResponse, RegisterHandler>();
    options.AddHandler<RefreshTokenCommand, RefreshTokenResponse, RefreshTokenHandler>();
});

using var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();
var function = new Function(mediator);

var bootstrap = LambdaBootstrapBuilder
    .Create<APIGatewayHttpApiV2ProxyRequest, APIGatewayHttpApiV2ProxyResponse>(
        function.FunctionHandler,
        new DefaultLambdaJsonSerializer())
    .Build();

await bootstrap.RunAsync();
