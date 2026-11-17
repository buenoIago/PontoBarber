# Spec Delta

## Purpose

Define como o proprietário consulta os clientes relacionados à barbearia e como o cliente acessa e mantém os próprios dados cadastrais, sem que o estabelecimento possa criar, editar ou excluir contas de clientes.

## ADDED Requirements

### Requirement: Relação do cliente com a barbearia

Um cliente SHALL ser considerado relacionado a uma barbearia quando possuir agendamento atual ou histórico nela. Cliente sem nenhum agendamento MUST NOT aparecer na consulta do estabelecimento.

#### Scenario: Cliente com agendamento aparece na lista

- **WHEN** o proprietário consulta os clientes da barbearia e um cliente possui agendamento atual ou histórico nela
- **THEN** o cliente é incluído no resultado

#### Scenario: Cliente sem agendamento não aparece

- **WHEN** um cliente nunca agendou na barbearia
- **THEN** ele não é exibido na lista do estabelecimento

### Requirement: Consulta e pesquisa de clientes pelo proprietário

O proprietário SHALL listar e pesquisar os clientes relacionados à própria barbearia e consultar os dados de um cliente relacionado. Barbeiros MUST NOT acessar a área de clientes do estabelecimento.

#### Scenario: Pesquisa por cliente cadastrado

- **WHEN** o proprietário pesquisa por um cliente que possui agendamentos na barbearia
- **THEN** o resultado correspondente é exibido

#### Scenario: Barbeiro tenta acessar a lista de clientes

- **WHEN** um barbeiro vinculado, que não é proprietário, tenta consultar a lista de clientes
- **THEN** o acesso é negado

#### Scenario: Acesso a cliente de outra barbearia

- **WHEN** o proprietário tenta consultar um cliente relacionado apenas a outra barbearia
- **THEN** o acesso é negado

### Requirement: Ausência de gestão de clientes pelo estabelecimento

O sistema MUST NOT oferecer ao proprietário cadastro manual, edição ou exclusão de contas de clientes.

#### Scenario: Proprietário tenta cadastrar cliente

- **WHEN** o proprietário procura a opção de cadastrar um cliente manualmente
- **THEN** a operação não é disponibilizada

#### Scenario: Proprietário tenta editar ou excluir cliente

- **WHEN** o proprietário tenta alterar ou excluir os dados de um cliente
- **THEN** a operação é recusada

### Requirement: Consulta e edição dos próprios dados pelo cliente

O cliente autenticado SHALL consultar e editar nome, e-mail e telefone da própria conta. Cada cliente MUST alterar apenas os próprios dados.

#### Scenario: Edição válida dos próprios dados

- **WHEN** o cliente autenticado informa nome, e-mail e telefone válidos para a própria conta
- **THEN** os dados são atualizados

#### Scenario: Cliente tenta editar dados de outro cliente

- **WHEN** um cliente tenta alterar dados de outro cliente
- **THEN** o acesso é negado

#### Scenario: E-mail já utilizado por outra conta

- **WHEN** o cliente tenta salvar um e-mail pertencente a outra conta
- **THEN** a alteração é recusada e os dados anteriores são preservados

### Requirement: Preservação do histórico após alteração cadastral

Alterações nos dados pessoais do cliente MUST NOT modificar informações históricas dos agendamentos.

#### Scenario: Histórico após edição do perfil

- **WHEN** o cliente altera seus dados pessoais após possuir agendamentos concluídos
- **THEN** os agendamentos históricos permanecem inalterados
