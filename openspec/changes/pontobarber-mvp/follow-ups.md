# Pendências registradas — pontobarber-mvp

Registradas após a revisão da tarefa 1.5, sem alteração de ADRs, sem renumeração de tarefas e sem implementação nesta etapa.

1. **Testcontainers.MsSql 4.x e fixture reutilizável para testes de integração** — substituir o LocalDB usado em `TestesFiltroGlobalTenant` por SQL Server em container e extrair a criação das opções do DbContext para uma fixture reutilizável, sem reescrever os cenários. Deve ser tratado **antes da tarefa de CI (1.9 atual)**.
2. **Migração dos projetos de testes de xUnit para MSTest** — alinhar `PontoBarber.Testes.Unidade`, `PontoBarber.Testes.Integracao` e `PontoBarber.Testes.Arquitetura` ao TDD §15.1 e ADR-012. Deve ser tratada **antes da tarefa de CI (1.9 atual)**.
3. **Teste de descoberta automática do filtro global** — quando as primeiras entidades `IPertencenteTenant` reais forem criadas no Domínio, comprovar que `ContextoPontoBarber.OnModelCreating` aplica o filtro sem chamada manual a `AplicarFiltrosTenant` no teste.
4. **Propor a inclusão de `internal.error` (HTTP 500) no catálogo do TDD §11.1** — justificar sua necessidade como código estável de fallback exigido pelo ADR-017, que obriga todas as respostas de erro a usarem `ProblemDetails` com `code`. Hoje o código existe na implementação, mas não possui rastreabilidade documental.
5. **Avaliar a criação de `auth.unauthenticated`** — diferenciar a ausência de autenticação (401) de `auth.invalid_credentials` (credenciais inválidas), hoje usado como código genérico de autenticação na resposta do middleware de tenant.
6. **Verificação de jobs no health check de prontidão** — o TDD §10.2 prevê `/health/ready` com banco **e jobs**, mas a estratégia de execução de processos periódicos está pendente da decisão do ADR-027. Nenhum check de jobs foi implementado; a inclusão deve ocorrer somente após a escolha do adaptador de scheduler, sem criar verificação fictícia.
7. **Mascaramento de texto no sink OTLP — resolvido na revisão da tarefa 1.8** — a redação foi movida para `RedacaoLogEventSink`, que sanitiza mensagem, template, propriedades estruturadas e exceções antes de encaminhar o evento a qualquer destino, inclusive OTLP. Mantido apenas o acompanhamento de novos sinks que venham a ser adicionados sem passar pelo redutor.
8. **Instrumentação e amostragem adicionais** — TDD §14.2 prevê instrumentação de EF Core, HttpClient e do adaptador de scheduler, além de amostragem adaptativa de 10% em produção/100% em staging. Implementadas apenas ASP.NET Core e SQL Client, sem amostragem adaptativa; os demais itens dependem das tarefas de infraestrutura e do ADR-027.
9. **Destino de telemetria** — o endpoint OTLP é configurável por `Observabilidade:OTLP:Endpoint` e vazio por padrão; a escolha do coletor/destino permanece pendente do ADR-028.

Estas pendências não alteram as decisões dos ADRs nem o escopo aprovado das tarefas 1.5, 1.6, 1.7 e 1.8.
