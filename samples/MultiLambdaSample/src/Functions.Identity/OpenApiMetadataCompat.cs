using NativeLambdaRouter;

namespace NativeLambdaRouter
{
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
}

namespace NativeLambdaRouter.OpenApi.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class EndpointNameAttribute : Attribute
    {
        public EndpointNameAttribute(string name) => Name = name;

        public string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class EndpointSummaryAttribute : Attribute
    {
        public EndpointSummaryAttribute(string summary) => Summary = summary;

        public string Summary { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class EndpointDescriptionAttribute : Attribute
    {
        public EndpointDescriptionAttribute(string description) => Description = description;

        public string Description { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class AcceptsAttribute : Attribute
    {
        public AcceptsAttribute(string contentType) => ContentType = contentType;

        public string ContentType { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class TagsAttribute : Attribute
    {
        public TagsAttribute(params string[] tags) => Tags = tags;

        public string[] Tags { get; }
    }
}
