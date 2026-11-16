# PontoBarber — SPEC

## Descrição do Projeto

O PontoBarber é uma aplicação para gerenciamento de barbearias, desenvolvida para facilitar a administração dos estabelecimentos e organizar o relacionamento entre proprietários, barbeiros e clientes.

A plataforma permite que proprietários cadastrem e configurem suas barbearias, gerenciem profissionais, serviços, horários de funcionamento e acompanhem os agendamentos realizados.

Os barbeiros podem administrar sua disponibilidade, acompanhar sua agenda, gerenciar atendimentos e registrar pagamentos.

Os clientes podem consultar barbearias, visualizar serviços e horários disponíveis, realizar agendamentos e acompanhar seus compromissos.

O sistema busca centralizar a gestão da barbearia e simplificar o processo de agendamento, proporcionando uma experiência organizada para todos os envolvidos.

## 1. Módulo de Autenticação e Cadastro

### Requisitos Funcionais

- O sistema deve disponibilizar uma tela inicial com duas opções de acesso:
  - Sou cliente.
  - Sou barbeiro.
- O sistema deve disponibilizar formulários de cadastro separados para clientes e barbeiros.
- O sistema deve disponibilizar uma tela de login compartilhada entre todos os perfis.
- O sistema deve permitir que clientes criem uma conta para utilizar as funcionalidades de agendamento.
- O sistema deve permitir que barbeiros criem uma conta para utilizar a plataforma.
- Após o cadastro, o barbeiro deve escolher entre:
  - Criar uma nova barbearia.
  - Entrar em uma barbearia existente por meio de um código de convite.
- O sistema deve permitir que o barbeiro que criar uma barbearia passe a atuar como proprietário.
- O sistema deve permitir que o proprietário também exerça a função de barbeiro, possuindo sua própria agenda e disponibilidade.
- O sistema deve permitir que o proprietário compartilhe o código de convite da barbearia com outros profissionais.
- O sistema deve permitir que um barbeiro entre em uma barbearia existente utilizando o código de convite.
- O sistema deve permitir que o proprietário cadastre os dados iniciais da barbearia durante sua criação.

### Regras de Negócio

- O sistema possui três perfis de usuário:
  - Proprietário.
  - Barbeiro.
  - Cliente.
- O cadastro de clientes e barbeiros deve ocorrer por meio de fluxos separados.
- O login deve ser compartilhado entre todos os perfis.
- Clientes precisam possuir uma conta para realizar agendamentos. Não será permitido realizar agendamentos anonimamente.
- Um usuário cadastrado como barbeiro poderá criar uma barbearia ou entrar em uma existente.
- O barbeiro que criar uma barbearia será automaticamente definido como proprietário daquele estabelecimento.
- O proprietário poderá também atuar como barbeiro, mantendo sua própria agenda e disponibilidade.
- O código de convite deve ser único para cada barbearia e permanecer o mesmo ao longo do tempo.
- Somente o proprietário poderá compartilhar o código de convite com novos profissionais.
- Caso um barbeiro saia ou seja removido de uma barbearia, sua conta continuará existindo, perdendo apenas o vínculo com aquele estabelecimento.
- A remoção do vínculo de um barbeiro não deve excluir sua conta nem seu histórico de atividades.
- O cadastro inicial da barbearia não deve exigir CNPJ ou documentos pessoais.
- O telefone comercial da barbearia deverá passar por verificação, mas o momento e o procedimento ainda serão definidos.
- O Instagram da barbearia será opcional.

### Campos obrigatórios

**Cadastro de cliente:**
- Nome.
- E-mail.
- Telefone.
- Senha.
- Confirmar senha.

**Cadastro de barbeiro:**
- Nome.
- E-mail.
- Telefone.
- Senha.
- Confirmar senha.

**Login:**
- E-mail.
- Senha.

**Criação de barbearia:**
- Nome da barbearia.
- Telefone comercial.
- E-mail comercial.
- Cidade.
- Endereço.

**Campo opcional da barbearia:**
- Instagram.

**Entrada em uma barbearia existente:**
- Código de convite.

### Restrições e validações

- Todos os campos de cadastro de cliente e barbeiro devem ser obrigatórios.
- O e-mail deve possuir um formato válido.
- O sistema não deve permitir o cadastro de contas utilizando um e-mail já cadastrado.
- A confirmação de senha deve corresponder à senha informada.
- Os campos obrigatórios da barbearia devem ser preenchidos durante sua criação.
- O Instagram não deve ser obrigatório.
- O cadastro da barbearia não deve exigir CNPJ ou documentos pessoais.
- O telefone comercial da barbearia deverá ser verificado, conforme procedimento ainda a ser definido.
- O código de convite informado deve corresponder a uma barbearia existente.
- O sistema não deve permitir que um barbeiro entre em uma barbearia utilizando um código inválido.

## 2. Módulo de Empresa (Barbearia)

### Requisitos Funcionais

- O sistema deve permitir que o proprietário consulte os dados cadastrados da barbearia.
- O sistema deve permitir que o proprietário edite os dados da barbearia.
- O sistema deve permitir a atualização das seguintes informações:
  - Nome da barbearia.
  - Telefone comercial.
  - E-mail comercial.
  - Cidade.
  - Endereço.
  - Instagram.

### Regras de Negócio

- Cada barbearia deve possuir suas próprias informações cadastrais.
- Somente o proprietário poderá alterar os dados da empresa.
- Os barbeiros vinculados à barbearia não poderão alterar as informações do estabelecimento.
- O Instagram será opcional.
- O telefone comercial deverá passar por verificação, mas o momento e o procedimento ainda serão definidos.
- A alteração dos dados cadastrais não deverá modificar informações históricas dos agendamentos.
- Os horários gerais de funcionamento serão configurados separadamente.

### Campos obrigatórios

- Nome da barbearia.
- Telefone comercial.
- E-mail comercial.
- Cidade.
- Endereço.

**Campo opcional:**
- Instagram.

### Restrições e validações

- Todos os campos obrigatórios devem ser preenchidos.
- O e-mail comercial deve possuir um formato válido.
- O telefone comercial deve possuir um formato válido.
- Somente o proprietário poderá alterar os dados cadastrais da barbearia.
- O telefone comercial deverá passar por verificação, conforme procedimento ainda a ser definido.
- Alterações cadastrais não devem modificar os dados históricos dos agendamentos.

## 3. Módulo de Serviços

### Requisitos Funcionais

- O sistema deve disponibilizar um catálogo inicial de serviços como modelo para as barbearias.
- O catálogo inicial deve conter:
  - Corte — 40 minutos.
  - Barba — 30 minutos.
  - Corte + Barba — 70 minutos.
- O sistema deve permitir que o proprietário adicione serviços à sua barbearia.
- O sistema deve permitir que o proprietário edite os serviços cadastrados.
- O sistema deve permitir que o proprietário remova serviços do catálogo da barbearia.
- O sistema deve permitir que o proprietário defina o preço de cada serviço.
- O sistema deve permitir que o proprietário altere a duração de cada serviço.
- O sistema deve disponibilizar os serviços ativos para os clientes durante o agendamento público.
- O sistema deve permitir que os barbeiros visualizem os serviços disponíveis na barbearia.

### Regras de Negócio

- Cada barbearia deve possuir seu próprio catálogo de serviços.
- O catálogo inicial serve como modelo, permitindo que cada barbearia personalize seus serviços.
- O proprietário é o único responsável por adicionar, editar e remover serviços.
- Os barbeiros podem apenas visualizar os serviços cadastrados.
- Cada serviço deve possuir nome, duração e preço.
- O serviço Corte + Barba deve ser independente, possuindo preço e duração próprios.
- Cada agendamento deve permitir a seleção de apenas um serviço.
- A remoção de um serviço deve desativá-lo para novos agendamentos, sem excluir seus registros históricos.
- Agendamentos antigos devem preservar o nome, a duração e o preço do serviço utilizado no momento da contratação.

### Campos obrigatórios

**Cadastro de serviço:**
- Nome.
- Duração.
- Preço.

### Restrições e validações

- O nome do serviço deve ser obrigatório.
- A duração deve ser obrigatória e possuir um valor válido.
- O preço deve ser obrigatório e não poderá ser negativo.
- Somente o proprietário poderá cadastrar, editar ou remover serviços.
- Serviços removidos não devem aparecer como opções para novos agendamentos.
- A remoção de um serviço não deve excluir nem modificar agendamentos históricos.
- Cada agendamento poderá possuir apenas um serviço.

## 4. Módulo de Horários e Disponibilidade

### Requisitos Funcionais

- O sistema deve permitir que o proprietário configure os horários gerais de funcionamento da barbearia.
- O sistema deve permitir a configuração dos horários para cada dia da semana.
- Para cada dia, o proprietário poderá definir:
  - Horário de abertura.
  - Horário de fechamento.
  - Dia fechado.
- O sistema deve permitir que cada barbeiro configure sua própria disponibilidade semanal.
- O sistema deve permitir que cada barbeiro cadastre folgas e períodos de indisponibilidade.
- O sistema deve permitir que cada profissional bloqueie períodos específicos em sua agenda.
- O sistema deve permitir informar um motivo opcional para o bloqueio.

### Regras de Negócio

- Os horários gerais de funcionamento pertencem à barbearia e são configurados pelo proprietário.
- A disponibilidade semanal pertence individualmente a cada profissional.
- A disponibilidade de um profissional deve respeitar os horários gerais de funcionamento da barbearia.
- Um profissional não poderá receber agendamentos fora de sua disponibilidade.
- Os horários semanais devem ser separados dos bloqueios pontuais.
- Os bloqueios pontuais devem impedir novos agendamentos durante o período bloqueado.
- O proprietário também poderá configurar sua própria disponibilidade quando atuar como barbeiro.

### Campos obrigatórios

**Horário geral da barbearia:**
- Dia da semana.
- Horário de abertura.
- Horário de fechamento.

**Disponibilidade semanal do profissional:**
- Dia da semana.
- Horário de início.
- Horário de término.

**Bloqueio pontual:**
- Data e horário de início.
- Data e horário de término.

**Campo opcional do bloqueio:**
- Motivo.

### Restrições e validações

- Os horários de funcionamento devem ser configurados individualmente para cada dia da semana.
- Dias definidos como fechados não devem disponibilizar horários para agendamento.
- O horário de fechamento deve ser posterior ao horário de abertura.
- A disponibilidade do profissional não poderá ultrapassar o horário de funcionamento da barbearia.
- O sistema não deve disponibilizar horários fora da jornada configurada para o profissional.
- O horário inicial do bloqueio deve ser anterior ao horário final.
- O sistema não deve permitir novos agendamentos em períodos bloqueados.
- O sistema deve validar conflitos entre agendamentos e bloqueios.
- A alteração da disponibilidade não deve modificar automaticamente agendamentos já existentes.

## 5. Módulo de Agenda e Agendamentos

### Requisitos Funcionais

- O sistema deve permitir que o proprietário e os barbeiros visualizem os agendamentos conforme suas permissões.
- O sistema deve disponibilizar três visualizações da agenda:
  - Dia.
  - Semana.
  - Mês.
- O sistema deve permitir navegar entre períodos anteriores, atuais e próximos.
- O sistema deve permitir que o proprietário filtre os agendamentos por profissional.
- O sistema deve permitir que o proprietário ou barbeiro crie agendamentos manualmente pela agenda.
- O sistema deve disponibilizar a opção "+ Novo" para iniciar um agendamento manual.
- O sistema deve permitir informar os seguintes dados no agendamento manual:
  - Cliente.
  - Profissional.
  - Data.
  - Horário de início.
  - Serviço.
  - Observação opcional.
- O sistema deve calcular automaticamente a duração, o horário de término e o valor do agendamento com base no serviço selecionado.
- O sistema deve permitir consultar os detalhes de cada agendamento.
- O sistema deve permitir que o cliente consulte seus próprios agendamentos.
- O sistema deve permitir que o cliente cancele um agendamento.
- O sistema deve permitir que o cliente remarque um agendamento.
- O sistema deve permitir que o barbeiro cancele agendamentos de sua própria agenda.
- O sistema deve permitir que o proprietário cancele qualquer agendamento da barbearia.
- O sistema deve permitir que o barbeiro sugira outro horário quando precisar cancelar um agendamento.
- O sistema deve permitir que o cliente aceite ou rejeite o horário sugerido.
- O sistema deve registrar quem realizou o cancelamento e quando ele ocorreu.

### Regras de Negócio

- Os agendamentos públicos e manuais devem ser confirmados automaticamente após sua criação.
- Todo agendamento deve estar associado a uma barbearia, cliente, profissional e serviço.
- Cada agendamento deve possuir apenas um serviço.
- O horário de término deve ser calculado considerando a duração do serviço selecionado.
- O valor do agendamento deve corresponder ao preço do serviço no momento da contratação.
- O sistema deve validar os horários de funcionamento da barbearia, a disponibilidade individual do profissional, os bloqueios e os conflitos com outros agendamentos.
- Um horário ocupado não poderá receber outro agendamento conflitante.
- O cliente poderá cancelar seu agendamento a qualquer momento antes do início do atendimento, sem exigência de antecedência mínima.
- O cliente poderá remarcar seu agendamento a qualquer momento antes do início do atendimento, sem exigência de antecedência mínima.
- A remarcação deverá atualizar o agendamento existente, preservando seu histórico.
- O barbeiro poderá cancelar agendamentos de sua própria agenda.
- O proprietário poderá cancelar qualquer agendamento da barbearia.
- O cancelamento não exige justificativa, mas o sistema deve registrar quem cancelou e quando.
- Quando o barbeiro cancelar um agendamento e sugerir outro horário:
  - O motivo do cancelamento deverá ser informado.
  - O horário sugerido ficará temporariamente reservado por 24 horas.
  - Se o cliente aceitar, o agendamento será transferido para o horário sugerido.
  - Se o cliente rejeitar ou não responder dentro das 24 horas, o horário sugerido será liberado.
  - O agendamento original permanecerá cancelado caso a sugestão seja rejeitada ou expire.
- Após o início do atendimento, o cliente não poderá cancelar nem remarcar o agendamento.
- Após o início do atendimento, o barbeiro e o proprietário também não poderão cancelar o agendamento.

### Campos obrigatórios

**Agendamento manual:**
- Cliente.
- Profissional.
- Data.
- Horário de início.
- Serviço.

**Campo opcional:**
- Observação.

**Sugestão de novo horário:**
- Novo horário sugerido.
- Motivo do cancelamento.

### Restrições e validações

- O cliente deve possuir uma conta para realizar agendamentos.
- O profissional selecionado deve estar vinculado à barbearia e ativo.
- O serviço selecionado deve estar ativo.
- A data e o horário devem respeitar o funcionamento da barbearia e a disponibilidade do profissional.
- O sistema deve impedir conflitos entre agendamentos do mesmo profissional.
- O sistema deve impedir agendamentos durante bloqueios de agenda.
- O sistema deve calcular automaticamente o horário de término.
- O sistema deve impedir cancelamentos e remarcações após o início do atendimento.
- O sistema deve impedir que um barbeiro altere agendamentos de profissionais que não pertencem à sua agenda.
- Somente o proprietário poderá cancelar agendamentos de qualquer profissional da barbearia.
- O motivo deve ser obrigatório quando o barbeiro sugerir um novo horário.
- O horário sugerido deve permanecer reservado por, no máximo, 24 horas.
- Caso o cliente rejeite ou não responda dentro do prazo, o horário sugerido deverá ser liberado.
- O histórico do agendamento deve ser preservado durante remarcações e alterações de horário.

### Identificação visual da agenda

- Agendamentos confirmados: verde.
- Agendamentos cancelados: vermelho.
- Bloqueios: cinza.
- Não será utilizada a cor amarela para representar agendamentos pendentes.

## 6. Módulo de Atendimento e Pagamentos

### Requisitos Funcionais

- O sistema deve permitir que o barbeiro consulte os detalhes do agendamento.
- O sistema deve permitir que o barbeiro inicie o atendimento quando o cliente chegar.
- O sistema deve registrar automaticamente o horário real de início do atendimento.
- O sistema deve permitir que o barbeiro finalize o atendimento após sua conclusão.
- O sistema deve registrar automaticamente o horário real de término do atendimento.
- O sistema deve permitir que o barbeiro registre o pagamento do atendimento.
- O sistema deve permitir o registro de pagamentos realizados por:
  - Dinheiro.
  - Pix.
  - Cartão de débito.
  - Cartão de crédito.
- O sistema deve permitir que o barbeiro marque um cliente como não compareceu.
- O sistema deve apresentar separadamente a situação do atendimento e a situação do pagamento.

### Regras de Negócio

- O atendimento e o pagamento possuem estados independentes.
- Um atendimento finalizado poderá permanecer com o pagamento pendente.
- O atendimento deve ser iniciado pelo barbeiro quando o cliente chegar à barbearia.
- O horário real de início deve ser registrado pelo sistema no momento da inicialização.
- O horário real de término deve ser registrado pelo sistema no momento da finalização.
- O pagamento será realizado presencialmente, após ou ao finalizar o atendimento.
- Não haverá pagamento antecipado nem integração com gateway de pagamento na primeira versão.
- O valor registrado como recebido deverá atualizar o indicador "Recebido no mês" do dashboard.
- A situação de não comparecimento não deve ser tratada como cancelamento.
- Um agendamento marcado como não compareceu não poderá iniciar ou finalizar um atendimento.
- O cancelamento do agendamento não será permitido após o início do atendimento.

### Campos e informações

**Registro de atendimento:**
- Agendamento.
- Horário real de início, registrado automaticamente.
- Horário real de término, registrado automaticamente.

**Registro de pagamento:**
- Valor recebido.
- Forma de pagamento.

**Formas de pagamento disponíveis:**
- Dinheiro.
- Pix.
- Cartão de débito.
- Cartão de crédito.

### Restrições e validações

- O atendimento somente poderá ser iniciado quando o cliente comparecer.
- O sistema deve registrar automaticamente os horários reais de início e término.
- O atendimento não poderá ser finalizado antes de ser iniciado.
- O pagamento deve possuir um valor válido e não negativo.
- A forma de pagamento deve corresponder a uma das opções disponíveis.
- O atendimento finalizado poderá permanecer com pagamento pendente.
- O sistema não deve considerar agendamentos cancelados como pagamentos recebidos.
- O status de não comparecimento não deve permitir a execução de um atendimento.
- O sistema deve impedir o cancelamento de um agendamento após o início do atendimento.

## 7. Módulo de Clientes

### Requisitos Funcionais

- O sistema deve disponibilizar uma área para consulta dos clientes relacionados à barbearia.
- O sistema deve permitir que o proprietário visualize a lista de clientes que possuem ou já tiveram agendamentos na barbearia.
- O sistema deve permitir que o proprietário pesquise clientes cadastrados.
- O sistema deve permitir que o cliente consulte seus próprios dados cadastrais.
- O sistema deve permitir que o cliente edite suas informações pessoais.
- O sistema deve permitir que o cliente atualize:
  - Nome.
  - E-mail.
  - Telefone.

### Regras de Negócio

- Os clientes devem criar suas próprias contas no sistema.
- O proprietário não poderá cadastrar clientes manualmente.
- O proprietário não poderá editar ou excluir contas de clientes.
- A relação de um cliente com a barbearia será estabelecida por meio de seus agendamentos.
- A listagem da barbearia deverá considerar clientes que possuem ou já tiveram agendamentos no estabelecimento.
- Cada cliente poderá editar apenas os próprios dados cadastrais.
- A alteração dos dados pessoais do cliente não deverá modificar informações históricas dos agendamentos.

### Campos obrigatórios

**Dados cadastrais do cliente:**
- Nome.
- E-mail.
- Telefone.

### Restrições e validações

- O nome deve ser obrigatório.
- O e-mail deve ser obrigatório e possuir um formato válido.
- O telefone deve ser obrigatório e possuir um formato válido.
- O cliente somente poderá editar os próprios dados.
- O proprietário não poderá alterar ou excluir contas de clientes.
- O proprietário não poderá cadastrar clientes manualmente.
- A listagem de clientes deve apresentar apenas clientes relacionados à barbearia por meio de agendamentos.
- Alterações cadastrais não devem modificar os dados históricos dos agendamentos.

## 8. Módulo de Profissionais

### Requisitos Funcionais

- O sistema deve disponibilizar uma área para gerenciamento dos profissionais vinculados à barbearia.
- O sistema deve permitir que o proprietário visualize a lista de profissionais.
- A listagem deve apresentar:
  - Nome.
  - Telefone.
  - Situação (ativo ou inativo).
- O sistema deve permitir que o proprietário consulte os dados de cada profissional.
- O sistema deve permitir que o proprietário edite os dados profissionais.
- O sistema deve permitir que o proprietário ative ou desative profissionais.
- O sistema deve permitir que o proprietário remova o vínculo de um profissional com a barbearia.
- O sistema deve disponibilizar o código de convite da barbearia para compartilhamento pelo proprietário.

### Regras de Negócio

- Os profissionais devem criar suas próprias contas e entrar na barbearia por meio do código de convite.
- Somente o proprietário poderá gerenciar os vínculos dos profissionais com a barbearia.
- Um profissional desativado não poderá receber novos agendamentos.
- A desativação de um profissional não deverá excluir sua conta nem seu histórico.
- A remoção do vínculo de um profissional não deverá excluir sua conta nem os registros históricos relacionados à barbearia.
- Cada profissional será responsável por administrar sua própria disponibilidade e seus bloqueios de agenda.
- O proprietário também poderá atuar como profissional, possuindo sua própria disponibilidade e agenda.

### Campos e informações apresentados

**Listagem de profissionais:**
- Nome.
- Telefone.
- Situação (ativo ou inativo).

**Dados profissionais:**
- Nome.
- Telefone.

### Restrições e validações

- Somente o proprietário poderá ativar, desativar, editar ou remover vínculos de profissionais.
- Profissionais desativados não poderão receber novos agendamentos.
- A desativação não deverá excluir a conta do profissional.
- A remoção do vínculo não deverá excluir o histórico de agendamentos e atendimentos.
- O profissional somente poderá administrar sua própria disponibilidade e seus bloqueios.
- O sistema deverá impedir que profissionais sem vínculo ativo recebam novos agendamentos naquela barbearia.

## 9. Módulo de Dashboard

### Requisitos Funcionais

- O sistema deve disponibilizar um dashboard inicial para o proprietário após a configuração inicial da barbearia.
- O dashboard deve apresentar os seguintes indicadores:
  - Hoje: quantidade de agendamentos do dia.
  - Esta semana: quantidade de agendamentos da semana.
  - Cancelamentos: quantidade de agendamentos cancelados no mês.
  - Recebido no mês: soma dos pagamentos registrados no mês.
- O sistema deve apresentar uma lista de próximos agendamentos.
- Cada próximo agendamento deve apresentar:
  - Horário.
  - Cliente.
  - Profissional.
  - Status.
- O sistema deve apresentar os indicadores mesmo quando não houver movimentações.

### Regras de Negócio

- O dashboard será a primeira tela apresentada ao proprietário após a configuração inicial da barbearia.
- O indicador "Hoje" deve considerar os agendamentos da data atual.
- O indicador "Esta semana" deve considerar os agendamentos da semana atual.
- O indicador "Cancelamentos" deve considerar os agendamentos cancelados durante o mês atual.
- O indicador "Recebido no mês" deve considerar exclusivamente os pagamentos registrados no mês atual.
- O valor recebido deve corresponder à soma dos pagamentos efetivamente registrados.
- Os próximos agendamentos devem apresentar os compromissos futuros da barbearia.
- Quando não houver dados para determinado indicador, o sistema deverá apresentar o valor zero.

### Campos e informações apresentados

**Indicadores:**
- Hoje.
- Esta semana.
- Cancelamentos.
- Recebido no mês.

**Próximos agendamentos:**
- Horário.
- Cliente.
- Profissional.
- Status.

### Restrições e validações

- Os indicadores devem considerar os períodos correspondentes.
- Agendamentos cancelados não devem ser contabilizados como pagamentos recebidos.
- O indicador financeiro deve considerar apenas pagamentos registrados.
- O dashboard deve apresentar zero quando não houver dados.
- Os dados apresentados devem pertencer à barbearia correspondente ao proprietário autenticado.

## 10. Módulo de Agendamento Público

### Requisitos Funcionais

- O sistema deve disponibilizar uma página pública específica para cada barbearia.
- A página deve apresentar as informações da barbearia e seus serviços disponíveis.
- O sistema deve permitir que o cliente inicie um agendamento selecionando:
  1. Serviço.
  2. Profissional.
  3. Data e horário disponíveis.
  4. Observação opcional.
  5. Confirmação do agendamento.
- O sistema deve permitir que o cliente visualize os horários disponíveis de cada profissional.
- O sistema deve permitir que o cliente confirme o agendamento.
- O sistema deve disponibilizar o agendamento confirmado na agenda da barbearia e do profissional.

### Regras de Negócio

- Cada página pública deve corresponder a uma barbearia específica.
- O cliente deverá estar autenticado para realizar um agendamento.
- Os dados pessoais do cliente devem ser obtidos de sua própria conta.
- Cada agendamento deverá possuir apenas um serviço.
- Os horários disponibilizados devem respeitar:
  - Horários gerais de funcionamento da barbearia.
  - Disponibilidade semanal do profissional.
  - Bloqueios cadastrados na agenda.
  - Agendamentos existentes.
- Todos os agendamentos públicos serão confirmados automaticamente após a confirmação do cliente.
- Não haverá necessidade de aprovação manual do proprietário ou barbeiro.
- O horário selecionado deverá ser ocupado pelo agendamento, impedindo reservas conflitantes.
- O sistema deverá validar novamente a disponibilidade no momento da confirmação para evitar conflitos entre agendamentos simultâneos.

### Campos obrigatórios

**Agendamento público:**
- Serviço.
- Profissional.
- Data.
- Horário.

**Campo opcional:**
- Observação.

**Dados do cliente:**
- Obtidos automaticamente da conta autenticada.

### Restrições e validações

- O cliente deverá estar autenticado para concluir o agendamento.
- O serviço selecionado deverá estar ativo.
- O profissional deverá estar ativo e vinculado à barbearia.
- O horário selecionado deverá estar disponível.
- O sistema deverá impedir agendamentos fora do horário de funcionamento da barbearia.
- O sistema deverá impedir agendamentos fora da disponibilidade do profissional.
- O sistema deverá impedir agendamentos durante bloqueios de agenda.
- O sistema deverá impedir conflitos com outros agendamentos.
- A confirmação deverá ocorrer automaticamente após a validação dos dados.
- O sistema deverá impedir que o mesmo horário seja reservado simultaneamente por agendamentos conflitantes.

## 11. Módulo de Configurações

### Requisitos Funcionais

- O sistema deve disponibilizar uma área de configurações para o proprietário da barbearia.
- A área deve permitir a administração dos horários gerais de funcionamento.
- O sistema deve permitir que o proprietário configure os horários de cada dia da semana.
- O sistema deve permitir que o proprietário defina dias em que a barbearia permanecerá fechada.
- O sistema deve disponibilizar uma área para consulta dos usuários vinculados à barbearia.
- O sistema deve permitir que o proprietário consulte e compartilhe o código de convite com novos profissionais.
- Cada barbeiro deve possuir acesso à configuração de sua própria disponibilidade semanal.
- Cada barbeiro deve poder administrar seus próprios bloqueios de agenda.

### Regras de Negócio

- Somente o proprietário poderá configurar os horários gerais de funcionamento da barbearia.
- Cada barbeiro será responsável por configurar sua própria disponibilidade.
- A disponibilidade individual deverá respeitar os horários gerais da barbearia.
- Os horários gerais e a disponibilidade individual deverão ser tratados separadamente.
- Os bloqueios pontuais deverão ser administrados na agenda do próprio profissional.
- O código de convite será único e permanente para cada barbearia.
- Somente o proprietário poderá compartilhar o código de convite com novos profissionais.
- A entrada de novos barbeiros deverá ocorrer por meio do código de convite, não por criação direta de contas pelo proprietário.
- A primeira versão deverá manter as configurações simples, deixando recursos adicionais para versões futuras.

### Campos obrigatórios

**Horários gerais:**
- Dia da semana.
- Horário de abertura.
- Horário de fechamento.

**Configuração de dia fechado:**
- Dia da semana.

**Disponibilidade individual do profissional:**
- Dia da semana.
- Horário de início.
- Horário de término.

### Restrições e validações

- Os horários gerais deverão respeitar os dias de funcionamento definidos pelo proprietário.
- O horário de fechamento deverá ser posterior ao horário de abertura.
- Dias definidos como fechados não deverão disponibilizar horários para agendamento.
- A disponibilidade do profissional não poderá ultrapassar o horário geral da barbearia.
- Cada barbeiro poderá administrar somente sua própria disponibilidade.
- Somente o proprietário poderá compartilhar o código de convite para entrada de novos profissionais.
- O sistema não deverá permitir a criação direta de contas de barbeiros pelo proprietário.
- As configurações deverão respeitar as permissões de cada perfil.

## 12. Módulo de Notificações

### Requisitos Funcionais

- O sistema deve disponibilizar notificações internas relacionadas aos agendamentos.

### Regras de Negócio

- As notificações da primeira versão serão internas ao sistema.
- O envio automático de mensagens pelo WhatsApp ficará para uma versão futura.