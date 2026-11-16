# Checklist de qualidade do PRD

Revise o documento completo antes de gravar. Corrija qualquer item reprovado.

## 1. Estrutura e forma

- [ ] Todas as seções do template estão presentes ou justificadas.
- [ ] Não restaram espaços reservados `{{...}}` nem comentários HTML.
- [ ] Títulos numerados de forma hierárquica e sem saltos.
- [ ] Tabelas com cabeçalho, colunas consistentes e linhas completas.
- [ ] Idioma único em todo o documento, alinhado ao projeto.
- [ ] Metadados (versão, status, data, responsável) preenchidos.
- [ ] Histórico de alterações atualizado.

## 2. Problema e objetivos

- [ ] O problema está descrito com público e consequência.
- [ ] O objetivo geral é único e mensurável.
- [ ] Cada objetivo específico aponta para ao menos um módulo do escopo.
- [ ] As métricas de sucesso têm meta e forma de medição.

## 3. Perfis e permissões

- [ ] Todos os perfis que interagem com o produto estão listados.
- [ ] Cada perfil tem ações permitidas e restrições explícitas.
- [ ] Não há permissões conflitantes entre perfis ou módulos.
- [ ] A matriz de permissões cobre todos os módulos do escopo.
- [ ] Perfis administrativos e de suporte foram considerados.

## 4. Escopo

- [ ] O que entra na versão atual está claro e priorizado.
- [ ] O que fica fora está explícito e justificado.
- [ ] Premissas e dependências externas estão registradas.
- [ ] Não existem funcionalidades ambíguas entre dentro e fora do escopo.

## 5. Jornadas

- [ ] As jornadas críticas cobrem o caminho principal de cada perfil.
- [ ] Cada jornada tem ator, pré-condições, passos e resultado esperado.
- [ ] Exceções e fluxos alternativos relevantes estão descritos.
- [ ] As jornadas não contradizem regras de negócio.

## 6. Requisitos funcionais

- [ ] Cada módulo tem visão geral, requisitos, regras e validações.
- [ ] Todo requisito tem ID único e prioridade.
- [ ] Todo requisito é verificável e está escrito em termos de comportamento.
- [ ] Campos obrigatórios, tipos e validações estão definidos.
- [ ] Estados e transições estão descritos quando o domínio exige.
- [ ] Não há requisitos duplicados entre módulos.

## 7. Requisitos não funcionais

- [ ] Desempenho, segurança, disponibilidade e privacidade foram avaliados.
- [ ] Acessibilidade e compatibilidade foram avaliadas quando aplicável.
- [ ] Cada requisito não funcional tem critério mensurável.
- [ ] Requisitos legais (por exemplo LGPD) estão contemplados quando houver dados pessoais.

## 8. Critérios de aceite e rastreabilidade

- [ ] Todo requisito essencial tem ao menos um critério de aceite.
- [ ] Critérios usam Dado/Quando/Então ou formato igualmente verificável.
- [ ] Critérios referenciam os requisitos que validam.
- [ ] Existe rastreabilidade entre objetivo, módulo, requisito e critério.
- [ ] A matriz de rastreabilidade está preenchida e sem lacunas.

## 9. Consistência

- [ ] Nomes de entidades, estados e perfis são idênticos em todo o documento.
- [ ] Regras de negócio não se contradizem entre módulos.
- [ ] O documento não contradiz specs ou decisões existentes no repositório.
- [ ] O documento é autocontido e não depende da conversa original.
- [ ] Não há detalhes de implementação que pertençam a um documento técnico.

## 10. Transparência

- [ ] Suposições feitas durante a geração estão registradas como premissas.
- [ ] Questões não resolvidas estão na seção de pontos em aberto, com impacto.
- [ ] Riscos têm impacto, probabilidade e mitigação.
- [ ] Decisões fechadas estão separadas de propostas futuras.

## 11. Entrega

- [ ] O arquivo foi gravado no caminho combinado (`docs/prd.md` por padrão).
- [ ] Conteúdo anterior foi preservado quando o arquivo já existia.
- [ ] Versão e data foram atualizadas em caso de revisão.
- [ ] O resumo final informa caminho, volumes, premissas e pontos abertos.
