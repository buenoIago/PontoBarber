# Pendências registradas — pontobarber-mvp

Registradas após a revisão da tarefa 1.5, sem alteração de ADRs, sem renumeração de tarefas e sem implementação nesta etapa.

1. **Testcontainers.MsSql 4.x e fixture reutilizável para testes de integração** — substituir o LocalDB usado em `TestesFiltroGlobalTenant` por SQL Server em container e extrair a criação das opções do DbContext para uma fixture reutilizável, sem reescrever os cenários. Deve ser tratado **antes da tarefa de CI (1.9 atual)**.
2. **Migração dos projetos de testes de xUnit para MSTest** — alinhar `UnitTests`, `IntegrationTests` e `ArchitectureTests` ao TDD §15.1 e ADR-012. Deve ser tratada **antes da tarefa de CI (1.9 atual)**.
3. **Teste de descoberta automática do filtro global** — quando as primeiras entidades `IPertencenteTenant` reais forem criadas no Domínio, comprovar que `ContextoPontoBarber.OnModelCreating` aplica o filtro sem chamada manual a `AplicarFiltrosTenant` no teste.
4. **Propor a inclusão de `internal.error` (HTTP 500) no catálogo do TDD §11.1** — justificar sua necessidade como código estável de fallback exigido pelo ADR-017, que obriga todas as respostas de erro a usarem `ProblemDetails` com `code`. Hoje o código existe na implementação, mas não possui rastreabilidade documental.
5. **Avaliar a criação de `auth.unauthenticated`** — diferenciar a ausência de autenticação (401) de `auth.invalid_credentials` (credenciais inválidas), hoje usado como código genérico de autenticação na resposta do middleware de tenant.

Estas pendências não alteram as decisões dos ADRs nem o escopo aprovado das tarefas 1.5 e 1.6.
