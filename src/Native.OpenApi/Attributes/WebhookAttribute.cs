namespace Native.OpenApi.Attributes;

/// <summary>
/// Declares a top-level <c>webhook</c> entry in the generated OpenAPI 3.1 specification.
/// Apply at the assembly level. Emitted under the root <c>webhooks:</c> section (sibling of
/// <c>paths:</c>), which is an OpenAPI 3.1-only feature.
/// </summary>
/// <remarks>
/// <para>Webhooks describe push-notifications that your API <em>sends</em> to subscribed
/// callers when domain events occur. The payload type's schema is automatically registered
/// in <c>components/schemas</c> (same mechanism as regular request/response types).</para>
/// <para>This is a <b>compile-time-only</b> marker consumed by the Roslyn Source Generator.
/// Zero reflection at runtime.</para>
/// <para>Emitted YAML example (with <c>PayloadType = typeof(OrderCreatedEvent)</c>):</para>
/// <code>
/// webhooks:
///   orderCreated:
///     post:
///       summary: "Order created notification"
///       requestBody:
///         required: true
///         content:
///           application/json:
///             schema:
///               $ref: '#/components/schemas/OrderCreatedEvent'
///       responses:
///         '200':
///           description: "Webhook received"
/// </code>
/// </remarks>
/// <example>
/// <code>
/// [assembly: Webhook("orderCreated",
///     typeof(OrderCreatedEvent),
///     Method = "post",
///     Summary = "Fired when a new order is placed.",
///     Description = "Subscribe via the webhook registration endpoint.")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class WebhookAttribute : Attribute
{
    /// <summary>Webhook name / key (e.g. "orderCreated"). Must be unique within the spec.</summary>
    public string Name { get; }

    /// <summary>
    /// CLR type of the webhook payload. Its simple name becomes a <c>$ref</c> in
    /// <c>components/schemas</c>. The type must be in the same assembly.
    /// </summary>
    public Type PayloadType { get; }

    /// <summary>HTTP method for the webhook notification. Defaults to "post".</summary>
    public string Method { get; set; } = "post";

    /// <summary>Optional short summary of what this webhook signals.</summary>
    public string? Summary { get; set; }

    /// <summary>Optional detailed description of the webhook semantics.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="WebhookAttribute"/>.
    /// </summary>
    /// <param name="name">Webhook key name.</param>
    /// <param name="payloadType">CLR type of the event payload.</param>
    public WebhookAttribute(string name, Type payloadType)
    {
        Name = name;
        PayloadType = payloadType;
    }
}
