# Spec Delta

## Purpose

Define o painel inicial do proprietário com os quatro indicadores operacionais e a lista de próximos agendamentos, sempre restritos à barbearia autenticada e calculados no fuso do estabelecimento.

## ADDED Requirements

### Requirement: Dashboard como primeira tela do proprietário

Após concluir a configuração inicial da barbearia, o proprietário SHALL ser conduzido ao dashboard ao acessar o sistema.

#### Scenario: Acesso após configuração inicial

- **WHEN** um proprietário que concluiu a configuração inicial acessa o sistema
- **THEN** o dashboard é a primeira tela apresentada

### Requirement: Indicadores operacionais

O dashboard SHALL apresentar os indicadores Hoje, Esta semana, Cancelamentos e Recebido no mês. "Hoje" MUST considerar os agendamentos da data atual; "Esta semana" MUST considerar a semana atual de segunda a domingo; "Cancelamentos" MUST considerar os agendamentos cancelados no mês atual; "Recebido no mês" MUST somar exclusivamente os pagamentos registrados no mês atual.

#### Scenario: Períodos refletem os dados corretos

- **WHEN** o dashboard é exibido em um mês com agendamentos, cancelamentos e pagamentos
- **THEN** cada indicador reflete apenas o período e o tipo de registro que lhe corresponde

#### Scenario: Agendamento cancelado não entra como recebimento

- **WHEN** existe agendamento cancelado no mês
- **THEN** ele não é somado ao indicador de recebimentos

#### Scenario: Cálculo no fuso da barbearia

- **WHEN** o dashboard é consultado próximo da virada do dia
- **THEN** os períodos são calculados no fuso horário da barbearia

### Requirement: Próximos agendamentos

O dashboard SHALL listar os próximos agendamentos futuros da barbearia, apresentando horário, cliente, profissional e situação em cada item.

#### Scenario: Lista com agendamentos futuros

- **WHEN** existem agendamentos futuros na barbearia
- **THEN** a lista apresenta horário, cliente, profissional e situação de cada um

#### Scenario: Lista sem agendamentos futuros

- **WHEN** não existem agendamentos futuros
- **THEN** a lista é apresentada vazia, sem erro

### Requirement: Indicadores sem movimentação

O dashboard MUST apresentar os indicadores mesmo quando não houver movimentação, exibindo valor zero.

#### Scenario: Dashboard sem dados

- **WHEN** a barbearia não possui agendamentos nem pagamentos no período
- **THEN** os indicadores são exibidos com valor zero

### Requirement: Isolamento dos dados do dashboard

Os dados apresentados MUST pertencer somente à barbearia do proprietário autenticado.

#### Scenario: Dois proprietários de barbearias distintas

- **WHEN** dois proprietários de barbearias diferentes acessam o dashboard
- **THEN** cada um visualiza apenas os dados da própria barbearia

#### Scenario: Barbeiro tenta acessar o dashboard

- **WHEN** um barbeiro vinculado, que não é proprietário, tenta consultar o dashboard
- **THEN** o acesso é negado
