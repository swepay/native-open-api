using Native.OpenApi;

namespace Functions.OpenApi;

/// <summary>
/// Custom merger that provides project-specific metadata.
/// </summary>
public sealed class MultiLambdaDocumentMerger : OpenApiDocumentMerger
{
    protected override string GetServerUrl() => "https://api.example.com";
    protected override string GetApiTitle() => "Multi-Lambda Platform API";
    protected override string GetApiDescription() => "Consolidated OpenAPI specification from Admin, Identity, and OpenID Connect Lambda functions.";
}
