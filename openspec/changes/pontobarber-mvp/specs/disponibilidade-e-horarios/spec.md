# Spec Delta

## Purpose

Define o funcionamento da barbearia, a disponibilidade semanal de cada profissional, os bloqueios pontuais e o cálculo de horários ofertados ao agendamento.

## ADDED Requirements

### Requirement: Horários gerais de funcionamento
O proprietário SHALL configurar, para cada dia da semana, horário de abertura, horário de fechamento e indicação de dia fechado. O fechamento MUST ser posterior à abertura e dias fechados MUST NOT oferecer horários.

#### Scenario: Configuração semanal válida
- **WHEN** o proprietário define abertura e fechamento para cada dia
- **THEN** a configuração é salva individualmente por dia

#### Scenario: Fechamento anterior à abertura
- **WHEN** o proprietário tenta salvar um dia com fechamento anterior ou igual à abertura
- **THEN** a operação é recusada e a configuração anterior permanece

#### Scenario: Dia fechado
- **WHEN** um dia está marcado como fechado
- **THEN** nenhum horário é ofertado para agendamento nesse dia

### Requirement: Disponibilidade semanal do profissional
Cada profissional SHALL configurar a própria disponibilidade por dia da semana, com horário de início e término. A disponibilidade MUST estar contida no funcionamento da barbearia.

#### Scenario: Disponibilidade dentro do funcionamento
- **WHEN** o profissional informa uma janela contida no horário de funcionamento do dia
- **THEN** a disponibilidade é salva para ele

#### Scenario: Disponibilidade fora do funcionamento
- **WHEN** o profissional informa uma janela que ultrapassa o funcionamento da barbearia
- **THEN** a operação é recusada

#### Scenario: Alteração de disponibilidade com agendamentos existentes
- **WHEN** o profissional altera sua disponibilidade semanal
- **THEN** os agendamentos já existentes permanecem inalterados

### Requirement: Bloqueios pontuais de agenda
Cada profissional SHALL cadastrar e remover bloqueios com data e hora de início e término, com motivo opcional. O início MUST ser anterior ao término e o período bloqueado MUST NOT receber novos agendamentos.

#### Scenario: Bloqueio válido
- **WHEN** o profissional cadastra um bloqueio com início anterior ao término
- **THEN** o bloqueio passa a impedir novos agendamentos no período

#### Scenario: Bloqueio com período invertido
- **WHEN** o profissional tenta cadastrar um bloqueio com início posterior ao término
- **THEN** a operação é recusada

#### Scenario: Bloqueio sobre agendamento existente
- **WHEN** um bloqueio é criado sobre um período que já possui agendamento
- **THEN** o agendamento existente é preservado e a criação do bloqueio é tratada conforme a validação de conflito

### Requirement: Cálculo de horários disponíveis
O sistema SHALL calcular os horários disponíveis considerando funcionamento da barbearia, disponibilidade semanal do profissional, bloqueios e agendamentos existentes, no fuso horário da barbearia, com janela padrão de 15 dias e limite máximo absoluto de 30 dias.

#### Scenario: Horário livre
- **WHEN** um dia está aberto, o profissional possui disponibilidade e não há bloqueio nem agendamento no intervalo
- **THEN** o horário é ofertado como disponível

#### Scenario: Horário fora da jornada
- **WHEN** o horário solicitado está fora do funcionamento ou da disponibilidade do profissional
- **THEN** o horário não é ofertado e a confirmação é recusada com indicação específica

#### Scenario: Data além do limite
- **WHEN** a consulta solicita data além do limite máximo de 30 dias
- **THEN** a consulta é recusada por validação

### Requirement: Autorização das configurações
Somente o proprietário SHALL configurar os horários gerais. Cada profissional MUST administrar apenas a própria disponibilidade e os próprios bloqueios, podendo o proprietário atuar como profissional.

#### Scenario: Barbeiro tenta alterar horários gerais
- **WHEN** um barbeiro não proprietário tenta salvar os horários de funcionamento
- **THEN** a operação é recusada

#### Scenario: Profissional tenta alterar bloqueio de outro
- **WHEN** um profissional tenta criar ou remover bloqueio de outro profissional
- **THEN** a operação é recusada
