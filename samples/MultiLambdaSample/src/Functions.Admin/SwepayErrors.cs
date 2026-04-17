using Native.OpenApi.Attributes;

namespace Functions.Admin;

/// <summary>
/// Shared error catalog for the Admin API. Referenced by commands via
/// <see cref="ErrorCatalogAttribute"/>; the Source Generator emits matched
/// codes on each operation (<c>x-swepay-errors</c>) and the full catalog at
/// document root (<c>x-swepay-error-catalog</c>).
/// </summary>
public static class SwepayErrors
{
    [ErrorDefinition(
        code: "USER_NOT_FOUND",
        httpStatus: 404,
        userMessage: "Usuário não encontrado para o id informado.",
        recovery: "Verifique o id ou consulte GET /v1/admin/users.",
        DocUrl = "https://docs.swepay.com.br/errors/USER_NOT_FOUND")]
    public const string UserNotFound = "USER_NOT_FOUND";

    [ErrorDefinition(
        code: "USER_ALREADY_EXISTS",
        httpStatus: 409,
        userMessage: "Já existe um usuário com o e-mail informado.",
        recovery: "Use outro e-mail ou recupere o acesso do usuário existente.",
        DocUrl = "https://docs.swepay.com.br/errors/USER_ALREADY_EXISTS")]
    public const string UserAlreadyExists = "USER_ALREADY_EXISTS";

    [ErrorDefinition(
        code: "USER_ROLE_INVALID",
        httpStatus: 422,
        userMessage: "O role informado não é válido para esse realm.",
        recovery: "Consulte a lista de roles em GET /v1/admin/roles e reenvie a requisição.",
        DocUrl = "https://docs.swepay.com.br/errors/USER_ROLE_INVALID")]
    public const string UserRoleInvalid = "USER_ROLE_INVALID";
}
