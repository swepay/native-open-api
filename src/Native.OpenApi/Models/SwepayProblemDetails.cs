namespace Native.OpenApi.Models;

/// <summary>
/// Canonical Swepay error payload. Superset of RFC 9457
/// (<c>application/problem+json</c>), adding machine-readable <see cref="Code"/>,
/// UX-written <see cref="Recovery"/> and trace-enabling <see cref="RequestId"/>.
/// </summary>
/// <remarks>
/// <para>RFC-DOCUMENTACAO-UX § F13. All operations that emit
/// <c>application/problem+json</c> reference <c>#/components/schemas/SwepayProblemDetails</c>.
/// The Source Generator injects the schema automatically whenever at least one
/// endpoint advertises this content-type — consumers do not need to declare it
/// manually.</para>
/// <para>Do <b>not</b> include stack traces, SQL errors, table names or internal
/// IDs in any field. Linter rule #8 enforces this on serialized samples.</para>
/// </remarks>
/// <param name="Type">Canonical error documentation URL, e.g.
/// <c>https://docs.swepay.com.br/errors/PAYMENT_INSUFFICIENT_FUNDS</c>.</param>
/// <param name="Title">Short, human-readable classification (e.g. <c>"Payment Insufficient Funds"</c>).</param>
/// <param name="Status">HTTP status code as an integer.</param>
/// <param name="Detail">End-user message. Same copy surfaced by
/// <see cref="Attributes.ErrorDefinitionAttribute.UserMessage"/>.</param>
/// <param name="Instance">URI reference identifying the specific occurrence,
/// typically the resource path (<c>/v1/payments/pay_01JRX...</c>).</param>
/// <param name="Code">Machine-readable code matching
/// <see cref="Attributes.ErrorDefinitionAttribute.Code"/>.</param>
/// <param name="Recovery">Concrete next step the caller should take.</param>
/// <param name="RequestId">Correlation id echoed from the <c>X-Request-Id</c>
/// request header (or newly minted server-side when absent).</param>
public sealed record SwepayProblemDetails(
    string Type,
    string Title,
    int Status,
    string Detail,
    string? Instance,
    string Code,
    string Recovery,
    string RequestId);
