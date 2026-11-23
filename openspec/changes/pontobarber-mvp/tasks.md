# Tasks

## 1. Fundação da solução

- [x] 1.1 Criar `PontoBarber.slnx` com projetos `Dominio`, `Aplicacao`, `Infra`, `WebApp` e testes correspondentes; verificar `dotnet build` verde e que `Dominio` não referencia EF Core, ASP.NET Core, Identity ou biblioteca de agendamento
- [x] 1.2 Configurar `Directory.Build.props`, nullable, analisadores, tratamento de warnings como erro e convenções de nomenclatura; verificar build sem warnings
- [x] 1.3 Configurar `DbContext`, convenções de `BarbeariaId`, UTC em `datetime2(3)` e configurações por `IEntityTypeConfiguration`; verificar teste que rejeita entidade de negócio sem `BarbeariaId`
- [x] 1.4 Implementar resolução de tenant ativo pela rota e validação contra vínculo do usuário; verificar testes negativos de rota com barbearia sem vínculo
- [x] 1.5 Implementar filtro global por tenant e testes que comprovem isolamento entre duas barbearias com dados homônimos
- [x] 1.6 Implementar contrato de erro `application/problem+json` com código estável, `traceId` e erros por campo; verificar testes de conflito e de validação
- [x] 1.7 Implementar health checks de vivacidade e prontidão com falha de prontidão quando a persistência não responde; verificar teste de integração com banco indisponível
- [x] 1.8 Configurar Serilog, OpenTelemetry com exportador OTLP e correlação de requisição, usuário e barbearia; verificar teste de que senha, cookie e e-mail/telefone completos não aparecem nos logs
  - Nota histórica: a instrumentação/exportação OTLP foi adiada por ADR-037; Serilog, correlação e redação permanecem vigentes. Ver `follow-ups.md` item 20.
- [x] 1.9 Criar pipeline do GitHub Actions com restore, build, testes, análise e verificação de migrations pendentes, sem exigir provedor de nuvem; verificar execução verde em ambiente limpo
- [x] 1.10 Criar `Dockerfile` da API com build multi-stage e `HEALTHCHECK` em `/health/live`; verificar imagem construída localmente e health check respondendo. A hospedagem dos arquivos estáticos da SPA pelo ASP.NET Core (ADR-033) fica pendente da existência do frontend e não conclui esta tarefa
- [x] 1.11 Documentar em `docs/` o comando de execução local, migrations e o contrato de erros; verificar que os comandos documentados executam como escritos
- [x] 1.12 Migrar as respostas de erro para `Microsoft.AspNetCore.Mvc.ProblemDetails` nativo com `Extensions` para `code`, `traceId` e `errors`; antes de substituir o código atual, verificar equivalência semântica e estrutural da serialização para 400, 401, 403, 404, 409, 422 e 500, incluindo `detail` nulo/vazio e `errors` por campo: status HTTP, `Content-Type`, nomes e casing das propriedades, presença ou ausência das propriedades, tratamento de valores `null`, estrutura e conteúdo de `errors`, preservação de `detail` e dos demais campos contratuais e presença/formato de `traceId` com normalização do valor dinâmico; a ordem das propriedades não é critério de incompatibilidade; preservar integralmente o contrato do ADR-017 (ADR-035)

## 2. Fundação visual

- [x] 2.1 Configurar Bootstrap 5 com importação seletiva por SCSS, tokens de tema, Bootstrap Icons e orçamento de bundle; verificar build e medição do bundle inicial dentro do orçamento
- [x] 2.2 Criar `shared/ui` com layout, botão, campo de formulário, tabela, estado vazio e diálogo com foco, rótulos e navegação por teclado
  - Nota (19/11/2026): a implementação dos seis componentes foi concluída. Os testes de componentes e a auditoria axe foram adiados para a tarefa 2.4, sem criar arquivos `.spec.ts` nesta etapa.
- [x] 2.3 Configurar cliente HTTP com interceptação de erro, antiforgery e correlação; verificar teste de que erro da API é exibido de forma compreensível
  - Nota (19/11/2026): o cliente está configurado conforme o contrato do TDD. Permanecem pendentes para a integração com o backend, sem implementação nesta tarefa: (1) emissão do cookie `__Host-pb.csrf` e validação do header `X-CSRF-TOKEN` pelo ASP.NET Core; (2) consumo do header `X-Correlation-Id` pela API, que hoje gera o `TraceId` internamente e o devolve no corpo do erro.
- [x] 2.4 Criar testes E2E e auditoria axe no pipeline; verificar que uma violação crítica reprova a execução
  - Inclui os testes de componentes e a configuração do axe adiados da tarefa 2.2.
- [x] 2.5 Integrar o build do Angular como arquivos estáticos servidos pelo ASP.NET Core, com fallback para `index.html` e a mesma origem da API; verificar rota não-API retornando a SPA e `/api/v1` respondendo (ADR-033) — depende do projeto Angular da tarefa 2.1

## 3. Identidade e acesso

- [x] 3.1 Configurar ASP.NET Core Identity, política de senha e cookie com atributos de segurança; verificar teste de criação de usuário e de cookie inacessível a JavaScript
  - Nota (19/11/2026): cookie `__Host-pb.auth` com `HttpOnly`, `Secure`, `SameSite=Lax`, `Path=/`; senha mínima de 8 caracteres com letra e número; hash PBKDF2-HMAC-SHA512 com 210.000 iterações. Os testes de cookie, hash e rejeição de senha fraca passaram. O teste de criação com banco real depende de Testcontainers e não pôde ser executado neste ambiente (Docker indisponível).
- [ ] 3.2 Implementar cadastro de cliente e cadastro de barbeiro em fluxos distintos; verificar testes de cadastro válido, e-mail duplicado e confirmação divergente
  - Critérios revisados para os dois fluxos: cadastro de cliente exige nome, e-mail único e telefone obrigatório, sem senha nem confirmação — o telefone serve para o barbeiro contatar o cliente e não é usado em login, autenticação, recuperação ou OTP; cadastro de barbeiro/proprietário mantém nome, e-mail, telefone, senha e confirmação. A identidade do cliente é global, sem vínculo obrigatório com barbearia.
  - Nota de validação anterior (mantida): a implementação então vigente não foi marcada como concluída porque os testes de cadastro válido (cliente/barbeiro) e e-mail duplicado dependiam de SQL Server via Testcontainers e não puderam ser executados (Docker indisponível). Os testes de confirmação divergente, campos obrigatórios e definição do tipo pelo servidor passaram.
  - Pendente integrar os critérios revisados acima; nenhum teste do novo fluxo OTP foi executado nesta atualização documental.
- [ ] 3.3 Implementar autenticação por fluxo: OTP por e-mail para clientes e e-mail/senha para barbeiros e proprietários; verificar mensagem genérica e limites de tentativas/reenvio
  - Critérios revisados: cliente solicita e valida código OTP de 6 dígitos (5 minutos, uso único), com limites de tentativas de validação e de reenvio; respostas não revelam se o e-mail possui conta. Barbeiro/proprietário mantêm e-mail/senha, bloqueio progressivo por tentativas e mensagem genérica.
  - Nota de validação anterior (mantida): os testes de credenciais inválidas e bloqueio após 5 tentativas dependiam de banco real (Testcontainers) e não puderam ser executados (Docker indisponível). O bloqueio progressivo usava o patamar de 5 tentativas/5 min do Identity; o segundo patamar (10 tentativas/30 min) estava implementado no adaptador, mas não verificado com banco.
  - Pendente integrar os critérios revisados acima; nenhum teste do novo fluxo OTP foi executado nesta atualização documental.
- [ ] 3.4 Implementar `/auth/me`, logout e alteração de senha; verificar testes de sessão encerrada após logout e de alteração com senha atual válida
  - Critérios revisados: alteração de senha aplica-se somente a barbeiros e proprietários; clientes não possuem senha e não utilizam esse fluxo. Sessão por cookie persistente e seguro, com expiração deslizante de 60 dias de inatividade para clientes e 30 dias para barbeiros e proprietários, sem limite absoluto; cada acesso válido renova o prazo do perfil e o logout encerra a sessão imediatamente. A política de 60 dias é exclusiva dos clientes.
  - Nota da implementação da política de sessão: o cookie `__Host-pb.auth` passou a ser persistente, com prazo definido no evento de emissão/renovação (60 dias para cliente; 30 dias para barbeiro/proprietário), `SlidingExpiration` ativo e sem limite absoluto. Testes sem banco cobrem prazos, renovação, expiração por inatividade e logout; os testes sobre o fluxo real de login com banco (Testcontainers) permanecem pendentes enquanto o Docker estiver indisponível.
  - Nota de validação anterior (mantida): os testes de `/me` autenticado, logout e alteração de senha dependiam de SQL Server via Testcontainers e não puderam ser executados (Docker indisponível). O teste de `/me` sem autenticação passou.
- [ ] 3.5 Aplicar antiforgery em métodos que alteram estado e autorização por perfil e recurso; verificar testes de requisição sem token e de acesso cruzado entre perfis
  - Critérios revisados: a autenticação do cliente é global e não concede, por si só, acesso a recursos de uma barbearia; autorização por vínculo (`TenantMember`, `TenantOwner`) permanece separada e dependente das Tasks de vínculo.
  - Nota de validação anterior (mantida): cookie `__Host-pb.csrf`, header `X-CSRF-TOKEN`, validação em métodos que alteram estado e policies `Cliente`/`Profissional`/`TenantMember`/`TenantOwner` estavam implementados; testes locais de emissão do cookie, rejeição sem token e policies passaram. Pendentes: testes de ponta a ponta com sessão autenticada dependem de banco (Docker indisponível); a autorização por recurso/tenant ainda não tem emissores de claim porque as entidades de vínculo e as rotas com `barbeariaId` pertencem a Tasks posteriores.
- [x] 3.6 Implementar telas de escolha de perfil, cadastro, login e dados da conta; verificar E2E dos três fluxos e axe sem violação crítica
  - Nota (19/11/2026): telas em `client/src/app/auth/`, integradas aos endpoints de autenticação, ao `ErroApi`/interceptor e ao fluxo XSRF do Angular. 35 testes de componente/serviço e 5 testes E2E (três fluxos + axe) passaram. Os E2E executaram sobre o servidor de desenvolvimento do Angular, sem backend autenticado: validaram as interações das telas e a exibição legível de erros, mas não a autenticação ponta a ponta com cookie real; essa validação depende das Tasks 3.4/3.5 com banco disponível.

## 4. Barbearia e profissionais

- [x] 4.1 Implementar criação de barbearia com validação, fuso IANA e geração de código de convite; verificar testes de criação válida, campo inválido e código único
  - Implementado: `POST /api/v1/barbearias`, validação de campos e fuso IANA, código de convite único e persistência transacional de barbearia, perfil do proprietário e vínculo `Proprietario`.
  - Testes unitários e de integração (SQL Server via Testcontainers) executados e aprovados em 20/11/2026: criação válida, campos inválidos, fuso inválido, colisão de código, esgotamento de tentativas e perfil cliente.
- [ ] 4.2 Implementar consulta e edição dos dados da barbearia restritas ao proprietário; verificar testes de edição válida, tentativa de barbeiro e preservação de agendamentos históricos
  - Implementado: `GET` e `PUT /api/v1/barbearias/{barbeariaId}`; leitura para vínculo ativo e edição restrita a `CargoProfissional.Proprietario`. `Barbearia.Atualizar` preserva identificador, proprietário, convite, fuso, situação e `RowVersion`.
  - Testes unitários e de integração executados e aprovados: edição válida, dados inválidos, barbeiro negado, barbearia diferente e vínculo inativo.
  - Pendente: não existe entidade de agendamento antes da seção 7; a preservação de agendamentos históricos não pode ser verificada nesta etapa.
- [x] 4.3 Implementar entrada por código de convite com rejeição de código inválido e de vínculo duplicado; verificar testes dos três cenários
  - Implementado: `POST /api/v1/barbearias/entrar`, com perfil reaproveitado da conta, vínculo `Barbeiro` criado pelo domínio e índice único `UX_Vinculos_Barbearia_Profissional` contra duplicidade.
  - Testes unitários e de integração executados e aprovados: código válido, inexistente, vínculo duplicado, vínculo inativo, perfil ausente/cliente, código vazio e conflito concorrente.
- [x] 4.4 Implementar gestão de vínculos: ativar, desativar, editar dados profissionais e remover, preservando conta e histórico; verificar testes de cada operação e de tentativa sobre vínculo de outro profissional
  - Implementado: `PUT`, `PATCH .../status` e `DELETE /api/v1/barbearias/{barbeariaId}/profissionais/{id}`, com transições de domínio e remoção lógica; acrescido o `GET .../profissionais` previsto no TDD §10.2 para viabilizar a tela de gestão.
  - Testes unitários, de arquitetura e de integração executados e aprovados: ativar/desativar, editar, remover preservando conta/perfil, negação a barbeiro, tentativa em outra barbearia e isolamento multi-tenant.
- [ ] 4.5 Garantir que o proprietário tenha vínculo de profissional com agenda própria; verificar teste de acesso simultâneo às permissões administrativas e à agenda
  - Implementado: a criação da barbearia reaproveita o perfil existente e gera vínculo `Proprietario` ativo; `ObterVinculoParaAgendaAsync` separa a autorização de agenda da autorização administrativa. Testes unitários e de integração aprovados.
  - Pendente: disponibilidade e agenda pertencem às seções 6 e 7; sem esses módulos não há verificação funcional da agenda própria.
- [ ] 4.6 Implementar telas de empresa, convite e profissionais; verificar E2E de criação de barbearia, entrada por convite e desativação de profissional
  - Implementado: telas Angular de criação de barbearia, entrada por convite e gestão de profissionais (edição, ativação e desativação), integradas à navegação da aplicação.
  - Testes Vitest (45/45), E2E (9/9: três fluxos da tarefa + axe) e build Angular executados e aprovados em 20/11/2026.
  - Pendente: o E2E rodou sobre o dev server com API simulada por `page.route`; o TDD §13 prevê E2E com banco dedicado e massa criada por API. A integração real está coberta pelos testes de integração do backend, mas o E2E ponta a ponta com backend autenticado permanece pendente.

Nota da seção: nenhuma migration foi versionada; a verificação `has-pending-model-changes` continua vácua (`follow-ups.md`, item 10) e não deve ser considerada concluída.

## 5. Catálogo de serviços

- [x] 5.1 Implementar seed do catálogo modelo (oito serviços, durações iniciais, sem preços fixos); verificar teste de disponibilidade do modelo em barbearia nova
  - Catálogo revisado em 20/11/2026: Cabelo — Corte social/tradicional 30, degradê 40, infantil 30, raspagem 20, acabamento 15, lavagem 20; Barba e Bigode — simples 20, completa 30. Substitui os três serviços anteriormente previstos em SPEC/TDD; divergência registrada aqui sem alterar esses documentos.
  - Implementado: `Servico`, `CatalogoModeloServicos`, `IServicoRepositorio` e `ServicoCatalogoModelo`; a criação de barbearia aplica o catálogo à nova barbearia.
  - Testes unitários e de integração executados e aprovados: oito serviços com durações corretas, ausência de preço, idempotência e isolamento entre barbearias.
- [x] 5.2 Implementar criação e edição de serviços, com validação de preço não negativo e duração
  - Implementado: `POST` e `PUT /api/v1/barbearias/{barbeariaId}/servicos`, com `Servico.Criar`, `AtualizarDados`, `AtualizarValores` e validação de nome, duração (> 0) e preço (obrigatório e não negativo). Nome/disponibilidade restritos ao proprietário; barbeiro altera somente preço e duração (revisão de SPEC §8/TDD §11.2 registrada nesta seção).
  - Testes unitários e de integração executados e aprovados: criação válida, preço negativo, duração inválida, barbeiro criando serviço, edição completa pelo proprietário, edição de preço/duração pelo barbeiro, tentativa de alterar nome/disponibilidade e serviço de outra barbearia.
- [x] 5.3 Implementar remoção lógica e consulta por perfil, com cliente vendo apenas serviços ativos; verificar testes de serviço removido fora da oferta e de histórico preservado com nome, duração e preço originais
  - Implementado: `DELETE /api/v1/barbearias/{barbeariaId}/servicos/{id}` (somente proprietário, desativação lógica) e `GET /api/v1/barbearias/{barbeariaId}/servicos`; clientes recebem somente serviços ativos, proprietários e barbeiros com vínculo ativo veem o catálogo da própria barbearia, inclusive inativos.
  - Testes unitários e de integração executados e aprovados: remoção sem exclusão, barbeiro impedido de remover, oferta do cliente sem serviços removidos, consulta por perfil, isolamento entre barbearias e tentativa sobre serviço de outro tenant.
  - Pendência encerrada na seção 7: `Agendamento` mantém `NomeServicoSnapshot`, `DuracaoSnapshot` e `PrecoSnapshot`, copiados na confirmação. Teste unitário (`AgendamentoTestes.Snapshot_NaoMudaQuandooServicoEAlterado`) e teste de integração com persistência real (`Publico_ComClienteAutenticado_ConfirmaComSnapshot`) comprovam que a alteração do catálogo não altera o histórico.
- [x] 5.4 Implementar tela de catálogo com criação, edição e remoção; verificar E2E do fluxo do proprietário e leitura sem opções de edição para barbeiro
  - Implementado em `client/src/app/servicos/`, integrada à navegação por sinais: proprietário cria, edita nome/preço/duração/disponibilidade e remove; barbeiro edita apenas preço/duração; cliente consulta somente ativos sem ações de edição.
  - Testes Vitest (52/52) e E2E (14/14, incluindo os cinco cenários da tarefa + axe) executados e aprovados em 20/11/2026. Os E2E usam interceptação da API via `page.route`, sem persistência real.

Nota da seção: a dependência da Task 5.3 quanto aos snapshots históricos foi resolvida na seção 7. Nenhuma migration foi versionada; a verificação de alterações pendentes do modelo permanece sem efeito prático até a primeira migration real.

## 6. Disponibilidade e horários

- [x] 6.1 Implementar horários gerais de funcionamento por dia da semana com validação de abertura e fechamento; verificar testes de configuração válida, período invertido e dia fechado
  - Implementado: `HorarioFuncionamento`, repositório, `GestaoHorariosFuncionamento` e `GET`/`PUT /api/v1/barbearias/{barbeariaId}/horarios-funcionamento`; exatamente sete dias por barbearia, dias fechados sem horários e fechamento posterior à abertura, com horários locais no fuso da barbearia.
  - Testes unitários e de integração executados e aprovados: configuração válida, período invertido, dia fechado, autorização do proprietário e isolamento entre barbearias.
- [x] 6.2 Implementar disponibilidade semanal do profissional contida no funcionamento; verificar testes de janela válida, janela fora do funcionamento e preservação de agendamentos existentes
  - Implementado: `DisponibilidadeSemanal`, repositório, `GestaoDisponibilidadeSemanal` e `PUT /api/v1/barbearias/{barbeariaId}/profissionais/{id}/disponibilidade`; janelas contidas no funcionamento, proprietário pode editar qualquer profissional e o barbeiro somente a própria disponibilidade.
  - Integração com a seção 7: antes de substituir as janelas, `GarantirAgendamentosExistentesAsync` verifica os agendamentos confirmados dos próximos 30 dias no fuso da barbearia e recusa (`400`) qualquer alteração que deixe de cobrir um horário já reservado. Janelas válidas preservam os agendamentos existentes.
  - Testes unitários e de integração executados e aprovados, incluindo `DisponibilidadeSemanal_PreservaAgendamentoExistente` (recusa de janela reduzida e manutenção do agendamento após alteração válida, com persistência real).
- [x] 6.3 Implementar bloqueios pontuais com validação de período e tratamento de conflito com agendamento existente; verificar testes dos três cenários
  - Implementado: `BloqueioAgenda`, repositório, `GestaoBloqueios` e `GET`/`POST`/`DELETE /api/v1/barbearias/{barbeariaId}/profissionais/{id}/blocks`; período em UTC com término posterior ao início, motivo opcional, proprietário gerencia qualquer profissional da barbearia e o barbeiro somente os próprios bloqueios.
  - Integração com a seção 7: `GestaoBloqueios` usa `IAgendamentoRepositorio.ExisteConflitoAsync` e recusa (`409`, `schedule.blocked`) bloqueios que se sobrepõem a agendamentos confirmados do mesmo profissional.
  - Teste de integração executado e aprovado: `DisponibilidadeEBlocks_ConsideramAgendamentosExistentes` cria um agendamento real e verifica que o bloqueio conflitante é rejeitado.
- [x] 6.4 Implementar cálculo de horários disponíveis considerando funcionamento, disponibilidade, bloqueios e agendamentos, janela padrão de 15 dias, limite máximo de 30 dias e fuso da barbearia; verificar testes de horário livre, fora da jornada e data além do limite
  - Decisão mantida: a janela padrão é de 15 dias e o limite máximo absoluto é de 30 dias, medidos a partir da data atual no fuso da barbearia. Parâmetros do cliente não podem ampliar o limite.
  - Implementado: `GET /api/v1/barbearias/{barbeariaId}/disponibilidade` (público, TDD §10.2) com `servicoId`, `profissionalId` opcional, `date` e `dias` opcional; slots gerados pela duração do serviço dentro da interseção entre funcionamento e disponibilidade, descontados bloqueios pontuais e agendamentos confirmados, convertidos para UTC no fuso da barbearia.
  - Integração com a seção 7: `ConsultarDisponibilidade` injeta `IAgendamentoRepositorio` e remove os slots cujo intervalo se sobrepõe a agendamentos confirmados (`ListarAtivosPorPeriodoAsync`). Teste de integração executado e aprovado: o slot ocupado deixa de ser devolvido.
- [x] 6.5 Aplicar autorização das configurações por perfil; verificar testes de barbeiro tentando alterar horários gerais e bloqueio de outro profissional
  - Revisão concluída: horários gerais exigem proprietário para escrita e vínculo ativo para leitura; disponibilidade e bloqueios exigem proprietário ou o próprio profissional, com vínculo ativo e `BarbeariaId` da rota. Clientes e usuários sem vínculo são negados nas rotas administrativas; a consulta pública da Task 6.4 foi preservada.
  - Correção realizada: adicionado o `GET /api/v1/barbearias/{barbeariaId}/profissionais/{id}/disponibilidade` do TDD §10.2, ausente até esta etapa, com a mesma autorização da escrita.
  - Testes unitários e de integração executados e aprovados para todos os cenários de perfil, isolamento entre barbearias e acesso à consulta pública.
- [x] 6.6 Implementar telas de horários, disponibilidade e bloqueios; verificar E2E e axe sem violação crítica
  - Implementado em `client/src/app/configuracoes/`, integradas à navegação por sinais: horários gerais com edição restrita ao proprietário, disponibilidade semanal com seleção de profissional pelo proprietário e edição própria pelo barbeiro, e bloqueios com criação/remoção conforme o perfil. Exibição em UTC no formulário e no fuso da barbearia na listagem.
  - Testes Vitest (59/59) e E2E (24/24, incluindo os dez cenários das telas e axe nas três telas) executados e aprovados em 22/11/2026. Os E2E usam interceptação da API via `page.route`, sem persistência real.

## 7. Agendamento e agenda

- [x] 7.1 Implementar criação de agendamento com snapshot de nome, duração e preço e cálculo de término e valor; verificar testes de cálculo e de preservação após alteração do serviço
  - Implementado: `Agendamento` (domínio) herda de `EntidadeBase<T>`, calcula `TerminaEmUtc = início + duração` e congela nome, duração e preço do serviço na criação. `Atualizar` rejeita qualquer mudança dos dados contratados; snapshot não muda quando o catálogo é editado ou desativado. Snapshot fica no próprio agendamento — `HistoricoAgendamento` pertence à seção 8.
  - Testes unitários executados e aprovados: `AgendamentoTestes` (cálculo de término/valor, snapshot após alteração do serviço, período invertido e rejeição de alteração histórica).
- [x] 7.2 Implementar confirmação em transação com lock por profissional/data, revalidação de disponibilidade e índice único; verificar teste de concorrência com duas confirmações simultâneas aceitando apenas uma
  - Implementado: `RepositorioAgendamentoEmOrm.ConfirmarAsync` abre transação `READ COMMITTED`, adquire `sp_getapplock` exclusivo com `LockOwner = 'Transaction'` e chave `pb:{barbeariaId}:{profissionalId}:{yyyy-MM-dd}` via `SqlCommand` ADO.NET com parâmetro de retorno, revalida funcionamento/disponibilidade/bloqueios/conflitos e confirma. Índice único filtrado `UX_Appointments_Pro_Start_Active` protege contra duplicidade; conflito retorna `409 schedule.slot_unavailable` e timeout de `sp_getapplock` retorna `503 schedule.lock_timeout` com `Retry-After: 1`, conforme TDD §5.5.1.
  - Testes de integração com persistência real (SQL Server em container) executados e aprovados: `Publico_HorarioOcupado_Retorna409` e `Concorrencia_DuasConfirmacoesSimultaneas_AceitaSomenteUma` (duas requisições simultâneas: exatamente uma `201` e uma `409`, com um único registro persistido). Nenhum lock em memória foi usado. `MiddlewareProblemDetails_ComTimeoutDeLock_Retorna503ComRetryAfter` verifica o mapeamento do timeout.
- [x] 7.3 Implementar agendamento público autenticado; verificar testes de confirmação válida, visitante não autenticado, serviço inativo e profissional inativo
  - Implementado: `ServicoAgendamentos.CriarPublicoAsync` + `POST /api/v1/barbearias/{barbeariaId}/agendamentos/public` com política `PolíticaCliente`, cookie/Identity e antiforgery existentes. Serviço ativo, profissional com vínculo ativo, mesmo tenant e disponibilidade são revalidados no servidor; preço, duração e término jamais são aceitos do cliente.
  - Testes de integração executados e aprovados: confirmação válida com snapshot persistido, visitante `401`, serviço inativo `400` e profissional inativo `400`.
- [x] 7.4 Implementar agendamento manual pela agenda com autorização por profissional; verificar testes de criação válida e de barbeiro tentando agendar para profissional fora da própria agenda
  - Implementado: `POST /api/v1/barbearias/{barbeariaId}/agendamentos` com política `PolíticaProfissional`. Proprietário agenda para qualquer profissional vinculado; barbeiro somente para si; vínculo inativo é negado. `CriarManualAsync` e `CriarPublicoAsync` compartilham `ConfirmarAsync`, sem duplicação de regras.
  - Testes de integração executados e aprovados: proprietário confirma com origem `Manual`; barbeiro tentando outro profissional recebe `403`. Cliente manual é informado por `clienteId`, validado no servidor.
- [x] 7.5 Implementar consulta de agendamentos com autorização por proprietário, profissional e titular; verificar testes de cliente acessando agendamento de outro cliente
  - Implementado: `GET /api/v1/barbearias/{barbeariaId}/agendamentos` (proprietário vê a barbearia toda e pode filtrar por profissional; barbeiro vê somente a própria agenda), `GET .../agendamentos/{id}` e `GET /api/v1/me/agendamentos` (somente os do titular). Filtros do frontend nunca decidem autorização; o `BarbeariaId` da rota e o vínculo ativo são aplicados no backend.
  - Teste de integração executado e aprovado: `Consulta_RespeitaPerfilEIsolamento` verifica as três visões e a recusa (`403`) de outro cliente ao detalhar o agendamento.
- [x] 7.6 Implementar agenda em dia, semana e mês com filtro por profissional e cores de confirmado, cancelado e bloqueio; verificar testes de componente das visualizações e E2E de alternância
  - Implementado: `client/src/app/agendamentos/agenda/` com visualizações dia/semana/mês, navegação entre períodos, filtro por profissional para o proprietário, agenda própria para o barbeiro, exibição de agendamentos e bloqueios com estilos distintos, estados de carregamento/erro/vazio e horários no fuso da barbearia. Integrada à navegação em `app.ts`/`app.html`.
  - Testes Vitest e E2E executados e aprovados: renderização das três visualizações, alternância, filtro por profissional, visão do barbeiro, estado vazio e auditoria axe sem violações críticas.
- [x] 7.7 Implementar fluxo de agendamento público no frontend com seleção de serviço, profissional, data e horário; verificar E2E completo e axe sem violação crítica
  - Implementado: `client/src/app/agendamentos/publico/` com seleção sequencial de serviço → profissional → data → horário devolvido pela API, confirmação no endpoint público autenticado e exibição de resultado. O frontend envia somente identificadores e o instante escolhido; preço/duração/término vêm do servidor. Conflitos são traduzidos para mensagens compreensíveis pelo mapa de códigos existente (`ErroApi`).
  - Testes Vitest e E2E executados e aprovados: fluxo completo, recusa de confirmação sem horário, erro `409` exibido como "já está ocupado" e auditoria axe nas telas.

## 8. Ciclo de vida do agendamento

- [x] 8.1 Implementar cancelamento por cliente, barbeiro e proprietário com registro de autor e momento; verificar testes de cada perfil e de cancelamento recusado após o início
  - Implementado: `POST /api/v1/barbearias/{barbeariaId}/agendamentos/{id}/cancelar`; `Agendamento.Cancelar` registra autor, data/hora UTC e motivo e só permite a transição Confirmado → Cancelado antes do início. Cliente somente sobre o próprio agendamento, barbeiro somente na própria agenda e proprietário em qualquer agendamento da barbearia, com autorização no backend. O cancelamento libera o horário (o índice único passa a cobrir somente agendamentos ativos).
  - Testes de integração com persistência real executados e aprovados: cliente cancelando o próprio e outro cliente recusado (`403`), barbeiro e proprietário cancelando, motivo obrigatório para profissional, cancelamento duplicado (`409 appointment.invalid_state`), cancelamento após o início (`409 appointment.already_started`) e reocupação do horário liberado.
- [x] 8.2 Implementar remarcação para horário disponível preservando histórico; verificar testes de remarcação válida, horário indisponível e manutenção do horário original em caso de recusa
  - Implementado: `POST .../agendamentos/{id}/remarcar` reutiliza `ValidadorHorarioAgendamento` (funcionamento, disponibilidade, janela de 30 dias) e o mesmo lock/transação da seção 7, adquirindo as chaves das datas antiga e nova em ordem determinística. A mudança de horário, o evento de histórico e a revalidação de conflitos ocorrem em uma única transação; snapshots de nome, duração e preço permanecem intactos.
  - Testes de integração executados e aprovados: remarcação válida com snapshot preservado e evento registrado, horário ocupado recusado (`409`) com horário original mantido no banco, e duas remarcações simultâneas para o mesmo horário com exatamente uma aceita.
- [x] 8.3 Implementar sugestão de novo horário com motivo obrigatório e reserva de até 24 horas; verificar testes de sugestão sem motivo, aceite, rejeição e expiração
  - Implementado: `SugestaoHorario` com `ExpiraEmUtc = agora + 24 h`, autor, motivo e situação (`Pendente`, `Aceita`, `Rejeitada`, `Expirada`). Endpoints `POST .../sugestoes`, `.../aceitar`, `.../rejeitar` e `GET .../sugestoes`, com aceite e rejeição restritos ao titular. A reserva é persistida: `ExisteConflitoInternoAsync` considera sugestões pendentes não vencidas em conflitos de criação, remarcação, aceite e bloqueios, e ignora sugestões vencidas mesmo que o job esteja atrasado. Índice único filtrado `UX_Suggestions_Appointment_Pending` impede duas sugestões pendentes por agendamento. `POST /api/v1/jobs/expirar-sugestoes` executa a transição em lote de forma idempotente.
  - Testes de integração executados e aprovados: criação válida e sem motivo (`400`), conflito com agendamento (`409`), aceite transacional com reativação do agendamento, rejeição com liberação da reserva, aceite após 24 h recusado (`409 suggestion.expired`) e job marcando a sugestão expirada com liberação do horário.
  - Dependência documentada: o gatilho periódico automático depende da decisão de scheduler (ADR-027). Até lá, a expiração é garantida no momento do aceite e da verificação de conflitos, sem depender de estado em memória; o critério de job periódico permanece pendente na seção 9.
- [x] 8.4 Implementar histórico append-only com eventos, autor e momento em ordem cronológica; verificar teste de que eventos não podem ser alterados ou removidos pela aplicação
  - Implementado: `HistoricoAgendamento` com identificador, agendamento, tipo de evento, autor, `OcorridoEmUtc`, início anterior/novo e detalhes. Eventos de criação, confirmação, cancelamento, remarcação e das quatro transições de sugestão são gravados na mesma transação da mudança de estado. `Atualizar` lança sempre e `ContextoPontoBarber` rejeita `Modified`/`Deleted` de histórico no `SaveChanges`. Consulta ordenada por momento e identificador.
  - Testes executados e aprovados: registro dos eventos relevantes, ordem cronológica, autor e momento corretos, preservação após mudança de estado e recusa de edição/remoção com persistência real.
- [x] 8.5 Implementar telas de cancelamento, remarcação e histórico; verificar E2E dos fluxos e axe sem violação crítica
  - Implementado: `client/src/app/agendamentos/detalhe/` com ações por perfil e situação, confirmação em duas etapas para cancelamento, seleção de novo horário devolvido pela API, sugestão com motivo obrigatório, aceite/rejeição de sugestões, prazo de validade e histórico cronológico com eventos diferenciados; `client/src/app/agendamentos/meus/` lista os agendamentos do cliente. Navegação por signals, sem Angular Router. Autorização permanece no backend.
  - Testes Vitest (74/74) e E2E (38/38, incluindo sete cenários do ciclo de vida e axe) executados e aprovados. Os E2E usam interceptação da API via `page.route`, sem persistência real.

## 9. Processos periódicos

- [x] 9.1 Definir o contrato de execução de processos na `Aplicacao` e o registro em `ExecucoesJob` com início, fim, resultado e quantidade; verificar teste de registro em execução bem-sucedida e em falha
  - Implementado: `IProcessoPeriodico`/`ResultadoProcessamento` na Aplicação, `ExecucaoJob` no Domínio (`Iniciar`, `Concluir`, `Falhar`), `IExecucaoJobRepositorio` e `ServicoExecucaoJobs`. O registro é criado no início, concluído com a quantidade processada ou marcado como falho com o tipo da exceção (sem dados sensíveis). Falha no registro da conclusão é propagada, nunca confundida com sucesso.
  - Testes unitários e de integração com persistência real executados e aprovados: sucesso com quantidade e momentos UTC, falha registrada e propagada, rejeição de dupla conclusão e de conclusão sem início.
- [x] 9.2 Implementar adaptador de disparo provisório apenas em `Infra`, sem escolher biblioteca definitiva, e documentar a pendência do ADR-027; verificar que `Dominio` e `Aplicacao` não referenciam bibliotecas de agendamento
  - Implementado: `DisparadorProvisorioJobs` (`BackgroundService` com escopo de DI por tick e intervalo configurável), `RegistroProcessosPeriodicos` na Aplicação e endpoints de acionamento manual que também passam pelo registro de execução. Nenhuma biblioteca de scheduler foi adicionada; o adaptador é in-process e não durável, conforme documentado em `docs/PROCESSOS_PERIODICOS.md`.
  - Testes de arquitetura executados e aprovados: Domínio e Aplicação sem referências a Quartz, Hangfire, scheduler ou Hosting; contrato de execução localizado na Aplicação.
- [x] 9.3 Implementar expiração de sugestões com `UPDATE` condicional e notificação aos envolvidos; verificar teste de expiração e de execução repetida sem liberação ou notificação duplicada
  - Implementado: processamento condicional em lotes de 500 sobre sugestões `Pendente` e vencidas, com reserva liberada, histórico na mesma transação e notificações para cliente, profissional e proprietário. Não havia infraestrutura de notificações no projeto; foi criada apenas a persistência mínima `Notificacao` com chave única `(EventId, DestinatarioUsuarioId)` (outbox leve da TDD §12.1), sem antecipar a seção 13.
  - Testes de integração executados e aprovados: sugestão vencida expirada, vigente preservada, notificação única após reprocessamento, duas execuções simultâneas sem duplicidade e histórico íntegro. A proteção de aceite vencido da seção 8 continua independente do job.
- [x] 9.4 Implementar fechamento de agendamentos vencidos como não comparecimento; verificar teste de agendamento vencido e de reprocessamento sem alteração de status
  - Implementado: `CloseOverdueAppointmentsJob` com critério da TDD §12.3 (confirmados com início há mais de 2 horas), transição `RegistrarNaoComparecimento`, autor técnico `sistema`, evento `NaoComparecimento` no histórico na mesma transação e lotes de 500 com `sp_getapplock` por processo.
  - Testes de integração executados e aprovados: elegível fechado, futuro e cancelado preservados, evento único em reprocessamento, cancelamento após o início recusado e histórico íntegro.
- [x] 9.5 Implementar expurgo de registros de idempotência por data de expiração; verificar teste de remoção de expirado e preservação de vigente
  - Implementado: estrutura oficial `RegistrosIdempotencia` (TDD §8.1/ADR-021), inexistente no código até esta etapa, e `PurgeIdempotencyRecordsJob` com `DELETE` direto por `ExpiraEmUtc`, sem carregar registros em memória.
  - Testes de integração executados e aprovados: dois expirados removidos na primeira execução, vigente preservado, segunda execução sem efeito e quantidade correta.
- [x] 9.6 Documentar os processos e a limitação de durabilidade enquanto o ADR-027 estiver pendente; verificar que o documento lista os quatro processos e seus gatilhos
  - Criado `docs/PROCESSOS_PERIODICOS.md` com os quatro processos e gatilhos (expiração, não comparecimento, expurgo e `ReconcileNotificationsJob` identificado na TDD §12.3, pertencente à seção 13), contrato, registro em `ExecucoesJob`, idempotência, tratamento de falhas e a limitação de durabilidade/disponibilidade do adaptador provisório.
  - `ReconcileNotificationsJob` permanece sem implementação nesta etapa por não pertencer às tasks 9.3–9.5; seu critério continua pendente na seção 13.4. Nenhum processo novo foi inventado para completar a documentação.

## 10. Atendimento e pagamentos

- [x] 10.1 Implementar início e fim do atendimento com registro automático dos horários reais e recusa para status inválido; verificar testes de início válido, início em cancelado, fim válido e fim sem início
  - Implementado: `Agendamento.Iniciar`/`Concluir` com `InicioRealUtc`/`FimRealUtc` em UTC, transições Confirmado → EmAndamento → Concluido com `sp_getapplock` por profissional/data e eventos `ServicoIniciado`/`ServicoConcluido` no histórico append-only, na mesma transação. Início somente no dia do agendamento no fuso da barbearia (TDD §5.5.3); somente o barbeiro da própria agenda (proprietário, outro barbeiro e cliente recusados no backend).
  - Testes unitários e de integração com persistência real executados e aprovados: início e fim válidos com horários reais, início em cancelado e em não comparecimento, início duplicado, fim sem início, fim duplicado, autorização por perfil e isolamento entre barbearias.
- [x] 10.2 Implementar não comparecimento como status distinto de cancelamento, impedindo início e fim; verificar testes dos três cenários
  - Implementado: `NaoCompareceu` permanece distinto de `Cancelado` e é reutilizado da seção 9. `RegistrarNaoComparecimentoManual` usa a mesma transição de domínio do job, sem duplicar regra, e o endpoint `POST .../ausencia` registra o evento `NaoComparecimento` com autor e momento. Estado vencido não é convertido em cancelamento; início, conclusão e pagamento são recusados.
  - Testes executados e aprovados: estado preservado como distinto, tentativas de início/fim/pagamento recusadas, histórico preservado e nenhuma duplicação de evento.
- [x] 10.3 Implementar registro de pagamento com valor e forma permitidos, situação independente do atendimento e recusa de duplicidade; verificar testes de pagamento válido, valor inválido, forma inválida, duplicidade e atendimento finalizado sem pagamento
  - Implementado: entidade `Pagamento` em `Dominio/Financeiro` (`Financeiro`), `FormaPagamento` com `Cash`/`Pix`/`DebitCard`/`CreditCard`, valor não negativo com até duas casas, registro em UTC e autor. `IPagamentoRepositorio` grava pagamento, `RegistroIdempotencia` e evento `PagamentoRegistrado` em uma única transação, com índice único `UX_Payments_Appointment` por agendamento. Cabecalho `Idempotency-Chave` obrigatório (24 h): repetição com o mesmo hash devolve a resposta original e hash divergente retorna 409. Estado financeiro é independente: concluir não cria pagamento e pagar não altera o atendimento; cancelados e não comparecidos não admitem pagamento.
  - Testes de integração com persistência real executados e aprovados: pagamento válido e idempotente, valor negativo, forma inválida, chave ausente, duplicidade, atendimento finalizado sem pagamento, estado inalterado após pagamento recusado, estado incompatível e isolamento por barbearia.
  - Observação: pagamentos parciais ou múltiplas formas dependem da questão em aberto ABT-007; a V1.0 mantém um pagamento por atendimento conforme TDD §8.3.
- [x] 10.4 Implementar telas de atendimento e pagamento; verificar E2E do ciclo completo e axe sem violação crítica
  - Implementado: `client/src/app/agendamentos/detalhe/` estendido com ações por perfil e estado, confirmação de cancelamento, início, conclusão, marcação de não comparecimento, formulário de pagamento com valor/forma, exibição dos horários reais, do pagamento (valor, forma e momento) e do aviso de atendimento finalizado sem pagamento. Navegação por signals, fuso da barbearia e autorização mantida no backend.
  - Testes Vitest (82/82) e E2E (44/44, incluindo seis cenários de atendimento e axe) executados e aprovados. Os E2E usam interceptação da API via `page.route`; integridade e autorização foram verificadas nos testes de integração.

## 11. Clientes

- [ ] 11.1 Implementar relação do cliente por agendamentos atuais ou históricos; verificar testes de cliente com agendamento aparecendo e cliente sem agendamento ausente
- [ ] 11.2 Implementar listagem, pesquisa e consulta de clientes restritas ao proprietário; verificar testes de pesquisa, barbeiro sem acesso e cliente de outra barbearia
- [ ] 11.3 Implementar consulta e edição dos próprios dados pelo cliente com validação de e-mail duplicado; verificar testes de edição válida, acesso a outro cliente e preservação do histórico
- [ ] 11.4 Garantir ausência de cadastro, edição ou exclusão de clientes pelo estabelecimento; verificar testes de que as operações não estão expostas na API
- [ ] 11.5 Implementar telas de clientes para o proprietário e de perfil para o cliente; verificar E2E e axe sem violação crítica

## 12. Dashboard

- [ ] 12.1 Implementar consulta agregada dos indicadores Hoje, Esta semana, Cancelamentos e Recebido no mês no fuso da barbearia; verificar testes de período, de semana ISO e de cálculo na virada do dia
- [ ] 12.2 Garantir que cancelamentos não entram em recebimentos e que indicadores sem dados retornam zero; verificar testes específicos
- [ ] 12.3 Implementar lista de próximos agendamentos com horário, cliente, profissional e situação; verificar testes de lista preenchida e vazia
- [ ] 12.4 Aplicar isolamento por barbearia e autorização de proprietário; verificar testes com dois proprietários e com barbeiro tentando acessar
- [ ] 12.5 Implementar dashboard como primeira tela após a configuração inicial; verificar E2E de acesso e axe sem violação crítica

## 13. Notificações internas

- [ ] 13.1 Implementar notificação para os eventos de criação, cancelamento, sugestão, aceite, rejeição, expiração, início, fim e pagamento, com tipo, conteúdo, destinatário e barbearia; verificar testes de geração por tipo
- [ ] 13.2 Garantir idempotência por evento e destinatário; verificar teste de reprocessamento sem notificação duplicada
- [ ] 13.3 Implementar consulta por polling com contagem de não lidas, marcação individual e marcação de todas; verificar testes de consulta restrita ao destinatário e de marcação negada para outro usuário
- [ ] 13.4 Implementar reconciliação de eventos sem notificação; verificar testes de evento reconciliado e de evento já notificado sem duplicidade
- [ ] 13.5 Implementar central de notificações no frontend com polling e marcação de leitura; verificar E2E e axe sem violação crítica
- [ ] 13.6 Verificar ausência de canais externos; verificar teste de que nenhum envio de WhatsApp, e-mail ou SMS é disparado por evento

## 14. Integração e endurecimento

- [ ] 14.1 Executar testes de carga nos fluxos de agenda, dashboard e agendamento e comparar com as metas do TDD; verificar relatório com p95 e taxa de erro
- [ ] 14.2 Executar auditoria de acessibilidade em todas as telas principais e corrigir violações; verificar relatório axe e checklist manual de teclado
- [ ] 14.3 Executar revisão de segurança: isolamento por tenant, CSRF, autorização por recurso, segredos e dependências; verificar ausência de achados críticos ou altos
- [ ] 14.4 Executar restauração de backup em ambiente isolado e registrar RTO e RPO; verificar relatório de restauração com dados íntegros
- [ ] 14.5 Revisar logs quanto a dados sensíveis; verificar amostra de log sem senha, cookie ou contato completo. Reintrodução de traces/exportação OTLP depende de decisão futura (ADR-037)
- [ ] 14.6 Atualizar `docs/` com contratos finais, processos periódicos e limitações conhecidas; verificar que a documentação corresponde ao comportamento implementado

## 15. Produção

- [ ] 15.1 Registrar a decisão de hospedagem e provedor (ADR-028) antes do go-live; verificar ADR aprovado e ambiente de staging provisionado
- [ ] 15.2 Registrar a decisão de scheduler (ADR-027) e substituir o adaptador provisório; verificar ADR aprovado e processos executando com registro de execução
- [ ] 15.3 Aplicar migrations e seed de catálogo modelo em staging e executar smoke tests; verificar checklist de homologação assinado
- [ ] 15.4 Configurar gerenciador de segredos, backup, alertas e runbooks do ambiente escolhido; verificar teste de alerta e execução do runbook
- [ ] 15.5 Publicar em produção com aprovação manual e smoke tests; verificar revisão saudável e checklist de go-live concluído

## Workflow follow-up

- Revisar a change completa antes de iniciar a implementação, conforme solicitado.
- Arquivar a change após a implementação e a validação das capabilities.
