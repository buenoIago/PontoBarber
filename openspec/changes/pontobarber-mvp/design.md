# Design

## Context

Ver `proposal.md` para a motivação. O projeto é greenfield: não existe `src/`, `tests/` nem histórico de commits com código. A única fonte técnica é `docs/TDD.md`, já corrigido para refletir o estado real das decisões:

- Stack fixada: .NET 10 (`.slnx`), Angular 22 standalone zoneless, SQL Server 2022 + EF Core 10, ASP.NET Core Identity por cookie (ADR-001 a ADR-007).
- UI: Bootstrap 5 + SCSS + Bootstrap Icons (ADR-026); ADR-008 rejeitado.
- Scheduler: pendente (ADR-027); ADR-009 rejeitado.
- Hospedagem, provedor, banco gerenciado, segredos e destino de telemetria: pendentes (ADR-028); ADR-013/025 seguem propostos com escopo reduzido.
- Multi-tenancy por banco único com `BarbeariaId` e filtros globais (ADR-006, aceito).

O design abaixo cobre apenas o "como" que não está fixado nos documentos e que precisa de decisão antes da implementação.

## Goals / Non-Goals

**Goals:**

- Definir a estrutura de solução e as fronteiras de camadas que sustentam as 11 capabilities sem acoplamento a provedor.
- Fixar os mecanismos transversais de isolamento por tenant, identidade, erros e concorrência de agenda.
- Definir a abstração de processos periódicos que permita implementar as regras de negócio sem escolher a biblioteca de agendamento.
- Definir o contrato de notificações internas e a estratégia de polling.
- Manter o pipeline construível e verificável sem provedor de nuvem configurado.

**Non-Goals:**

- Escolher scheduler, provedor de nuvem, banco gerenciado, gerenciador de segredos ou destino de telemetria.
- Implementar WhatsApp, e-mail, SMS ou qualquer canal externo.
- Implementar integrações de pagamento online: o pagamento é registrado presencialmente.
- Definir números de capacidade, custo ou RTO/RPO antes da decisão de hospedagem.
- Criar requisitos novos além dos rastreáveis em SPEC/PRD/TDD.

## Decisions

### 1. Monólito modular com Clean Architecture

Solução única `server/PontoBarber.slnx` com `Dominio`, `Aplicacao`, `Infra`, `Api` e projetos de teste. `Dominio` não referencia EF Core, ASP.NET Core, Identity nem bibliotecas de agendamento. `Aplicacao` expõe casos de uso por feature e depende de abstrações; `Infra` implementa persistência, identidade e adaptadores.

- **Por quê:** o TDD já fixa o estilo (ADR-003); manter as referências unidirecionais é o que viabiliza trocar o adaptador de scheduler e o destino de execução sem tocar em regra de negócio.
- **Alternativa descartada:** separar serviços por capability. Aumentaria custo operacional e contraria o monólito modular aprovado.

### 2. Isolamento multi-tenant em três camadas

1. `BarbeariaId` obrigatório nas entidades de negócio.
2. Filtro global no `DbContext` alimentado pelo tenant ativo da requisição.
3. Validação explícita na camada de aplicação: o `BarbeariaId` vem da rota e é confrontado com o vínculo ativo do usuário.

Consultas de dashboard e de clientes filtram por `BarbeariaId` e período mesmo com filtro global.

- **Por quê:** defesa em profundidade (ADR-006). O filtro global sozinho é insuficiente se o tenant ativo for resolvido de forma incorreta; a validação por vínculo cobre isso.
- **Alternativa descartada:** aceitar `BarbeariaId` do corpo da requisição — permite escalada de privilégio.

### 3. Identidade com cookie e proteção CSRF

ASP.NET Core Identity com cookie persistente `HttpOnly`, `Secure`, `SameSite=Lax`, expiração deslizante por perfil (60 dias de inatividade para clientes; 30 dias para barbeiros e proprietários), sem limite absoluto, e antiforgery obrigatório em métodos que alteram estado. `/auth/me` devolve perfil, vínculos e permissões derivadas. Cadastros de cliente e barbeiro são fluxos distintos sobre a mesma tabela de usuários; o perfil não é escolhido livremente após o cadastro. A autenticação também é distinta: barbeiros e proprietários usam e-mail e senha; clientes usam código OTP de 6 dígitos enviado por e-mail, com validade de 5 minutos, uso único e limites de tentativas e reenvio. A identidade do cliente é global, sem vínculo obrigatório com barbearia.

- **Por quê:** decisões ADR-005 e ADR-016 já aprovadas; cookie evita token em storage e o antiforgery cobre a superfície restante.
- **Alternativa descartada:** JWT em `localStorage`. Risco de XSS e reimplementação de revogação.

### 4. Reserva de horário com lock por profissional/data e índice único

Confirmação em transação: lock de aplicação `pb:schedule:{tenantId}:{profissionalId}:{yyyyMMdd}` no fuso da barbearia, revalidação de disponibilidade, verificação de sobreposição e índice único para duplicidade exata. O mesmo caminho serve ao agendamento público, ao manual e à remarcação.

- **Por quê:** o risco principal do produto é double booking sob concorrência (ADR-010). O índice único é a última barreira; o lock reduz conflito e melhora a mensagem de erro.
- **Alternativa descartada:** apenas verificação em leitura antes do insert — janela de corrida entre leitura e commit.

### 5. Processos periódicos por abstração + idempotência

`Aplicacao` define o contrato de execução (nome do processo, execução assíncrona, resultado com quantidade processada e erros) e cada processo é um caso de uso idempotente. `Infra` contém somente o adaptador que dispara esses casos de uso; a escolha do adaptador permanece pendente (ADR-027). Cada execução grava registro em `ExecucoesJob` com início, fim, resultado e quantidade. Os processos deste MVP são os rastreáveis no TDD §12.3: expiração de sugestão, fechamento de agendamentos vencidos, expurgo de idempotência e reconciliação de notificações.

- **Por quê:** satisfaz RN-AGD-015 a RN-AGD-017 sem compromisso prematuro de biblioteca, e mantém o domínio livre de dependência de agendamento.
- **Consequência explícita:** a garantia de execução durável e coordenada entre instâncias não pode ser afirmada até a decisão. O código não deve depender dela: cada processo precisa ser correto mesmo sob execução repetida ou simultânea.
- **Fora do escopo desta decisão:** recálculo de projeções de dashboard (otimização, não requisito) e verificação de saúde periódica (coberta por health checks).

### 6. Notificações internas no mesmo commit + polling

Eventos de domínio são coletados no `DbContext` e materializam notificações na mesma transação, com idempotência por `EventId` e destinatário. O frontend consulta por polling com contagem de não lidas e ações de marcar como lida. Não há broker externo.

- **Por quê:** o TDD §11.3 já define esse caminho; gravar a notificação na mesma transação elimina a necessidade de outbox completo neste escopo. A reconciliação cobre eventos sem notificação.
- **Alternativa descartada:** broker externo — custo operacional desproporcional ao MVP.

### 7. UI com Bootstrap e design system próprio

Bootstrap 5 importado por módulos (reboot, grid, utilities e componentes usados), tema por custom properties e SCSS, ícones por Bootstrap Icons. Componentes compartilhados — diálogo, campo de formulário, tabela, layout, estado vazio — em `shared/ui`, com foco, rótulos, teclado e contraste sob responsabilidade do projeto. A agenda usa CSS Grid.

- **Por quê:** ADR-026 aprovado. A importação seletiva controla o bundle e evita dependência de componentes Angular.
- **Consequência:** acessibilidade e comportamento de overlay não são herdados; exigem testes axe e revisão manual por tela.

### 8. Contrato de erro e observabilidade neutros de provedor

Erros em `application/problem+json` com código estável, `traceId` de correlação e erros por campo. Logs estruturados com Serilog em JSON no console, com correlação e redação de dados sensíveis. Instrumentação, métricas, traces e exportação OTLP foram adiados por ADR-037 até decisão de destino em ADR-028. Nenhum SDK de provedor entra em `Dominio`/`Aplicacao`; sinks proprietários não são obrigatórios.

- **Por quê:** permite trocar destino junto da decisão de hospedagem (ADR-028) sem reescrever código.
- **Alternativa descartada:** SDK de APM proprietário como dependência direta — lock-in prematuro.

### 9. Pipeline independente de destino

GitHub Actions executa restore, build, testes, análise, build de imagem e verificação de migrations sem conhecer o provedor. A publicação é etapa fina, parametrizada por ambiente, que consome a imagem publicada e os segredos do ambiente-alvo. Migrations são aplicadas como etapa separada e verificadas por ausência de alteração pendente no modelo.

- **Por quê:** atende RNF-DISPONIBILIDADE-001 sem antecipar ADR-028; a maior parte do pipeline permanece útil qualquer que seja o destino.
- **Alternativa descartada:** pipeline específico de um provedor desde o início — descartado quando o destino for diferente.

### 10. Estratégia de dados e histórico

Instantes em UTC (`datetime2(3)`), horários de funcionamento como `TimeOnly` local, fuso IANA na barbearia. Preço, duração e nome do serviço são copiados para o agendamento no momento da contratação. O histórico do agendamento é append-only. Remoções de serviço, vínculo e cliente são lógicas.

- **Por quê:** preserva indicadores e auditoria mesmo após alterações cadastrais (RF-SERV-008, RN-CLI-007).

### 11. Substituições de mecanismos e hospedagem da SPA

Mecanismos previstos no TDD que nunca foram adotados em código passam a ter decisões próprias; os ADRs anteriores são preservados como histórico com substituição parcial:

- Validação de entrada com DataAnnotations e erros em `application/problem+json` (ADR-029).
- Versionamento nativo por `/api/v1`, sem `Asp.Versioning.Mvc` (ADR-030).
- Testes arquiteturais próprios com MSTest e reflexão, sem NetArchTest (ADR-031).
- Isolamento com banco SQL Server exclusivo criado e removido por teste, sem Respawn (ADR-032).
- SPA servida como arquivos estáticos pelo ASP.NET Core, sem Nginx e sem imagem web separada (ADR-033). TLS/HSTS permanecem em ADR-028.
- Datas no frontend: `date-fns` / `@date-fns/tz` → `Date` e `Intl` nativos, utilizando `Intl.DateTimeFormat` e a opção `timeZone` com identificadores IANA da barbearia (ADR-034).
- Respostas de erro com `Microsoft.AspNetCore.Mvc.ProblemDetails` nativo e `Extensions` para `code`, `traceId` e `errors`, mantendo o contrato do ADR-017 (ADR-035).
- Assertivas com `Assert` nativo do MSTest; FluentAssertions não será utilizado (ADR-036).

- **Por quê:** reduzir dependências nunca adotadas, alinhar-se à lista oficial de tecnologias, manter o contrato `/api/v1` explícito e manter API + SPA na mesma origem, preservando cookies `Secure` e antiforgery.
- **Consequência:** a integração dos assets depende da criação futura do projeto Angular; nenhum scaffold foi criado nesta mudança. ADR-027 e ADR-028 não foram alterados.
- **Ferramentas complementares:** Vitest, axe-core, k6, OWASP ZAP e Swashbuckle/OpenAPI são mantidos por decisão explícita; nenhuma ferramenta semelhante será adicionada sem autorização.

## Risks / Trade-offs

- **Double booking sob concorrência** → lock por profissional/data, revalidação na transação, índice único e testes de concorrência dedicados.
- **Vazamento entre tenants** → filtro global, validação por vínculo ativo e testes negativos por capability.
- **Expiração de sugestão não durável enquanto ADR-027 está pendente** → processos idempotentes, registro de execução e alerta de atraso; a limitação fica documentada e não é mascarada como garantia.
- **Acessibilidade sem biblioteca de componentes** → primitivas acessíveis em `shared/ui`, axe no CI e checklist manual por tela.
- **Bundle acima do orçamento com Bootstrap** → importação seletiva e medição no pipeline.
- **Custo e operação indefinidos** → nenhum número de capacidade ou custo é afirmado antes de ADR-028; a decisão ocorre antes do go-live.
- **Notificação perdida em falha pós-commit** → idempotência por `EventId` e reconciliação periódica.
- **Migrations em ambiente sem provedor definido** → verificação de modelo pendente no CI e aplicação parametrizada no deploy.
- **Integração futura dos assets da SPA** → hospedagem pelo ASP.NET Core documentada em ADR-033; o build Angular usa o Angular CLI 22 e o Node.js 22 LTS (22.22.3 em `client/.nvmrc`, conforme TDD §6.1).
- **Formatação de datas dependente do navegador** → `Intl` com fusos IANA e testes em três fusos, incluindo horário de verão (ADR-034).
- **Mudança incompatível na serialização de erros** → gate obrigatório de comparação entre a serialização atual e a proposta com `ProblemDetails` nativo, preservando o contrato do ADR-017 (ADR-035, tarefa 1.12).

## Migration Plan

Não há sistema legado nem dados a migrar; a V1.0 inicia com banco vazio.

1. Criar solução, projetos, convenções e pipeline antes da primeira feature.
2. Aplicar migrations desde o ambiente local; cada migration é revisada e versionada no repositório.
3. Seed do catálogo modelo global (Corte 40 min, Barba 30 min, Corte + Barba 70 min), sem preços fixos.
4. Homologação em staging com dados sintéticos; produção após checklist de go-live.
5. **Rollback:** retorno à revisão anterior no ambiente escolhido; migrations escritas no padrão expandir/contrair para conviver com duas versões durante a janela de deploy.

## Open Questions

- Qual adaptador de scheduler será adotado (ADR-027)? Não altera specs nem a abstração; altera apenas `Infra` e a garantia de durabilidade.
- Qual provedor, serviço de banco, gerenciador de segredos e destino de telemetria (ADR-028)? Não altera `Dominio`/`Aplicacao` nem o pipeline até a etapa de publicação.
- Quais conteúdos e agrupamentos mínimos das notificações internas (ABT-008 no PRD)? A estrutura de tipos já está definida; o texto pode evoluir sem mudar contrato.
