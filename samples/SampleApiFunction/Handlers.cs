using NativeMediator;
using SampleApiFunction.Commands;
using SampleApiFunction.Responses;

namespace SampleApiFunction.Handlers;

public sealed class GetItemsHandler : IRequestHandler<Commands.GetItemsCommand, Responses.GetItemsResponse>
{
    public ValueTask<Responses.GetItemsResponse> Handle(Commands.GetItemsCommand request, CancellationToken cancellationToken)
    {
        var items = new List<ItemDto>
        {
            new("1", "Item 1", "Description 1", 10.99m),
            new("2", "Item 2", "Description 2", 20.99m),
            new("3", "Item 3", "Description 3", 30.99m)
        };

        return new ValueTask<Responses.GetItemsResponse>(new Responses.GetItemsResponse(items));
    }
}

public sealed class GetItemByIdHandler : IRequestHandler<Commands.GetItemByIdCommand, Responses.GetItemByIdResponse>
{
    public ValueTask<Responses.GetItemByIdResponse> Handle(Commands.GetItemByIdCommand request, CancellationToken cancellationToken)
    {
        var item = new ItemDto(request.Id, $"Item {request.Id}", $"Description for {request.Id}", 99.99m);
        return new ValueTask<Responses.GetItemByIdResponse>(new Responses.GetItemByIdResponse(item));
    }
}

public sealed class CreateItemHandler : IRequestHandler<Commands.CreateItemCommand, Responses.CreateItemResponse>
{
    public ValueTask<Responses.CreateItemResponse> Handle(Commands.CreateItemCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString();
        return new ValueTask<Responses.CreateItemResponse>(
            new Responses.CreateItemResponse(id, $"Item '{request.Name}' created successfully"));
    }
}

public sealed class UpdateItemHandler : IRequestHandler<Commands.UpdateItemCommand, Responses.UpdateItemResponse>
{
    public ValueTask<Responses.UpdateItemResponse> Handle(Commands.UpdateItemCommand request, CancellationToken cancellationToken)
    {
        return new ValueTask<Responses.UpdateItemResponse>(
            new Responses.UpdateItemResponse(request.Id, $"Item '{request.Id}' updated successfully"));
    }
}

public sealed class DeleteItemHandler : IRequestHandler<Commands.DeleteItemCommand, Responses.DeleteItemResponse>
{
    public ValueTask<Responses.DeleteItemResponse> Handle(Commands.DeleteItemCommand request, CancellationToken cancellationToken)
    {
        return new ValueTask<Responses.DeleteItemResponse>(
            new Responses.DeleteItemResponse(request.Id, $"Item '{request.Id}' deleted successfully"));
    }
}

public sealed class HealthCheckHandler : IRequestHandler<Commands.HealthCheckCommand, Responses.HealthCheckResponse>
{
    public ValueTask<Responses.HealthCheckResponse> Handle(Commands.HealthCheckCommand request, CancellationToken cancellationToken)
    {
        return new ValueTask<Responses.HealthCheckResponse>(
            new Responses.HealthCheckResponse("healthy", DateTime.UtcNow.ToString("o")));
    }
}

