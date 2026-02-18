using System.Text.Json.Nodes;

namespace Native.OpenApi;

/// <summary>
/// Merges multiple OpenAPI document parts into a single consolidated specification.
/// </summary>
public class OpenApiDocumentMerger
{
    /// <summary>
    /// Gets the server URL to use in the merged document.
    /// Override this method to provide environment-specific URLs.
    /// </summary>
    protected virtual string GetServerUrl() => "https://localhost:5001";

    /// <summary>
    /// Gets the API title for the merged document.
    /// </summary>
    protected virtual string GetApiTitle() => "API";

    /// <summary>
    /// Gets the API description for the merged document.
    /// </summary>
    protected virtual string GetApiDescription() => "Consolidated OpenAPI contract.";

    /// <summary>
    /// Merges common parts and partial specifications into a single OpenAPI document.
    /// </summary>
    /// <param name="commonSchemas">Common schema definitions.</param>
    /// <param name="commonResponses">Common response definitions.</param>
    /// <param name="commonSecurity">Common security definitions.</param>
    /// <param name="partials">Partial specifications with paths and additional components.</param>
    /// <returns>The merged JSON root node.</returns>
    public JsonNode Merge(
        OpenApiDocumentPart commonSchemas,
        OpenApiDocumentPart commonResponses,
        OpenApiDocumentPart commonSecurity,
        IReadOnlyList<OpenApiDocumentPart> partials)
    {
        var serverUrl = GetServerUrl();
        var root = new JsonObject
        {
            ["openapi"] = "3.1.0",
            ["info"] = new JsonObject
            {
                ["title"] = GetApiTitle(),
                ["version"] = "1.0.0",
                ["description"] = GetApiDescription()
            },
            ["servers"] = JsonNode.Parse($"[{{\"url\":\"{serverUrl}\",\"description\":\"API Gateway\"}}]"),
            ["paths"] = new JsonObject(),
            ["components"] = new JsonObject()
        };

        MergeComponents(root, commonSchemas.Root);
        MergeComponents(root, commonResponses.Root);
        MergeComponents(root, commonSecurity.Root);

        var paths = (JsonObject)root["paths"]!;

        foreach (var partial in partials)
        {
            if (partial.Root["paths"] is not JsonObject partialPaths)
            {
                continue;
            }

            foreach (var path in partialPaths)
            {
                if (paths.ContainsKey(path.Key))
                {
                    throw new InvalidOperationException($"Duplicate path '{path.Key}' from {partial.Name}.");
                }

                paths[path.Key] = path.Value?.DeepClone();
            }

            MergeComponents(root, partial.Root);
        }

        return root;
    }

    /// <summary>
    /// Merges components from a source document into the target root.
    /// When the same component key already exists, identical definitions are silently
    /// skipped while conflicting definitions raise an error.
    /// </summary>
    protected virtual void MergeComponents(JsonObject targetRoot, JsonNode sourceRoot)
    {
        if (sourceRoot["components"] is not JsonObject sourceComponents)
        {
            return;
        }

        var targetComponents = (JsonObject)targetRoot["components"]!;

        foreach (var component in sourceComponents)
        {
            if (component.Value is not JsonObject sourceSection)
            {
                continue;
            }

            if (!targetComponents.TryGetPropertyValue(component.Key, out var targetSectionNode) || targetSectionNode is not JsonObject targetSection)
            {
                targetSection = new JsonObject();
                targetComponents[component.Key] = targetSection;
            }

            foreach (var entry in sourceSection)
            {
                if (targetSection.ContainsKey(entry.Key))
                {
                    // Duplicate key — check if the definitions are equivalent.
                    // If they are, silently skip; otherwise report the conflict.
                    var existingJson = targetSection[entry.Key]?.ToJsonString() ?? "";
                    var incomingJson = entry.Value?.ToJsonString() ?? "";

                    if (string.Equals(existingJson, incomingJson, StringComparison.Ordinal))
                    {
                        // Identical definition — safe to skip
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"Conflicting component '{component.Key}.{entry.Key}'. "
                        + $"Existing: {existingJson}, Incoming: {incomingJson}.");
                }

                targetSection[entry.Key] = entry.Value?.DeepClone();
            }
        }
    }
}
