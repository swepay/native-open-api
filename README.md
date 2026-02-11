# Native.OpenApi

Bibliotecas .NET 10 para gerenciamento de documentos OpenAPI 3.1, otimizadas para Native AOT.

Este repositório contém dois pacotes NuGet:

  NativeOpenApi .............. Loading, linting, merging e rendering de documentos OpenAPI
  NativeOpenApiGenerator ..... Source Generator que gera specs OpenAPI em compile-time a partir de endpoints NativeLambdaRouter


NativeOpenApi
─────────────

Biblioteca core para carregar, validar, mesclar e renderizar documentos OpenAPI 3.1 sem reflection.

  - Carregamento de specs a partir de embedded resources (JSON e YAML)
  - Merge de múltiplas specs parciais em um documento consolidado
  - Linting configurável com regras de validação
  - Geração de páginas Redoc e Scalar
  - Interface IGeneratedOpenApiSpec para integração com o Source Generator
  - 100% Native AOT, zero reflection

  Instalação: dotnet add package NativeOpenApi

  Documentação completa: src/Native.OpenApi/README.md


NativeOpenApiGenerator
──────────────────────

Roslyn Source Generator que descobre endpoints MapGet, MapPost, MapPut, MapDelete, MapPatch
do NativeLambdaRouter e gera a spec OpenAPI 3.1 em compile-time.

  - Geração em compile-time (zero overhead em runtime)
  - Descoberta automática de endpoints via análise sintática/semântica
  - Operation IDs, tags, security e path parameters gerados automaticamente
  - Implementa IGeneratedOpenApiSpec quando NativeOpenApi está referenciado
  - Namespace customizável via OpenApiSpecName (resolve AssemblyName=bootstrap em AWS Lambda)

  Instalação: dotnet add package NativeOpenApiGenerator

  Documentação completa: src/NativeLambdaRouter.SourceGenerator.OpenApi/README.md


Quando usar cada pacote
───────────────────────

  Carregar/mesclar specs escritas manualmente ............... NativeOpenApi
  Gerar specs automaticamente a partir de endpoints ......... NativeOpenApiGenerator
  Gerar + mesclar com schemas comuns + servir documentação .. NativeOpenApi + NativeOpenApiGenerator


Estrutura do Repositório
────────────────────────

  src/
    Native.OpenApi/                                Biblioteca core
    NativeLambdaRouter.SourceGenerator.OpenApi/    Source Generator

  tests/
    Native.OpenApi.Tests/
    NativeLambdaRouter.SourceGenerator.OpenApi.Tests/

  samples/
    SampleApiFunction/                             Exemplo completo com AWS Lambda

  docs/
    CHANGELOG.md


Requisitos
──────────

  .NET 10.0+
  C# 12.0+


Build
─────

  git clone https://github.com/swepay/native-open-api.git
  cd native-open-api
  dotnet build
  dotnet test


License
───────

  MIT
