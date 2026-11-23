# Relatório de integração e endurecimento (Seção 14)

Registro das validações executadas sobre o estado atual do sistema. Os números
abaixo foram obtidos em 23/11/2026, em ambiente local isolado. Critérios que
dependiam de infraestrutura indisponível estão marcados como tal — nenhum
resultado foi simulado.

## 14.1 — Testes de carga

**Ferramenta:** k6 não está instalado neste ambiente (`k6` não encontrado no
PATH). Foi utilizado um harness em C# com `HttpClient` concorrente e
`WebApplicationFactory`, executando em banco SQL Server LocalDB exclusivo
(`PontoBarberCarga_*`), sem tocar em produção. O harness está em
`server/tests/PontoBarber.Testes.Integracao/TestesCarga.cs` e é marcado com
`[TestCategory("Carga")]`.

**Dados representativos:** 1 barbearia, 11 perfis profissionais (dono + 10),
vínculos ativos, 7 dias de funcionamento (9h–18h), 10 disponibilidades semanais
e 140 agendamentos confirmados distribuídos em 14 dias.

| Fluxo | Requisições | Concorrência | p50 | p95 | p99 | Erros | Vazão | Meta TDD |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Consulta da agenda | 200 | 10 | 57 ms | 120 ms | 453 ms | 0 (0%) | ~121 req/s | p95 ≤ 2 s / 50 req/s |
| Consulta do dashboard | 100 | 10 | 44 ms | 407 ms | 480 ms | 0 (0%) | ~116 req/s | p95 ≤ 2 s / 10 req/s |
| Criação/confirmação | 100 | 5 | 122 ms | 191 ms | 518 ms | 0 (0%) | ~36 req/s | p95 ≤ 3 s / 20 req/s |

**Conclusão:** todas as metas de latência e vazão foram atendidas. Não foi
identificado gargalo comprovado; nenhum cache, fila ou mecanismo de processamento
foi introduzido. Limitação: as medições usam o pipeline em processo do
`WebApplicationFactory`, não uma topologia deployment/APM real (ADR-028 pendente).

## 14.2 — Acessibilidade

- A suíte E2E executa axe (`@axe-core/playwright`, tags WCAG 2.0/2.1 A e AA)
  em autenticação, barbearia, profissionais, configurações, agenda, clientes,
  dashboard, notificações e fluxos de agendamento. Resultado: **0 violações críticas**.
- Foram adicionadas verificações automatizadas de teclado em
  `client/e2e/teclado.spec.ts`: nomes acessíveis nos botões de navegação, foco
  após `Tab`, indicador visual de foco e ordem de tabulação no formulário de
  perfil. Ambos os cenários passaram.
- Limitação: não foi realizada uma inspeção manual de teclado por pessoa; as
  verificações automatizadas não garantem acessibilidade completa.

## 14.3 — Segurança

| Verificação | Evidência | Resultado |
| --- | --- | --- |
| Isolamento por tenant | Testes de integração com duas barbearias em agenda, dashboard, clientes, pagamentos e notificações | Sem dados cruzados |
| Autorização por recurso | Barbeiro sem acesso a dashboard/clientes; cliente sem acesso a outro cliente | `403`/recusa confirmada |
| CSRF | `PostSemAntiforgery_Retorna400` e middleware antiforgery | Requisições sem token rejeitadas |
| Segredos versionados | Busca em arquivos rastreados por senha, chave, token e padrões de conexão | Nenhum segredo real encontrado |
| Configuração local | `.gitignore` cobre `.env`, `.env.*`, `appsettings.*.local.json`, `secrets.json`, `*.pfx`, `*.key`, `*.pem` | Adequado |
| Modelo de ambiente | `.env.example` criado com placeholders sem valores válidos | Adequado |
| Dependências .NET | `dotnet list package --vulnerable --include-transitive` | 0 vulneráveis nos 7 projetos |
| Dependências frontend | `npm audit --omit=dev` | 0 vulnerabilidades (info/low/moderate/high/critical) |

Nenhum achado crítico ou alto. OWASP ZAP não está disponível neste ambiente;
a auditoria automatizada de dependências foi complementada com revisão de código
e testes de integração, não com varredura dinâmica de vulnerabilidades.

## 14.4 — Restauração de backup

Não existe serviço de banco gerenciado ou backup corporativo definido (ADR-028
pendente), portanto não havia backup de produção utilizável. Foi executada uma
restauração real em instância **isolada** do SQL Server LocalDB:
`server/tests/PontoBarber.Testes.Integracao/TestesRestauracao.cs`.

- Banco: `PontoBarberRestore_*`, criado e utilizado exclusivamente pelo teste.
- Dados sintéticos: 1 usuário, 1 barbearia, 1 agendamento.
- Backup `BACKUP DATABASE ... TO DISK` seguido de inserção de um pagamento
  marcador; restauração `WITH REPLACE` em modo single-user.
- **RTO:** 3,71 s (limite documentado: 2 h).
- **RPO:** 4,94 s (intervalo entre o backup e a alteração marcadora; o limite
  documentado é 15 min).
- **Integridade após restauração:** usuários=1, barbearias=1, agendamentos=1,
  pagamentos=0 (o registro posterior ao backup foi corretamente descartado).

Nenhum banco compartilhado ou de produção foi alterado; nenhum dado de produção
foi usado. Este teste valida o procedimento técnico, não a política de backup
do provedor final, que permanece pendente.

## 14.5 — Revisão de logs

- Cadeia de redação confirmada em `Program.cs`: `PoliticaRedacaoSensitive`,
  `CorrelacaoLogEnricher` e `RedacaoLogEventSink` cobrem e-mail, telefone,
  senha, token, cookie, autorização, API key, connection string e bearer.
- Amostra inspecionada: 2.811 linhas do log de testes de integração da Seção 13.
- Padrões buscados e **não encontrados**: `Senha123`, `P@ssw0rd`,
  `__Host-pb.xsrf`, `X-CSRF-TOKEN`, `@exemplo.com`, `11999999999`.
- Nenhum log registra corpo de requisição de login, OTP ou cookie de sessão.
- Nenhuma integração de traces/OTLP foi adicionada (ADR-037 mantido).

## 14.6 — Documentação

- `docs/PROCESSOS_PERIODICOS.md`: `ReconcileNotificationsJob` passou de
  “não implementado” para “Implementado na Seção 13”, com descrição do outbox
  leve, idempotência e lock de job.
- `.env.example`: modelo de variáveis de ambiente sem valores reais.
- Este relatório consolida contratos, evidências, limitações e procedimentos
  de operação/recuperação validados nesta seção.
- `docs/EXECUCAO-LOCAL.md` e `docs/TDD.md` permanecem como fontes oficiais de
  execução local e metas de desempenho; não foram reescritos.

## Limitações gerais

- k6 e OWASP ZAP indisponíveis: carga medida com harness local e segurança
  auditada por Dependências + testes de integração, não por scan dinâmico.
- Provedor de nuvem, backup gerenciado e topologia de produção indefinidos
  (ADR-028/ABT-014); RTO/RPO reais de produção não podem ser afirmados.
- Verificação de teclado é automatizada, não manual.
- Cache, filas, traces e OTLP não foram introduzidos.
