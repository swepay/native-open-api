# GitHub Copilot Instructions – Native Source Generators

## Objetivo

Este repositório contém **Source Generators AOT-first** para .NET 10.

Copilot deve:

- Gerar código **sem reflection**
- Nunca usar `Activator`, `dynamic`, `Expression`
- Preferir código explícito e determinístico
- Priorizar `IIncrementalGenerator`
- Falhar em compile-time, nunca em runtime

## Estrutura do repositorio

.github/
└─ workflows/
└─ dotnet.yml

src/
├─ Native.OpenApi/
└─ NativeLambdaRouter.SourceGenerator.OpenApi/

tests/
├─ Native.OpenApi.Tests/
└─ NativeLambdaRouter.SourceGenerator.OpenApi.Tests/

Native.OpenApi.slnx
README.md

## Regras de Ouro

- Nenhum uso de `ConfigurationBinder`
- Nenhum scanning de assemblies
- Todos os construtores devem ser gerados
- Todo código deve ser compatível com Native AOT
- Always pass CancellationToken through all async calls.
- No sync-over-async (no .Result/.Wait).
- No Task.Run inside request handlers.
- Gerar README.md para cada biblioteca
- Sempre atualizar documentação do README.md após mudanças que impactem seu funcionamento
- Criar um arquivo docs/CHANGELOG.md para documentar todas as mudanças realizadas e break changes.

## Estilo

- Código simples
- Métodos pequenos
- Zero alocação desnecessária
- Sem LINQ em código gerado crítico

## Testes

Sempre que uma nova implementação ou um bug ofr encontrar deve ser criado um teste unitário cobrindo o cenario do bug

Para a biblioteca NativeLambdaRouter.SourceGenerator.OpenApi, a cada nova alteração de código deve-se implementar um novo projeto em `root/samples/` para testar os atributos e a geração de código, se deve criar uma solução seguindo do dotnet-lambda-solution como template `dotnet new dotnet-lambda-solution -n NativeLambdaRouter.SourceGenerator.OpenApi.Sample` e uma função lambda do tipo apigateway seguindo o seguinte comando

### Testes de exemplo

1. Criar a Solution

```bash
# Criar nova solution com camadas compartilhadas
dotnet new dotnet-lambda-solution -n NativeLambdaRouter.SourceGenerator.OpenApi.Sample

# Entrar no diretório
cd NativeLambdaRouter.SourceGenerator.OpenApi.Sample
```

2. Adicionar Functions

```bash
# Adicionar API Gateway Function
dotnet new dotnet-lambda-function -n NativeLambdaRouter.SourceGenerator.OpenApi.Sample.ApiGatewayFunction \
    --triggerType apigateway \
    --solutionName NativeLambdaRouter.SourceGenerator.OpenApi.Sample \
    -o .
```

Testes unitários devem seguir a estrutura AAA, sempre gerar testes usando:

- NSubstitute
- Bogus
- Shouldly

## Diagnósticos

Todo erro deve gerar:

- DiagnosticDescriptor
- ID consistente (CONFIGxxx, DIxxx, MEDxxx)

## Sempre execute ao finalizar uma alteração de código

- Build: `dotnet build`
- Test: `dotnet test`
- Pack: `dotnet pack src/Native.csproj`
- Format: `dotnet format`

## Output format

- Prefer short sections, small code blocks, and explain trade-offs.
- When making changes: show diff-level guidance + why.

## Proibições

Copilot **NÃO DEVE**:

- Usar reflection
- Gerar código mágico
- Depender de runtime behavior
- Deixar implementação incompleta
- Gerar implementações nulas
