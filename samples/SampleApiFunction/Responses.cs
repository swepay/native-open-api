namespace SampleApiFunction.Responses;

public sealed record GetItemsResponse(List<ItemDto> Items);

public sealed record GetItemByIdResponse(ItemDto Item);

public sealed record CreateItemResponse(string Id, string Message);

public sealed record UpdateItemResponse(string Id, string Message);

public sealed record DeleteItemResponse(string Id, string Message);

public sealed record HealthCheckResponse(string Status, string Timestamp);

public sealed record ItemDto(string Id, string Name, string Description, decimal Price);

public sealed record ErrorResponse(string Message, string? Details = null);

public sealed record ProblemDetails(
    string Type,
    string Title,
    int Status,
    string? Detail = null,
    string? Instance = null);
