# Spec Delta

## Purpose

Define a criação de agendamentos públicos e manuais, a confirmação automática, a prevenção de conflitos e as visualizações da agenda por barbearia e por profissional.

## ADDED Requirements

### Requirement: Agendamento público
Um cliente autenticado SHALL agendar escolhendo serviço, profissional, data, horário e observação opcional, com confirmação automática. Os dados pessoais MUST ser obtidos da conta autenticada.

#### Scenario: Agendamento público confirmado
- **WHEN** um cliente autenticado confirma um horário disponível com serviço e profissional ativos
- **THEN** o agendamento é criado como confirmado, sem aprovação manual

#### Scenario: Visitante não autenticado
- **WHEN** um visitante não autenticado tenta concluir um agendamento
- **THEN** o sistema exige autenticação e nenhum agendamento é criado

#### Scenario: Serviço ou profissional inativo
- **WHEN** o cliente tenta confirmar com serviço inativo ou profissional inativo ou sem vínculo
- **THEN** a confirmação é recusada

### Requirement: Agendamento manual
Proprietário ou barbeiro SHALL criar agendamentos pela agenda informando cliente, profissional, data, horário de início, serviço e observação opcional, com confirmação automática.

#### Scenario: Criação manual válida
- **WHEN** o usuário autorizado informa os campos obrigatórios com dados válidos
- **THEN** o agendamento é criado como confirmado e exibido na agenda

#### Scenario: Barbeiro cria para outro profissional
- **WHEN** um barbeiro tenta criar agendamento para profissional fora da própria agenda
- **THEN** a operação é recusada

### Requirement: Cálculo de término e valor
O sistema SHALL calcular automaticamente o horário de término pela duração do serviço e o valor pelo preço vigente, preservando nome, duração e preço como snapshot do agendamento.

#### Scenario: Cálculo com base no serviço
- **WHEN** um agendamento é criado para um serviço de duração e preço conhecidos
- **THEN** término e valor correspondem ao serviço e ficam preservados no agendamento

### Requirement: Reserva sem conflito
O sistema MUST impedir que dois agendamentos ocupem o mesmo profissional no mesmo intervalo. A validação de disponibilidade MUST ocorrer novamente no momento da confirmação.

#### Scenario: Confirmações simultâneas no mesmo horário
- **WHEN** duas confirmações para o mesmo profissional e horário chegam simultaneamente
- **THEN** apenas uma é aceita e a outra recebe erro de horário indisponível

#### Scenario: Horário bloqueado
- **WHEN** a confirmação ocorre em período bloqueado
- **THEN** o agendamento é recusado com indicação de bloqueio

#### Scenario: Horário já ocupado
- **WHEN** o horário já possui agendamento ativo do mesmo profissional
- **THEN** a confirmação é recusada sem persistir novo agendamento

### Requirement: Visualizações da agenda
Proprietário e barbeiros SHALL visualizar a agenda por dia, semana e mês, navegar entre períodos anteriores, atual e futuros e filtrar por profissional quando autorizado.

#### Scenario: Alternância de visualização
- **WHEN** um usuário autorizado acessa a agenda
- **THEN** pode alternar entre dia, semana e mês

#### Scenario: Filtro por profissional
- **WHEN** o proprietário filtra a agenda por um profissional
- **THEN** apenas os agendamentos desse profissional são exibidos

#### Scenario: Barbeiro consulta a própria agenda
- **WHEN** um barbeiro acessa a agenda
- **THEN** visualiza apenas os agendamentos da própria atuação

### Requirement: Identificação visual da agenda
A agenda SHALL identificar agendamentos confirmados em verde, cancelados em vermelho e bloqueios em cinza. A cor amarela MUST NOT ser usada para agendamentos pendentes.

#### Scenario: Exibição de status na agenda
- **WHEN** um dia com agendamentos confirmados, cancelados e bloqueios é exibido
- **THEN** cada item usa a cor correspondente ao seu tipo e nenhum agendamento usa amarelo

### Requirement: Consulta de agendamentos conforme permissão
O sistema SHALL permitir consultar os detalhes de um agendamento ao proprietário, ao profissional do agendamento e ao cliente titular. O cliente MUST consultar apenas os próprios agendamentos.

#### Scenario: Cliente consulta os próprios agendamentos
- **WHEN** um cliente autenticado consulta sua lista de agendamentos
- **THEN** recebe apenas os agendamentos dos quais é titular

#### Scenario: Acesso a agendamento de outro cliente
- **WHEN** um cliente tenta consultar um agendamento de outro cliente
- **THEN** o acesso é negado
