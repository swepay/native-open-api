namespace Native.OpenApi.Extensions;

/// <summary>
/// Fluent-chain extensions recognised by the Native.OpenApi Source Generator.
/// </summary>
/// <remarks>
/// <para>Every method in this class is a <b>compile-time marker</b>: the generator
/// walks the syntax tree of the fluent chain (<c>routes.MapPost&lt;...&gt;(...).ExcludeFromDocs()</c>)
/// and reacts to the method name. At runtime the calls are identity pass-throughs
/// — they return the builder unchanged and perform no work.</para>
/// <para>The generic <c>TBuilder</c> signature keeps the extension usable with any
/// route-builder implementation (<c>IRouteBuilder</c>, <c>RouteBuilder</c>,
/// <c>IRouteEndpointBuilder</c>, or a wrapper) without forcing a project reference
/// from <c>Native.OpenApi</c> onto the router packages.</para>
/// </remarks>
public static class OpenApiRouteExtensions
{
    /// <summary>
    /// Marks the endpoint for exclusion from the generated OpenAPI spec.
    /// Equivalent to annotating the <c>TCommand</c> type with
    /// <see cref="Attributes.HideFromDocsAttribute"/>, but scoped to a single
    /// mapping (handy when the same command is exposed at multiple paths and
    /// only one of them should be hidden).
    /// </summary>
    /// <remarks>
    /// See RFC-DOCUMENTACAO-UX § F01.
    /// </remarks>
    /// <typeparam name="TBuilder">Route-builder type, preserved through the chain.</typeparam>
    /// <param name="builder">The route builder. Returned untouched.</param>
    /// <returns><paramref name="builder"/>, unmodified.</returns>
    public static TBuilder ExcludeFromDocs<TBuilder>(this TBuilder builder) where TBuilder : notnull
    {
        return builder;
    }
}
