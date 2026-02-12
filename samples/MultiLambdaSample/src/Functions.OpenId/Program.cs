using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using NativeMediator;
using Functions.OpenId;
using Functions.OpenId.Commands;
using Functions.OpenId.Responses;
using Functions.OpenId.Handlers;

var services = new ServiceCollection();
services.AddNativeMediator(options =>
{
    options.MediatorLifetime = ServiceLifetime.Singleton;
    options.AddHandler<OpenIdConfigurationCommand, OpenIdConfigurationResponse, OpenIdConfigurationHandler>();
    options.AddHandler<JwksCommand, JwksResponse, JwksHandler>();
    options.AddHandler<TokenCommand, TokenResponse, TokenHandler>();
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
