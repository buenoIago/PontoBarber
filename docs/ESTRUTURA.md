# Estrutura do repositório

Este documento oferece uma visão rápida da organização do PontoBarber. Para decisões arquiteturais e a árvore detalhada, consulte `docs/TDD.md` (especialmente a seção 7 e os ADRs).

## Visão geral

O repositório é um monólito modular em .NET 10 com quatro camadas, documentação de produto, especificações OpenSpec, pipeline de CI e uma imagem Docker da API. Ferramentas locais de IA/documentação ficam separadas do código de execução.

```text
PontoBarber/
├── .agents/skills/          # Skills OpenSpec para agentes de código
├── .config/                 # Manifesto de ferramentas locais (dotnet-ef)
├── .github/workflows/       # Pipeline de CI
├── .opencode/commands/      # Comandos de geração de PRD/TDD
├── .opencode/skills/        # Skills de geração de PRD/TDD
├── deploy/docker/           # Dockerfile da API
├── docs/                    # PRD, SPEC, TDD e este guia
├── client/                  # Workspace Angular 22 (Bootstrap 5 + SCSS + Bootstrap Icons)
├── openspec/config.yaml     # Configuração do OpenSpec
├── openspec/specs/          # Especificações consolidadas
├── openspec/changes/        # Proposta, design, specs delta e tarefas
├── server/                  # Solução, quatro projetos e testes
│   ├── PontoBarber.slnx
│   ├── src/
│   └── tests/
├── .dockerignore
├── .editorconfig
├── .gitignore
├── AGENTS.md                # Instruções para agentes (opcional)
├── Directory.Build.props
├── Directory.Packages.props
└── global.json
```

## Responsabilidade dos diretórios

| Diretório | Responsabilidade |
| --- | --- |
| `.agents/skills/` | Skills do OpenSpec usadas por agentes de código. Não participam do build nem do runtime. |
| `.config/` | `dotnet-tools.json` fixa o `dotnet-ef` usado pelo CI e pelos comandos de migration. |
| `.github/workflows/` | Workflow de restore, build, formatação, testes e verificação de modelo EF. |
| `client/` | Workspace Angular 22 com Bootstrap 5 por importação seletiva SCSS, tokens de tema e Bootstrap Icons (ADR-001, ADR-026); orçamento de 300 KB gzip medido por `scripts/medir-bundle.mjs`. |
| `.opencode/` | Comandos e skills para geração de PRD/TDD. São ferramentas de apoio, não dependências da aplicação. |
| `server/PontoBarber.slnx` | Lista os sete projetos da solução, organizados em `/src/` e `/tests/`. |
| `deploy/docker/` | `api.Dockerfile`: imagem Linux multi-stage da API, com `HEALTHCHECK` em `/health/live`. |
| `docs/` | `PRD.md`, `SPEC.md`, `TDD.md` e `ESTRUTURA.md`. Este último resume a organização; os demais são a fonte de requisitos e decisões. |
| `openspec/` | Especificações executáveis, proposta, design, plano de tarefas e pendências. Não altera o runtime. |
| `server/src/PontoBarber.Dominio/` | Entidades, contratos e regras de domínio; não depende de infraestrutura (ADR-003). |
| `server/src/PontoBarber.Aplicacao/` | Casos de uso, DTOs, portas e erros de aplicação (ADR-003, ADR-017). |
| `server/src/PontoBarber.Infra/` | EF Core, SQL Server, convenções de persistência e design-time factory (ADR-004). |
| `server/src/PontoBarber.Api/` | Composição da API ASP.NET Core, controllers, middleware, health checks e observabilidade. |
| `server/tests/` | `PontoBarber.Testes.Unidade`, `PontoBarber.Testes.Integracao` e `PontoBarber.Testes.Arquitetura` (ADR-012, ADR-031, ADR-032). |

## Arquivos na raiz

| Arquivo | Papel |
| --- | --- |
| `.dockerignore` | Reduz o contexto de build e impede que segredos e artefatos entrem na imagem. |
| `.editorconfig` | Convenções de formatação compartilhadas. |
| `.gitignore` | Ignora `bin/`, `obj/`, `TestResults/`, segredos e saídas geradas. |
| `AGENTS.md` | Instruções opcionais para agentes de código; atualmente sem conteúdo. |
| `Directory.Build.props` | TFM `net10.0`, C# 14, nullable, analisadores e warnings como erro. |
| `Directory.Packages.props` | Versões centralizadas de pacotes, incluindo a correção de CVE do `Microsoft.OpenApi`. |
| `global.json` | Fixa o SDK .NET 10.0.103. |

## Convenções

- `bin/`, `obj/` e `TestResults/` são gerados localmente e nunca versionados.
- `client/` é o workspace Angular 22; funcionalidades ficam diretamente em `src/app/` e `shared/` fica em `src/app/shared/`, criados conforme as tarefas existirem, sem antecipá-los. Sem roteamento: os arquivos principais são `app.ts`, `app.html` e `app.config.ts`.
- `client/src/environments/` segue o padrão `environment.ts` / `environment.development.ts`, com `fileReplacements` apenas na configuração de desenvolvimento. A API é servida na mesma origem (ADR-033), então não há URL de API, proxy nem segredos nesses arquivos.
- Não existem `scripts/`, `publish/` ou `docker-compose.yml` nesta fase do projeto.
- A SPA será servida pelo próprio ASP.NET Core quando o frontend existir (ADR-033), sem Nginx e sem imagem web separada.
- Convenção de organização futura por funcionalidade: `Dominio/Modulos/<Feature>/`, `Aplicacao/Modulos/<Feature>/`, `Infra/Modulos/<Feature>/` e `Api/Modulos/<Feature>/`. As pastas serão criadas somente quando as features existirem, sem fundir ou renomear as quatro camadas atuais.
- Código transversal de cada camada fica em `Compartilhado/`: `Dominio/Compartilhado/`, `Aplicacao/Compartilhado/`, `Infra/Compartilhado/Orm/` e `Api/Compartilhado/{Erros,Http,Observabilidade,Tenant}/`.

## Referências

- `docs/TDD.md` seção 7 — árvore detalhada e convenções.
- `docs/TDD.md` seção 19 — ADRs aprovados.
- `openspec/changes/pontobarber-mvp/tasks.md` — planejamento de implementação.
