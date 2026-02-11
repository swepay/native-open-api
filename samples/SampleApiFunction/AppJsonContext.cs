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
[JsonSerializable(typeof(ItemDto))]
[JsonSerializable(typeof(List<ItemDto>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppJsonContext : JsonSerializerContext
{
}
