# Checklist de qualidade do TDD

Revise o documento completo antes de gravar. Corrija qualquer item reprovado.

## 1. Estrutura e forma

- [ ] Todas as seções do template estão presentes ou justificadas.
- [ ] Não restaram espaços reservados `{{...}}` nem comentários HTML.
- [ ] Títulos numerados de forma hierárquica e sem saltos.
- [ ] Tabelas com cabeçalho, colunas consistentes e linhas completas.
- [ ] Idioma único em todo o documento, alinhado ao projeto.
- [ ] Metadados (versão, status, data, responsável, documento de referência) preenchidos.
- [ ] Histórico de alterações atualizado.
- [ ] Diagramas Mermaid renderizáveis e coerentes com o texto.

## 2. Contexto e escopo

- [ ] O contexto do produto e o problema técnico estão claros.
- [ ] Os objetivos técnicos são verificáveis.
- [ ] O escopo técnico da versão está explícito.
- [ ] O que está fora do escopo está justificado.
- [ ] Premissas estão registradas com impacto.
- [ ] O documento é autocontido e não depende da conversa original.

## 3. Requisitos e rastreabilidade

- [ ] Requisitos funcionais críticos estão mapeados para componentes.
- [ ] Requisitos não funcionais têm meta, solução e forma de verificação.
- [ ] Todo requisito do PRD/SPEC tem componente responsável ou justificativa de ausência.
- [ ] A matriz de rastreabilidade está preenchida.
- [ ] Não há requisitos técnicos inventados sem origem.
- [ ] Conflitos com PRD, SPEC ou código existente estão apontados.

## 4. Arquitetura

- [ ] O estilo arquitetural está declarado e justificado.
- [ ] Fronteiras e responsabilidades dos módulos estão claras.
- [ ] Regras de dependência e acoplamentos proibidos estão explícitos.
- [ ] Fluxos críticos descrevem entradas, transações, falhas e compensações.
- [ ] Multi-tenancy e isolamento de dados estão definidos.
- [ ] A arquitetura é proporcional ao prazo, equipe e escala.
- [ ] Diagramas e texto contam a mesma história.

## 5. Stack e bibliotecas

- [ ] Linguagem, runtime e versões estão definidos.
- [ ] Frameworks de backend e frontend estão justificados.
- [ ] Cada biblioteca tem papel, versão e justificativa.
- [ ] Alternativas rejeitadas estão registradas.
- [ ] Não há bibliotecas redundantes ou sem uso.
- [ ] Licenças e riscos relevantes estão avaliados.
- [ ] A stack é compatível com a experiência da equipe e o orçamento.

## 6. Dados

- [ ] Entidades, campos e relacionamentos estão definidos.
- [ ] Chaves, índices e restrições atendem às consultas críticas.
- [ ] A estratégia de migrations e rollback está definida.
- [ ] Histórico, retenção, anonimização e exclusão estão tratados.
- [ ] O modelo suporta as regras de negócio do PRD.
- [ ] A estratégia de backup e restauração está definida.
- [ ] Dados sensíveis estão classificados e protegidos.

## 7. APIs e contratos

- [ ] O estilo de API e o versionamento estão definidos.
- [ ] Endpoints críticos têm entrada, saída, validação e erros.
- [ ] Autorização por perfil e por recurso está explícita.
- [ ] Paginação, filtros, limites e idempotência estão definidos quando aplicáveis.
- [ ] Eventos assíncronos têm produtor, consumidor, payload e garantia.
- [ ] Contratos são consistentes com o modelo de dados.

## 8. Segurança

- [ ] Autenticação, sessão, expiração e recuperação estão definidos.
- [ ] Hash de senha e gestão de segredos estão definidos.
- [ ] Ameaças relevantes têm mitigação.
- [ ] O isolamento entre tenants é validado no servidor.
- [ ] Privacidade e LGPD estão contempladas.
- [ ] Logs e auditoria não expõem dados sensíveis.

## 9. Concorrência e processos assíncronos

- [ ] Operações críticas têm mecanismo de lock, transação ou constraint.
- [ ] A reserva de horários impede sobreposição.
- [ ] Idempotência e retries estão definidos.
- [ ] Jobs têm gatilho, frequência, falha, retry e alerta.
- [ ] Expirações e processos periódicos estão cobertos.
- [ ] Fuso horário e limites de calendário estão definidos.

## 10. Qualidade e testes

- [ ] A estratégia cobre unitários, integração, contrato, E2E e carga quando aplicável.
- [ ] Cenários críticos estão mapeados para requisitos.
- [ ] Metas de cobertura e quality gates estão definidos.
- [ ] Dados de teste e isolamento estão previstos.
- [ ] Testes de concorrência e isolamento entre tenants estão previstos.
- [ ] A estratégia é executável no pipeline.

## 11. Operação

- [ ] Ambientes, configuração e segredos estão definidos.
- [ ] Pipeline de CI/CD e critérios de sucesso estão definidos.
- [ ] Release, rollback e compatibilidade de migrations estão definidos.
- [ ] Logs, métricas, traces e alertas estão definidos.
- [ ] Metas de desempenho, capacidade e custos estão definidas.
- [ ] Riscos operacionais têm mitigação e responsável.

## 12. Decisões arquiteturais

- [ ] Toda decisão relevante tem ADR com ID estável.
- [ ] Cada ADR tem contexto, decisão, alternativas e consequências.
- [ ] O status do ADR é coerente com a validação obtida.
- [ ] Decisões substituídas apontam o ADR sucessor.
- [ ] O índice de ADRs está completo e sem duplicidade.
- [ ] Decisões propostas aparecem nos pontos em aberto.

## 13. Consistência e transparência

- [ ] Nomes de componentes, entidades e decisões são iguais em todo o documento.
- [ ] A stack descrita é a mesma usada nos diagramas, dados e operação.
- [ ] Não há contradições entre seções.
- [ ] Suposições estão separadas das decisões aceitas.
- [ ] Questões abertas têm impacto e responsável.
- [ ] Riscos têm impacto, probabilidade e mitigação.
- [ ] Não há detalhes de implementação além do necessário para o design.

## 14. Entrega

- [ ] O arquivo foi gravado no caminho combinado (`docs/TDD.md` por padrão).
- [ ] Conteúdo anterior foi preservado quando o arquivo já existia.
- [ ] Versão, data e histórico foram atualizados em caso de revisão.
- [ ] O resumo final informa caminho, volumes, decisões pendentes e premissas.
