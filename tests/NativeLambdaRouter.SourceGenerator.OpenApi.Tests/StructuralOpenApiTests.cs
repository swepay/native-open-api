namespace NativeLambdaRouter.SourceGenerator.OpenApi.Tests;

/// <summary>
/// Tests for Structural OpenAPI 3.1 Wave 5 features:
/// 1. Query parameters (in: query) via [QueryParameter]
/// 2. Header parameters (in: header) via [HeaderParameter]
/// 3. Response headers via [ResponseHeader] (responses.{code}.headers)
/// 4. Response links via [ResponseLink] (responses.{code}.links)
/// 5. Callbacks via [Callback] (operation.callbacks)
/// 6. Webhooks via [assembly: Webhook] (top-level webhooks: section)
/// </summary>
public sealed class StructuralOpenApiTests
{
    // ── helpers ───────────────────────────────────────────────────────

    private static EndpointInfo MakeEndpoint(string method = "GET", string path = "/v1/items")
        => new()
        {
            Method = method,
            Path = path,
            CommandTypeName = "App.Command",
            ResponseTypeName = "App.Response",
            CommandSimpleName = "Command",
            ResponseSimpleName = "Response"
        };

    private static string Generate(EndpointInfo endpoint)
        => OpenApiYamlGenerator.Generate(
            new List<EndpointInfo> { endpoint },
            "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null,
            null,
            new List<OpenApiServerInfo>(),
            new List<WebhookInfo>());

    private static string GenerateWithWebhooks(EndpointInfo endpoint, List<WebhookInfo> webhooks)
        => OpenApiYamlGenerator.Generate(
            new List<EndpointInfo> { endpoint },
            "Test API", "1.0.0",
            new List<ErrorCatalogEntry>(),
            new List<TagMetadataInfo>(),
            new List<TagGroupInfo>(),
            null,
            null,
            new List<OpenApiServerInfo>(),
            webhooks);

    // ══════════════════════════════════════════════════════════════════
    // 1. Query parameters
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_WithNoQueryParams_DoesNotEmitExtraParameters()
    {
        var endpoint = MakeEndpoint();
        var yaml = Generate(endpoint);

        // No parameters section when no path params and no explicit params
        yaml.Should().NotContain("in: query");
        yaml.Should().NotContain("in: header");
    }

    [Fact]
    public void Generate_WithSingleQueryParam_EmitsQueryParameter()
    {
        var endpoint = MakeEndpoint();
        endpoint.QueryParameters.Add(new ExplicitParameterInfo
        {
            Name = "page",
            In = "query",
            SchemaType = "integer",
            SchemaFormat = "int32",
            Required = false,
            Description = "Page number"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("      parameters:");
        yaml.Should().Contain("        - name: \"page\"");
        yaml.Should().Contain("          in: query");
        yaml.Should().Contain("          required: false");
        yaml.Should().Contain("          description: \"Page number\"");
        yaml.Should().Contain("            type: integer");
        yaml.Should().Contain("            format: int32");
    }

    [Fact]
    public void Generate_WithRequiredQueryParam_EmitsRequiredTrue()
    {
        var endpoint = MakeEndpoint();
        endpoint.QueryParameters.Add(new ExplicitParameterInfo
        {
            Name = "filter",
            In = "query",
            SchemaType = "string",
            Required = true
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("          required: true");
    }

    [Fact]
    public void Generate_MultipleQueryParams_SortedAlphabetically()
    {
        var endpoint = MakeEndpoint();
        endpoint.QueryParameters.Add(new ExplicitParameterInfo { Name = "pageSize", In = "query", SchemaType = "integer" });
        endpoint.QueryParameters.Add(new ExplicitParameterInfo { Name = "filter",   In = "query", SchemaType = "string" });
        endpoint.QueryParameters.Add(new ExplicitParameterInfo { Name = "page",     In = "query", SchemaType = "integer" });

        var yaml = Generate(endpoint);

        var filterIdx   = yaml.IndexOf("\"filter\"", System.StringComparison.Ordinal);
        var pageIdx     = yaml.IndexOf("\"page\"", System.StringComparison.Ordinal);
        var pageSizeIdx = yaml.IndexOf("\"pageSize\"", System.StringComparison.Ordinal);

        filterIdx.Should().BeLessThan(pageIdx, "filter < page alphabetically");
        pageIdx.Should().BeLessThan(pageSizeIdx, "page < pageSize alphabetically");
    }

    [Fact]
    public void Generate_WithQueryParamNoFormat_OmitsFormatLine()
    {
        var endpoint = MakeEndpoint();
        endpoint.QueryParameters.Add(new ExplicitParameterInfo
        {
            Name = "search",
            In = "query",
            SchemaType = "string",
            SchemaFormat = null
        });

        var yaml = Generate(endpoint);

        // format line must not appear for this param (string has no format)
        var paramBlock = ExtractBetween(yaml, "\"search\"", "required:");
        paramBlock.Should().NotContain("format:");
    }

    // ══════════════════════════════════════════════════════════════════
    // 2. Header parameters
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_WithSingleHeaderParam_EmitsHeaderParameter()
    {
        var endpoint = MakeEndpoint();
        endpoint.HeaderParameters.Add(new ExplicitParameterInfo
        {
            Name = "X-Tenant-Id",
            In = "header",
            SchemaType = "string",
            Required = true,
            Description = "Tenant identifier"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("        - name: \"X-Tenant-Id\"");
        yaml.Should().Contain("          in: header");
        yaml.Should().Contain("          required: true");
        yaml.Should().Contain("          description: \"Tenant identifier\"");
    }

    [Fact]
    public void Generate_PathParamsEmittedBeforeQueryAndHeader()
    {
        var endpoint = MakeEndpoint("GET", "/v1/items/{id}");
        endpoint.QueryParameters.Add(new ExplicitParameterInfo { Name = "expand", In = "query", SchemaType = "string" });
        endpoint.HeaderParameters.Add(new ExplicitParameterInfo { Name = "X-Api-Version", In = "header", SchemaType = "string" });

        var yaml = Generate(endpoint);

        var pathIdx   = yaml.IndexOf("in: path", System.StringComparison.Ordinal);
        var queryIdx  = yaml.IndexOf("in: query", System.StringComparison.Ordinal);
        var headerIdx = yaml.IndexOf("in: header", System.StringComparison.Ordinal);

        pathIdx.Should().BeLessThan(queryIdx,  "path params come before query params");
        queryIdx.Should().BeLessThan(headerIdx, "query params come before header params");
    }

    [Fact]
    public void Generate_MultipleHeaderParams_SortedAlphabetically()
    {
        var endpoint = MakeEndpoint();
        endpoint.HeaderParameters.Add(new ExplicitParameterInfo { Name = "X-Tenant-Id", In = "header", SchemaType = "string" });
        endpoint.HeaderParameters.Add(new ExplicitParameterInfo { Name = "Accept-Language", In = "header", SchemaType = "string" });

        var yaml = Generate(endpoint);

        var acceptIdx  = yaml.IndexOf("\"Accept-Language\"", System.StringComparison.Ordinal);
        var tenantIdx  = yaml.IndexOf("\"X-Tenant-Id\"", System.StringComparison.Ordinal);

        acceptIdx.Should().BeLessThan(tenantIdx, "Accept-Language < X-Tenant-Id alphabetically");
    }

    // ══════════════════════════════════════════════════════════════════
    // 3. Response headers
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_WithResponseHeader_EmitsHeadersUnderResponse()
    {
        var endpoint = MakeEndpoint();
        endpoint.ResponseHeaders.Add(new ResponseHeaderInfo
        {
            StatusCode = 200,
            Name = "X-RateLimit-Limit",
            SchemaType = "integer",
            SchemaFormat = "int32",
            Description = "Max requests per window",
            Required = false
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("          headers:");
        yaml.Should().Contain("            \"X-RateLimit-Limit\":");
        yaml.Should().Contain("              description: \"Max requests per window\"");
        yaml.Should().Contain("              required: false");
        yaml.Should().Contain("                type: integer");
        yaml.Should().Contain("                format: int32");
    }

    [Fact]
    public void Generate_ResponseHeader_OnlyEmittedForMatchingStatusCode()
    {
        var endpoint = MakeEndpoint();
        // 404 header should not appear on the 200 response
        endpoint.ResponseHeaders.Add(new ResponseHeaderInfo
        {
            StatusCode = 404,
            Name = "X-Error-Code",
            SchemaType = "string"
        });

        var yaml = Generate(endpoint);

        // 200 response block should not have headers
        var response200Block = ExtractBetween(yaml, "\"200\":", "\"400\":");
        response200Block.Should().NotContain("headers:");
    }

    [Fact]
    public void Generate_MultipleResponseHeaders_SortedAlphabetically()
    {
        var endpoint = MakeEndpoint();
        endpoint.ResponseHeaders.Add(new ResponseHeaderInfo { StatusCode = 200, Name = "X-RateLimit-Remaining", SchemaType = "integer" });
        endpoint.ResponseHeaders.Add(new ResponseHeaderInfo { StatusCode = 200, Name = "X-RateLimit-Limit",     SchemaType = "integer" });

        var yaml = Generate(endpoint);

        var limitIdx     = yaml.IndexOf("\"X-RateLimit-Limit\"",     System.StringComparison.Ordinal);
        var remainingIdx = yaml.IndexOf("\"X-RateLimit-Remaining\"", System.StringComparison.Ordinal);

        limitIdx.Should().BeLessThan(remainingIdx, "Limit < Remaining alphabetically");
    }

    [Fact]
    public void Generate_RequiredResponseHeader_EmitsRequiredTrue()
    {
        var endpoint = MakeEndpoint("POST", "/v1/items");
        endpoint.AdditionalProduces.Add(new ProducesInfo { StatusCode = 201, ResponseTypeSimpleName = "Response", ContentType = "application/json" });
        endpoint.ResponseHeaders.Add(new ResponseHeaderInfo
        {
            StatusCode = 201,
            Name = "Location",
            SchemaType = "string",
            Required = true,
            Description = "URL of the created resource"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("\"Location\":");
        yaml.Should().Contain("              required: true");
    }

    // ══════════════════════════════════════════════════════════════════
    // 4. Response links
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_WithResponseLink_EmitsLinksUnderResponse()
    {
        var endpoint = MakeEndpoint("POST", "/v1/items");
        endpoint.AdditionalProduces.Add(new ProducesInfo { StatusCode = 201, ResponseTypeSimpleName = "Response", ContentType = "application/json" });
        endpoint.ResponseLinks.Add(new ResponseLinkInfo
        {
            StatusCode = 201,
            LinkId = "GetCreatedItem",
            OperationId = "GetItemById",
            Parameters = "id=$response.body#/id",
            Description = "Retrieve the created item"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("          links:");
        yaml.Should().Contain("            GetCreatedItem:");
        yaml.Should().Contain("              operationId: GetItemById");
        yaml.Should().Contain("              description: \"Retrieve the created item\"");
        yaml.Should().Contain("              parameters:");
        yaml.Should().Contain("                id: \"$response.body#/id\"");
    }

    [Fact]
    public void Generate_ResponseLink_OnlyEmittedForMatchingStatusCode()
    {
        var endpoint = MakeEndpoint();
        endpoint.ResponseLinks.Add(new ResponseLinkInfo
        {
            StatusCode = 404,
            LinkId = "SearchItems",
            OperationId = "ListItems"
        });

        var yaml = Generate(endpoint);

        var response200Block = ExtractBetween(yaml, "\"200\":", "\"400\":");
        response200Block.Should().NotContain("links:");
    }

    [Fact]
    public void Generate_WithMultipleLinkParams_ParsesAllPairs()
    {
        var endpoint = MakeEndpoint("POST", "/v1/items");
        endpoint.AdditionalProduces.Add(new ProducesInfo { StatusCode = 201, ResponseTypeSimpleName = "Response", ContentType = "application/json" });
        endpoint.ResponseLinks.Add(new ResponseLinkInfo
        {
            StatusCode = 201,
            LinkId = "GetOwnerItem",
            OperationId = "GetOwnerItem",
            Parameters = "userId=$response.body#/userId, itemId=$response.body#/id"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("                userId: \"$response.body#/userId\"");
        yaml.Should().Contain("                itemId: \"$response.body#/id\"");
    }

    [Fact]
    public void Generate_MultipleResponseLinks_SortedAlphabetically()
    {
        var endpoint = MakeEndpoint("POST", "/v1/items");
        endpoint.AdditionalProduces.Add(new ProducesInfo { StatusCode = 201, ResponseTypeSimpleName = "Response", ContentType = "application/json" });
        endpoint.ResponseLinks.Add(new ResponseLinkInfo { StatusCode = 201, LinkId = "ZetaLink", OperationId = "Z" });
        endpoint.ResponseLinks.Add(new ResponseLinkInfo { StatusCode = 201, LinkId = "AlphaLink", OperationId = "A" });

        var yaml = Generate(endpoint);

        var alphaIdx = yaml.IndexOf("AlphaLink:", System.StringComparison.Ordinal);
        var zetaIdx  = yaml.IndexOf("ZetaLink:",  System.StringComparison.Ordinal);
        alphaIdx.Should().BeLessThan(zetaIdx);
    }

    [Fact]
    public void Generate_ResponseLinkWithNoParams_OmitsParametersBlock()
    {
        var endpoint = MakeEndpoint("POST", "/v1/items");
        endpoint.AdditionalProduces.Add(new ProducesInfo { StatusCode = 201, ResponseTypeSimpleName = "Response", ContentType = "application/json" });
        endpoint.ResponseLinks.Add(new ResponseLinkInfo
        {
            StatusCode = 201,
            LinkId = "ListItems",
            OperationId = "ListItems",
            Parameters = null
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("            ListItems:");
        yaml.Should().Contain("              operationId: ListItems");
        // No parameters block when Parameters is null
        var linkBlock = ExtractBetween(yaml, "ListItems:", "\"400\":");
        linkBlock.Should().NotContain("parameters:");
    }

    // ══════════════════════════════════════════════════════════════════
    // 5. Callbacks
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_WithNoCallbacks_DoesNotEmitCallbacksSection()
    {
        var endpoint = MakeEndpoint();
        var yaml = Generate(endpoint);
        yaml.Should().NotContain("      callbacks:");
    }

    [Fact]
    public void Generate_WithCallback_EmitsCallbacksSection()
    {
        var endpoint = MakeEndpoint("POST", "/v1/orders");
        endpoint.Callbacks.Add(new CallbackInfo
        {
            Name = "onOrderStatusChange",
            Expression = "{$request.body#/callbackUrl}",
            Method = "post",
            Summary = "Order status change notification"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("      callbacks:");
        yaml.Should().Contain("        onOrderStatusChange:");
        yaml.Should().Contain("          \"{$request.body#/callbackUrl}\":");
        yaml.Should().Contain("            post:");
        yaml.Should().Contain("              summary: \"Order status change notification\"");
        yaml.Should().Contain("              responses:");
        yaml.Should().Contain("                \"200\":");
        yaml.Should().Contain("                  description: \"Callback received\"");
    }

    [Fact]
    public void Generate_CallbackWithPayloadType_EmitsRequestBody()
    {
        var endpoint = MakeEndpoint("POST", "/v1/orders");
        endpoint.Callbacks.Add(new CallbackInfo
        {
            Name = "onOrderComplete",
            Expression = "{$request.body#/callbackUrl}",
            Method = "post",
            PayloadTypeName = "OrderStatusEvent"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("              requestBody:");
        yaml.Should().Contain("                required: true");
        yaml.Should().Contain("                  application/json:");
        yaml.Should().Contain("                      $ref: \"#/components/schemas/OrderStatusEvent\"");
    }

    [Fact]
    public void Generate_CallbackWithPayloadType_RegistersSchemaInComponents()
    {
        var endpoint = MakeEndpoint("POST", "/v1/orders");
        endpoint.Callbacks.Add(new CallbackInfo
        {
            Name = "onEvent",
            Expression = "{$request.body#/callbackUrl}",
            Method = "post",
            PayloadTypeName = "OrderStatusEvent"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("    OrderStatusEvent:");
    }

    [Fact]
    public void Generate_MultipleCallbacks_SortedAlphabetically()
    {
        var endpoint = MakeEndpoint("POST", "/v1/orders");
        endpoint.Callbacks.Add(new CallbackInfo { Name = "zetaCallback", Expression = "{$url}", Method = "post" });
        endpoint.Callbacks.Add(new CallbackInfo { Name = "alphaCallback", Expression = "{$url}", Method = "post" });

        var yaml = Generate(endpoint);

        var alphaIdx = yaml.IndexOf("alphaCallback:", System.StringComparison.Ordinal);
        var zetaIdx  = yaml.IndexOf("zetaCallback:",  System.StringComparison.Ordinal);
        alphaIdx.Should().BeLessThan(zetaIdx);
    }

    [Fact]
    public void Generate_CallbacksEmittedBeforeResponses()
    {
        var endpoint = MakeEndpoint("POST", "/v1/orders");
        endpoint.Callbacks.Add(new CallbackInfo
        {
            Name = "onEvent",
            Expression = "{$url}",
            Method = "post"
        });

        var yaml = Generate(endpoint);

        var callbackIdx  = yaml.IndexOf("      callbacks:", System.StringComparison.Ordinal);
        var responsesIdx = yaml.IndexOf("      responses:", System.StringComparison.Ordinal);
        callbackIdx.Should().BeLessThan(responsesIdx, "callbacks: emitted before responses:");
    }

    // ══════════════════════════════════════════════════════════════════
    // 6. Webhooks (top-level)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_WithNoWebhooks_DoesNotEmitWebhooksSection()
    {
        var endpoint = MakeEndpoint();
        var yaml = Generate(endpoint);
        yaml.Should().NotContain("webhooks:");
    }

    [Fact]
    public void Generate_WithWebhook_EmitsWebhooksSection()
    {
        var endpoint = MakeEndpoint();
        var webhooks = new List<WebhookInfo>
        {
            new WebhookInfo
            {
                Name = "orderCreated",
                Method = "post",
                PayloadTypeName = "OrderCreatedEvent",
                Summary = "Fired when an order is created"
            }
        };

        var yaml = GenerateWithWebhooks(endpoint, webhooks);

        yaml.Should().Contain("webhooks:");
        yaml.Should().Contain("  orderCreated:");
        yaml.Should().Contain("    post:");
        yaml.Should().Contain("      summary: \"Fired when an order is created\"");
        yaml.Should().Contain("      requestBody:");
        yaml.Should().Contain("        required: true");
        yaml.Should().Contain("          application/json:");
        yaml.Should().Contain("              $ref: \"#/components/schemas/OrderCreatedEvent\"");
        yaml.Should().Contain("      responses:");
        yaml.Should().Contain("        \"200\":");
        yaml.Should().Contain("          description: \"Webhook received successfully\"");
    }

    [Fact]
    public void Generate_Webhook_RegistersPayloadSchemaInComponents()
    {
        var endpoint = MakeEndpoint();
        var webhooks = new List<WebhookInfo>
        {
            new WebhookInfo
            {
                Name = "itemCreated",
                Method = "post",
                PayloadTypeName = "ItemCreatedEvent"
            }
        };

        var yaml = GenerateWithWebhooks(endpoint, webhooks);

        yaml.Should().Contain("    ItemCreatedEvent:");
    }

    [Fact]
    public void Generate_WebhooksEmittedAfterPaths()
    {
        var endpoint = MakeEndpoint();
        var webhooks = new List<WebhookInfo>
        {
            new WebhookInfo { Name = "test", Method = "post", PayloadTypeName = "TestEvent" }
        };

        var yaml = GenerateWithWebhooks(endpoint, webhooks);

        var pathsIdx    = yaml.IndexOf("paths:", System.StringComparison.Ordinal);
        var webhooksIdx = yaml.IndexOf("webhooks:", System.StringComparison.Ordinal);
        pathsIdx.Should().BeLessThan(webhooksIdx, "paths: comes before webhooks:");
    }

    [Fact]
    public void Generate_MultipleWebhooks_SortedAlphabetically()
    {
        var endpoint = MakeEndpoint();
        var webhooks = new List<WebhookInfo>
        {
            new WebhookInfo { Name = "zetaEvent", Method = "post", PayloadTypeName = "ZetaPayload" },
            new WebhookInfo { Name = "alphaEvent", Method = "post", PayloadTypeName = "AlphaPayload" }
        };

        var yaml = GenerateWithWebhooks(endpoint, webhooks);

        var alphaIdx = yaml.IndexOf("  alphaEvent:", System.StringComparison.Ordinal);
        var zetaIdx  = yaml.IndexOf("  zetaEvent:",  System.StringComparison.Ordinal);
        alphaIdx.Should().BeLessThan(zetaIdx, "alpha < zeta alphabetically");
    }

    [Fact]
    public void Generate_WebhookWithDescription_EmitsDescription()
    {
        var endpoint = MakeEndpoint();
        var webhooks = new List<WebhookInfo>
        {
            new WebhookInfo
            {
                Name = "paymentProcessed",
                Method = "post",
                PayloadTypeName = "PaymentProcessedEvent",
                Summary = "Payment processed",
                Description = "Sent after a payment has been successfully processed."
            }
        };

        var yaml = GenerateWithWebhooks(endpoint, webhooks);

        yaml.Should().Contain("      description: \"Sent after a payment has been successfully processed.\"");
    }

    [Fact]
    public void Generate_WebhookWithGetMethod_EmitsGetMethod()
    {
        var endpoint = MakeEndpoint();
        var webhooks = new List<WebhookInfo>
        {
            new WebhookInfo
            {
                Name = "pollEvent",
                Method = "get",
                PayloadTypeName = "PollEventPayload"
            }
        };

        var yaml = GenerateWithWebhooks(endpoint, webhooks);

        yaml.Should().Contain("    get:");
    }

    // ══════════════════════════════════════════════════════════════════
    // 7. Combined integration: query + header params + response headers
    //    + links + callbacks + webhook in one spec
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_AllStructuralFeatures_ProducesValidYaml()
    {
        var endpoint = new EndpointInfo
        {
            Method = "POST",
            Path = "/v1/orders",
            CommandTypeName = "App.CreateOrderCommand",
            ResponseTypeName = "App.CreateOrderResponse",
            CommandSimpleName = "CreateOrderCommand",
            ResponseSimpleName = "CreateOrderResponse"
        };

        endpoint.QueryParameters.Add(new ExplicitParameterInfo
        {
            Name = "dryRun", In = "query", SchemaType = "boolean", Required = false,
            Description = "Validate without persisting"
        });
        endpoint.HeaderParameters.Add(new ExplicitParameterInfo
        {
            Name = "X-Idempotency-Key", In = "header", SchemaType = "string", Required = true,
            Description = "Idempotency key"
        });
        endpoint.AdditionalProduces.Add(new ProducesInfo
        {
            StatusCode = 201, ResponseTypeSimpleName = "CreateOrderResponse", ContentType = "application/json"
        });
        endpoint.ResponseHeaders.Add(new ResponseHeaderInfo
        {
            StatusCode = 201, Name = "Location", SchemaType = "string", Required = true,
            Description = "URL of created order"
        });
        endpoint.ResponseLinks.Add(new ResponseLinkInfo
        {
            StatusCode = 201, LinkId = "GetCreatedOrder", OperationId = "GetOrderById",
            Parameters = "orderId=$response.body#/orderId",
            Description = "Navigate to the created order"
        });
        endpoint.Callbacks.Add(new CallbackInfo
        {
            Name = "onOrderStatusChange", Expression = "{$request.body#/callbackUrl}",
            Method = "post", Summary = "Order status changed"
        });

        var webhooks = new List<WebhookInfo>
        {
            new WebhookInfo
            {
                Name = "orderCreated", Method = "post",
                PayloadTypeName = "OrderCreatedEvent",
                Summary = "Order was created"
            }
        };

        var yaml = GenerateWithWebhooks(endpoint, webhooks);

        // Must contain all sections
        yaml.Should().Contain("in: query");
        yaml.Should().Contain("in: header");
        yaml.Should().Contain("          headers:");
        yaml.Should().Contain("          links:");
        yaml.Should().Contain("      callbacks:");
        yaml.Should().Contain("webhooks:");
        yaml.Should().Contain("    OrderCreatedEvent:");
    }

    // ══════════════════════════════════════════════════════════════════
    // 8. Schema resolution from ExplicitParameterInfo / ResponseHeaderInfo
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Generate_QueryParamWithIntegerType_EmitsIntegerSchema()
    {
        var endpoint = MakeEndpoint();
        endpoint.QueryParameters.Add(new ExplicitParameterInfo
        {
            Name = "count", In = "query", SchemaType = "integer", SchemaFormat = "int64"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("            type: integer");
        yaml.Should().Contain("            format: int64");
    }

    [Fact]
    public void Generate_HeaderParamWithBooleanType_EmitsBooleanSchema()
    {
        var endpoint = MakeEndpoint();
        endpoint.HeaderParameters.Add(new ExplicitParameterInfo
        {
            Name = "X-Debug", In = "header", SchemaType = "boolean"
        });

        var yaml = Generate(endpoint);

        yaml.Should().Contain("            type: boolean");
    }

    // ── helper ────────────────────────────────────────────────────────

    /// <summary>Extracts text between two landmark strings (exclusive).</summary>
    private static string ExtractBetween(string source, string start, string end)
    {
        var startIdx = source.IndexOf(start, System.StringComparison.Ordinal);
        if (startIdx < 0) return "";
        startIdx += start.Length;

        var endIdx = source.IndexOf(end, startIdx, System.StringComparison.Ordinal);
        if (endIdx < 0) endIdx = source.Length;

        return source.Substring(startIdx, endIdx - startIdx);
    }
}
