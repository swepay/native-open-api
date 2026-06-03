# Troubleshooting — native-open-api

Guia de problemas conhecidos ao gerar, empacotar e renderizar specs OpenAPI 3.1
com `native-open-api`. Cada item segue o formato **Sintoma → Causa → Solução**.

> Política do projeto: **Scalar-first** (o Scalar é a UI de referência; o Redoc é
> secundário). Várias extensões são `x-scalar-*`/`x-enum-*` e só renderizam no Scalar.

---

## Índice

- [Renderização](#renderização)
  - [1. Documentação aparece vazia / tags em branco](#1-documentação-aparece-vazia--tags-em-branco)
  - [2. "Document could not be loaded" / spec não carrega](#2-document-could-not-be-loaded--spec-não-carrega)
  - [3. Exemplos quebrados ou 404 ao abrir uma operação](#3-exemplos-quebrados-ou-404-ao-abrir-uma-operação)
  - [4. Redoc mostra só o cabeçalho; nenhuma operação](#4-redoc-mostra-só-o-cabeçalho-nenhuma-operação)
  - [5. Exemplo do Scalar vem com valores vazios](#5-exemplo-do-scalar-vem-com-valores-vazios)
  - [6. Descrição de enum/recurso aparece no Scalar mas não no Redoc](#6-descrição-de-enumrecurso-aparece-no-scalar-mas-não-no-redoc)
- [Geração do spec](#geração-do-spec)
  - [7. `$ref` apontando para schema inexistente (dangling)](#7-ref-apontando-para-schema-inexistente-dangling)
  - [8. Schema de webhook/callback sai vazio](#8-schema-de-webhookcallback-sai-vazio)
  - [9. YAML inválido com `description` multilinha](#9-yaml-inválido-com-description-multilinha)
  - [10. Gerador não encontra endpoints (NLOAPI002)](#10-gerador-não-encontra-endpoints-nloapi002)
  - [11. O spec gerado não aparece em disco](#11-o-spec-gerado-não-aparece-em-disco)
- [Empacotamento / build](#empacotamento--build)
  - [12. `dotnet pack` falha com NU5128 / NU5017](#12-dotnet-pack-falha-com-nu5128--nu5017)
  - [13. Restore falha com NU1605 (downgrade YamlDotNet)](#13-restore-falha-com-nu1605-downgrade-yamldotnet)
  - [14. Warnings de AOT / trim (IL2xxx, IL3xxx)](#14-warnings-de-aot--trim-il2xxx-il3xxx)
- [Validação local](#validação-local)
  - [15. Como servir a doc localmente para validar](#15-como-servir-a-doc-localmente-para-validar)

---

## Renderização

### 1. Documentação aparece vazia / tags em branco

**Sintoma:** o Scalar/Redoc carrega, o cabeçalho (`info`) aparece, mas as tags do
menu estão vazias (sem endpoints) — ou nenhuma operação é exibida.

**Causa:** inconsistência de **tags**. Quando você usa `x-tagGroups`, o Redoc
**só exibe tags que estão dentro de algum grupo** — tags fora de grupo ficam
ocultas. O caso clássico: as operações recebem uma tag auto-derivada do path
(ex.: `V1` para `/v1/...`) porque os commands não têm tag explícita, enquanto o
`[assembly: TagGroup(...)]` / `[assembly: TagMetadata(...)]` referenciam outros
nomes (ex.: `Items`, `Health`). Resultado: a tag com as operações (`V1`) não está
em grupo nenhum → escondida; as tags dos grupos (`Items`/`Health`) não têm
operação → vazias.

**Solução:** garanta consistência entre as tags das operações e os grupos:

1. Atribua tags explícitas às operações (via `.WithTags("Items")` na rota ou
   `[Tags("Items")]` no command).
2. Toda tag usada por uma operação deve estar em **algum** `x-tagGroup`.
3. Toda tag declarada em um `x-tagGroup`/`TagMetadata` deve ter **pelo menos uma**
   operação.

Checklist rápido sobre o YAML gerado:

```bash
# tags usadas nas operações
grep -E '^        - "' openapi.yaml | sort -u
# tags declaradas em x-tagGroups  →  os dois conjuntos devem ser coerentes
```

---

### 2. "Document could not be loaded" / spec não carrega

**Sintoma:** o viewer abre mas mostra "Document could not be loaded" ou fica em
branco; no DevTools/Network há um **404** ao buscar o spec.

**Causa:** a URL do spec passada para o renderer não corresponde ao endereço em
que o spec é realmente servido. A partir da **v1.8.3** o renderer honra o
`specPath` **verbatim** (`RenderScalar(specPath, ...)` → `data-url="{specPath}"`;
`RenderRedoc(specPath, ...)` → `Redoc.init('{specPath}', ...)`). Versões
anteriores recompunham a URL a partir de `window.location` assumindo a rota
`/docs/{viewer}` e o nome `openapi.yaml` — o que quebrava em qualquer outro ponto
de montagem.

**Solução:**

- Passe em `specPath` exatamente a URL onde o spec está servido, **do ponto de
  vista do navegador**:
  - Absoluto (`"/openapi.yaml"`) → resolvido a partir da raiz do domínio.
  - Relativo (`"openapi.yaml"`) → resolvido relativo à página atual (útil quando a
    doc está atrás de um prefixo de stage, ex.: API Gateway `/prod`).
- Garanta que existe uma rota servindo o spec naquele caminho.
- Em versões < 1.8.3: sirva a página em `/docs/scalar` (ou `/docs/redoc`) e o spec
  em `/openapi.yaml` na raiz, para casar com a convenção embutida.

---

### 3. Exemplos quebrados ou 404 ao abrir uma operação

**Sintoma:** no Network aparecem 404 para arquivos como
`examples/.../happy.json`; o painel de exemplo fica vazio e, em alguns casos, a
operação não renderiza por completo.

**Causa:** `[ApiExample(RequestJson = "examples/...json", ResponseJson = "...")]`
emite `externalValue:` apontando para arquivos que precisam ser **servidos** junto
da API. Se esses arquivos não existem na URL esperada, o viewer recebe 404.

**Solução:** escolha uma das duas abordagens:

- **Inline (mais portátil):** use `RequestValue`/`ResponseValue` com o JSON
  embutido — sem dependência de arquivos externos:
  ```csharp
  [ApiExample(name: "happy", summary: "Item criado",
      RequestValue  = "{\"name\":\"Widget\",\"price\":9.99}",
      ResponseStatus = 201,
      ResponseValue = "{\"id\":\"item_abc\",\"name\":\"Widget\"}")]
  ```
- **externalValue:** mantenha `RequestJson`/`ResponseJson` **e sirva** os arquivos
  na mesma origem, no caminho referenciado (relativo ao spec).

---

### 4. Redoc mostra só o cabeçalho; nenhuma operação

**Sintoma:** o Redoc renderiza `info` (título, contato, descrição) mas nenhuma
operação aparece.

**Causa (mais comum):** um `$ref` que **não resolve** em algum lugar do `paths`.
O Redoc faz o *bundle* do documento antes de renderizar; um `$ref` órfão aborta a
renderização das operações. Veja também o item [7](#7-ref-apontando-para-schema-inexistente-dangling).
**Outra causa:** tags fora de grupo com `x-tagGroups` (item [1](#1-documentação-aparece-vazia--tags-em-branco)).

**Solução:** valide que todo `#/components/schemas/X` referenciado existe como
definição (item 7) e que o tagging está consistente (item 1). Faça hard-reload
(Ctrl+F5) após corrigir — o navegador pode estar servindo o spec/HTML em cache.

---

### 5. Exemplo do Scalar vem com valores vazios

**Sintoma:** o exemplo de um webhook/operação aparece com placeholders como
`{"eventId": "", "price": 1, "occurredAt": ""}`.

**Causa:** **comportamento esperado.** Quando não há `example` explícito, o Scalar
**sintetiza** um exemplo a partir do schema (string → `""`, número → `1`, etc.).

**Solução:** forneça exemplos reais por propriedade:

```csharp
public sealed class ItemCreatedEvent
{
    [OpenApiProperty(Example = "evt_01HXYZ")]   public string EventId { get; init; } = "";
    [OpenApiProperty(Example = "9.99")]          public decimal Price  { get; init; }
}
```

ou um exemplo de payload completo via `[ApiExample(... ResponseValue = "...")]`.

---

### 6. Descrição de enum/recurso aparece no Scalar mas não no Redoc

**Sintoma:** `x-enum-descriptions`, `x-enum-varnames`, `x-scalar-stability`,
`x-order` não aparecem no Redoc.

**Causa:** política **Scalar-first**. Essas são extensões específicas do Scalar; o
Redoc usa outros nomes (`x-enumDescriptions`) ou não as suporta. A biblioteca
**não** faz dual-emit.

**Solução:** use o Scalar como UI de referência. Se precisar de paridade no Redoc
para descrições de enum, isso exigiria emitir `x-enumDescriptions` adicionalmente
— hoje fora de escopo por decisão de projeto.

---

## Geração do spec

### 7. `$ref` apontando para schema inexistente (dangling)

**Sintoma:** viewers não renderizam operações; validadores acusam
`Reference to unknown component "#/components/schemas/X"`.

**Causa:** um tipo é **referenciado** mas seu schema não é **emitido** em
`components/schemas`. Casos históricos:
- Tipos usados apenas em `[ApiResponse(statusCode, typeof(T))]` (corrigido na
  v1.8.x — agora o gerador roda descoberta de schema para esses tipos).
- Herança/base type cross-assembly (base definida em outro pacote): o gerador não
  consegue resolver as propriedades da base externa.

**Solução:**
- Atualize para a versão mais recente (fix dos tipos de `[ApiResponse]`).
- Mantenha os tipos referenciados no **mesmo assembly** do gerador, ou exponha-os
  via `Map<TCommand,TResponse>`/atributos que o gerador escaneia.
- Verifique com um diff referenciados × definidos:
  ```bash
  grep -oE '#/components/schemas/[A-Za-z0-9_]+' openapi.yaml | sed 's#.*/##' | sort -u > refs.txt
  # compare com os nomes definidos sob components.schemas
  ```

---

### 8. Schema de webhook/callback sai vazio

**Sintoma:** `components/schemas/{Payload}` aparece como `type: object` sem
propriedades.

**Causa:** o payload do webhook/callback (declarado via
`[assembly: Webhook(name, typeof(Payload))]` ou `[Callback(... PayloadType = ...)]`)
não passou pela descoberta de propriedades. Corrigido na v1.8.x: o payload agora é
resolvido via `TypePropertyExtractor`.

**Solução:** atualize a biblioteca. Garanta que o tipo de payload é um
record/classe resolvível no mesmo assembly.

---

### 9. YAML inválido com `description` multilinha

**Sintoma:** parsers estritos (YamlDotNet, Spectral) rejeitam o spec quando uma
`description`/`summary` contém quebras de linha; erro do tipo
*"while scanning a multi-line double-quoted scalar, found wrong indentation"*.

**Causa:** versões antigas do `EscapeYamlString` não escapavam `\n`/`\r`,
produzindo um scalar double-quoted inválido. Corrigido na v1.8.x.

**Solução:** atualize a biblioteca. Em descrições, prefira usar `\n` como
sequência de escape no texto do atributo C#.

---

### 10. Gerador não encontra endpoints (NLOAPI002)

**Sintoma:** warning `NLOAPI002: No NativeLambdaRouter endpoints were discovered`.

**Causa:** o gerador não encontrou invocações `MapGet<TCommand,TResponse>` /
`MapPost` / etc. em `IRouteBuilder`. Comum em projetos que só consomem a
biblioteca (testes, runners de doc) ou quando as rotas estão fora do padrão
escaneado.

**Solução:**
- Se o projeto realmente não tem rotas (ex.: projeto de testes/runner), suprima:
  ```xml
  <NoWarn>$(NoWarn);NLOAPI002</NoWarn>
  ```
- Caso contrário, confirme que usa os mapeadores genéricos
  `Map{Get,Post,Put,Delete,Patch}<TCommand,TResponse>` em `IRouteBuilder` e que a
  classe da função é `partial`.

---

### 11. O spec gerado não aparece em disco

**Sintoma:** você quer inspecionar o YAML mas não acha o arquivo `.g.cs`.

**Causa:** por padrão o source generator injeta o código **em memória** durante a
compilação; ele não escreve em disco.

**Solução:** habilite a persistência no `.csproj`:
```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
```
O arquivo aparece em:
```
obj/<Config>/<TFM>/generated/NativeLambdaRouter.SourceGenerator.OpenApi/.../GeneratedOpenApiSpec.g.cs
```
Em runtime, acesse via `GeneratedOpenApiSpec.Instance.Yaml` (ou a constante
`GeneratedOpenApiSpec.YamlContent`).

---

## Empacotamento / build

### 12. `dotnet pack` falha com NU5128 / NU5017

**Sintoma:**
- `NU5128: Some target frameworks ... do not have exact matches ...`
- `NU5017: Cannot create a package that has no dependencies nor content.`

**Causa:** empacotamento de pacote **analyzer/source generator**. O assembly deve
ir para `analyzers/dotnet/cs`, não para `lib/`. Se o build output vai para `lib/`
com dependências suprimidas → NU5128. Se você desliga `IncludeBuildOutput` e
adiciona o DLL só via `<None>` para `analyzers/`, o NuGet não conta isso como
conteúdo → NU5017.

**Solução (config canônica do projeto do generator):**
```xml
<PropertyGroup>
  <IncludeBuildOutput>true</IncludeBuildOutput>
  <BuildOutputTargetFolder>analyzers/dotnet/cs</BuildOutputTargetFolder>
  <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
  <DevelopmentDependency>true</DevelopmentDependency>
</PropertyGroup>
```
Isso mantém um build output real no pacote (satisfaz NU5017) e sem `lib/ref`
(evita NU5128). Verifique o conteúdo:
```bash
# o DLL deve estar sob analyzers/dotnet/cs/...
```

---

### 13. Restore falha com NU1605 (downgrade YamlDotNet)

**Sintoma:** `error NU1605: Detected package downgrade: YamlDotNet from 18.0.0 to 16.3.0`.

**Causa:** uma referência direta fixa uma versão menor de `YamlDotNet` do que a
exigida transitivamente por `NativeOpenApi`.

**Solução:** alinhe a versão de `YamlDotNet` (>= a exigida pela lib — atualmente
**18.0.0**) em todos os projetos e no `Directory.Packages.props`, se houver
Central Package Management.

---

### 14. Warnings de AOT / trim (IL2xxx, IL3xxx)

**Sintoma:** warnings de trim/AOT ao publicar com `PublishAot=true`.

**Causa:** uso de reflection em runtime. A premissa da biblioteca é **zero
reflection** — toda a geração de metadados é compile-time (source generator).

**Solução:** não introduza reflection no caminho de runtime. Serialização JSON
deve usar `JsonSerializerContext` (source-gen), nunca o caminho reflexivo. O
projeto do generator permanece em `netstandard2.0`.

---

## Validação local

### 15. Como servir a doc localmente para validar

O `SampleApiFunction` é uma Lambda (não um web server). Para validar o Scalar/Redoc
no navegador, use um pequeno runner Kestrel que serve o spec e o HTML do renderer.

```csharp
using Native.OpenApi;
using SampleApiFunction.Generated;

var app = WebApplication.CreateBuilder(args).Build();
var spec = GeneratedOpenApiSpec.Instance;
var renderer = new OpenApiHtmlRenderer();

app.MapGet("/openapi.yaml", () => Results.Text(spec.Yaml, "application/yaml"));
app.MapGet("/docs/scalar", () => Results.Text(renderer.RenderScalar("/openapi.yaml", "API"), "text/html"));
app.MapGet("/docs/redoc",  () => Results.Text(renderer.RenderRedoc("/openapi.yaml", "API"),  "text/html"));
app.Run("http://localhost:5080");
```

Abra `http://localhost:5080/docs/scalar`. Dicas:
- Faça **hard-reload** (Ctrl+F5) após mudanças — o navegador cacheia HTML e spec.
- Confira o **Network** do DevTools: o fetch de `/openapi.yaml` deve dar **200** e
  não deve haver 404 de `examples/...` (item 3).
- Os viewers carregam o bundle via **CDN** (jsdelivr/redoc) — precisa de internet,
  salvo se você configurar assets locais (`OpenApiScalarViewerOptions.LocalAssetPath`).

Para o `MultiLambdaSample`, já existe um runner pronto:
```bash
dotnet run --project samples/MultiLambdaSample/src/Functions.OpenApi.LocalRunner
# http://localhost:5000/docs/scalar
```
