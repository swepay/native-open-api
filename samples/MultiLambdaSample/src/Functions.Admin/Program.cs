using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using NativeMediator;
using Functions.Admin;
using Functions.Admin.Commands;
using Functions.Admin.Responses;
using Functions.Admin.Handlers;

var services = new ServiceCollection();
services.AddNativeMediator(options =>
{
    options.MediatorLifetime = ServiceLifetime.Singleton;
    options.AddHandler<ListUsersCommand, ListUsersResponse, ListUsersHandler>();
    options.AddHandler<CreateUserCommand, CreateUserResponse, CreateUserHandler>();
    options.AddHandler<DeleteUserCommand, DeleteUserResponse, DeleteUserHandler>();
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
