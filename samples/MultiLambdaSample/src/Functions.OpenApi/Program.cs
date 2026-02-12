using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using NativeMediator;
using Functions.OpenApi;

// Functions.OpenApi does not use mediator.Send() — the Function handles
// OpenAPI document generation directly.  We still need a valid IMediator
// instance because the base class RoutedApiGatewayFunction requires it.
var services = new ServiceCollection();
services.AddNativeMediator(options =>
{
    options.MediatorLifetime = ServiceLifetime.Singleton;
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
