# Banco de perguntas para o TDD

Use este banco para conduzir a entrevista técnica. Pergunte apenas o que for relevante para o produto, o repositório e as restrições. Agrupe no máximo 5 perguntas por rodada, com opções concretas e a recomendação primeiro.

## Como usar

1. Descarte perguntas já respondidas pelo repositório ou pelo prompt.
2. Priorize decisões com maior custo de mudança.
3. Apresente opções, não perguntas abertas, quando for possível.
4. Explique o impacto de cada opção em uma frase.
5. Se o usuário delegar, decida com base nas restrições e registre um ADR.
6. Se a resposta não for crítica, registre uma premissa e avance.

## Rodada 1 — Contexto, restrições e escala

**1. Qual é o contexto de entrega?**
- Por que: define arquitetura, processos e nível de automação.
- Opções: MVP para validar rápido; produto inicial para operação real; evolução de sistema existente; reescrita.
- Recomendação: MVP com arquitetura simples e caminho de evolução claro.

**2. Qual é o prazo e o tamanho da equipe?**
- Por que: restringe tecnologia, escopo e complexidade operacional.
- Opções: 1 desenvolvedor; 2 a 4; 5 a 10; mais de 10.
- Recomendação: escolher stack que a equipe já domina e minimizar serviços operados.

**3. Qual é a escala esperada no primeiro ano?**
- Por que: dimensiona banco, cache, filas e infraestrutura.
- Opções: até 100 barbearias; até 1.000; até 10.000; acima de 10.000.
- Complemento: agendamentos por dia e pico simultâneo.

**4. Existe orçamento ou restrição de infraestrutura?**
- Por que: elimina opções caras ou exige serviços gerenciados.
- Opções: mínimo possível; serviços gerenciados moderados; sem restrição relevante.
- Recomendação: serviços gerenciados para reduzir custo operacional.

**5. Existe stack, padrão corporativo ou legado obrigatório?**
- Por que: evita retrabalho e conflito com o repositório.
- Opções: liberdade total; stack definida; integração com sistema existente.
- Complemento: linguagens, cloud, banco, autenticação e padrões já adotados.

**6. Há requisitos de conformidade ou residência de dados?**
- Por que: afeta provedor, criptografia, logs e retenção.
- Opções: LGPD básica; setor regulado; dados fora do Brasil; nenhum requisito adicional.
- Recomendação: LGPD desde o design, com minimização de dados.

## Rodada 2 — Linguagem e runtime

**1. Qual linguagem principal será usada no backend?**
- Por que: define ecossistema, bibliotecas, contratação e desempenho.
- Opções: TypeScript; Python; Go; Java/Kotlin; C#; PHP; outra.
- Recomendação: linguagem com maior domínio pela equipe; TypeScript é uma escolha comum para web com compartilhamento de tipos.

**2. Qual runtime e versão?**
- Por que: versão define recursos, suporte e compatibilidade.
- Opções: Node.js LTS; Deno; Bun; JVM LTS; Python estável; Go atual; .NET LTS.
- Recomendação: versão LTS com suporte de segurança.

**3. Qual gerenciador de pacotes e build?**
- Opções: npm; pnpm; yarn; poetry/uv; Maven/Gradle; go modules; dotnet CLI.
- Recomendação: o padrão do ecossistema e o mais simples para a equipe.

**4. A aplicação será única ou dividida por linguagem?**
- Opções: uma linguagem; backend e frontend separados; polyglot.
- Recomendação: uma linguagem no backend e uma no frontend, evitando polyglot sem necessidade.

**5. Qual é a estratégia de tipagem e validação?**
- Opções: tipagem estática; tipagem dinâmica com validação em runtime; ambas.
- Recomendação: tipagem estática e validação de entrada em runtime.

## Rodada 3 — Frameworks

**1. Qual framework de backend?**
- Por que: define estrutura, injeção de dependências, convenções e produtividade.
- Opções por ecossistema:
  - TypeScript: NestJS; Fastify; Express; AdonisJS.
  - Python: Django; FastAPI; Flask.
  - Java: Spring Boot; Quarkus.
  - C#: ASP.NET Core.
  - PHP: Laravel; Symfony.
  - Go: net/http; Gin; Echo; Fiber.
- Recomendação: framework maduro, documentado e compatível com a experiência da equipe.

**2. Qual abordagem de frontend?**
- Opções: SPA; SSR/SSG; MPA renderizada no servidor; sem frontend separado.
- Recomendação: SSR ou SPA conforme necessidade de SEO e simplicidade operacional.

**3. Qual framework de frontend?**
- Opções: React; Next.js; Vue/Nuxt; Angular; Svelte/SvelteKit; HTMX; templates server-side.
- Recomendação: React ou Next.js para interfaces ricas; server-side para equipes menores e menor complexidade.

**4. Qual biblioteca de UI e estilo?**
- Opções: Tailwind CSS; CSS Modules; styled-components; Bootstrap; Material UI; shadcn/ui; design system próprio.
- Recomendação: solução simples, acessível e consistente com a equipe.

**5. Será um monorepo ou repositórios separados?**
- Opções: monorepo; repositórios separados; aplicação única.
- Recomendação: monorepo para compartilhar tipos e contratos quando frontend e backend evoluem juntos.

## Rodada 4 — Arquitetura e estrutura

**1. Qual estilo arquitetural?**
- Opções: monólito simples; monólito modular; microsserviços; serverless; híbrido.
- Recomendação: monólito modular para o primeiro ano, com fronteiras claras e extração futura.

**2. Como os módulos serão organizados?**
- Opções: por camada técnica; por domínio/feature; por bounded context.
- Recomendação: organização por domínio, com camadas internas quando necessário.

**3. Quais são as fronteiras e dependências permitidas?**
- Por que: evita acoplamento e facilita testes.
- Complemento: definir módulos, contratos públicos e regras de importação.

**4. A API será síncrona, assíncrona ou ambas?**
- Opções: apenas REST; REST + jobs; GraphQL; RPC; eventos.
- Recomendação: REST para operações transacionais e jobs para processos demorados ou expiráveis.

**5. Como será feito o isolamento entre barbearias (multi-tenant)?**
- Opções: banco compartilhado com `tenant_id`; schema por tenant; banco por tenant.
- Recomendação: banco compartilhado com `tenant_id` e políticas de acesso obrigatórias na camada de dados.

**6. Onde ficam as regras de negócio?**
- Opções: controllers; services; camada de domínio; banco.
- Recomendação: domínio/serviços de aplicação, com controllers finos.

**7. Como será tratada a concorrência na reserva de horários?**
- Opções: validação simples; transação com lock; constraint de exclusão; fila serializada.
- Recomendação: transação com constraint ou lock que impeça sobreposição por profissional.

**8. Como datas, horários e fusos serão tratados?**
- Opções: horário local da barbearia; UTC no banco; ambos.
- Recomendação: armazenar em UTC e converter pelo fuso da barbearia.

## Rodada 5 — Dados e persistência

**1. Qual banco de dados principal?**
- Opções: PostgreSQL; MySQL; SQLite; SQL Server; MongoDB; outro.
- Recomendação: PostgreSQL para integridade relacional, transações e recursos de agendamento.

**2. Qual estratégia de acesso a dados?**
- Opções: ORM; query builder; SQL puro; híbrido.
- Recomendação: ORM para CRUD e SQL para consultas críticas, quando necessário.

**3. Qual ferramenta de migrations?**
- Opções: Prisma Migrate; Drizzle Kit; TypeORM migrations; Alembic; Flyway; Liquibase; knex.
- Recomendação: migrations versionadas no repositório e aplicadas no pipeline.

**4. Há necessidade de cache?**
- Opções: sem cache; cache em memória; Redis; CDN.
- Recomendação: adiar cache até existir gargalo mensurável.

**5. Há arquivos ou mídias?**
- Opções: sem upload; disco local; object storage; CDN.
- Recomendação: object storage com URLs assinadas quando houver upload.

**6. Qual é a política de histórico e retenção?**
- Opções: exclusão física; exclusão lógica; histórico imutável; arquivamento.
- Recomendação: preservar histórico de agendamentos e pagamentos; anonimizar dados pessoais conforme a LGPD.

**7. Qual é a estratégia de backup?**
- Opções: snapshot diário; point-in-time recovery; replicação; exportação lógica.
- Recomendação: backup automático com restauração testada.

## Rodada 6 — Bibliotecas e integrações

**1. Autenticação: própria ou provedor externo?**
- Opções: implementação própria; Auth.js; Clerk; Auth0; Firebase Auth; Keycloak; Cognito.
- Recomendação: provedor gerenciado para reduzir risco; implementação própria apenas com requisitos claros.

**2. Sessão: cookie ou token?**
- Opções: cookie httpOnly; JWT em memória; JWT em storage; sessão no servidor.
- Recomendação: cookie httpOnly, secure e sameSite para aplicações web.

**3. Quais bibliotecas de validação e contratos?**
- Opções: Zod; Yup; class-validator; Pydantic; Valibot; JSON Schema.
- Recomendação: uma biblioteca única para entrada, ambiente e contratos.

**4. Há necessidade de filas ou jobs?**
- Opções: sem fila; cron; BullMQ; pg-boss; SQS; RabbitMQ; Temporal.
- Recomendação: começar com cron ou fila simples; evoluir conforme volume.

**5. Quais integrações externas são necessárias?**
- Complemento: pagamento, WhatsApp, e-mail, SMS, verificação de telefone, calendário.
- Recomendação: integrar apenas o necessário para a V1.0; isolar cada integração atrás de uma interface.

**6. Quais bibliotecas de observabilidade?**
- Opções: logs estruturados; OpenTelemetry; Sentry; Prometheus/Grafana; serviço gerenciado.
- Recomendação: logs estruturados e rastreamento de erros desde o início.

## Rodada 7 — Infraestrutura e entrega

**1. Onde será hospedado?**
- Opções: VPS; PaaS; AWS; GCP; Azure; Cloudflare; ambiente próprio.
- Recomendação: PaaS ou serviços gerenciados para reduzir operação.

**2. Como será empacotado e implantado?**
- Opções: containers; serverless; processo gerenciado; binário.
- Recomendação: container ou PaaS com deploy reproduzível.

**3. Quais ambientes existirão?**
- Opções: local; desenvolvimento; staging; produção.
- Recomendação: local, staging e produção, com dados isolados.

**4. Qual pipeline de CI/CD?**
- Opções: GitHub Actions; GitLab CI; CircleCI; Jenkins; nenhum.
- Recomendação: pipeline com lint, tipos, testes, build e deploy automatizado.

**5. Como segredos e configurações serão gerenciados?**
- Opções: variáveis de ambiente; secret manager; arquivo criptografado.
- Recomendação: secret manager ou variáveis protegidas por ambiente.

**6. Qual estratégia de rollback?**
- Opções: redeploy da versão anterior; blue-green; canary; feature flags.
- Recomendação: deploy versionado com rollback simples e migrations compatíveis.

## Rodada 8 — Qualidade, segurança e operação

**1. Qual estratégia de testes?**
- Opções: unitários; integração; contrato; E2E; carga.
- Recomendação: pirâmide com foco em unitários e integração; E2E nos fluxos críticos.

**2. Qual meta de cobertura?**
- Opções: sem meta; 60%; 80%; 90% em domínio crítico.
- Recomendação: cobertura útil, com foco em regras de negócio e concorrência.

**3. Quais são os fluxos críticos que exigem teste?**
- Complemento: cadastro, login, agendamento público, conflito de horário, cancelamento com sugestão, pagamento, dashboard.

**4. Quais ameaças precisam de tratamento explícito?**
- Complemento: acesso entre tenants, sequestro de sessão, força bruta, injeção, CSRF, XSS, vazamento de dados, abuso de código de convite.

**5. Quais métricas e alertas operacionais?**
- Complemento: latência, erros, disponibilidade, jobs atrasados, falhas de integração, uso de banco.

**6. Qual é a estratégia de evolução do schema?**
- Opções: migrations destrutivas; expandir e contrair; versionamento de API.
- Recomendação: expandir e contrair para permitir deploy sem downtime.

## Heurísticas de recomendação

- Sem informação sobre a equipe: priorizar stack mainstream, tipada e com comunidade ativa.
- Equipe pequena: monólito modular, banco relacional gerenciado, poucos serviços.
- Prazo curto: evitar microsserviços, filas complexas e infraestrutura própria.
- Alta concorrência em reservas: transação e constraint no banco, não apenas validação na aplicação.
- Dados financeiros: PostgreSQL, transações, auditoria e idempotência.
- Requisitos de SEO: SSR/SSG no frontend.
- Necessidade de evolução futura: interfaces e fronteiras claras, sem antecipar distribuição.

## Sequência sugerida

1. Contexto, prazo, equipe e escala.
2. Linguagem, runtime e frameworks.
3. Estilo arquitetural, multi-tenancy e concorrência.
4. Banco, persistência e migrations.
5. Autenticação, autorização e integrações.
6. Infraestrutura, CI/CD e observabilidade.
7. Testes, segurança e metas de qualidade.
8. Confirmação das decisões e registro dos ADRs.

Se o usuário responder "decida por mim", escolha a opção recomendada compatível com as restrições, registre a decisão e siga sem nova rodada.
