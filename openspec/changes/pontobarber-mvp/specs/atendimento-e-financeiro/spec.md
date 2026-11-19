# Spec Delta

## Purpose

Define a execução do atendimento no dia do agendamento, o registro dos horários reais, a marcação de não comparecimento e o pagamento presencial com situação independente.

## ADDED Requirements

### Requirement: Início do atendimento
O barbeiro SHALL iniciar o atendimento de um agendamento confirmado quando o cliente comparecer. O sistema MUST registrar automaticamente o horário real de início.

#### Scenario: Início válido
- **WHEN** o barbeiro inicia um agendamento confirmado
- **THEN** o atendimento passa a em andamento e o horário real de início é registrado

#### Scenario: Início em agendamento não confirmado
- **WHEN** o barbeiro tenta iniciar atendimento de agendamento cancelado ou não comparecido
- **THEN** a operação é recusada

### Requirement: Fim do atendimento
O barbeiro SHALL finalizar um atendimento em andamento. O sistema MUST registrar automaticamente o horário real de término e MUST NOT permitir finalizar atendimento não iniciado.

#### Scenario: Finalização válida
- **WHEN** o barbeiro finaliza um atendimento em andamento
- **THEN** o atendimento passa a finalizado e o horário real de término é registrado

#### Scenario: Finalização sem início
- **WHEN** o barbeiro tenta finalizar um atendimento não iniciado
- **THEN** a operação é recusada

### Requirement: Não comparecimento
O barbeiro SHALL marcar um agendamento confirmado como não compareceu. O status MUST ser distinto de cancelamento e MUST impedir início ou finalização de atendimento.

#### Scenario: Marcação de não comparecimento
- **WHEN** o barbeiro marca um agendamento confirmado como não compareceu
- **THEN** o status é registrado como não comparecimento, sem tratá-lo como cancelamento

#### Scenario: Atendimento em agendamento não comparecido
- **WHEN** o barbeiro tenta iniciar ou finalizar atendimento de um agendamento marcado como não compareceu
- **THEN** a operação é recusada

### Requirement: Registro de pagamento
O barbeiro SHALL registrar o pagamento de um atendimento informando valor não negativo e forma entre dinheiro, Pix, cartão de débito ou cartão de crédito. A situação do pagamento MUST ser independente da situação do atendimento.

#### Scenario: Pagamento válido
- **WHEN** o barbeiro informa valor válido e forma permitida
- **THEN** o pagamento é registrado e passa a compor os recebimentos da barbearia

#### Scenario: Atendimento finalizado sem pagamento
- **WHEN** um atendimento é finalizado sem pagamento registrado
- **THEN** permanece finalizado com pagamento pendente, com as duas situações exibidas separadamente

#### Scenario: Valor negativo ou forma inválida
- **WHEN** o barbeiro informa valor negativo ou forma de pagamento não permitida
- **THEN** o pagamento não é registrado

#### Scenario: Pagamento duplicado
- **WHEN** já existe pagamento para o atendimento e um novo é enviado
- **THEN** a operação é recusada sem duplicar o registro

### Requirement: Recebimentos excluem agendamentos cancelados
O sistema MUST NOT considerar agendamentos cancelados como pagamentos recebidos.

#### Scenario: Agendamento cancelado com registro financeiro
- **WHEN** o total recebido do período é calculado
- **THEN** agendamentos cancelados não são somados como recebimento
