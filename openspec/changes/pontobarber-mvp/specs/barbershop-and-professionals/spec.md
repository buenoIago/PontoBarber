# Spec Delta

## Purpose

Cobre a criação e manutenção da barbearia, o código de convite permanente e a gestão dos vínculos dos profissionais com o estabelecimento.

## ADDED Requirements

### Requirement: Criação da barbearia
Um barbeiro autenticado SHALL poder criar uma barbearia informando nome, telefone comercial, e-mail comercial, cidade e endereço, com Instagram opcional. O criador MUST tornar-se proprietário e o cadastro MUST NOT exigir CNPJ ou documentos pessoais.

#### Scenario: Criação com dados válidos
- **WHEN** um barbeiro autenticado informa todos os campos obrigatórios válidos
- **THEN** a barbearia é criada, o criador recebe o papel de proprietário e um código de convite é gerado

#### Scenario: Campo obrigatório ausente
- **WHEN** um dos campos obrigatórios não é informado ou é inválido
- **THEN** a barbearia não é criada e os dados anteriores são preservados

### Requirement: Código de convite único e permanente
Cada barbearia SHALL possuir um código de convite único, aleatório e imutável ao longo do tempo. Somente o proprietário MUST poder consultá-lo.

#### Scenario: Consulta do código pelo proprietário
- **WHEN** o proprietário consulta o código de convite da própria barbearia
- **THEN** o código é exibido para compartilhamento

#### Scenario: Barbeiro tenta consultar o código
- **WHEN** um barbeiro vinculado, que não é proprietário, tenta consultar o código de convite
- **THEN** o acesso é negado

### Requirement: Consulta e edição dos dados da barbearia
O proprietário SHALL poder consultar e editar nome, telefone comercial, e-mail comercial, cidade, endereço e Instagram. Barbeiros vinculados MUST ter apenas leitura. Alterações cadastrais MUST NOT modificar agendamentos históricos.

#### Scenario: Edição pelo proprietário
- **WHEN** o proprietário altera um campo obrigatório com valor válido
- **THEN** os dados são atualizados e os agendamentos existentes permanecem inalterados

#### Scenario: Barbeiro tenta editar
- **WHEN** um barbeiro vinculado tenta salvar alterações nos dados da barbearia
- **THEN** a operação é recusada e os dados anteriores são preservados

### Requirement: Entrada de profissional por código de convite
Um barbeiro autenticado SHALL poder entrar em uma barbearia existente informando o código de convite. Código inexistente MUST ser rejeitado e vínculo já existente MUST NOT ser duplicado.

#### Scenario: Código válido
- **WHEN** um barbeiro informa o código de convite de uma barbearia existente
- **THEN** o vínculo ativo é criado e o barbeiro passa a acessar os serviços e a própria disponibilidade

#### Scenario: Código inválido
- **WHEN** um barbeiro informa um código que não corresponde a nenhuma barbearia
- **THEN** a operação é recusada e nenhum vínculo é criado

#### Scenario: Vínculo já existente
- **WHEN** um barbeiro já vinculado tenta entrar novamente na mesma barbearia
- **THEN** a operação é recusada sem duplicar o vínculo

### Requirement: Gestão de vínculos pelo proprietário
O proprietário SHALL visualizar nome, telefone e situação dos profissionais vinculados, editar dados profissionais, ativar, desativar e remover vínculos. A desativação e a remoção MUST preservar a conta e o histórico.

#### Scenario: Desativação de profissional
- **WHEN** o proprietário desativa um profissional ativo
- **THEN** o profissional deixa de receber novos agendamentos e mantém conta e histórico

#### Scenario: Remoção de vínculo
- **WHEN** o proprietário remove o vínculo de um profissional
- **THEN** o vínculo passa a removido, a conta permanece e os registros históricos são preservados

#### Scenario: Profissional tenta gerenciar outro vínculo
- **WHEN** um barbeiro tenta ativar, desativar, editar ou remover o vínculo de outro profissional
- **THEN** a operação é recusada

### Requirement: Proprietário atuando como barbeiro
O proprietário SHALL possuir vínculo que lhe permita atuar como profissional, com disponibilidade e agenda próprias, sem perder as permissões administrativas.

#### Scenario: Acesso à própria agenda como profissional
- **WHEN** o proprietário acessa sua atuação como barbeiro
- **THEN** visualiza e administra a própria disponibilidade e agenda
