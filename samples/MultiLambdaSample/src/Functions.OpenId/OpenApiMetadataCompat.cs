using NativeLambdaRouter;

namespace NativeLambdaRouter;

public static class OpenApiRouteEndpointBuilderExtensions
{
    public static IRouteEndpointBuilder WithName(this IRouteEndpointBuilder builder, string operationId) => builder;

    public static IRouteEndpointBuilder WithSummary(this IRouteEndpointBuilder builder, string summary) => builder;

    public static IRouteEndpointBuilder WithDescription(this IRouteEndpointBuilder builder, string description) => builder;

    public static IRouteEndpointBuilder WithTags(this IRouteEndpointBuilder builder, params string[] tags) => builder;

    public static IRouteEndpointBuilder Accepts(this IRouteEndpointBuilder builder, string contentType) => builder;

    public static IRouteEndpointBuilder Produces<TResponse>(this IRouteEndpointBuilder builder, int statusCode) => builder;

    public static IRouteEndpointBuilder ProducesProblem(this IRouteEndpointBuilder builder, int statusCode) => builder;
}
