# {{NOME_DO_PRODUTO}} — PRD

> **Documento:** Product Requirements Document
> **Projeto:** {{NOME_DO_PRODUTO}}
> **Versão:** {{VERSAO}}
> **Status:** {{RASCUNHO | EM REVISÃO | CONSOLIDADO}}
> **Data:** {{DATA}}
> **Responsável:** {{RESPONSAVEL}}

<!--
Instruções de uso:
- Substitua todos os espaços reservados {{...}} e remova os comentários HTML antes de entregar.
- Mantenha a numeração das seções.
- Adapte a profundidade ao produto, mas não elimine seções: justifique quando não se aplicarem.
- Use IDs RF, RN, RNF e CA conforme a convenção da skill.
-->

## Histórico de alterações

| Versão | Data | Autor | Alteração |
| --- | --- | --- | --- |
| {{VERSAO}} | {{DATA}} | {{AUTOR}} | Criação do documento |

---

# 1. Descrição do projeto

## 1.1. Visão geral

<!-- Síntese em 1 a 3 parágrafos: o que é o produto, para quem e qual resultado entrega. -->

{{VISAO_GERAL}}

## 1.2. Problema

<!-- Descreva a dor atual, quem sente e o custo de não resolvê-la. -->

{{PROBLEMA}}

## 1.3. Público-alvo

<!-- Segmentos atendidos, contexto de uso e necessidades principais. -->

| Segmento | Necessidades principais | Contexto de uso |
| --- | --- | --- |
| {{SEGMENTO}} | {{NECESSIDADES}} | {{CONTEXTO}} |

## 1.4. Proposta de valor

<!-- Diferenciais objetivos em relação à alternativa atual. -->

- {{DIFERENCIAL_1}}
- {{DIFERENCIAL_2}}

---

# 2. Objetivos do sistema

## 2.1. Objetivo geral

{{OBJETIVO_GERAL}}

## 2.2. Objetivos específicos

- {{OBJETIVO_ESPECIFICO_1}}
- {{OBJETIVO_ESPECIFICO_2}}

## 2.3. Métricas de sucesso

<!-- Indicadores observáveis que permitem avaliar se o produto atingiu o objetivo. -->

| Métrica | Descrição | Meta inicial | Como medir |
| --- | --- | --- | --- |
| {{METRICA}} | {{DESCRICAO}} | {{META}} | {{MEDICAO}} |

---

# 3. Perfis de usuários

<!-- Repita o bloco para cada perfil. Inclua também perfis administrativos e de suporte. -->

## 3.1. {{PERFIL}}

**Descrição:** {{DESCRICAO_DO_PERFIL}}

**Pode:**

- {{PERMISSAO_1}}
- {{PERMISSAO_2}}

**Não pode:**

- {{RESTRICAO_1}}

## 3.2. Matriz de permissões

<!-- Marque o nível de acesso por módulo: Total, Parcial, Leitura ou Nenhum. -->

| Módulo | {{PERFIL_A}} | {{PERFIL_B}} | {{PERFIL_C}} |
| --- | --- | --- | --- |
| {{MODULO}} | {{ACESSO}} | {{ACESSO}} | {{ACESSO}} |

---

# 4. Escopo do projeto

## 4.1. Funcionalidades incluídas em {{VERSAO}}

| Módulo | Funcionalidades | Prioridade |
| --- | --- | --- |
| {{MODULO}} | {{FUNCIONALIDADES}} | {{PRIORIDADE}} |

## 4.2. Funcionalidades fora do escopo de {{VERSAO}}

<!-- Explicite o que não será feito agora e, quando útil, a versão prevista. -->

- {{ITEM_FORA_DE_ESCOPO}} — {{MOTIVO}}

## 4.3. Premissas e dependências

| Tipo | Descrição | Impacto se não se confirmar |
| --- | --- | --- |
| Premissa | {{PREMISSA}} | {{IMPACTO}} |
| Dependência | {{DEPENDENCIA}} | {{IMPACTO}} |

---

# 5. Jornadas principais

<!-- Repita o bloco para cada jornada crítica. Passos numerados, atores e resultado esperado. -->

## 5.1. {{NOME_DA_JORNADA}}

**Ator:** {{ATOR}}
**Pré-condições:** {{PRE_CONDICOES}}

1. {{PASSO_1}}
2. {{PASSO_2}}
3. {{PASSO_3}}

**Resultado esperado:** {{RESULTADO}}
**Exceções:** {{EXCECOES}}

---

# 6. Módulos e funcionalidades

<!--
Repita o bloco 6.N para cada módulo do escopo.
Cada módulo deve conter: visão geral, requisitos funcionais, regras de negócio,
campos obrigatórios, restrições e validações, estados/fluxos quando existirem.
-->

## 6.1. {{MODULO}}

### Visão geral

{{DESCRICAO_DO_MODULO}}

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-{{MODULO}}-001 | {{REQUISITO}} | Essencial |
| RF-{{MODULO}}-002 | {{REQUISITO}} | Importante |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-{{MODULO}}-001 | {{REGRA}} | {{JUSTIFICATIVA}} |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| {{CAMPO}} | {{TIPO}} | Sim/Não | {{VALIDACAO}} |

### Restrições e validações

- {{RESTRICAO_1}}
- {{RESTRICAO_2}}

### Estados e transições

<!-- Preencha quando o módulo tiver ciclo de vida próprio (ex.: pedido, assinatura, agendamento). -->

| Estado | Descrição | Transições permitidas |
| --- | --- | --- |
| {{ESTADO}} | {{DESCRICAO}} | {{TRANSICOES}} |

---

# 7. Requisitos não funcionais

<!-- Requisitos mensuráveis. Ajuste as categorias ao produto. -->

| ID | Categoria | Requisito | Critério mensurável |
| --- | --- | --- | --- |
| RNF-DESEMPENHO-001 | Desempenho | {{REQUISITO}} | {{METRICA}} |
| RNF-SEGURANCA-001 | Segurança | {{REQUISITO}} | {{METRICA}} |
| RNF-DISPONIBILIDADE-001 | Disponibilidade | {{REQUISITO}} | {{METRICA}} |
| RNF-ACESSIBILIDADE-001 | Acessibilidade | {{REQUISITO}} | {{METRICA}} |
| RNF-PRIVACIDADE-001 | Privacidade e LGPD | {{REQUISITO}} | {{METRICA}} |
| RNF-COMPATIBILIDADE-001 | Compatibilidade | {{REQUISITO}} | {{METRICA}} |

---

# 8. Critérios de aceite

<!--
Formato sugerido: Dado / Quando / Então.
Cada critério deve referenciar os requisitos que valida.
-->

## 8.1. {{MODULO}}

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-{{MODULO}}-001 | RF-{{MODULO}}-001 | Dado {{CONTEXTO}}, quando {{ACAO}}, então {{RESULTADO}} | {{RESULTADO_VERIFICAVEL}} |

---

# 9. Dados e informações do domínio

<!-- Entidades principais, campos relevantes, relacionamentos e ciclo de vida dos dados. -->

## 9.1. Entidades

| Entidade | Descrição | Campos principais | Relacionamentos |
| --- | --- | --- | --- |
| {{ENTIDADE}} | {{DESCRICAO}} | {{CAMPOS}} | {{RELACIONAMENTOS}} |

## 9.2. Ciclo de vida e retenção

- {{POLITICA_DE_RETENCAO}}
- {{EXCLUSAO_OU_ANONIMIZACAO}}

---

# 10. Integrações e notificações

## 10.1. Integrações

| Integração | Objetivo | Direção | Criticidade | Observações |
| --- | --- | --- | --- | --- |
| {{INTEGRACAO}} | {{OBJETIVO}} | {{DIRECAO}} | {{CRITICIDADE}} | {{OBSERVACOES}} |

## 10.2. Notificações

| Evento | Canal | Destinatário | Conteúdo mínimo | Gatilho |
| --- | --- | --- | --- | --- |
| {{EVENTO}} | {{CANAL}} | {{DESTINATARIO}} | {{CONTEUDO}} | {{GATILHO}} |

---

# 11. Riscos e pontos de atenção

| Risco | Impacto | Probabilidade | Mitigação |
| --- | --- | --- | --- |
| {{RISCO}} | Alto/Médio/Baixo | Alta/Média/Baixa | {{MITIGACAO}} |

---

# 12. Decisões consolidadas e evolução do produto

## 12.1. Decisões fechadas

| ID | Decisão | Justificativa | Data |
| --- | --- | --- | --- |
| DEC-001 | {{DECISAO}} | {{JUSTIFICATIVA}} | {{DATA}} |

## 12.2. Pontos a especificar no detalhamento

- {{PONTO_ABERTO}}

## 12.3. Evolução futura

| Item | Descrição | Versão prevista |
| --- | --- | --- |
| {{ITEM}} | {{DESCRICAO}} | {{VERSAO}} |

---

# 13. Premissas e questões em aberto

<!-- Tudo o que foi assumido para permitir a escrita do documento e ainda não foi validado. -->

| ID | Tipo | Descrição | Impacto | Responsável por validar |
| --- | --- | --- | --- | --- |
| ABT-001 | Premissa | {{DESCRICAO}} | {{IMPACTO}} | {{RESPONSAVEL}} |
| ABT-002 | Questão | {{DESCRICAO}} | {{IMPACTO}} | {{RESPONSAVEL}} |

---

# Apêndice A — Glossário

| Termo | Definição |
| --- | --- |
| {{TERMO}} | {{DEFINICAO}} |

# Apêndice B — Matriz de rastreabilidade

| Objetivo | Módulo | Requisitos | Critérios de aceite |
| --- | --- | --- | --- |
| {{OBJETIVO}} | {{MODULO}} | {{IDS_RF}} | {{IDS_CA}} |
