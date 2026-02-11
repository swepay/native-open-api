namespace SampleApiFunction.Responses;

public sealed record GetItemsResponse(List<ItemDto> Items);

public sealed record GetItemByIdResponse(ItemDto Item);

public sealed record CreateItemResponse(string Id, string Message);

public sealed record UpdateItemResponse(string Id, string Message);

public sealed record DeleteItemResponse(string Id, string Message);

public sealed record HealthCheckResponse(string Status, string Timestamp);

public sealed record ItemDto(string Id, string Name, string Description, decimal Price);
