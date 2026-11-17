# Spec Delta

## Purpose

Define o catálogo de serviços de cada barbearia, incluindo o catálogo modelo, a manutenção pelo proprietário e a preservação dos dados contratados nos agendamentos.

## ADDED Requirements

### Requirement: Catálogo modelo
O sistema SHALL disponibilizar um catálogo inicial com Corte de 40 minutos, Barba de 30 minutos e Corte + Barba de 70 minutos, sem preços fixos, como modelo para cada barbearia.

#### Scenario: Catálogo disponível
- **WHEN** uma nova barbearia é criada
- **THEN** o catálogo modelo pode ser usado para compor os serviços da barbearia

### Requirement: Manutenção de serviços pelo proprietário
Somente o proprietário SHALL poder adicionar, editar e remover serviços da própria barbearia, informando nome, duração e preço.

#### Scenario: Criação de serviço
- **WHEN** o proprietário informa nome, duração válida e preço não negativo
- **THEN** o serviço é criado no catálogo da barbearia

#### Scenario: Preço negativo ou duração inválida
- **WHEN** o proprietário tenta salvar um serviço com preço negativo ou duração inválida
- **THEN** a operação é recusada e o serviço não é criado nem alterado

#### Scenario: Barbeiro tenta alterar o catálogo
- **WHEN** um barbeiro vinculado tenta criar, editar ou remover um serviço
- **THEN** a operação é recusada

### Requirement: Leitura do catálogo conforme perfil
Barbeiros SHALL visualizar os serviços da barbearia sem permissão de edição. Clientes MUST visualizar apenas serviços ativos.

#### Scenario: Barbeiro consulta o catálogo
- **WHEN** um barbeiro vinculado acessa os serviços
- **THEN** visualiza os serviços e não recebe opções de edição

#### Scenario: Cliente consulta serviços para agendamento
- **WHEN** um cliente consulta os serviços disponíveis para agendamento
- **THEN** apenas serviços ativos são retornados

### Requirement: Remoção lógica de serviço
Remover um serviço SHALL desativá-lo para novos agendamentos sem excluir registros históricos. Agendamentos existentes MUST preservar nome, duração e preço do serviço no momento da contratação.

#### Scenario: Serviço removido deixa de ser ofertado
- **WHEN** o proprietário remove um serviço
- **THEN** o serviço não aparece como opção para novos agendamentos

#### Scenario: Agendamento histórico com serviço removido
- **WHEN** um agendamento antigo que usou o serviço removido é consultado
- **THEN** nome, duração e preço originais permanecem preservados
