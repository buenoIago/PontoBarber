---
name: generate-tdd
description: Cria um Technical Design Document detalhado, com entrevista de decisões arquiteturais, stack, estrutura, dados, APIs, segurança, testes e operação. Use quando o usuário pedir TDD, design técnico, arquitetura, escolha de linguagem, framework ou bibliotecas.
---

# Generate TDD

Produza um **Technical Design Document (TDD)** detalhado e acionável, que centraliza as decisões técnicas do projeto, explica os trade-offs e serve como referência única para implementação, revisão e operação.

## Quando usar

Use esta skill quando o usuário pedir para:

- Criar um TDD, documento de design técnico ou documento de arquitetura.
- Definir linguagem, framework, bibliotecas, banco de dados ou infraestrutura.
- Registrar decisões arquiteturais e estruturais antes de implementar.
- Detalhar a solução técnica a partir de um PRD, SPEC ou ideia de produto.

## Entradas

- **Prompt inicial** (obrigatório): contexto do produto, restrições, stack existente ou decisões já tomadas.
- **Documentos de produto** (quando existirem): `docs/PRD.md`, `docs/SPEC.md`, `README.md`, `AGENTS.md`.
- **Repositório**: manifests, lockfiles, configurações, código, migrations, specs e ADRs existentes.
- **Restrições do usuário**: prazo, equipe, orçamento, cloud, compliance, escala esperada e preferências de stack.

Se o prompt inicial estiver vazio, peça uma descrição mínima do produto, do contexto técnico e das restrições antes de continuar.

## Fluxo de trabalho

### 1. Levantar o contexto

Antes de perguntar qualquer coisa, investigue o que o repositório já responde:

- `AGENTS.md`, `README.md` e instruções do projeto.
- `docs/PRD.md`, `docs/SPEC.md`, `docs/TDD.md` e documentos equivalentes.
- `openspec/specs/`, `openspec/changes/` e ADRs existentes.
- Manifestos e lockfiles (`package.json`, `pnpm-lock.yaml`, `pyproject.toml`, `go.mod`, `Cargo.toml`, `composer.json`, `*.csproj`).
- Configurações de build, containers, CI/CD, banco, lint, testes e deploy.
- Estrutura de diretórios e código existente, apenas para entender o que já foi decidido.

Classifique cada decisão em:

- **Já resolvida pelo repositório**: apenas registre e cite a evidência.
- **Parcialmente resolvida**: confirme a intenção e complete os detalhes.
- **Em aberto**: precisa de pergunta ao usuário.
- **Não aplicável**: registre o motivo.

Não contradiga decisões existentes sem apontar o conflito e obter confirmação.

### 2. Extrair os direcionadores técnicos

A partir do PRD/SPEC e do prompt, organize:

- Requisitos funcionais que mais influenciam a arquitetura.
- Requisitos não funcionais: desempenho, disponibilidade, segurança, privacidade, acessibilidade, compatibilidade e retenção.
- Volumetria esperada: barbearias, usuários, agendamentos por dia, picos de acesso.
- Necessidades de consistência e concorrência, especialmente reserva de horários.
- Integrações previstas e futuras.
- Restrições de equipe, prazo, orçamento, cloud e compliance.
- Decisões já registradas no repositório.

### 3. Conduzir a entrevista técnica

Leia `references/question-bank.md` e use-o como guia. Pergunte **antes** de escrever o documento.

Regras da entrevista:

- Use a ferramenta `question` quando disponível.
- Faça no máximo 5 perguntas por rodada, agrupadas por tema.
- Priorize decisões que mudam a arquitetura ou o custo de mudança.
- Sempre ofereça opções concretas, com a recomendação primeiro e o motivo.
- Não pergunte o que o repositório já responde; confirme apenas se houver ambiguidade.
- Adapte as perguntas ao produto; ignore categorias irrelevantes.
- Quando o usuário delegar a decisão, escolha com base nas restrições, registre a justificativa e siga.
- Encerre a entrevista quando as decisões restantes puderem ser registradas como premissas ou pontos em aberto.

Cobertura mínima da entrevista:

1. Linguagem, runtime e versão.
2. Framework de backend e de frontend, quando aplicável.
3. Estilo arquitetural e limites entre módulos.
4. Banco de dados, persistência e estratégia de migrations.
5. Bibliotecas que definem a arquitetura: ORM, validação, autenticação, filas, cache, UI, testes e observabilidade.
6. Autenticação, autorização e modelo de multi-tenant.
7. Infraestrutura, deploy, ambientes e CI/CD.
8. Estratégia de testes e qualidade.
9. Segurança, privacidade e conformidade.
10. Escala, desempenho e custo operacional.

### 4. Fechar as decisões arquiteturais

Para cada decisão relevante, crie um ADR com:

- ID estável (`ADR-001`, `ADR-002`...).
- Título curto e específico.
- Status: `Proposto`, `Aceito`, `Substituído` ou `Rejeitado`.
- Contexto e forças envolvidas.
- Decisão tomada, com versões e nomes exatos.
- Alternativas consideradas e motivo da rejeição.
- Consequências positivas, negativas e custos de reversão.
- Requisitos e componentes afetados.

Leia `references/adr-template.md` e registre os ADRs no próprio TDD. Decisões ainda não confirmadas devem permanecer como `Proposto` e aparecer nos pontos em aberto.

### 5. Projetar a solução

Descreva a arquitetura em níveis úteis para implementação:

- **Contexto**: atores, sistemas externos e fronteiras.
- **Contêineres**: aplicações, banco, cache, fila, armazenamento e serviços externos.
- **Componentes**: módulos internos, responsabilidades e dependências permitidas.

Inclua diagramas Mermaid quando ajudarem. Modele explicitamente:

- Fronteiras dos módulos e regras de dependência.
- Fluxo de dados das jornadas críticas.
- Transações, locks, idempotência e tratamento de concorrência.
- Multi-tenancy e isolamento de dados.
- Fuso horário, datas e horários.
- Processos assíncronos, jobs e expirações.
- Estratégia de erros, retries e compensações.

### 6. Detalhar a stack e as bibliotecas

Produza uma tabela com:

| Item | Versão | Papel | Justificativa | Alternativas rejeitadas | Risco/licença |
| --- | --- | --- | --- | --- | --- |

Regras:

- Informe versões ou faixas compatíveis; não use "a definir" sem registrar como ponto aberto.
- Inclua apenas bibliotecas com papel claro; evite dependências redundantes.
- Explique por que a biblioteca foi escolhida e o que foi descartado.
- Registre licenças e riscos relevantes.
- Evite detalhes de implementação que não sejam decisões estruturais.

### 7. Especificar dados, APIs e segurança

**Dados:**

- Modelo conceitual e relacional.
- Entidades, campos, chaves, índices e restrições.
- Estratégia de migrations e versionamento do schema.
- Integridade, transações e histórico.
- Retenção, arquivamento e exclusão.

**APIs e interfaces:**

- Estilo (REST, GraphQL, RPC) e versionamento.
- Recursos, operações, autenticação e autorização.
- Contratos de entrada e saída, validação e erros.
- Idempotência, paginação, filtros e limites.
- Eventos e contratos assíncronos.

**Segurança:**

- Modelo de autenticação e sessão.
- Autorização por perfil e por recurso.
- Proteção de dados, segredos e comunicação.
- Ameaças relevantes e mitigações.
- Privacidade, LGPD, auditoria e logs.

### 8. Definir qualidade e operação

Inclua:

- Estratégia de testes: unitários, integração, contrato, E2E e carga.
- Cenários críticos e metas de cobertura.
- Lint, formatação, tipos e análise estática.
- Ambientes, configuração e segredos.
- Pipeline de CI/CD, artefatos e estratégia de release.
- Observabilidade: logs, métricas, traces e alertas.
- Backup, restauração, rollback e continuidade.
- Capacidade, desempenho e custos operacionais.

### 9. Escrever o TDD

Leia `references/tdd-template.md` e use a estrutura completa. Regras:

- Numere seções e subseções hierarquicamente.
- Referencie requisitos do PRD/SPEC pelos IDs quando existirem.
- Use IDs de ADR em todas as decisões relevantes.
- Seja específico: nomes, versões, contratos, limites e métricas.
- Registre trade-offs em vez de afirmar que uma escolha é universalmente melhor.
- Não invente decisões; o que não foi validado deve ser marcado como proposta, premissa ou ponto aberto.
- Remova todos os comentários e espaços reservados do template antes de entregar.
- Use português do Brasil, salvo indicação contrária do projeto.

### 10. Validar antes de gravar

Leia `references/tdd-checklist.md` e revise item a item. Corrija, no mínimo:

- Requisitos do PRD sem componente responsável.
- Requisitos não funcionais sem solução ou critério mensurável.
- Decisões sem alternativa, justificativa ou consequência.
- Stack sem versão ou com bibliotecas redundantes.
- Modelo de dados inconsistente com as regras de negócio.
- APIs sem autenticação, validação ou tratamento de erro.
- Riscos de concorrência sem mitigação.
- Contradições com documentos existentes.
- Seções genéricas ou com marcadores não preenchidos.

### 11. Gravar o resultado

- Caminho padrão: `docs/TDD.md`.
- Se o usuário informar outro caminho, respeite.
- Crie os diretórios necessários.
- Se o arquivo já existir, leia antes, preserve o conteúdo válido e atualize versão, data e histórico. Só substitua integralmente quando o usuário pedir regeneração do zero.
- Mantenha o documento autocontido: ele não pode depender da conversa que o originou.

### 12. Reportar a entrega

Ao final, informe de forma curta:

- Caminho do arquivo gerado.
- Quantidade de ADRs, componentes e endpoints especificados.
- Decisões pendentes e premissas adotadas.
- Conflitos encontrados com PRD, SPEC ou código existente.

## Regras de qualidade

- **Centralização:** o TDD é a fonte única das decisões técnicas; ADRs devem ser rastreáveis.
- **Especificidade:** toda decisão tem nome, versão, limite ou contrato verificável.
- **Rastreabilidade:** requisito → componente → decisão → teste.
- **Consistência:** arquitetura, dados, APIs e operação contam a mesma história.
- **Trade-offs explícitos:** alternativas e consequências sempre registradas.
- **Testabilidade:** toda decisão crítica tem estratégia de verificação.
- **Segurança por padrão:** autenticação, autorização, validação e proteção de dados desde o design.
- **Proporcionalidade:** profundidade suficiente para implementar, sem antecipar código completo.
- **Transparência:** suposições e questões abertas separadas das decisões aceitas.
