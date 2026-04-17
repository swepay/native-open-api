using Native.OpenApi.Attributes;

namespace SampleApiFunction;

/// <summary>
/// Central catalog of error codes returned by this sample API.
/// Wired into individual commands via <see cref="ErrorCatalogAttribute"/>;
/// the Source Generator emits the matched codes on each operation and the
/// full catalog at the document root.
/// </summary>
public static class SwepayErrors
{
    [ErrorDefinition(
        code: "ITEM_NOT_FOUND",
        httpStatus: 404,
        userMessage: "Item não encontrado para o id informado.",
        recovery: "Confira o id ou liste os itens em GET /v1/items.",
        DocUrl = "https://docs.swepay.com.br/errors/ITEM_NOT_FOUND")]
    public const string ItemNotFound = "ITEM_NOT_FOUND";

    [ErrorDefinition(
        code: "ITEM_NAME_REQUIRED",
        httpStatus: 422,
        userMessage: "O campo name é obrigatório.",
        recovery: "Reenvie a requisição preenchendo o campo name.",
        DocUrl = "https://docs.swepay.com.br/errors/ITEM_NAME_REQUIRED")]
    public const string ItemNameRequired = "ITEM_NAME_REQUIRED";

    [ErrorDefinition(
        code: "ITEM_PRICE_INVALID",
        httpStatus: 422,
        userMessage: "O campo price deve ser maior que zero.",
        recovery: "Corrija o valor de price e reenvie a requisição.",
        DocUrl = "https://docs.swepay.com.br/errors/ITEM_PRICE_INVALID")]
    public const string ItemPriceInvalid = "ITEM_PRICE_INVALID";
}
