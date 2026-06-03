// Assembly-level OpenAPI metadata.
// These attributes are consumed exclusively by the Roslyn Source Generator at compile time
// and have zero runtime overhead.
//
// Features exercised here:
//   - [assembly: OpenApiInfo]        — rich info block (description, summary, ToS, contact, license).
//   - [assembly: OpenApiServer]      — root servers array (production + sandbox).
//   - [assembly: TagMetadata]        — enriches each tag with description, x-displayName and externalDocs.
//   - [assembly: TagGroup]           — groups tags in the Redoc/Scalar sidebar as x-tagGroups.
//   - [assembly: OpenApiExternalDocs] — adds a top-level externalDocs link to the spec.
//   - [assembly: Webhook]            — structural Wave 5: top-level webhooks section.
//
// [EndpointExternalDocs] and [ApiExample] are per-command attributes declared alongside the
// command type (see Commands.cs) rather than here.

using Native.OpenApi.Attributes;
using SampleApiFunction.Responses;

// ── Rich info block (Wave 3) ────────────────────────────────────────────────────
[assembly: OpenApiInfo(
    Description    = "Swepay Marketplace API — manage items, subscriptions and partner integrations.\n\nRefer to the [developer guide](https://docs.example.com/marketplace) for authentication and rate-limit details.",
    Summary        = "Swepay Marketplace API",
    TermsOfService = "https://example.com/terms",
    ContactName    = "Swepay Developer Support",
    ContactUrl     = "https://example.com/contact",
    ContactEmail   = "api-support@example.com",
    LicenseName    = "Apache 2.0",
    LicenseUrl     = "https://www.apache.org/licenses/LICENSE-2.0")]

// ── Servers (Wave 3) ────────────────────────────────────────────────────────────
[assembly: OpenApiServer("https://api.example.com",         Description = "Production")]
[assembly: OpenApiServer("https://api.sandbox.example.com", Description = "Sandbox (safe for testing)")]

// ── Root-level external documentation ──────────────────────────────────────────
[assembly: OpenApiExternalDocs(
    "https://docs.example.com/marketplace",
    Description = "Swepay Marketplace developer guide")]

// ── Tag groups (rendered as collapsible sections in Redoc/Scalar sidebar) ──────
// Every tag used by a visible operation must appear in at least one group;
// tags absent from x-tagGroups are hidden by Redoc when x-tagGroups is present.
[assembly: TagGroup("Commerce",  new[] { "Items", "Products", "Orders" })]
[assembly: TagGroup("Payments",  new[] { "Payments" })]

// ── Tag metadata (description + display name + per-tag external docs) ───────────
[assembly: TagMetadata(
    "Items",
    Description = "Operations for managing the item catalog — listing, creating, updating and deleting items.",
    DisplayName = "Item Catalog",
    ExternalDocsDescription = "Item domain guide",
    ExternalDocsUrl = "https://docs.example.com/marketplace/items")]

[assembly: TagMetadata(
    "Products",
    Description = "Operations for managing marketplace products — creating and configuring product listings.",
    DisplayName = "Products")]

[assembly: TagMetadata(
    "Orders",
    Description = "Operations for placing and tracking marketplace orders.",
    DisplayName = "Orders")]

[assembly: TagMetadata(
    "Payments",
    Description = "Operations for managing payment methods — listing and retrieving card or bank transfer details.",
    DisplayName = "Payment Methods")]

// ── Webhooks (Structural Wave 5 — OpenAPI 3.1 top-level webhooks) ──────────────
// The ItemCreatedEvent payload type is defined in Responses.cs.
// The source generator registers it automatically in components/schemas.
[assembly: Webhook(
    "itemCreated",
    typeof(ItemCreatedEvent),
    Method = "post",
    Summary = "Fired when a new item is created in the catalog.",
    Description = "Subscribe via POST /v1/webhooks. The payload contains the full item representation at the time of creation.")]
