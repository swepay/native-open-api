using Amazon.Lambda.Core;
using NativeMediator;
using NativeLambdaRouter;
using SampleApiFunction.Commands;
using SampleApiFunction.Responses;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SampleApiFunction;

public sealed class Function : RoutedApiGatewayFunction
{
    public Function(IMediator mediator)
        : base(mediator)
    {
    }

    protected override void ConfigureRoutes(IRouteBuilder routes)
    {
        // GET /v1/items
        routes.MapGet<GetItemsCommand, GetItemsResponse>(
            "/v1/items",
            ctx => new GetItemsCommand());

        // GET /v1/items/{id}
        routes.MapGet<GetItemByIdCommand, GetItemByIdResponse>(
            "/v1/items/{id}",
            ctx => new GetItemByIdCommand(ctx.PathParameters["id"]));

        // POST /v1/items
        routes.MapPost<CreateItemCommand, CreateItemResponse>(
            "/v1/items",
            ctx => JsonSerializer.Deserialize(ctx.Body!, AppJsonContext.Default.CreateItemCommand)!);

        // PUT /v1/items/{id}
        routes.MapPut<UpdateItemCommand, UpdateItemResponse>(
            "/v1/items/{id}",
            ctx => new UpdateItemCommand(
                ctx.PathParameters["id"],
                JsonSerializer.Deserialize(ctx.Body!, AppJsonContext.Default.UpdateItemRequest)!));

        // DELETE /v1/items/{id}
        routes.MapDelete<DeleteItemCommand, DeleteItemResponse>(
            "/v1/items/{id}",
            ctx => new DeleteItemCommand(ctx.PathParameters["id"]));

        // Health check endpoint
        routes.MapGet<HealthCheckCommand, Responses.HealthCheckResponse>(
            "/health",
            ctx => new HealthCheckCommand());
    }

    protected override async Task<object> ExecuteCommandAsync(RouteMatch match, RouteContext context, IMediator mediator)
    {
        var command = match.Route.CommandFactory(context);

        return command switch
        {
            GetItemsCommand cmd => await mediator.Send(cmd),
            GetItemByIdCommand cmd => await mediator.Send(cmd),
            CreateItemCommand cmd => await mediator.Send(cmd),
            UpdateItemCommand cmd => await mediator.Send(cmd),
            DeleteItemCommand cmd => await mediator.Send(cmd),
            HealthCheckCommand cmd => await mediator.Send(cmd),
            _ => throw new InvalidOperationException($"Unknown command: {command.GetType().Name}")
        };
    }

    protected override string SerializeResponse(object response)
    {
        return response switch
        {
            GetItemsResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.GetItemsResponse),
            GetItemByIdResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.GetItemByIdResponse),
            CreateItemResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.CreateItemResponse),
            UpdateItemResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.UpdateItemResponse),
            DeleteItemResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.DeleteItemResponse),
            Responses.HealthCheckResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.HealthCheckResponse),
            NativeLambdaRouter.ErrorResponse r => JsonSerializer.Serialize(r, RouterJsonContext.Default.ErrorResponse),
            NativeLambdaRouter.HealthCheckResponse r => JsonSerializer.Serialize(r, RouterJsonContext.Default.HealthCheckResponse),
            RouteNotFoundResponse r => JsonSerializer.Serialize(r, RouterJsonContext.Default.RouteNotFoundResponse),
            _ => throw new NotSupportedException($"No serializer for {response.GetType().Name}")
        };
    }
}
