using NativeMediator;
using SampleApiFunction.Responses;

namespace SampleApiFunction.Commands;

public sealed class GetItemsCommand : IRequest<GetItemsResponse>
{
}

public sealed class GetItemByIdCommand : IRequest<GetItemByIdResponse>
{
    public string Id { get; init; } = string.Empty;

    public GetItemByIdCommand(string id)
    {
        Id = id;
    }
}

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

public sealed class DeleteItemCommand : IRequest<DeleteItemResponse>
{
    public string Id { get; init; } = string.Empty;

    public DeleteItemCommand(string id)
    {
        Id = id;
    }
}

public sealed class HealthCheckCommand : IRequest<HealthCheckResponse>
{
}

