# Spec Delta

## Purpose

Define as notificações internas geradas por eventos de agendamento, sua consulta por polling e a execução idempotente dos processos periódicos já descritos no TDD, sem canais externos na V1.0.

## ADDED Requirements

### Requirement: Notificação interna por evento de agendamento

O sistema SHALL criar notificações internas para os usuários envolvidos quando ocorrer evento relacionado a agendamento: criação, cancelamento, sugestão de novo horário, aceite, rejeição ou expiração da sugestão, início e fim do atendimento e registro de pagamento. Cada notificação MUST ter tipo, conteúdo, destinatário, barbearia e momento de criação.

#### Scenario: Evento gera notificação para os envolvidos

- **WHEN** um evento relacionado a agendamento ocorre
- **THEN** os usuários envolvidos recebem uma notificação interna correspondente ao tipo do evento

#### Scenario: Notificação sem duplicidade

- **WHEN** o mesmo evento é processado mais de uma vez
- **THEN** nenhuma notificação adicional é criada para o mesmo evento e destinatário

### Requirement: Notificações restritas à V1.0

As notificações SHALL ser exclusivamente internas ao sistema. Nenhum envio automático por WhatsApp, e-mail, SMS ou outro canal externo MUST ocorrer na V1.0.

#### Scenario: Nenhum canal externo disparado

- **WHEN** um evento de agendamento gera notificação na V1.0
- **THEN** nenhuma mensagem é enviada por WhatsApp, e-mail ou SMS

### Requirement: Consulta por polling e marcação de leitura

O usuário autenticado SHALL consultar as próprias notificações e a quantidade de não lidas por polling, marcar uma notificação como lida e marcar todas as próprias notificações como lidas. Nenhum usuário MUST acessar ou alterar notificações de outro destinatário.

#### Scenario: Consulta das próprias notificações

- **WHEN** o usuário autenticado consulta suas notificações
- **THEN** recebe apenas as notificações destinadas a ele, com a contagem de não lidas

#### Scenario: Marcação de leitura

- **WHEN** o destinatário marca uma notificação como lida
- **THEN** a notificação passa a lida e a contagem de não lidas é atualizada

#### Scenario: Marcação em notificação de outro usuário

- **WHEN** um usuário tenta marcar como lida uma notificação de outro destinatário
- **THEN** a operação é negada

### Requirement: Expiração de sugestão de horário

O sistema SHALL liberar o horário sugerido que atingir o prazo de 24 horas sem resposta do cliente, notificando cliente, profissional e proprietário. O processo MUST ser idempotente, de modo que execuções repetidas ou concorrentes não liberem o horário duas vezes nem gerem notificações duplicadas.

#### Scenario: Sugestão expirada

- **WHEN** uma sugestão pendente atinge o prazo de 24 horas
- **THEN** o horário é liberado e os envolvidos recebem a notificação de expiração

#### Scenario: Execução repetida após a expiração

- **WHEN** o processo executa novamente após já ter expirado a mesma sugestão
- **THEN** nenhum horário adicional é liberado e nenhuma notificação duplicada é criada

### Requirement: Fechamento de agendamentos vencidos

O sistema SHALL marcar como não comparecimento os agendamentos confirmados cujo início já passou do limite definido no TDD, sem alterar agendamentos em andamento ou finalizados. O processo MUST ser idempotente.

#### Scenario: Agendamento confirmado vencido

- **WHEN** um agendamento confirmado ultrapassa o limite posterior ao horário de início sem atendimento
- **THEN** ele passa a não comparecimento

#### Scenario: Reprocessamento do fechamento

- **WHEN** o processo executa novamente sobre agendamentos já tratados
- **THEN** nenhum status é alterado novamente

### Requirement: Expurgo de registros de idempotência

O sistema SHALL remover os registros de idempotência cujo prazo de expiração já passou, preservando a proteção enquanto o prazo estiver vigente.

#### Scenario: Registro expirado é removido

- **WHEN** um registro de idempotência atinge a data de expiração
- **THEN** ele é removido do armazenamento

#### Scenario: Registro vigente é preservado

- **WHEN** um registro de idempotência ainda está dentro do prazo
- **THEN** ele é mantido

### Requirement: Reconciliação de notificações

O sistema SHALL reprocessar eventos de agendamento que não resultaram em notificação, garantindo que a notificação correspondente seja criada uma única vez por evento e destinatário.

#### Scenario: Evento sem notificação é reconciliado

- **WHEN** existe evento de agendamento sem notificação correspondente
- **THEN** a reconciliação cria a notificação faltante

#### Scenario: Evento já reconciliado

- **WHEN** a reconciliação encontra evento que já possui notificação
- **THEN** nenhuma notificação adicional é criada

### Requirement: Execução idempotente independente do mecanismo de agendamento

Os processos periódicos SHALL ser executados por um adaptador de scheduler cuja escolha permanece pendente. Cada execução MUST registrar início, fim, resultado e quantidade processada, e a aplicação MUST NOT depender de biblioteca de agendamento no domínio.

#### Scenario: Execução registrada

- **WHEN** qualquer processo periódico é executado
- **THEN** o registro de execução contém início, fim, resultado e quantidade processada

#### Scenario: Falha em uma execução

- **WHEN** um processo periódico falha durante a execução
- **THEN** a falha é registrada e a próxima execução reprocessa os registros pendentes sem duplicar efeitos
