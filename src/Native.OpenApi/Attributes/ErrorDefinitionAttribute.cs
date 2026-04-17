namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares one entry in a Swepay error catalog. Applied to a <c>const string</c>
/// (or <c>static readonly string</c>) field whose value <b>is</b> the error code.
/// </summary>
/// <remarks>
/// <para>RFC-DOCUMENTACAO-UX § F12. The catalog class (see
/// <see cref="ErrorCatalogAttribute"/>) groups all codes an API can emit; the
/// generator emits them on the operation as <c>x-swepay-errors</c> and on the
/// document root as <c>x-swepay-error-catalog</c>, consumed by the Redoc
/// "Error Catalog" page.</para>
/// <para>The contract is fixed on purpose: every error must answer
/// <i>what happened</i> (code + userMessage), <i>what to do about it</i> (recovery)
/// and <i>where to learn more</i> (docUrl). Linter rule #5 enforces this.</para>
/// </remarks>
/// <example>
/// <code>
/// public static class SwepayErrors
/// {
///     [ErrorDefinition(
///         code: "PAYMENT_INSUFFICIENT_FUNDS",
///         httpStatus: 402,
///         userMessage: "Saldo insuficiente no método de pagamento.",
///         recovery: "Tente outro método de pagamento ou adicione saldo.",
///         docUrl: "https://docs.swepay.com.br/errors/PAYMENT_INSUFFICIENT_FUNDS")]
///     public const string PaymentInsufficientFunds = "PAYMENT_INSUFFICIENT_FUNDS";
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ErrorDefinitionAttribute : Attribute
{
    /// <summary>
    /// Machine-readable error code. Must match the field value — the generator
    /// uses the field <b>value</b> as the canonical code; this argument exists
    /// for source-search discoverability.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// HTTP status code mapped to this error (402, 404, 409, 422, ...).
    /// </summary>
    public int HttpStatus { get; }

    /// <summary>
    /// End-user-facing message. Must <b>not</b> leak infrastructure details
    /// (stack traces, SQL errors, internal IDs). Linter rule #8.
    /// </summary>
    public string UserMessage { get; }

    /// <summary>
    /// Concrete next step the caller should take. Imperative, actionable.
    /// </summary>
    public string Recovery { get; }

    /// <summary>
    /// Canonical URL with the full explanation. Rendered as <c>type</c> in
    /// the <c>problem+json</c> payload.
    /// </summary>
    public string? DocUrl { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="ErrorDefinitionAttribute"/>.
    /// </summary>
    public ErrorDefinitionAttribute(string code, int httpStatus, string userMessage, string recovery)
    {
        Code = code;
        HttpStatus = httpStatus;
        UserMessage = userMessage;
        Recovery = recovery;
    }
}
