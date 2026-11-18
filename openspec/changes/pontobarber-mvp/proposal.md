# Proposal

## Why

O PontoBarber tem SPEC, PRD e TDD aprovados, com 25 decisões arquiteturais registradas, mas nenhuma linha de código, nenhuma especificação executável e nenhuma capability formalizada no OpenSpec. As decisões estão dispersas entre documentos e o TDD ainda carregava divergências em relação aos ADRs (Material, Quartz e Azure). Sem um recorte rastreável, a implementação começaria sem contrato de comportamento nem ordem de dependências.

Esta change estabelece o MVP completo do PontoBarber como base implementável: fundação técnica, identidade, barbearia, catálogo, disponibilidade, agenda, atendimento, pagamentos, clientes, dashboard e notificações, preservando o escopo funcional aprovado e sem antecipar decisões que ainda não têm aprovação.

## What Changes

- Formaliza o MVP completo do PontoBarber em 11 capabilities, com requisitos e cenários rastreáveis ao PRD/SPEC/TDD.
- Corrige o TDD para refletir as decisões vigentes: **Bootstrap 5 + SCSS + Bootstrap Icons** (ADR-026, aprovado), **estratégia de scheduler pendente** (ADR-027) e **hospedagem/provedor de nuvem pendentes** (ADR-028).
- Preserva os ADRs rejeitados (ADR-008 Material e ADR-009 Quartz) como histórico, marcados como substituídos, sem apagá-los.
- Normaliza status documentais divergentes (ADR-006 e ADR-016 aceitos; ADR-008 e ADR-009 rejeitados; ADR-013, ADR-025 e ADR-028 pendentes/propostos).
- Desacopla o pipeline GitHub Actions de qualquer provedor de hospedagem: build, testes, imagem e migration não dependem do destino; deploy é etapa fina e parametrizada.
- Define a fundação de execução de processos periódicos por abstração e idempotência, sem escolher biblioteca ou serviço.
- Substitui mecanismos previstos no TDD que nunca foram adotados em código por recursos nativos ou próprios: DataAnnotations (ADR-029), versionamento `/api/v1` por rota (ADR-030), testes arquiteturais próprios com MSTest (ADR-031), recriação controlada do banco de testes (ADR-032) e `Date`/`Intl` no frontend (ADR-034).
- Substitui o contêiner Nginx e a imagem web separada pela hospedagem dos arquivos estáticos da SPA no ASP.NET Core (ADR-033), sem criar o projeto Angular nesta mudança.
- Preserva ADR-003, ADR-007, ADR-011, ADR-012, ADR-013 e ADR-015 como histórico, com substituições parciais registradas; ADR-027 e ADR-028 permanecem inalterados.
- Registra a lista oficial de tecnologias como fonte autoritativa. FluentAssertions não será utilizado (Assert nativo do MSTest, ADR-036); Vitest, axe-core, k6, OWASP ZAP e Swashbuckle/OpenAPI permanecem como ferramentas complementares autorizadas por decisão explícita.
- Padroniza as respostas de erro com `Microsoft.AspNetCore.Mvc.ProblemDetails` nativo e `Extensions` para `code`, `traceId` e `errors`, preservando integralmente o contrato HTTP/JSON do ADR-017 (ADR-035). A migração depende de gate obrigatório de comparação de serialização e não remove o código atual antes da verificação.
- Não introduz novos requisitos funcionais: todo comportamento especificado tem origem na SPEC, no PRD ou no TDD.
- **BREAKING**: nenhuma interface existente, pois o projeto ainda não possui código.

## Capabilities

### New Capabilities

- `platform-foundation`: estrutura da solução, isolamento multi-tenant, convenções de persistência e API, design system Bootstrap, observabilidade, health checks, pipeline independente de hospedagem e artefatos de container.
- `identity-and-access`: cadastro separado de cliente e barbeiro, login compartilhado, sessão por cookie, CSRF, bloqueio por tentativas, autorização por perfil e por recurso.
- `barbershop-and-professionals`: criação e edição da barbearia, código de convite, entrada de profissional, gestão de vínculos e dados da empresa.
- `service-catalog`: catálogo modelo, cadastro, edição e desativação lógica de serviços, com preço e duração preservados em agendamentos.
- `availability-and-hours`: horários gerais de funcionamento, disponibilidade semanal, bloqueios pontuais e cálculo de horários disponíveis.
- `booking-and-agenda`: agendamento público e manual com confirmação automática, reserva sem conflito, agenda por dia/semana/mês e consulta de agendamentos.
- `appointment-lifecycle`: cancelamento com autor e data, remarcação com histórico, sugestão de novo horário com reserva de 24 horas e expiração.
- `service-delivery-and-payments`: início e fim do atendimento com horários reais, não comparecimento e registro de pagamento presencial.
- `customer-relationships`: consulta de clientes vinculados à barbearia por agendamentos e edição dos próprios dados pelo cliente.
- `operational-dashboard`: indicadores de hoje, semana, cancelamentos e recebidos no mês, além de próximos agendamentos, isolados por barbearia.
- `internal-notifications`: notificações internas por evento de agendamento, consulta por polling e processos periódicos idempotentes com rastreabilidade no TDD.

### Modified Capabilities

Nenhuma: o projeto ainda não possui specs em `openspec/specs/`.

## Impact

- **Documentação**: `docs/TDD.md` atualizado nas seções de premissas, visão de contêineres, stack, estrutura, contrato de API, fuso horário, testes, infraestrutura, ADRs e rastreabilidade; ADR-029 a ADR-036 registrados. Nenhum ADR foi removido; os anteriores seguem como histórico.
- **Código**: criação de `server/src` (.NET 10, `.slnx`), `server/tests` e `client` (Angular 22 + Bootstrap 5), todos ainda inexistentes. Nenhum scaffold frontend foi antecipado nesta mudança.
- **APIs**: superfície `/api/v1` conforme TDD, com versionamento nativo por rota e sem versão anterior a preservar.
- **Dependências**: lista oficial de tecnologias registrada no TDD §6.3; sem Angular Material, Quartz, SDK de provedor de nuvem, FluentValidation, FluentAssertions, `Asp.Versioning.Mvc`, NetArchTest, Respawn, `date-fns`, Moment.js ou Nginx. Vitest, axe-core, k6, OWASP ZAP e Swashbuckle/OpenAPI permanecem como ferramentas complementares autorizadas.
- **Operação**: hospedagem, serviço de banco, gerenciador de segredos e destino de telemetria permanecem pendentes (ADR-028); o pipeline entrega artefatos independentes do destino. A SPA será servida pelo ASP.NET Core (ADR-033), sem contêiner web separado.
- **Riscos**: reserva concorrente (ADR-010), isolamento por tenant (ADR-006), durabilidade da expiração de sugestão enquanto ADR-027 estiver pendente, integração futura dos assets Angular ao ASP.NET Core e mudança incompatível na serialização de erros, mitigada pelo gate do ADR-035.
