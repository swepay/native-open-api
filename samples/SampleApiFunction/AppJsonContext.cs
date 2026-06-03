using SampleApiFunction.Commands;
using SampleApiFunction.Responses;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SampleApiFunction;

[JsonSerializable(typeof(GetItemsCommand))]
[JsonSerializable(typeof(GetItemsResponse))]
[JsonSerializable(typeof(GetItemByIdCommand))]
[JsonSerializable(typeof(GetItemByIdResponse))]
[JsonSerializable(typeof(CreateItemCommand))]
[JsonSerializable(typeof(CreateItemResponse))]
[JsonSerializable(typeof(UpdateItemCommand))]
[JsonSerializable(typeof(UpdateItemRequest))]
[JsonSerializable(typeof(UpdateItemResponse))]
[JsonSerializable(typeof(DeleteItemCommand))]
[JsonSerializable(typeof(DeleteItemResponse))]
[JsonSerializable(typeof(HealthCheckCommand))]
[JsonSerializable(typeof(HealthCheckResponse))]
[JsonSerializable(typeof(CreateProductCommand))]
[JsonSerializable(typeof(CreateProductResponse))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ItemDto))]
[JsonSerializable(typeof(List<ItemDto>))]
// Polymorphism Wave 4 — payment methods
[JsonSerializable(typeof(GetPaymentMethodCommand))]
[JsonSerializable(typeof(GetPaymentMethodResponse))]
[JsonSerializable(typeof(ListPaymentMethodsCommand))]
[JsonSerializable(typeof(ListPaymentMethodsResponse))]
[JsonSerializable(typeof(PaymentMethod))]
[JsonSerializable(typeof(CardPayment))]
[JsonSerializable(typeof(BankTransfer))]
[JsonSerializable(typeof(List<PaymentMethod>))]
// Structural Wave 5 — query/header params, response headers, links, callbacks, webhooks
[JsonSerializable(typeof(ListItemsPagedCommand))]
[JsonSerializable(typeof(ListItemsPagedResponse))]
[JsonSerializable(typeof(CreateOrderCommand))]
[JsonSerializable(typeof(CreateOrderResponse))]
[JsonSerializable(typeof(ItemCreatedEvent))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppJsonContext : JsonSerializerContext
{
}
