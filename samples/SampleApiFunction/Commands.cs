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

// Operation Richness — code sample (curl), stable stability marker.
// Wave 3 — inline response example (value: instead of externalValue:).
[CodeSample(
    lang: "curl",
    source: "curl -X GET https://api.example.com/v1/items/{id} -H \"Authorization: Bearer {token}\"")]
[ScalarStability(Stability.Stable)]
[ErrorCatalog(typeof(SwepayErrors))]
[ApiExample(
    name: "found",
    summary: "Item found",
    ResponseStatus = 200,
    ResponseValue = "{\"id\": \"item_abc123\", \"name\": \"Widget Pro\", \"description\": \"A sample widget\", \"price\": 9.99}")]
[ApiExample(
    name: "not-found",
    summary: "Item not found — 404 inline example",
    ResponseStatus = 422,
    ResponseValue = "{\"type\": \"https://docs.example.com/errors/item-not-found\", \"title\": \"Item not found\", \"status\": 404, \"code\": \"ITEM_NOT_FOUND\"}")]
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
// Navigation Foundation — per-operation external documentation link.
// Operation Richness — code samples (curl + C#), beta badge, experimental stability.
[ApiExample(
    name: "happy-path",
    summary: "Simple item with valid price",
    RequestJson = "examples/create-item/happy.json",
    ResponseStatus = 201,
    ResponseJson = "examples/create-item/happy-response.json")]
[ApiExample(
    name: "inline-minimal",
    summary: "Minimal inline request (Wave 3)",
    RequestValue = "{\"name\": \"Inline Widget\", \"description\": \"Created inline\", \"price\": 4.99}",
    ResponseStatus = 201,
    ResponseValue = "{\"id\": \"item_inline001\", \"name\": \"Inline Widget\"}")]
[ApiExample(
    name: "validation-error",
    summary: "Missing name",
    ResponseStatus = 422,
    ResponseJson = "examples/create-item/validation-error.json")]
[ErrorCatalog(typeof(SwepayErrors))]
[EndpointExternalDocs(
    "https://docs.example.com/marketplace/items/create",
    Description = "Item creation flow and validation rules")]
[CodeSample(
    lang: "curl",
    source: "curl -X POST https://api.example.com/v1/items \\\n  -H \"Authorization: Bearer {token}\" \\\n  -H \"Content-Type: application/json\" \\\n  -d '{\"name\":\"Widget\",\"description\":\"A sample widget\",\"price\":9.99}'")]
[CodeSample(
    lang: "csharp",
    source: "var response = await httpClient.PostAsJsonAsync(\"/v1/items\", new { name = \"Widget\", description = \"A sample widget\", price = 9.99 });",
    Label = "C# (HttpClient)")]
[OperationBadge(name: "beta", Position = "after", Color = "#e5a505")]
[ScalarStability(Stability.Experimental)]
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

// Schema Richness Wave 3 — exercises description/example/default, string/numeric/array
// constraints, x-order, enum x-enum-descriptions / x-enum-varnames, x-additionalPropertiesName.
[CodeSample(lang: "curl", source: "curl -X POST https://api.example.com/v1/products -H \"Authorization: Bearer {token}\" -d '{\"name\":\"Widget\"}'")]
[ScalarStability(Stability.Experimental)]
public sealed class CreateProductCommand : IRequest<CreateProductResponse>
{
    [OpenApiProperty(
        Description = "The product name, must be alphanumeric.",
        Example = "Widget Pro",
        MinLength = 1,
        MaxLength = 100,
        Pattern = @"^[A-Za-z0-9 ]+$",
        Order = 1)]
    public string Name { get; init; } = string.Empty;

    [OpenApiProperty(
        Description = "Price in USD cents.",
        Example = "999",
        Default = "0",
        Minimum = 0,
        Maximum = 9999999,
        Order = 2)]
    public int PriceCents { get; init; }

    [OpenApiProperty(
        Description = "Stock keeping unit identifier.",
        Example = "SKU-001",
        Pattern = @"^SKU-\d+$",
        Order = 3)]
    public string? Sku { get; init; }

    [OpenApiProperty(
        Description = "Product availability status.",
        Order = 4)]
    public ProductStatus Status { get; init; }

    [OpenApiProperty(
        Description = "Searchable tags for this product.",
        MinItems = 1,
        MaxItems = 10,
        UniqueItems = true,
        Order = 5)]
    public List<string> Tags { get; init; } = new();

    [OpenApiProperty(
        Description = "Arbitrary key-value metadata for this product.",
        AdditionalPropertiesName = "MetadataValue",
        Order = 6)]
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>Product availability status.</summary>
public enum ProductStatus
{
    [OpenApiEnumMember(Description = "Product is listed and available for purchase.", DisplayName = "AVAILABLE")]
    Available,

    [OpenApiEnumMember(Description = "Product exists but is not listed publicly.", DisplayName = "HIDDEN")]
    Hidden,

    [OpenApiEnumMember(Description = "Product has been retired and cannot be purchased.", DisplayName = "DISCONTINUED")]
    Discontinued,
}

public sealed record CreateProductResponse(string Id, string Name, ProductStatus Status);

// ── Polymorphism Wave 4 — payment method commands ──────────────────────────────

/// <summary>Get a single payment method by id (returns polymorphic PaymentMethod).</summary>
[ScalarStability(Stability.Stable)]
[CodeSample(
    lang: "curl",
    source: "curl -X GET https://api.example.com/v1/payment-methods/{id} -H \"Authorization: Bearer {token}\"")]
public sealed class GetPaymentMethodCommand : IRequest<GetPaymentMethodResponse>
{
    public string Id { get; init; } = string.Empty;

    public GetPaymentMethodCommand(string id) { Id = id; }
}

/// <summary>List all payment methods for the authenticated customer.</summary>
[ScalarStability(Stability.Stable)]
[CodeSample(
    lang: "curl",
    source: "curl -X GET https://api.example.com/v1/payment-methods -H \"Authorization: Bearer {token}\"")]
public sealed class ListPaymentMethodsCommand : IRequest<ListPaymentMethodsResponse>
{
}

// ── Structural Wave 5 demo commands ─────────────────────────────────────────────

/// <summary>
/// Paginated item listing — exercises query parameters (page, pageSize, filter)
/// and a header parameter (X-Tenant-Id).
/// </summary>
[ScalarStability(Stability.Stable)]
[QueryParameter("page",     typeof(int),    Required = false, Description = "Zero-based page number. Defaults to 0.")]
[QueryParameter("pageSize", typeof(int),    Required = false, Description = "Number of items per page. Max 100. Defaults to 20.")]
[QueryParameter("filter",   typeof(string), Required = false, Description = "Optional free-text filter applied to item names.")]
[HeaderParameter("X-Tenant-Id", typeof(string), Required = true, Description = "Tenant identifier for multi-tenant routing.")]
[CodeSample(
    lang: "curl",
    source: "curl -X GET \"https://api.example.com/v1/items/paged?page=0&pageSize=20\" -H \"Authorization: Bearer {token}\" -H \"X-Tenant-Id: acme\"")]
public sealed class ListItemsPagedCommand : IRequest<ListItemsPagedResponse>
{
}

/// <summary>
/// Create an order — exercises response headers (X-RateLimit-Limit, Location),
/// a response link to GetOrderById, and a callback when the order status changes.
/// </summary>
[ScalarStability(Stability.Stable)]
// Response headers on the 201 Created response
[ResponseHeader(201, "Location",              typeof(string), Required = true,  Description = "URL of the newly created order resource.")]
[ResponseHeader(201, "X-RateLimit-Limit",     typeof(int),    Required = false, Description = "Maximum number of requests allowed per minute.")]
[ResponseHeader(201, "X-RateLimit-Remaining", typeof(int),    Required = false, Description = "Remaining requests in the current rate-limit window.")]
// Response link: from 201 to GetOrderById
[ResponseLink(201, "GetCreatedOrder",
    OperationId = "GetOrderById",
    Parameters = "orderId=$response.body#/orderId",
    Description = "Retrieve the order that was just created.")]
// Minimal callback: webhook-style notification when order status changes
[Callback("onOrderStatusChange",
    Expression = "{$request.body#/callbackUrl}",
    Method = "post",
    Summary = "Order status change notification")]
[CodeSample(
    lang: "curl",
    source: "curl -X POST https://api.example.com/v1/orders -H \"Authorization: Bearer {token}\" -H \"Content-Type: application/json\" -d '{\"items\":[{\"id\":\"item_abc\",\"qty\":2}]}'")]
public sealed class CreateOrderCommand : IRequest<CreateOrderResponse>
{
    public List<string> Items { get; init; } = new();
}
