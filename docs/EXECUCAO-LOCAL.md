# Execução local

Este guia cobre apenas execução local, migrations e contrato de erros. Para arquitetura e decisões, consulte `docs/TDD.md`; para a organização do repositório, consulte `docs/ESTRUTURA.md`.

## Pré-requisitos

- SDK .NET 10.0.103 (`global.json`).
- Docker com engine Linux.
- `dotnet-ef` 10.0.15 restaurado pelo manifesto em `.config/`.
- SQL Server 2022 em container local.

> **User Secrets:** é a abordagem aprovada para desenvolvimento local, mas **ainda não está configurada** neste repositório. Até que seja configurada, use a variável de ambiente `ConnectionStrings__DefaultConnection` abaixo. Não inicialize User Secrets como parte deste guia.

## 1. Preparação

```powershell
dotnet --version
dotnet tool restore
dotnet restore server/PontoBarber.slnx
dotnet build server/PontoBarber.slnx --no-restore -warnaserror
dotnet format server/PontoBarber.slnx --verify-no-changes --no-restore
dotnet test server/PontoBarber.slnx --no-build --no-restore
```

## 2. Banco local descartável

Defina no shell uma senha **temporária** que atenda à política do SQL Server. O valor não deve ser gravado neste arquivo nem no repositório:

```text
PB_SQL_SA_PASSWORD = <senha temporária fornecida no shell>
```

Suba **um único container descartável**, com nome e porta dedicados:

```powershell
docker run -d --name pb-sql-1-11-validation -p 15433:1433 `
  -e ACCEPT_EULA=Y `
  -e MSSQL_SA_PASSWORD="$env:PB_SQL_SA_PASSWORD" `
  mcr.microsoft.com/mssql/server:2022-CU28-ubuntu-22.04
```

Aguardando o banco ficar pronto:

```powershell
do {
  Start-Sleep -Seconds 2
  docker exec pb-sql-1-11-validation /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:PB_SQL_SA_PASSWORD" -C -Q 'SELECT 1' > $null 2>&1
} until ($LASTEXITCODE -eq 0)
```

## 3. Migrations

Estado atual: **não existem migrations versionadas** em `server/src/PontoBarber.Infra`.

Verificação do modelo (não abre conexão, pois usa a factory de design-time com string fictícia):

```powershell
dotnet ef migrations has-pending-model-changes `
  --project server/src/PontoBarber.Infra/PontoBarber.Infra.csproj `
  --startup-project server/src/PontoBarber.Api/PontoBarber.Api.csproj
```

Listar migrations sem conectar ao banco:

```powershell
dotnet ef migrations list `
  --project server/src/PontoBarber.Infra/PontoBarber.Infra.csproj `
  --startup-project server/src/PontoBarber.Api/PontoBarber.Api.csproj `
  --no-connect
```

Aplicar migrations (usando **somente o banco descartável** `PontoBarberValidacao111`):

```powershell
dotnet ef database update `
  --project server/src/PontoBarber.Infra/PontoBarber.Infra.csproj `
  --startup-project server/src/PontoBarber.Api/PontoBarber.Api.csproj `
  --connection "Server=localhost,15433;Database=PontoBarberValidacao111;User Id=sa;Password=$env:PB_SQL_SA_PASSWORD;TrustServerCertificate=True;"
```

Como hoje não há migrations, o EF Core pode inicializar o banco e/ou a tabela `__EFMigrationsHistory`, mas **nenhuma migration é aplicada**. Não se deve afirmar que uma migration foi executada.

Para conferir o que foi criado no banco descartável:

```powershell
docker exec pb-sql-1-11-validation /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "$env:PB_SQL_SA_PASSWORD" -C `
  -d PontoBarberValidacao111 `
  -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES"
```

A criação da primeira migration é procedimento futuro e **não faz parte deste guia executável**:

```text
dotnet ef migrations add InitialCreate
```

## 4. Execução da API por `dotnet run`

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,15433;Database=PontoBarberValidacao111;User Id=sa;Password=$env:PB_SQL_SA_PASSWORD;TrustServerCertificate=True;"

dotnet run --project server/src/PontoBarber.Api/PontoBarber.Api.csproj --launch-profile http
```

Porta real: `http://localhost:5116`. O perfil `https` usa `https://localhost:7253`. O TDD §16.1 menciona `https://localhost:7001`; trata-se de divergência documental conhecida.

Health checks:

```powershell
curl.exe -s -o NUL -w '%{http_code}\n' http://localhost:5116/health/live
curl.exe -s -o NUL -w '%{http_code}\n' http://localhost:5116/health/ready
```

Esperado: `200` para ambos quando o banco responde. Sem connection string ou com banco indisponível, `/health/ready` retorna `503` e `/health/live` continua `200`.

## 4.1 E-mail local (Mailpit) para o OTP de clientes

No desenvolvimento, o código OTP de clientes é enviado por SMTP para o Mailpit. Ele não é usado em produção e não exige credenciais no repositório:

```powershell
docker run -d --name pb-mailpit -p 1025:1025 -p 8025:8025 \
  axllent/mailpit:latest
```

- SMTP: `localhost:1025` (configurado em `appsettings.Development.json`).
- Interface web: `http://localhost:8025`.
- Remetente: `nao-responder@pontobarber.local` (configurável por `Email:From`).
- Para parar/remover: `docker rm -f pb-mailpit`.

O ambiente de produção **não** herda essa configuração: sem `Email:Smtp:Host`, o envio falha com erro explícito até que o provedor definitivo seja decidido (ABT-018). Os testes automatizados usam `Email:Modo=Memoria` e nunca falam com SMTP.

Contrato OTP (Task 3.3):

| Método | Rota | Resposta |
| --- | --- | --- |
| `POST` | `/api/v1/auth/otp/request` | `202` genérica, com ou sem conta existente |
| `POST` | `/api/v1/auth/otp/verify` | `204` com cookie de sessão; `401` genérica para código inválido, expirado, usado ou após excesso de tentativas |

O código tem 6 dígitos, expira em 5 minutos, é armazenado apenas como `salt + HMAC-SHA256`, é válido uma única vez e nunca aparece em logs ou respostas HTTP. Reenvios são limitados a 3 por janela de validade, com intervalo mínimo de 60 segundos; novo envio invalida o código anterior.

## 5. Execução da imagem Docker da API

```powershell
docker build -f deploy/docker/api.Dockerfile -t pontobarber-api:local .

docker run -d --name pb-api-1-11-validation -p 8080:8080 `
  -e "ConnectionStrings__DefaultConnection=Server=host.docker.internal,15433;Database=PontoBarberValidacao111;User Id=sa;Password=$env:PB_SQL_SA_PASSWORD;TrustServerCertificate=True;" `
  pontobarber-api:local
```

Dentro do container, `host.docker.internal` aponta para o host Docker, onde o SQL Server descartável publica a porta `15433`.

Health checks na imagem:

```powershell
curl.exe -s -o NUL -w '%{http_code}\n' http://localhost:8080/health/live
curl.exe -s -o NUL -w '%{http_code}\n' http://localhost:8080/health/ready
```

A imagem expõe a porta `8080`, roda como usuário `app` e possui `HEALTHCHECK` em `/health/live`.

## 6. Contrato de erros

Respostas de erro usam `Content-Type: application/problem+json` e os campos. A serialização usa `Microsoft.AspNetCore.Mvc.ProblemDetails` nativo, com `code`, `traceId` e `errors` em `Extensions` (ADR-035, tarefa 1.12):

| Campo | Origem |
| --- | --- |
| `type` | `about:blank` |
| `title` | título do erro |
| `status` | status HTTP |
| `detail` | detalhe da mensagem; pode ser `null` |
| `instance` | caminho da requisição |
| `code` | código estável do catálogo |
| `traceId` | `HttpContext.TraceIdentifier` |
| `errors` | dicionário de erros por campo; `{}` quando ausente |

Mapeamentos implementados:

| Situação | HTTP | `code` |
| --- | --- | --- |
| `ExcecaoValidacaoAplicacao` | 400 | `validation.failed` |
| `ExcecaoAutenticacaoAplicacao` (OTP inválido/expirado/usado) | 401 | `auth.invalid_credentials` |
| `ExcecaoConflitoAplicacao` | 409 | `conflito.Codigo` |
| `DbUpdateConcurrencyException` | 409 | `concurrency.conflict` |
| Exceção não tratada | 500 | `internal.error` |
| Rota com tenant sem autenticação | 401 | `auth.invalid_credentials` |
| Tenant inválido ou sem vínculo | 403 | `tenant.forbidden` |

Validação pelo conjunto de testes existente:

```powershell
dotnet test server/PontoBarber.slnx --no-build --no-restore `
  --filter "FullyQualifiedName~TestesMiddlewareProblemDetails|FullyQualifiedName~TestesHealthChecks"
```

### Divergências conhecidas (não corrigidas neste guia)

- TDD §11.1 registra `validacao.failed`; o código usa `validation.failed`.
- TDD §11.1 registra `concurrency.conflito`; o código usa `concurrency.conflict`.
- `internal.error` existe no código mas não consta no catálogo do TDD.
- TDD prevê `tenant.not_found`/404; a implementação retorna 403/`tenant.forbidden` para tenant inválido ou sem vínculo.
- O 401 usa `auth.invalid_credentials`; há sugestão pendente de `auth.unauthenticated` nas pendências do OpenSpec.
- ADR-029 prevê 422 para validação de DataAnnotations; a implementação atual ainda usa 400.

## Referências

- `docs/TDD.md` §11.1 — catálogo de erros.
- `docs/TDD.md` §16.1 — ambientes.
- `docs/TDD.md` §16.3 — pipeline.
- ADR-004 — SQL Server e migrations.
- ADR-017 — contrato de erros.
- ADR-029 — validação e 422.
- ADR-033 — hospedagem da SPA pelo ASP.NET Core.
