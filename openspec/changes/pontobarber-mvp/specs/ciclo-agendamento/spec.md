# Spec Delta

## Purpose

Define cancelamento, remarcação, sugestão de novo horário e preservação do histórico após a criação do agendamento.

## ADDED Requirements

### Requirement: Cancelamento do agendamento
O cliente SHALL cancelar o próprio agendamento a qualquer momento antes do início, sem antecedência mínima. O barbeiro SHALL cancelar agendamentos da própria agenda e o proprietário MUST poder cancelar qualquer agendamento da barbearia.

#### Scenario: Cliente cancela antes do início
- **WHEN** o cliente cancela um agendamento próprio ainda não iniciado
- **THEN** o agendamento passa a cancelado

#### Scenario: Barbeiro cancela agendamento da própria agenda
- **WHEN** o barbeiro cancela um agendamento da própria agenda ainda não iniciado
- **THEN** o cancelamento é aceito

#### Scenario: Proprietário cancela agendamento de outro profissional
- **WHEN** o proprietário cancela um agendamento de qualquer profissional da barbearia
- **THEN** o cancelamento é aceito

### Requirement: Bloqueio de cancelamento após o início
Nenhum perfil SHALL cancelar ou remarcar um agendamento após o início do atendimento.

#### Scenario: Cancelamento após início
- **WHEN** cliente, barbeiro ou proprietário tenta cancelar um agendamento já iniciado
- **THEN** a operação é recusada e o agendamento permanece em andamento

### Requirement: Registro do cancelamento
Todo cancelamento SHALL registrar o autor e a data e hora da ocorrência. Justificativa MUST NOT ser exigida, exceto na sugestão de novo horário.

#### Scenario: Auditoria do cancelamento
- **WHEN** um cancelamento é concluído
- **THEN** o autor e o momento ficam registrados e consultáveis

### Requirement: Remarcação pelo cliente
O cliente SHALL remarcar o próprio agendamento ainda não iniciado para um horário disponível, atualizando o agendamento existente e preservando seu histórico.

#### Scenario: Remarcação válida
- **WHEN** o cliente escolhe novo horário disponível para um agendamento próprio não iniciado
- **THEN** o agendamento é atualizado e o histórico anterior é preservado

#### Scenario: Novo horário indisponível
- **WHEN** o novo horário está ocupado ou fora das regras de disponibilidade
- **THEN** a remarcação é recusada e o agendamento permanece no horário original

### Requirement: Sugestão de novo horário
Ao cancelar um agendamento da própria agenda, o barbeiro SHALL informar motivo e sugerir novo horário. O horário sugerido MUST ficar temporariamente reservado por até 24 horas. O cliente SHALL aceitar ou rejeitar.

#### Scenario: Sugestão sem motivo
- **WHEN** o barbeiro tenta cancelar com sugestão sem informar o motivo
- **THEN** a sugestão é recusada

#### Scenario: Cliente aceita a sugestão
- **WHEN** o cliente aceita o horário sugerido dentro do prazo
- **THEN** o agendamento é transferido para o novo horário e o histórico é preservado

#### Scenario: Cliente rejeita a sugestão
- **WHEN** o cliente rejeita o horário sugerido
- **THEN** o horário é liberado e o agendamento original permanece cancelado

#### Scenario: Sugestão expira
- **WHEN** o cliente não responde dentro de 24 horas
- **THEN** o horário sugerido é liberado e o agendamento original permanece cancelado

### Requirement: Histórico append-only do agendamento
O histórico SHALL registrar criação, cancelamento, remarcação, sugestão e transferência, sem alteração ou exclusão posterior pela aplicação.

#### Scenario: Consulta do histórico
- **WHEN** um agendamento com eventos anteriores é consultado
- **THEN** os eventos são retornados em ordem cronológica com autor e momento
