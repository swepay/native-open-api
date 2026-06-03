namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares an OpenAPI 3.1 <c>callback</c> on the operation of the decorated command type.
/// Emitted as an entry in the <c>callbacks</c> map on the operation object.
/// </summary>
/// <remarks>
/// <para>Callbacks describe asynchronous notifications that the API will send to the caller's
/// registered endpoint after an operation completes. They use runtime expressions to build
/// the callback URL from the original request/response.</para>
/// <para>Apply this attribute on the <c>TCommand</c> class. It is <b>multi-use</b>.</para>
/// <para>This is a <b>compile-time-only</b> marker consumed by the Roslyn Source Generator.
/// Zero reflection at runtime.</para>
/// <para><b>Limitations (minimal implementation):</b> only a single path expression per
/// callback is supported. Complex callback scenarios (multiple paths, multiple methods,
/// full response objects) require a manual overlay spec.</para>
/// <para>Emitted YAML example:</para>
/// <code>
/// callbacks:
///   onPaymentStatusChange:
///     '{$request.body#/callbackUrl}':
///       post:
///         summary: "Payment status change notification"
///         requestBody:
///           required: true
///           content:
///             application/json:
///               schema:
///                 $ref: '#/components/schemas/PaymentStatusEvent'
///         responses:
///           '200':
///             description: "Callback received"
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [Callback("onPaymentStatusChange",
///     Expression = "{$request.body#/callbackUrl}",
///     Method = "post",
///     Summary = "Payment status notification",
///     PayloadType = typeof(PaymentStatusEvent))]
/// public sealed record CreatePaymentCommand(...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class CallbackAttribute : Attribute
{
    /// <summary>Callback key name in the callbacks map (e.g. "onPaymentStatusChange").</summary>
    public string Name { get; }

    /// <summary>
    /// Runtime expression that resolves to the callback URL.
    /// Example: <c>"{$request.body#/callbackUrl}"</c>.
    /// </summary>
    public string Expression { get; set; } = "";

    /// <summary>HTTP method for the callback request. Defaults to "post".</summary>
    public string Method { get; set; } = "post";

    /// <summary>Optional summary for the callback operation.</summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Optional payload type whose simple name becomes a <c>$ref</c> in components.schemas.
    /// The type must be discoverable in the compilation (same assembly).
    /// </summary>
    public Type? PayloadType { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="CallbackAttribute"/>.
    /// </summary>
    /// <param name="name">The callback key name.</param>
    public CallbackAttribute(string name)
    {
        Name = name;
    }
}
