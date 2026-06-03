namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares an explicit query-string parameter for the OpenAPI operation of the decorated
/// command type. Emitted as an entry in the <c>parameters</c> array with <c>in: query</c>.
/// </summary>
/// <remarks>
/// <para>Apply this attribute on the <c>TCommand</c> class. It is <b>multi-use</b>:
/// declare one per query parameter.</para>
/// <para>This is a <b>compile-time-only</b> marker consumed by the Roslyn Source Generator.
/// Zero reflection at runtime.</para>
/// <para>Emitted YAML example:</para>
/// <code>
/// parameters:
///   - name: page
///     in: query
///     required: false
///     schema:
///       type: integer
///       format: int32
///     description: "Zero-based page number for pagination."
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [QueryParameter("page", typeof(int), Required = false, Description = "Page number (0-based).")]
/// [QueryParameter("pageSize", typeof(int), Required = false, Description = "Items per page.")]
/// public sealed record ListItemsCommand();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class QueryParameterAttribute : Attribute
{
    /// <summary>Parameter name as it appears in the query string.</summary>
    public string Name { get; }

    /// <summary>
    /// CLR type used to derive the OpenAPI schema type and format.
    /// Supported: <see cref="string"/>, <see cref="int"/>, <see cref="long"/>,
    /// <see cref="bool"/>, <see cref="double"/>, <see cref="decimal"/>, <see cref="System.Guid"/>.
    /// Defaults to <see cref="string"/> when <c>null</c>.
    /// </summary>
    public Type? ParameterType { get; }

    /// <summary>Whether the query parameter is required. Defaults to <c>false</c>.</summary>
    public bool Required { get; set; }

    /// <summary>Human-readable description of the parameter.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="QueryParameterAttribute"/>.
    /// </summary>
    /// <param name="name">The query-string parameter name.</param>
    /// <param name="parameterType">The CLR type of the parameter value (used for schema inference).</param>
    public QueryParameterAttribute(string name, Type? parameterType = null)
    {
        Name = name;
        ParameterType = parameterType;
    }
}
