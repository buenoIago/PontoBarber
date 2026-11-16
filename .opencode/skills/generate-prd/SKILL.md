---
name: generate-prd
description: Gera um Documento de Requisitos de Produto (PRD) completo, rastreável e pronto para implementação a partir de um prompt inicial e do contexto do repositório. Use quando o usuário pedir um PRD, especificação de produto, ou invocar /generate-prd.
---

# Generate PRD

Transforme um prompt inicial de produto em um Documento de Requisitos de Produto (PRD) completo, consistente e pronto para ser usado por desenvolvimento, design, QA e negócio.

## Quando usar

Use esta skill quando o usuário pedir para:

- Criar um PRD, documento de requisitos, especificação de produto ou escopo funcional.
- Estruturar uma ideia de produto antes de planejar a implementação.
- Detalhar módulos, regras de negócio, perfis e critérios de aceite de um sistema.

## Entradas

- **Prompt inicial** (obrigatório): descrição do produto, problema, funcionalidades desejadas ou contexto de negócio.
- **Contexto do repositório** (quando existir): código, documentação, specs, decisões anteriores e terminologia já adotada.
- **Restrições do usuário**: versão do produto, prazo, plataformas, integrações, idioma e formato de saída.

Se o prompt inicial estiver vazio, peça ao usuário uma descrição mínima do produto (problema, público e objetivo principal) antes de continuar.

## Fluxo de trabalho

### 1. Analisar o prompt inicial

Extraia e organize:

- Problema que o produto resolve.
- Público-alvo e perfis de usuário.
- Objetivos e resultados esperados.
- Funcionalidades citadas e implícitas.
- Restrições, integrações e exclusões mencionadas.
- Lacunas que impedem decisões de escopo.

Não invente requisitos que o prompt não sustente. Quando precisar completar o documento, registre a decisão como premissa.

### 2. Investigar o contexto do repositório

Antes de escrever, verifique o que já existe:

- `AGENTS.md`, `README.md` e arquivos de instrução do projeto.
- Diretórios `docs/`, `openspec/specs/`, `openspec/changes/` e documentação equivalente.
- Manifestos e configurações que revelem stack, plataformas e restrições (por exemplo `package.json`, `pyproject.toml`, `composer.json`).
- Código-fonte e migrations, apenas para alinhar terminologia, entidades e funcionalidades já decididas.

Use o repositório para:

- Reaproveitar nomes de domínio, módulos e perfis já adotados.
- Evitar contradizer decisões documentadas.
- Identificar o que já está implementado, planejado ou explicitamente fora de escopo.

Não copie detalhes de implementação para o PRD. O documento descreve **produto e comportamento**, não arquitetura de código.

Se o repositório não tiver contexto relevante, siga apenas com o prompt inicial e declare isso na seção de premissas.

### 3. Resolver lacunas críticas

Identifique decisões que mudam materialmente o escopo, por exemplo:

- Perfis de usuário e permissões.
- O que entra e o que fica fora da versão atual.
- Modelo de cobrança ou monetização.
- Canais e integrações obrigatórios.
- Fluxos de aprovação, cancelamento ou notificação.

Se restarem dúvidas críticas, faça no máximo 5 perguntas agrupadas e objetivas, usando a ferramenta `question` quando disponível. Ofereça opções claras e uma recomendação.

Se o usuário pedir para não interromper, ou se a dúvida não for crítica, avance registrando a suposição na seção **Premissas e questões em aberto** do PRD.

### 4. Definir a estrutura do documento

Leia `references/prd-template.md` e use a estrutura completa como base. Adapte a profundidade ao tamanho do produto, mas não remova seções: quando uma seção não se aplicar, explique brevemente o motivo.

Regras de modelagem:

- Numere seções e subseções de forma hierárquica.
- Atribua IDs estáveis e únicos:
  - `RF-<MODULO>-NNN` para requisitos funcionais.
  - `RN-<MODULO>-NNN` para regras de negócio.
  - `RNF-<CATEGORIA>-NNN` para requisitos não funcionais.
  - `CA-<MODULO>-NNN` para critérios de aceite.
- Use prioridade `Essencial`, `Importante` ou `Desejável` em cada requisito.
- Descreva regras de negócio de forma verificável, sem ambiguidade.
- Relacione cada critério de aceite a um ou mais requisitos funcionais.
- Mantenha a terminologia igual em todo o documento.

### 5. Escrever o PRD

Preencha o template com conteúdo específico do produto:

- Substitua todos os espaços reservados e comentários do template.
- Descreva jornadas em passos numerados, incluindo o resultado esperado.
- Liste funcionalidades fora de escopo de forma explícita, para evitar expansão silenciosa.
- Inclua tabelas de campos obrigatórios, validações e permissões quando ajudarem a remover ambiguidade.
- Inclua requisitos não funcionais mensuráveis (desempenho, segurança, disponibilidade, acessibilidade, privacidade, compatibilidade).
- Registre riscos com impacto, probabilidade e mitigação.
- Separe decisões fechadas de pontos ainda abertos.

Mantenha o documento autocontido: ele não pode depender da conversa que o originou.

### 6. Validar antes de gravar

Leia `references/prd-checklist.md` e revise o documento item a item. Corrija, no mínimo:

- Objetivos sem módulo que os atenda.
- Requisitos sem regra ou critério de aceite correspondente.
- Permissões conflitantes entre perfis.
- Campos e validações inconsistentes entre módulos.
- Seções genéricas, vagas ou com marcadores não preenchidos.
- Contradições com documentos existentes no repositório.

### 7. Gravar o resultado

- Caminho padrão: `docs/prd.md`.
- Se o usuário informar outro caminho ou nome, respeite.
- Crie os diretórios necessários.
- Se o arquivo já existir, leia-o antes. Preserve o conteúdo válido e atualize a versão, a data e o histórico de alterações. Só substitua integralmente quando o usuário pedir uma regeneração do zero.
- Use o idioma do prompt inicial e dos documentos do projeto. Quando não houver indicação, use português do Brasil.
- Use Markdown com títulos numerados e tabelas legíveis.

### 8. Reportar a entrega

Ao final, informe de forma curta:

- Caminho do arquivo gerado.
- Quantidade de módulos, requisitos funcionais e critérios de aceite.
- Premissas adotadas e questões que continuam abertas.
- Qualquer conflito encontrado com documentação existente.

## Regras de qualidade

- **Completude:** todas as seções do template preenchidas ou justificadas.
- **Rastreabilidade:** objetivo → módulo → requisito → critério de aceite.
- **Verificabilidade:** todo requisito pode ser testado ou observado.
- **Consistência:** mesmos nomes, estados e permissões em todo o documento.
- **Escopo explícito:** incluído e excluído, sem zonas cinzentas.
- **Sem invenção silenciosa:** suposições sempre declaradas.
- **Sem implementação prematura:** o PRD define o quê e por quê; o como fica para documentos técnicos.
- **Proporcionalidade:** documento completo, mas sem repetição ou texto de preenchimento.
