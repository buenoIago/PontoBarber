# Pendências registradas — pontobarber-mvp

Registradas após a revisão da tarefa 1.5, sem alteração de ADRs, sem renumeração de tarefas e sem implementação nesta etapa.

1. **Testcontainers.MsSql 4.x e fixture reutilizável para testes de integração** — substituir o LocalDB usado em `TenantGlobalFilterTests` por SQL Server em container e extrair a criação das opções do DbContext para uma fixture reutilizável, sem reescrever os cenários. Deve ser tratado **antes da tarefa de CI (1.9 atual)**.
2. **Migração dos projetos de testes de xUnit para MSTest** — alinhar `UnitTests`, `IntegrationTests` e `ArchitectureTests` ao TDD §15.1 e ADR-012. Deve ser tratada **antes da tarefa de CI (1.9 atual)**.
3. **Teste de descoberta automática do filtro global** — quando as primeiras entidades `ITenantOwned` reais forem criadas no Domínio, comprovar que `PontoBarberDbContext.OnModelCreating` aplica o filtro sem chamada manual a `ApplyTenantFilters` no teste.

Estas pendências não alteram as decisões dos ADRs nem o escopo aprovado da tarefa 1.5.
