namespace Native.OpenApi.Attributes;

/// <summary>
/// Provides rich metadata for the OpenAPI <c>info</c> object at the document root.
/// </summary>
/// <remarks>
/// <para>Apply this attribute at the <b>assembly level</b> to complement the mandatory
/// <c>title</c> and <c>version</c> (already supplied via MSBuild properties
/// <c>OpenApiSpecTitle</c> / <c>OpenApiSpecVersion</c>) with the optional
/// fields defined in OpenAPI 3.1 §4.8.3:</para>
/// <list type="bullet">
///   <item><description><c>info.description</c> — CommonMark Markdown description.</description></item>
///   <item><description><c>info.summary</c> — Short, one-line summary (OpenAPI 3.1+).</description></item>
///   <item><description><c>info.termsOfService</c> — URL to the terms of service.</description></item>
///   <item><description><c>info.contact</c> — Contact object {name, url, email}.</description></item>
///   <item><description><c>info.license</c> — License object {name, url|identifier}.</description></item>
/// </list>
/// <para>This is a <b>compile-time-only</b> marker read by the Roslyn source generator.
/// Zero reflection at runtime.</para>
/// <para>Emitted YAML:</para>
/// <code>
/// info:
///   title: "My API"
///   version: "1.0.0"
///   summary: "Short one-liner"
///   description: "Markdown **description** here."
///   termsOfService: "https://swepay.com/terms"
///   contact:
///     name: "Swepay Support"
///     url: "https://swepay.com/contact"
///     email: "api@swepay.com"
///   license:
///     name: "Apache 2.0"
///     url: "https://www.apache.org/licenses/LICENSE-2.0"
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [assembly: OpenApiInfo(
///     Description    = "Swepay Marketplace API — manage items and subscriptions.",
///     Summary        = "Marketplace API",
///     TermsOfService = "https://swepay.com/terms",
///     ContactName    = "Swepay Support",
///     ContactUrl     = "https://swepay.com/contact",
///     ContactEmail   = "api@swepay.com",
///     LicenseName    = "Apache 2.0",
///     LicenseUrl     = "https://www.apache.org/licenses/LICENSE-2.0")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class OpenApiInfoAttribute : Attribute
{
    /// <summary>
    /// CommonMark Markdown description of the API. Rendered as the main API description
    /// in Redoc / Scalar.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Short one-line summary of the API (OpenAPI 3.1 field).
    /// Appears in UI headers and overview cards.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// URL to the terms of service for the API. Must be an absolute URL.
    /// </summary>
    public string? TermsOfService { get; set; }

    // ── Contact ────────────────────────────────────────────────────────

    /// <summary>Identifying name of the contact person/organization.</summary>
    public string? ContactName { get; set; }

    /// <summary>URL pointing to the contact information.</summary>
    public string? ContactUrl { get; set; }

    /// <summary>Email address of the contact person/organization.</summary>
    public string? ContactEmail { get; set; }

    // ── License ────────────────────────────────────────────────────────

    /// <summary>
    /// The license name used for the API, e.g. "Apache 2.0" or "MIT".
    /// </summary>
    public string? LicenseName { get; set; }

    /// <summary>
    /// URL to the license or an SPDX identifier (e.g. "Apache-2.0").
    /// Per OpenAPI 3.1, <c>url</c> and <c>identifier</c> are mutually exclusive;
    /// this property is emitted as <c>url</c> when it contains "://" and as
    /// <c>identifier</c> otherwise.
    /// </summary>
    public string? LicenseUrl { get; set; }
}
