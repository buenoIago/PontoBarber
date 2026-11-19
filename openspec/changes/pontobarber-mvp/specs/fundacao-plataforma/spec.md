# Spec Delta

## Purpose

Estabelece a base executável e as garantias transversais do PontoBarber: isolamento entre barbearias, contrato de erros, saúde operacional, observabilidade, fundação visual acessível e entrega verificável independente de hospedagem.

## ADDED Requirements

### Requirement: Isolamento de dados entre barbearias
Toda operação autenticada vinculada a uma barbearia SHALL acessar somente dados da barbearia do vínculo ativo do usuário. O identificador da barbearia MUST ser derivado da rota e validado contra o vínculo, nunca aceito do corpo da requisição.

#### Scenario: Dois tenants com dados homônimos
- **WHEN** dois estabelecimentos possuem serviços e agendamentos com nomes iguais e um usuário autenticado consulta um deles
- **THEN** a resposta contém apenas os registros da barbearia vinculada ao usuário

#### Scenario: Manipulação do identificador da barbearia na rota
- **WHEN** um usuário autenticado informa na rota o identificador de uma barbearia sem vínculo ativo
- **THEN** a operação é recusada com erro de autorização ou recurso não encontrado, sem expor dados

### Requirement: Contrato de erros padronizado
Respostas de erro da API SHALL usar `application/problem+json` com um código estável, identificador de correlação e erros por campo quando aplicável.

#### Scenario: Conflito de horário
- **WHEN** uma confirmação de agendamento encontra horário indisponível
- **THEN** a API responde com status de conflito e código estável de horário indisponível

#### Scenario: Falha de validação
- **WHEN** uma requisição envia campos inválidos
- **THEN** a API responde com status de validação e a lista de erros por campo

### Requirement: Saúde operacional
A aplicação SHALL expor verificação de vivacidade e de prontidão. A prontidão MUST falhar quando a persistência ou os processos essenciais não estiverem operacionais.

#### Scenario: Banco indisponível
- **WHEN** a persistência não responde
- **THEN** a verificação de prontidão retorna indisponível e a de vivacidade continua respondendo

### Requirement: Observabilidade sem dados sensíveis
Logs, métricas e traces SHALL correlacionar requisição, usuário e barbearia. Senhas, tokens, cookies, e-mail completo e telefone completo MUST NOT aparecer nos registros.

#### Scenario: Registro de login
- **WHEN** um login é processado
- **THEN** o evento é registrado com identificadores e resultado, sem senha, cookie ou e-mail completo

### Requirement: Fundação visual acessível e responsiva
As telas SHALL compartilhar um mesmo conjunto de componentes visuais reutilizáveis e atender WCAG 2.1 AA nas telas principais, com navegação por teclado e foco visível.

#### Scenario: Auditoria de acessibilidade
- **WHEN** as telas principais são auditadas com verificação automatizada e revisão manual
- **THEN** não há violação crítica de acessibilidade e os fluxos principais são operáveis apenas por teclado

### Requirement: Entrega verificável independente da hospedagem
A construção SHALL produzir artefatos executáveis e aplicar testes automatizados sem depender de um provedor de nuvem específico. Build, testes, imagem e migração MUST NOT exigir serviço proprietário; o destino de execução é parametrizado por ambiente.

#### Scenario: Execução em ambiente sem provedor definido
- **WHEN** o pipeline é executado sem configuração de provedor de nuvem
- **THEN** build, testes, imagem e migração concluem, e somente a etapa de publicação exige parâmetros do ambiente-alvo

#### Scenario: Migração pendente
- **WHEN** o modelo de dados possui alteração sem migração correspondente
- **THEN** a verificação de pipeline falha
