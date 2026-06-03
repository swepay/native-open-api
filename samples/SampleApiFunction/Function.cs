using Amazon.Lambda.Core;
using NativeMediator;
using NativeLambdaRouter;
using Native.OpenApi.Extensions;
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
        // GET /v1/items — deprecated via [Deprecated] on GetItemsCommand.
        routes.MapGet<GetItemsCommand, GetItemsResponse>(
            "/v1/items",
            ctx => new GetItemsCommand())
            .WithTags("Items");

        // GET /v1/items/{id}
        routes.MapGet<GetItemByIdCommand, GetItemByIdResponse>(
            "/v1/items/{id}",
            ctx => new GetItemByIdCommand(ctx.PathParameters["id"]))
            .WithTags("Items");

        // POST /v1/items — the 422 response on the handler has no typed body;
        // the generator points it at SwepayProblemDetails (RFC § F13).
        routes.MapPost<CreateItemCommand, CreateItemResponse>(
            "/v1/items",
            ctx => JsonSerializer.Deserialize(ctx.Body!, AppJsonContext.Default.CreateItemCommand)!)
            .WithTags("Items");

        // PUT /v1/items/{id}
        routes.MapPut<UpdateItemCommand, UpdateItemResponse>(
            "/v1/items/{id}",
            ctx => new UpdateItemCommand(
                ctx.PathParameters["id"],
                JsonSerializer.Deserialize(ctx.Body!, AppJsonContext.Default.UpdateItemRequest)!))
            .WithTags("Items");

        // DELETE /v1/items/{id}
        routes.MapDelete<DeleteItemCommand, DeleteItemResponse>(
            "/v1/items/{id}",
            ctx => new DeleteItemCommand(ctx.PathParameters["id"]))
            .WithTags("Items");

        // RFC § F01 — hidden via [HideFromDocs] on the TCommand.
        routes.MapGet<HealthCheckCommand, Responses.HealthCheckResponse>(
            "/health",
            ctx => new HealthCheckCommand());

        // Schema Richness Wave 3 — exercises [OpenApiProperty] constraints, enum descriptions,
        // x-additionalPropertiesName, x-order.
        routes.MapPost<CreateProductCommand, CreateProductResponse>(
            "/v1/products",
            ctx => JsonSerializer.Deserialize(ctx.Body!, AppJsonContext.Default.CreateProductCommand)!)
            .WithTags("Products");

        // RFC § F01 — hidden via the fluent .ExcludeFromDocs() marker on the
        // route (alternative to annotating the TCommand).
        routes.MapGet<InternalDiagnosticsCommand, Responses.HealthCheckResponse>(
            "/internal/diagnostics",
            ctx => new InternalDiagnosticsCommand())
            .ExcludeFromDocs();

        // Polymorphism Wave 4 — payment method endpoints.
        // These routes exercise [OpenApiDiscriminator] + [OpenApiSubType] on PaymentMethod
        // (abstract base) which causes the source generator to emit:
        //   • PaymentMethod → oneOf [CardPayment, BankTransfer] + discriminator
        //   • CardPayment   → allOf [$ref PaymentMethod, {own props}]
        //   • BankTransfer  → allOf [$ref PaymentMethod, {own props}]
        routes.MapGet<GetPaymentMethodCommand, GetPaymentMethodResponse>(
            "/v1/payment-methods/{id}",
            ctx => new GetPaymentMethodCommand(ctx.PathParameters["id"]))
            .WithTags("Payments");

        routes.MapGet<ListPaymentMethodsCommand, ListPaymentMethodsResponse>(
            "/v1/payment-methods",
            ctx => new ListPaymentMethodsCommand())
            .WithTags("Payments");

        // Structural Wave 5 — query+header params, response headers, links and callbacks.
        routes.MapGet<ListItemsPagedCommand, ListItemsPagedResponse>(
            "/v1/items/paged",
            ctx => new ListItemsPagedCommand())
            .WithTags("Items");

        routes.MapPost<CreateOrderCommand, CreateOrderResponse>(
            "/v1/orders",
            ctx => new CreateOrderCommand())
            .WithTags("Orders");
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
            CreateProductCommand cmd => await mediator.Send(cmd),
            InternalDiagnosticsCommand cmd => await mediator.Send(cmd),
            GetPaymentMethodCommand cmd => await mediator.Send(cmd),
            ListPaymentMethodsCommand cmd => await mediator.Send(cmd),
            ListItemsPagedCommand cmd => await mediator.Send(cmd),
            CreateOrderCommand cmd => await mediator.Send(cmd),
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
            CreateProductResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.CreateProductResponse),
            GetPaymentMethodResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.GetPaymentMethodResponse),
            ListPaymentMethodsResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.ListPaymentMethodsResponse),
            ListItemsPagedResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.ListItemsPagedResponse),
            CreateOrderResponse r => JsonSerializer.Serialize(r, AppJsonContext.Default.CreateOrderResponse),
            NativeLambdaRouter.ErrorResponse r => JsonSerializer.Serialize(r, RouterJsonContext.Default.ErrorResponse),
            NativeLambdaRouter.HealthCheckResponse r => JsonSerializer.Serialize(r, RouterJsonContext.Default.HealthCheckResponse),
            RouteNotFoundResponse r => JsonSerializer.Serialize(r, RouterJsonContext.Default.RouteNotFoundResponse),
            _ => throw new NotSupportedException($"No serializer for {response.GetType().Name}")
        };
    }
}
