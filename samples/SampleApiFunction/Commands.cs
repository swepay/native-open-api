using NativeMediator;
using Native.OpenApi.Attributes;
using SampleApiFunction.Responses;

namespace SampleApiFunction.Commands;

// RFC § F01 — HealthCheck is an infra-only endpoint; hide it from partner docs.
[HideFromDocs("Health check is ops-only, not part of the public contract.")]
public sealed class HealthCheckCommand : IRequest<HealthCheckResponse>
{
}

// RFC § F03 — This v1 endpoint will be retired; point readers to v2.
// RFC § F12 — Link to the Swepay error catalog.
[Deprecated(
    sunset: "2026-12-31",
    alternative: "GET /v2/items",
    reason: "v1 returns flat list with no pagination; v2 adds cursor pagination.")]
[ErrorCatalog(typeof(SwepayErrors))]
public sealed class GetItemsCommand : IRequest<GetItemsResponse>
{
}

[ErrorCatalog(typeof(SwepayErrors))]
public sealed class GetItemByIdCommand : IRequest<GetItemByIdResponse>
{
    public string Id { get; init; } = string.Empty;

    public GetItemByIdCommand(string id)
    {
        Id = id;
    }
}

// RFC § F09 — Named request/response examples rendered by Redoc/Scalar.
// RFC § F12 — Error catalog wiring: the generator slices codes matching the
// declared response statuses (422 → ITEM_NAME_REQUIRED / ITEM_PRICE_INVALID).
[ApiExample(
    name: "happy-path",
    summary: "Simple item with valid price",
    RequestJson = "examples/create-item/happy.json",
    ResponseStatus = 201,
    ResponseJson = "examples/create-item/happy-response.json")]
[ApiExample(
    name: "validation-error",
    summary: "Missing name",
    ResponseStatus = 422,
    ResponseJson = "examples/create-item/validation-error.json")]
[ErrorCatalog(typeof(SwepayErrors))]
public sealed class CreateItemCommand : IRequest<CreateItemResponse>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }

    public CreateItemCommand(string name, string description, decimal price)
    {
        Name = name;
        Description = description;
        Price = price;
    }
}

[ErrorCatalog(typeof(SwepayErrors))]
public sealed class UpdateItemCommand : IRequest<UpdateItemResponse>
{
    public string Id { get; init; } = string.Empty;
    public UpdateItemRequest Request { get; init; } = new("", "", 0);

    public UpdateItemCommand(string id, UpdateItemRequest request)
    {
        Id = id;
        Request = request;
    }
}

public sealed record UpdateItemRequest(string Name, string Description, decimal Price);

[ErrorCatalog(typeof(SwepayErrors))]
public sealed class DeleteItemCommand : IRequest<DeleteItemResponse>
{
    public string Id { get; init; } = string.Empty;

    public DeleteItemCommand(string id)
    {
        Id = id;
    }
}

// Internal diagnostic command, hidden from docs at the route level
// via .ExcludeFromDocs() in Function.cs.
public sealed class InternalDiagnosticsCommand : IRequest<HealthCheckResponse>
{
}
