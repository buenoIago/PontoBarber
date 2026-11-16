# PontoBarber — PRD

> **Documento:** Product Requirements Document  
> **Projeto:** PontoBarber  
> **Versão:** 1.0  
> **Status:** Consolidado para desenvolvimento  
> **Data:** 16/11/2026

---

# 1. Descrição do projeto

O **PontoBarber** é um sistema de gerenciamento e agendamento para barbearias, desenvolvido para centralizar a administração do estabelecimento, o gerenciamento de profissionais, os serviços oferecidos, os horários de funcionamento e os atendimentos.

A plataforma permitirá que proprietários configurem suas barbearias, cadastrem serviços, gerenciem profissionais e acompanhem os agendamentos em uma agenda centralizada. Os barbeiros poderão administrar sua própria disponibilidade, acompanhar os atendimentos e registrar pagamentos.

Os clientes poderão criar uma conta, consultar a página pública de uma barbearia, escolher um serviço, selecionar um profissional e realizar agendamentos automaticamente, sem necessidade de aprovação manual.

O sistema também permitirá que clientes acompanhem seus agendamentos, realizem cancelamentos e solicitem remarcações diretamente pela plataforma.

O objetivo é oferecer uma solução simples e completa para organizar a rotina de barbearias, reduzindo a dependência de agendamentos manuais e facilitando o relacionamento entre estabelecimentos, profissionais e clientes.

# 2. Objetivos do sistema

## 2.1. Objetivo geral

Desenvolver uma plataforma de agendamento e gerenciamento de barbearias que permita organizar os processos administrativos e operacionais do estabelecimento em um único sistema.

## 2.2. Objetivos específicos

- Permitir o cadastro de proprietários, barbeiros e clientes.
- Permitir a criação e configuração de barbearias.
- Disponibilizar um catálogo inicial de serviços.
- Permitir que cada barbearia personalize seus serviços e preços.
- Gerenciar horários gerais de funcionamento.
- Gerenciar a disponibilidade individual dos profissionais.
- Disponibilizar uma agenda com visualizações diária, semanal e mensal.
- Permitir agendamentos públicos e manuais.
- Confirmar automaticamente os agendamentos realizados.
- Permitir cancelamentos e remarcações.
- Registrar o início e a conclusão dos atendimentos.
- Registrar pagamentos realizados presencialmente.
- Disponibilizar indicadores para acompanhamento da operação.

# 3. Perfis de usuários

## 3.1. Proprietário

Responsável pela administração da barbearia.

Poderá:

- Criar uma barbearia.
- Configurar os dados do estabelecimento.
- Gerenciar os serviços oferecidos.
- Configurar os horários gerais de funcionamento.
- Gerenciar os profissionais vinculados.
- Disponibilizar o código de convite da barbearia.
- Consultar os clientes relacionados ao estabelecimento.
- Visualizar e gerenciar todos os agendamentos.
- Acompanhar indicadores no Dashboard.
- Atuar também como barbeiro, possuindo agenda e disponibilidade próprias.

## 3.2. Barbeiro

Profissional vinculado a uma barbearia por meio de um código de convite.

Poderá:

- Consultar sua agenda.
- Configurar sua disponibilidade semanal.
- Bloquear horários específicos.
- Acompanhar seus agendamentos.
- Consultar os detalhes dos agendamentos.
- Iniciar e finalizar atendimentos.
- Registrar pagamentos.
- Cancelar agendamentos de sua própria agenda.
- Marcar clientes como não comparecentes.

O barbeiro não poderá administrar os serviços da barbearia.

## 3.3. Cliente

Pessoa que utiliza a plataforma para realizar agendamentos.

Poderá:

- Criar uma conta.
- Editar seus próprios dados cadastrais.
- Consultar a página pública de uma barbearia.
- Escolher um serviço.
- Escolher um profissional.
- Consultar horários disponíveis.
- Realizar agendamentos.
- Consultar seus agendamentos.
- Cancelar agendamentos antes do início do atendimento.
- Remarcar agendamentos antes do início do atendimento.
- Aceitar ou rejeitar sugestões de novos horários feitas pelo barbeiro.

# 4. Escopo do projeto

## 4.1. Funcionalidades incluídas na V1.0

| Módulo | Funcionalidades |
|---|---|
| Autenticação | Cadastro de clientes e barbeiros, login e identificação dos perfis |
| Empresas | Cadastro e configuração da barbearia |
| Serviços | Catálogo inicial e gerenciamento de serviços |
| Horários | Funcionamento geral e disponibilidade individual |
| Dashboard | Indicadores e próximos agendamentos |
| Agenda | Visualização, filtros, agendamentos e bloqueios |
| Agendamentos | Criação pública e manual, cancelamento e remarcação |
| Clientes | Consulta e pesquisa de clientes relacionados à barbearia |
| Profissionais | Gerenciamento dos profissionais vinculados |
| Atendimentos | Registro de início, conclusão e não comparecimento |
| Pagamentos | Registro presencial e acompanhamento de pagamentos |
| Página pública | Consulta de serviços, profissionais e horários disponíveis |

## 4.2. Funcionalidades fora do escopo da V1.0

As funcionalidades abaixo não fazem parte da primeira versão:

- Envio automático de mensagens pelo WhatsApp.
- Pagamentos antecipados.
- Integração com gateways de pagamento.
- Agendamento sem criação de conta.
- Aprovação manual obrigatória dos agendamentos.
- Gestão financeira avançada.
- Funcionalidades adicionais de administração não previstas nos módulos definidos.

Essas possibilidades poderão ser avaliadas em versões futuras.

# 5. Jornadas principais

## 5.1. Cadastro do proprietário

1. Usuário acessa a plataforma.
2. Seleciona a opção "Sou barbeiro".
3. Realiza o cadastro com seus dados pessoais.
4. Escolhe a opção de criar uma barbearia.
5. Informa os dados do estabelecimento.
6. Seleciona os serviços disponíveis no catálogo ou cadastra serviços personalizados.
7. Define os preços e as durações.
8. Configura os horários gerais de funcionamento.
9. Configura sua própria disponibilidade como profissional.
10. Acessa o Dashboard.

## 5.2. Entrada de um profissional

1. Usuário seleciona "Sou barbeiro".
2. Realiza seu cadastro.
3. Escolhe a opção de entrar em uma barbearia.
4. Informa o código de convite.
5. O sistema vincula sua conta à barbearia.
6. O profissional configura sua disponibilidade semanal.
7. Passa a receber agendamentos conforme sua disponibilidade.

## 5.3. Agendamento público

1. Cliente acessa a página pública de uma barbearia.
2. Realiza login ou cria uma conta.
3. Escolhe um serviço.
4. Escolhe um profissional.
5. Escolhe uma data e um horário disponível.
6. Informa uma observação opcional.
7. Confirma o agendamento.
8. O sistema registra o agendamento como confirmado.

## 5.4. Atendimento presencial

1. Barbeiro consulta o agendamento.
2. Cliente comparece à barbearia.
3. Barbeiro inicia o atendimento.
4. O sistema registra automaticamente o horário de início.
5. Barbeiro realiza o serviço.
6. Barbeiro finaliza o atendimento.
7. O sistema registra automaticamente o horário de término.
8. O pagamento é realizado presencialmente.
9. O profissional registra o pagamento e sua forma.

## 5.5. Remarcação pelo cliente

1. Cliente acessa seus agendamentos.
2. Seleciona um agendamento ainda não iniciado.
3. Solicita a remarcação.
4. Escolhe uma nova data e horário disponíveis.
5. O sistema valida a disponibilidade.
6. O agendamento existente é atualizado para o novo horário.

# 6. Módulos e funcionalidades

## 6.1. Autenticação e cadastro

### Requisitos Funcionais

- **RF-001:** O sistema deverá disponibilizar uma tela inicial com as opções "Sou cliente" e "Sou barbeiro".
- **RF-002:** O sistema deverá disponibilizar formulários de cadastro específicos para clientes e barbeiros.
- **RF-003:** O sistema deverá permitir que usuários realizem login por meio de uma tela compartilhada.
- **RF-004:** O sistema deverá permitir que barbeiros escolham entre criar uma barbearia ou entrar em uma existente.
- **RF-005:** O sistema deverá permitir que barbeiros ingressem em uma barbearia utilizando um código de convite.
- **RF-006:** O sistema deverá manter a conta do profissional mesmo após a remoção de seu vínculo com uma barbearia.

### Regras de Negócio

- **RN-001:** Todo usuário deverá possuir uma conta para utilizar as funcionalidades que exigem autenticação.
- **RN-002:** Clientes deverão possuir conta para realizar agendamentos.
- **RN-003:** O cadastro de cliente não deverá exigir vínculo prévio com uma barbearia.
- **RN-004:** O login será compartilhado entre os diferentes perfis de usuário.
- **RN-005:** O código de convite será permanente e único por barbearia.
- **RN-006:** Somente o proprietário poderá disponibilizar o código de convite aos funcionários.
- **RN-007:** Ao sair de uma barbearia, o profissional perderá somente o vínculo com o estabelecimento. Sua conta permanecerá ativa na plataforma.

### Campos obrigatórios

| Campo | Obrigatoriedade |
|---|---|
| Nome | Obrigatório |
| E-mail | Obrigatório |
| Telefone | Obrigatório |
| Senha | Obrigatório |
| Confirmar senha | Obrigatório |

### Restrições e validações

- O e-mail deverá possuir formato válido.
- A confirmação de senha deverá corresponder à senha informada.
- O sistema deverá impedir o cadastro de contas com e-mail já utilizado.
- O código de convite deverá corresponder a uma barbearia existente.
- O sistema deverá identificar corretamente o perfil e o vínculo do usuário.

## 6.2. Cadastro e gerenciamento de empresas

### Requisitos Funcionais

- **RF-007:** O sistema deverá permitir que um barbeiro crie uma barbearia.
- **RF-008:** O sistema deverá permitir que o proprietário consulte os dados do estabelecimento.
- **RF-009:** O sistema deverá permitir que o proprietário edite os dados da barbearia.
- **RF-010:** O sistema deverá disponibilizar um código de convite permanente para o estabelecimento.

### Regras de Negócio

- **RN-008:** O barbeiro responsável pela criação da barbearia será definido como proprietário.
- **RN-009:** Cada barbearia deverá possuir um proprietário responsável pela administração.
- **RN-010:** O cadastro deverá solicitar somente os dados necessários para identificação e funcionamento do estabelecimento.
- **RN-011:** Não será exigido CNPJ ou documentação pessoal para o cadastro inicial.
- **RN-012:** O telefone comercial deverá ser verificado.
- **RN-013:** Instagram será uma informação opcional.
- **RN-014:** A presença digital poderá servir como informação complementar de confiança, sem ser obrigatória.

### Campos obrigatórios

| Campo | Obrigatoriedade |
|---|---|
| Nome da barbearia | Obrigatório |
| Telefone comercial | Obrigatório |
| E-mail comercial | Obrigatório |
| Cidade | Obrigatório |
| Endereço | Obrigatório |
| Instagram | Opcional |

### Restrições e validações

- O telefone comercial deverá possuir formato válido.
- O telefone comercial deverá passar pelo processo de verificação.
- O e-mail comercial deverá possuir formato válido.
- Cidade e endereço deverão ser informados.
- O Instagram, quando preenchido, deverá possuir formato válido.
- Não serão solicitados CEP ou divisão detalhada do endereço na V1.0.

## 6.3. Gerenciamento de serviços

### Requisitos Funcionais

- **RF-011:** O sistema deverá disponibilizar um catálogo inicial de serviços.
- **RF-012:** O proprietário deverá poder selecionar serviços do catálogo para sua barbearia.
- **RF-013:** O proprietário deverá poder cadastrar serviços personalizados.
- **RF-014:** O proprietário deverá poder editar nome, preço e duração dos serviços.
- **RF-015:** O proprietário deverá poder remover serviços da lista de serviços disponíveis.
- **RF-016:** Os profissionais deverão poder consultar os serviços cadastrados, sem permissão para alterá-los.

### Catálogo inicial

| Serviço | Duração inicial |
|---|---:|
| Corte | 40 minutos |
| Barba | 30 minutos |
| Corte + Barba | 70 minutos |

Os valores apresentados representam durações iniciais sugeridas. O proprietário poderá ajustá-las.

### Regras de Negócio

- **RN-015:** O catálogo será utilizado como modelo para facilitar a configuração inicial.
- **RN-016:** Cada barbearia terá seus próprios serviços independentes.
- **RN-017:** O proprietário deverá definir o preço de cada serviço.
- **RN-018:** O proprietário poderá ajustar a duração de cada serviço.
- **RN-019:** Corte + Barba será tratado como um serviço independente, com preço e duração próprios.
- **RN-020:** Somente o proprietário poderá adicionar, editar ou remover serviços.
- **RN-021:** A remoção de um serviço deverá desvinculá-lo dos serviços disponíveis da barbearia.
- **RN-022:** Serviços removidos não poderão ser selecionados em novos agendamentos.
- **RN-023:** Agendamentos anteriores deverão preservar as informações originais dos serviços contratados, incluindo nome, duração e valor.

### Campos obrigatórios

| Campo | Obrigatoriedade |
|---|---|
| Nome | Obrigatório |
| Preço | Obrigatório |
| Duração | Obrigatório |

### Restrições e validações

- O preço deverá ser maior que zero.
- A duração deverá ser maior que zero.
- O nome do serviço deverá ser informado.
- Serviços removidos não deverão aparecer na página pública.
- A remoção não deverá alterar registros históricos.

## 6.4. Configuração de horários

### Requisitos Funcionais

- **RF-017:** O sistema deverá permitir que o proprietário configure os horários gerais de funcionamento da barbearia.
- **RF-018:** O sistema deverá permitir configurar horários individualmente para cada dia da semana.
- **RF-019:** O sistema deverá permitir marcar dias como fechados.
- **RF-020:** O sistema deverá permitir que cada barbeiro configure sua própria disponibilidade semanal.
- **RF-021:** O sistema deverá permitir que barbeiros alterem sua disponibilidade posteriormente.

### Regras de Negócio

- **RN-024:** Os horários gerais representarão o período de funcionamento do estabelecimento.
- **RN-025:** Os horários individuais representarão a disponibilidade de cada profissional.
- **RN-026:** A disponibilidade individual deverá estar dentro do horário geral da barbearia.
- **RN-027:** Um profissional não poderá receber agendamentos fora de sua disponibilidade.
- **RN-028:** Os horários semanais serão independentes dos bloqueios pontuais realizados na agenda.

### Campos obrigatórios

**Horário geral:**

| Campo | Obrigatoriedade |
|---|---|
| Dia da semana | Obrigatório |
| Horário de abertura | Obrigatório quando o dia estiver aberto |
| Horário de fechamento | Obrigatório quando o dia estiver aberto |
| Indicador de dia fechado | Obrigatório |

**Disponibilidade individual:**

| Campo | Obrigatoriedade |
|---|---|
| Profissional | Obrigatório |
| Dia da semana | Obrigatório |
| Horário de início | Obrigatório quando houver expediente |
| Horário de término | Obrigatório quando houver expediente |
| Indicador de folga | Obrigatório |

### Restrições e validações

- O horário de fechamento deverá ser posterior ao horário de abertura.
- A disponibilidade individual deverá respeitar os horários gerais.
- Dias marcados como fechados não poderão receber novos agendamentos.
- Folgas individuais deverão impedir novos agendamentos para o profissional.

## 6.5. Dashboard

### Requisitos Funcionais

- **RF-022:** O sistema deverá apresentar um Dashboard após a conclusão da configuração inicial da barbearia.
- **RF-023:** O Dashboard deverá apresentar a quantidade de agendamentos do dia.
- **RF-024:** O Dashboard deverá apresentar a quantidade de agendamentos da semana.
- **RF-025:** O Dashboard deverá apresentar a quantidade de cancelamentos do mês.
- **RF-026:** O Dashboard deverá apresentar o valor recebido no mês.
- **RF-027:** O Dashboard deverá apresentar os próximos agendamentos.
- **RF-028:** O Dashboard deverá apresentar informações reais do estabelecimento.

### Indicadores

| Indicador | Informação apresentada |
|---|---|
| Hoje | Quantidade de agendamentos do dia atual |
| Esta semana | Quantidade de agendamentos da semana atual |
| Cancelamentos | Quantidade de cancelamentos do mês atual |
| Recebido no mês | Soma dos pagamentos registrados no mês atual |
| Próximos agendamentos | Horário, cliente, profissional e status |

### Regras de Negócio

- **RN-029:** Os indicadores deverão considerar os dados da barbearia correspondente.
- **RN-030:** O valor recebido no mês deverá considerar pagamentos efetivamente registrados.
- **RN-031:** Na ausência de agendamentos ou pagamentos, os indicadores deverão apresentar zero.
- **RN-032:** Os próximos agendamentos deverão apresentar informações suficientes para identificar o atendimento.

## 6.6. Agenda

### Requisitos Funcionais

- **RF-029:** O sistema deverá disponibilizar uma agenda centralizada.
- **RF-030:** A agenda deverá possuir visualizações diária, semanal e mensal.
- **RF-031:** O usuário deverá poder navegar entre datas anteriores e posteriores.
- **RF-032:** A agenda deverá possuir uma opção para retornar à data atual.
- **RF-033:** O proprietário deverá poder filtrar a agenda por profissional.
- **RF-034:** O sistema deverá permitir bloquear períodos específicos.
- **RF-035:** O sistema deverá permitir criar agendamentos manualmente.
- **RF-036:** O sistema deverá apresentar os agendamentos e bloqueios visualmente diferenciados.

### Visualizações

- **Dia:** apresenta os horários e agendamentos do dia selecionado.
- **Semana:** apresenta os dias de segunda-feira a domingo.
- **Mês:** apresenta a distribuição dos agendamentos ao longo do mês.

### Identificação visual

| Cor | Significado |
|---|---|
| Verde | Agendamento confirmado |
| Vermelho | Agendamento cancelado |
| Cinza | Horário bloqueado |

### Bloqueio de horários

#### Requisitos Funcionais

- **RF-037:** O profissional deverá poder bloquear períodos em sua própria agenda.
- **RF-038:** O sistema deverá permitir informar data e horário de início e término.
- **RF-039:** O sistema deverá permitir informar um motivo opcional para o bloqueio.

#### Regras de Negócio

- **RN-033:** O bloqueio deverá impedir novos agendamentos durante o período informado.
- **RN-034:** Cada profissional deverá administrar seus próprios bloqueios.
- **RN-035:** Os bloqueios deverão respeitar a agenda do profissional correspondente.

#### Campos obrigatórios

| Campo | Obrigatoriedade |
|---|---|
| Data de início | Obrigatório |
| Horário de início | Obrigatório |
| Data de término | Obrigatório |
| Horário de término | Obrigatório |
| Motivo | Opcional |

### Criação manual de agendamentos

#### Requisitos Funcionais

- **RF-040:** O sistema deverá permitir que usuários autorizados criem agendamentos diretamente pela agenda.
- **RF-041:** O sistema deverá permitir selecionar um cliente.
- **RF-042:** O sistema deverá permitir selecionar um profissional.
- **RF-043:** O sistema deverá permitir selecionar uma data e horário.
- **RF-044:** O sistema deverá permitir selecionar um serviço.
- **RF-045:** O sistema deverá permitir informar observações opcionais.
- **RF-046:** O sistema deverá calcular automaticamente o horário de término e o valor total.

#### Campos obrigatórios

| Campo | Obrigatoriedade |
|---|---|
| Cliente | Obrigatório |
| Profissional | Obrigatório |
| Data | Obrigatório |
| Horário de início | Obrigatório |
| Serviço | Obrigatório |
| Observações | Opcional |

#### Regras de Negócio

- **RN-036:** Agendamentos manuais deverão ser confirmados automaticamente.
- **RN-037:** O horário de término deverá ser calculado com base na duração do serviço.
- **RN-038:** O valor deverá ser obtido com base no preço do serviço no momento da contratação.
- **RN-039:** O sistema deverá impedir agendamentos fora do funcionamento da barbearia.
- **RN-040:** O sistema deverá respeitar a disponibilidade individual do profissional.
- **RN-041:** O sistema deverá impedir conflitos com outros agendamentos e bloqueios.

## 6.7. Agendamentos

### Requisitos Funcionais

- **RF-047:** O sistema deverá permitir a criação de agendamentos pela página pública.
- **RF-048:** O sistema deverá confirmar automaticamente os agendamentos válidos.
- **RF-049:** O sistema deverá permitir que clientes consultem seus próprios agendamentos.
- **RF-050:** O sistema deverá permitir que clientes cancelem seus agendamentos antes do início do atendimento.
- **RF-051:** O sistema deverá permitir que clientes remarquem seus agendamentos antes do início do atendimento.
- **RF-052:** O sistema deverá permitir que barbeiros cancelem agendamentos de suas próprias agendas.
- **RF-053:** O proprietário deverá poder cancelar qualquer agendamento da barbearia.
- **RF-054:** O sistema deverá registrar quem realizou o cancelamento e quando.
- **RF-055:** O sistema deverá permitir que barbeiros sugiram novos horários para clientes.
- **RF-056:** O sistema deverá permitir que clientes aceitem ou rejeitem horários sugeridos.
- **RF-057:** O sistema deverá permitir que barbeiros marquem agendamentos como não comparecimento.

### Regras de Negócio

- **RN-042:** Todo agendamento público deverá exigir autenticação do cliente.
- **RN-043:** Os dados do cliente deverão ser obtidos de sua própria conta.
- **RN-044:** Cada agendamento deverá permitir selecionar apenas um serviço.
- **RN-045:** Todo agendamento válido deverá ser confirmado automaticamente.
- **RN-046:** Não haverá aprovação manual obrigatória do barbeiro.
- **RN-047:** O cliente poderá cancelar a qualquer momento antes do início do atendimento, sem antecedência mínima.
- **RN-048:** O cliente poderá remarcar a qualquer momento antes do início do atendimento, sem antecedência mínima.
- **RN-049:** A remarcação deverá atualizar o agendamento existente, preservando seu histórico.
- **RN-050:** A nova data e horário deverão respeitar o funcionamento da barbearia, a disponibilidade do profissional, os bloqueios e a ausência de conflitos.
- **RN-051:** O barbeiro poderá cancelar agendamentos de sua própria agenda.
- **RN-052:** O proprietário poderá cancelar qualquer agendamento da barbearia.
- **RN-053:** O cancelamento não exigirá justificativa.
- **RN-054:** O sistema deverá registrar o responsável pelo cancelamento e o momento em que ocorreu.
- **RN-055:** Após o início do atendimento, o cancelamento e a remarcação deverão ser bloqueados.
- **RN-056:** Quando o barbeiro sugerir outro horário, deverá informar o motivo.
- **RN-057:** O cliente terá 24 horas para aceitar ou rejeitar a sugestão.
- **RN-058:** Durante o prazo de decisão, o horário sugerido ficará temporariamente reservado.
- **RN-059:** Caso o cliente aceite, o agendamento será transferido para o novo horário.
- **RN-060:** Caso o cliente rejeite a sugestão ou não responda dentro de 24 horas, o horário sugerido será liberado e o agendamento original permanecerá cancelado.
- **RN-061:** O barbeiro poderá marcar um agendamento como "Não compareceu".
- **RN-062:** O não comparecimento não deverá ser tratado como cancelamento.
- **RN-063:** Os registros históricos deverão ser preservados.

### Informações do agendamento

| Campo | Descrição |
|---|---|
| Cliente | Pessoa responsável pelo agendamento |
| Profissional | Barbeiro selecionado |
| Serviço | Serviço contratado |
| Data | Data do atendimento |
| Horário inicial | Início previsto |
| Horário final | Término previsto |
| Valor | Preço do serviço |
| Status | Situação do agendamento |
| Observação | Informação opcional |

## 6.8. Detalhes do agendamento

### Requisitos Funcionais

- **RF-058:** O sistema deverá disponibilizar uma tela de detalhes para cada agendamento.
- **RF-059:** A tela deverá apresentar os dados do cliente.
- **RF-060:** A tela deverá apresentar os dados do profissional.
- **RF-061:** A tela deverá apresentar data, horário inicial e horário final.
- **RF-062:** A tela deverá apresentar o serviço contratado, sua duração e seu valor.
- **RF-063:** A tela deverá apresentar o status do agendamento.
- **RF-064:** A tela deverá apresentar as informações de pagamento.
- **RF-065:** A tela deverá disponibilizar ações de início e finalização do atendimento conforme sua situação.

### Informações apresentadas

- Identificador do agendamento.
- Cliente.
- Profissional.
- Data e horário.
- Status do agendamento.
- Observações.
- Serviço contratado.
- Duração do serviço.
- Valor do serviço.
- Situação do pagamento.
- Forma de pagamento, quando registrada.
- Horário real de início do atendimento.
- Horário real de término do atendimento.

### Regras de Negócio

- **RN-064:** Os dados originais do agendamento deverão permanecer preservados.
- **RN-065:** A tela deverá apresentar separadamente a situação do agendamento, do atendimento e do pagamento.
- **RN-066:** A finalização do atendimento não deverá registrar automaticamente o pagamento.
- **RN-067:** O pagamento poderá permanecer pendente mesmo após a conclusão do atendimento.

## 6.9. Gerenciamento de atendimentos

### Requisitos Funcionais

- **RF-066:** O barbeiro deverá poder iniciar um atendimento.
- **RF-067:** O sistema deverá registrar automaticamente o horário real de início.
- **RF-068:** O barbeiro deverá poder finalizar um atendimento iniciado.
- **RF-069:** O sistema deverá registrar automaticamente o horário real de término.
- **RF-070:** O barbeiro deverá poder registrar o não comparecimento do cliente.

### Regras de Negócio

- **RN-068:** O atendimento será iniciado quando o cliente chegar à barbearia e o profissional acionar a opção "Iniciar".
- **RN-069:** O horário de início será registrado pelo sistema.
- **RN-070:** Ao concluir o serviço, o profissional deverá acionar a opção "Finalizar".
- **RN-071:** O horário de término será registrado pelo sistema.
- **RN-072:** Um atendimento iniciado não poderá ser cancelado ou remarcado.
- **RN-073:** O atendimento e o pagamento possuirão estados independentes.
- **RN-074:** O não comparecimento deverá ser registrado separadamente de um cancelamento.

## 6.10. Pagamentos

### Requisitos Funcionais

- **RF-071:** O sistema deverá permitir registrar pagamentos de agendamentos.
- **RF-072:** O sistema deverá permitir selecionar a forma de pagamento.
- **RF-073:** O sistema deverá apresentar o valor relacionado ao serviço contratado.
- **RF-074:** O sistema deverá apresentar a situação do pagamento.
- **RF-075:** O sistema deverá atualizar o indicador de recebimento mensal após o registro de um pagamento.

### Formas de pagamento da V1.0

- Dinheiro.
- Pix.
- Cartão de débito.
- Cartão de crédito.

### Regras de Negócio

- **RN-075:** Os pagamentos serão realizados presencialmente na barbearia.
- **RN-076:** Não haverá pagamento antecipado pela plataforma.
- **RN-077:** O registro do pagamento será realizado após ou ao final do atendimento.
- **RN-078:** O pagamento deverá possuir situação independente da situação do atendimento.
- **RN-079:** O Dashboard deverá considerar os pagamentos registrados para calcular o valor recebido no mês.

### Campos obrigatórios

| Campo | Obrigatoriedade |
|---|---|
| Agendamento | Obrigatório |
| Valor | Obrigatório |
| Forma de pagamento | Obrigatório |
| Situação do pagamento | Obrigatório |

### Restrições e validações

- A forma de pagamento deverá corresponder a uma das opções disponíveis.
- O valor registrado deverá ser válido.
- O sistema deverá impedir que um pagamento seja contabilizado mais de uma vez indevidamente.

## 6.11. Clientes

### Requisitos Funcionais

- **RF-076:** O sistema deverá disponibilizar uma tela de consulta de clientes relacionados à barbearia.
- **RF-077:** O sistema deverá permitir pesquisar clientes por nome.
- **RF-078:** O sistema deverá permitir consultar os dados cadastrais de um cliente.
- **RF-079:** O sistema deverá permitir que clientes editem seus próprios dados cadastrais.

### Regras de Negócio

- **RN-080:** Um cliente será relacionado a uma barbearia por meio de seus agendamentos.
- **RN-081:** A listagem deverá apresentar somente clientes que possuam relação com o estabelecimento.
- **RN-082:** O proprietário poderá consultar e pesquisar clientes.
- **RN-083:** O proprietário não poderá cadastrar, editar ou excluir clientes pela tela de gerenciamento.
- **RN-084:** O cliente poderá atualizar seu próprio nome, e-mail e telefone.
- **RN-085:** Alterações cadastrais não deverão modificar os dados históricos dos agendamentos anteriores.

### Campos apresentados

- Nome.
- E-mail.
- Telefone.

## 6.12. Profissionais

### Requisitos Funcionais

- **RF-080:** O sistema deverá disponibilizar uma tela de gerenciamento dos profissionais vinculados à barbearia.
- **RF-081:** O sistema deverá apresentar nome, telefone e situação do profissional.
- **RF-082:** O proprietário deverá poder consultar os dados profissionais.
- **RF-083:** O proprietário deverá poder editar dados profissionais.
- **RF-084:** O proprietário deverá poder ativar ou desativar profissionais.
- **RF-085:** O proprietário deverá poder remover o vínculo de um profissional com a barbearia.
- **RF-086:** O sistema deverá permitir que profissionais configurem sua própria disponibilidade.

### Regras de Negócio

- **RN-086:** Os profissionais deverão criar suas próprias contas.
- **RN-087:** O proprietário não poderá criar contas de funcionários arbitrariamente pela tela de gerenciamento.
- **RN-088:** A entrada de profissionais ocorrerá por meio do código de convite.
- **RN-089:** Ao desativar um profissional, ele não poderá receber novos agendamentos.
- **RN-090:** Ao remover um profissional, somente seu vínculo com a barbearia será encerrado.
- **RN-091:** A conta do profissional permanecerá na plataforma.
- **RN-092:** Os registros históricos dos agendamentos deverão ser preservados.
- **RN-093:** A listagem de profissionais não deverá carregar o histórico completo de atendimentos de cada profissional.

### Campos apresentados

- Nome.
- Telefone.
- Situação: ativo ou inativo.

## 6.13. Configurações

### Requisitos Funcionais

- **RF-087:** O sistema deverá disponibilizar uma área de configurações da barbearia.
- **RF-088:** O proprietário deverá poder consultar e alterar os horários gerais de funcionamento.
- **RF-089:** O proprietário deverá poder consultar e alterar os dados do estabelecimento.
- **RF-090:** O sistema deverá disponibilizar a consulta e o gerenciamento dos profissionais vinculados.

### Regras de Negócio

- **RN-094:** Somente o proprietário poderá alterar as configurações gerais da barbearia.
- **RN-095:** Cada profissional continuará responsável pela configuração de sua própria disponibilidade.
- **RN-096:** As configurações deverão refletir as informações utilizadas na agenda e na página pública.

# 7. Requisitos não funcionais

Os requisitos não funcionais estabelecem as características de qualidade esperadas do produto.

| Identificador | Requisito |
|---|---|
| RNF-001 | O sistema deverá exigir autenticação nas funcionalidades restritas. |
| RNF-002 | Os dados de cada barbearia deverão ser isolados, impedindo acesso indevido entre estabelecimentos. |
| RNF-003 | As permissões deverão respeitar o perfil e o vínculo do usuário. |
| RNF-004 | O sistema deverá preservar registros históricos de agendamentos e atendimentos. |
| RNF-005 | As operações de agendamento deverão evitar conflitos de horário. |
| RNF-006 | O sistema deverá apresentar mensagens claras para operações concluídas ou inválidas. |
| RNF-007 | O sistema deverá validar os dados recebidos antes de realizar operações. |
| RNF-008 | As informações exibidas no Dashboard deverão corresponder aos registros reais. |
| RNF-009 | O sistema deverá manter consistência entre agendamentos, atendimentos e pagamentos. |
| RNF-010 | A página pública deverá permitir que clientes consultem os horários disponíveis sem acessar funcionalidades administrativas. |

# 8. Critérios de aceite

A V1.0 será considerada funcional quando os fluxos abaixo puderem ser executados corretamente.

## 8.1. Cadastro e configuração

- [ ] Um barbeiro consegue criar uma conta.
- [ ] Um barbeiro consegue criar uma barbearia.
- [ ] O proprietário consegue cadastrar os dados do estabelecimento.
- [ ] O proprietário consegue selecionar serviços do catálogo.
- [ ] O proprietário consegue cadastrar serviços personalizados.
- [ ] O proprietário consegue configurar preços e durações.
- [ ] O proprietário consegue configurar os horários gerais.
- [ ] O proprietário consegue configurar sua disponibilidade individual.
- [ ] Outro barbeiro consegue entrar utilizando o código de convite.

## 8.2. Agenda e agendamentos

- [ ] O proprietário consegue visualizar a agenda.
- [ ] A agenda oferece visualizações diária, semanal e mensal.
- [ ] O proprietário consegue filtrar a agenda por profissional.
- [ ] O profissional consegue bloquear horários.
- [ ] O proprietário consegue criar agendamentos manualmente.
- [ ] O cliente consegue realizar agendamentos pela página pública.
- [ ] Os agendamentos válidos são confirmados automaticamente.
- [ ] O sistema impede conflitos de horário.
- [ ] O cliente consegue cancelar antes do início do atendimento.
- [ ] O cliente consegue remarcar antes do início do atendimento.
- [ ] O sistema impede cancelamentos e remarcações após o início do atendimento.
- [ ] O cliente consegue aceitar ou rejeitar uma sugestão de horário.
- [ ] A sugestão de horário respeita o prazo de 24 horas.

## 8.3. Atendimento e pagamento

- [ ] O profissional consegue iniciar um atendimento.
- [ ] O sistema registra automaticamente o início.
- [ ] O profissional consegue finalizar um atendimento.
- [ ] O sistema registra automaticamente o término.
- [ ] O profissional consegue marcar um não comparecimento.
- [ ] O profissional consegue registrar um pagamento.
- [ ] O pagamento permanece independente do status do atendimento.
- [ ] O Dashboard atualiza o valor recebido após o registro.

## 8.4. Gerenciamento

- [ ] O proprietário consegue consultar clientes relacionados à barbearia.
- [ ] O proprietário consegue pesquisar clientes.
- [ ] O cliente consegue editar os próprios dados cadastrais.
- [ ] O proprietário consegue consultar profissionais.
- [ ] O proprietário consegue ativar e desativar profissionais.
- [ ] O proprietário consegue remover o vínculo de um profissional.
- [ ] O sistema preserva os registros históricos após alterações e remoções.

# 9. Dados e informações do domínio

O sistema deverá trabalhar com os seguintes conceitos principais:

| Conceito | Responsabilidade |
|---|---|
| Usuário | Representa uma conta cadastrada na plataforma |
| Barbearia | Representa o estabelecimento |
| Vínculo profissional | Relaciona um barbeiro a uma barbearia |
| Serviço | Representa um serviço oferecido pelo estabelecimento |
| Horário de funcionamento | Define o funcionamento geral da barbearia |
| Disponibilidade profissional | Define os horários de trabalho de cada barbeiro |
| Bloqueio | Representa um período indisponível na agenda |
| Agendamento | Representa a contratação de um serviço em uma data e horário |
| Atendimento | Representa a execução do serviço |
| Pagamento | Representa o registro financeiro de um atendimento |
| Notificação | Representa uma comunicação apresentada ao usuário dentro do sistema |

A modelagem definitiva das entidades, propriedades, relacionamentos e cardinalidades será documentada no TDD e no MER do projeto.

# 10. Integrações e notificações

## 10.1. Notificações internas

A V1.0 deverá contemplar notificações dentro do próprio sistema para apoiar os fluxos de agendamento e comunicação entre clientes e profissionais.

## 10.2. WhatsApp

O envio automático de mensagens pelo WhatsApp não fará parte da V1.0.

A possibilidade de integração será avaliada em versões futuras.

## 10.3. Pagamentos externos

Não haverá integração com gateways de pagamento na primeira versão.

Os pagamentos serão realizados presencialmente e registrados no sistema.

# 11. Riscos e pontos de atenção

| Risco | Impacto | Tratamento esperado |
|---|---|---|
| Conflito de agendamentos | Sobreposição de atendimentos | Validar disponibilidade antes de confirmar ou remarcar |
| Acesso indevido entre barbearias | Exposição de dados | Garantir isolamento dos dados por estabelecimento |
| Alteração de serviços antigos | Perda de informações históricas | Preservar os dados contratados no agendamento |
| Remoção de profissionais | Perda de referências históricas | Manter os registros e encerrar somente o vínculo |
| Alterações simultâneas de horários | Reserva duplicada | Garantir consistência nas operações de disponibilidade |
| Pagamentos não registrados | Divergência nos indicadores | Atualizar os indicadores somente com pagamentos registrados |
| Sugestões de horários expiradas | Horários presos indevidamente | Liberar a reserva após o prazo de 24 horas |

# 12. Decisões consolidadas e evolução do produto

## 12.1. Decisões fechadas da V1.0

- O sistema terá três perfis: proprietário, barbeiro e cliente.
- O cadastro inicial permitirá escolher entre "Sou cliente" e "Sou barbeiro".
- O login será compartilhado.
- Todo cliente deverá possuir conta para agendar.
- O proprietário será o barbeiro responsável pela criação da barbearia.
- O ingresso de funcionários será realizado por código de convite permanente.
- Cada barbearia terá um único código de convite.
- O proprietário também poderá atuar como barbeiro.
- O catálogo de serviços será utilizado como modelo inicial.
- Cada barbearia terá serviços independentes.
- O proprietário será responsável pelo gerenciamento dos serviços.
- Os horários gerais serão configurados pelo proprietário.
- Cada profissional terá sua própria disponibilidade.
- A agenda terá visualizações diária, semanal e mensal.
- Os agendamentos públicos e manuais serão confirmados automaticamente.
- Cada agendamento permitirá apenas um serviço.
- O cliente poderá cancelar e remarcar antes do início do atendimento, sem antecedência mínima.
- O cancelamento e a remarcação serão bloqueados após o início do atendimento.
- O barbeiro poderá sugerir outro horário, com motivo informado.
- O cliente terá 24 horas para decidir sobre a sugestão.
- O atendimento terá registro automático de início e término.
- O pagamento será presencial.
- As formas de pagamento serão dinheiro, Pix, cartão de débito e cartão de crédito.
- O barbeiro poderá marcar um cliente como não comparecente.
- O proprietário poderá consultar clientes relacionados à barbearia, mas não poderá cadastrar, editar ou excluir esses clientes.
- O cliente poderá editar seus próprios dados cadastrais.
- O proprietário poderá gerenciar os profissionais vinculados.
- A remoção de um profissional não excluirá sua conta.
- O histórico de agendamentos deverá ser preservado.
- O WhatsApp automático e pagamentos online ficarão para versões futuras.

## 12.2. Pontos que deverão ser especificados no detalhamento

Os itens abaixo não impedem a definição do produto, mas precisam ser formalizados antes ou durante a implementação das funcionalidades correspondentes:

- Momento exato em que a verificação do telefone comercial será exigida.
- Estados completos dos agendamentos, atendimentos e pagamentos.
- Definição dos períodos considerados nos indicadores de agendamentos do Dashboard.
- Regras para pagamentos parciais, caso sejam necessários.
- Comportamento do agendamento quando houver alterações de serviço após sua criação.
- Conteúdo público e informações exibidas na página da barbearia.
- Regras específicas para alterações cadastrais e atualização de e-mail.
- Regras de apresentação e atualização das notificações internas.

## 12.3. Evolução futura

Após a conclusão e validação da V1.0, poderão ser avaliadas melhorias como:

- Integração com WhatsApp para mensagens automáticas.
- Pagamentos online.
- Recursos financeiros avançados.
- Relatórios gerenciais adicionais.
- Melhorias na comunicação entre clientes e profissionais.
- Novas ferramentas de administração para barbearias.

Essas funcionalidades não deverão ser implementadas antecipadamente sem uma definição de escopo para uma versão posterior.

---

**Fim do documento — PRD PontoBarber V1.0**