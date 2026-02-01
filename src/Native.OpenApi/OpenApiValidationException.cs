namespace Native.OpenApi;

/// <summary>
/// Exception thrown when OpenAPI validation fails.
/// </summary>
public sealed class OpenApiValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance with the list of validation errors.
    /// </summary>
    /// <param name="errors">The list of validation error messages.</param>
    public OpenApiValidationException(IReadOnlyList<string> errors)
        : base("OpenAPI validation failed. " + string.Join(" | ", errors))
    {
        Errors = errors;
    }

    /// <summary>
    /// The list of validation error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }
}
