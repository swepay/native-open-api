using NativeMediator;
using Functions.OpenApi.Responses;

namespace Functions.OpenApi.Commands;

public sealed class GetOpenApiJsonCommand : IRequest<GetOpenApiJsonResponse> { }
public sealed class GetRedocCommand : IRequest<GetRedocResponse> { }
public sealed class GetScalarCommand : IRequest<GetScalarResponse> { }
