using Native.OpenApi.Attributes;

namespace SampleApiFunction.Responses;

public sealed record GetItemsResponse(List<ItemDto> Items);

public sealed record GetItemByIdResponse(ItemDto Item);

public sealed record CreateItemResponse(string Id, string Message);

public sealed record UpdateItemResponse(string Id, string Message);

public sealed record DeleteItemResponse(string Id, string Message);

public sealed record HealthCheckResponse(string Status, string Timestamp);

public sealed record ItemDto(string Id, string Name, string Description, decimal Price);

public sealed record ErrorResponse(string Message, string? Details = null);

public sealed record ProblemDetails(
    string Type,
    string Title,
    int Status,
    string? Detail = null,
    string? Instance = null);

// ── Polymorphic payment-method hierarchy (Wave 4 — polymorphism demo) ──────────
//
// PaymentMethod is an abstract base with two concrete sub-types:
//   - CardPayment  (discriminator value: "card")
//   - BankTransfer (discriminator value: "bank_transfer")
//
// The [OpenApiDiscriminator] + [OpenApiSubType] attributes are read at compile-time by
// the source generator and produce:
//   PaymentMethod → oneOf + discriminator.mapping
//   CardPayment   → allOf: [$ref PaymentMethod, {own props}]
//   BankTransfer  → allOf: [$ref PaymentMethod, {own props}]

[OpenApiDiscriminator("paymentMethodType")]
[OpenApiSubType(typeof(CardPayment), "card")]
[OpenApiSubType(typeof(BankTransfer), "bank_transfer")]
public abstract class PaymentMethod
{
    /// <summary>Discriminator tag that identifies the concrete payment-method type.</summary>
    [OpenApiProperty(Description = "Discriminator tag that identifies the concrete payment-method type.", Example = "card")]
    public string PaymentMethodType { get; set; } = "";

    /// <summary>Human-readable label for the payment method.</summary>
    [OpenApiProperty(Description = "Human-readable label shown in checkout UIs.", Example = "Visa ending in 4242")]
    public string? Label { get; set; }
}

/// <summary>Card-based payment (credit or debit).</summary>
public sealed class CardPayment : PaymentMethod
{
    /// <summary>Masked card number, e.g. "•••• •••• •••• 4242".</summary>
    [OpenApiProperty(Description = "Masked PAN shown for display only.", Example = "•••• •••• •••• 4242", MaxLength = 19)]
    public string MaskedPan { get; set; } = "";

    /// <summary>Card network (Visa, Mastercard, Amex, …).</summary>
    [OpenApiProperty(Description = "Card network identifier.", Example = "Visa")]
    public string Network { get; set; } = "";

    /// <summary>Two-digit expiry month (01–12).</summary>
    [OpenApiProperty(Description = "Card expiry month (01–12).", Example = "12", Minimum = 1, Maximum = 12)]
    public int ExpiryMonth { get; set; }

    /// <summary>Four-digit expiry year.</summary>
    [OpenApiProperty(Description = "Card expiry year (four digits).", Example = "2028")]
    public int ExpiryYear { get; set; }
}

/// <summary>Bank-transfer payment (SEPA, PIX, etc.).</summary>
public sealed class BankTransfer : PaymentMethod
{
    /// <summary>IBAN or local account number.</summary>
    [OpenApiProperty(Description = "IBAN or local bank account number.", Example = "DE89370400440532013000", MaxLength = 34)]
    public string AccountNumber { get; set; } = "";

    /// <summary>ISO 9362 BIC/SWIFT code of the bank.</summary>
    [OpenApiProperty(Description = "BIC/SWIFT code of the receiving bank.", Example = "DEUTDEDB", MaxLength = 11)]
    public string Bic { get; set; } = "";

    /// <summary>ISO 4217 currency code.</summary>
    [OpenApiProperty(Description = "ISO 4217 currency code.", Example = "EUR", MinLength = 3, MaxLength = 3)]
    public string Currency { get; set; } = "";
}

/// <summary>Response wrapper that returns a polymorphic payment-method union.</summary>
public sealed record GetPaymentMethodResponse(PaymentMethod PaymentMethod);

/// <summary>Response listing all payment methods for a customer.</summary>
public sealed record ListPaymentMethodsResponse(List<PaymentMethod> PaymentMethods);

// ── Structural Wave 5 additions ──────────────────────────────────────────────────

/// <summary>
/// Payload delivered by the <c>itemCreated</c> webhook when an item is created.
/// Registered in components/schemas automatically by the source generator.
/// </summary>
public sealed record ItemCreatedEvent(
    string EventId,
    string ItemId,
    string Name,
    decimal Price,
    string OccurredAt);

/// <summary>Response for the paginated items listing endpoint (exercises query params).</summary>
public sealed record ListItemsPagedResponse(
    List<ItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>Response for create-order (exercises response headers + links + callbacks).</summary>
public sealed record CreateOrderResponse(
    string OrderId,
    string Status,
    decimal TotalAmount);
