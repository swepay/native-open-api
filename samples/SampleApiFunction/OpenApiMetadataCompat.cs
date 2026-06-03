// Sample-only shim: provides the .WithTags() fluent extension that the
// NativeLambdaRouter source generator recognises (by method name) at compile time.
// At runtime the call is an identity pass-through — no work performed.
// This mirrors the pattern used in MultiLambdaSample/src/Functions.*/OpenApiMetadataCompat.cs.

using NativeLambdaRouter;

namespace SampleApiFunction;

internal static class OpenApiMetadataCompat
{
    /// <summary>
    /// Assigns explicit OpenAPI tags to this endpoint.
    /// Recognised by the source generator as a fluent-chain marker that populates
    /// the <c>tags:</c> array for the operation, overriding the auto-derived path tag.
    /// </summary>
    public static IRouteEndpointBuilder WithTags(this IRouteEndpointBuilder builder, params string[] tags)
        => builder;
}
