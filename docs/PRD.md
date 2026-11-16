# PontoBarber — PRD

> **Documento:** Product Requirements Document
> **Projeto:** PontoBarber
> **Versão:** 1.0
> **Status:** Consolidado para desenvolvimento
> **Data:** 16/11/2026
> **Responsável:** Equipe PontoBarber

## Histórico de alterações

| Versão | Data | Autor | Alteração |
| --- | --- | --- | --- |
| 1.0 | 16/11/2026 | Equipe PontoBarber | Criação do documento a partir da SPEC do produto |

---

# 1. Descrição do projeto

## 1.1. Visão geral

O PontoBarber é uma aplicação para gerenciamento de barbearias, desenvolvida para centralizar a administração do estabelecimento e organizar o relacionamento entre proprietários, barbeiros e clientes.

A plataforma permite que proprietários cadastrem e configurem suas barbearias, gerenciem profissionais, serviços, horários de funcionamento e acompanhem os agendamentos realizados. Barbeiros administram sua disponibilidade, acompanham sua agenda, gerenciam atendimentos e registram pagamentos. Clientes consultam barbearias, visualizam serviços e horários disponíveis, realizam agendamentos e acompanham seus compromissos.

## 1.2. Problema

A rotina de uma barbearia envolve a coordenação de horários de funcionamento, disponibilidade individual dos profissionais, serviços com durações e preços distintos, agendamentos, atendimentos e pagamentos. Quando essas informações ficam dispersas ou dependem de controles manuais, surgem conflitos de horário, perda de histórico e dificuldade de acompanhar a operação e o faturamento.

O PontoBarber resolve esse problema ao reunir a gestão da barbearia e o processo de agendamento em um único sistema, com regras explícitas de disponibilidade, confirmação automática e registro histórico das movimentações.

## 1.3. Público-alvo

| Segmento | Necessidades principais | Contexto de uso |
| --- | --- | --- |
| Proprietário de barbearia | Configurar o estabelecimento, serviços, horários, profissionais e acompanhar a operação | Gestão administrativa e acompanhamento diário da barbearia |
| Barbeiro | Administrar a própria disponibilidade, acompanhar a agenda, realizar atendimentos e registrar pagamentos | Operação diária da própria agenda |
| Cliente | Consultar a barbearia, escolher serviço e profissional, agendar, cancelar ou remarcar | Agendamento online e acompanhamento dos próprios compromissos |

## 1.4. Proposta de valor

- Centralização da gestão da barbearia, dos profissionais, dos serviços e dos horários em um único sistema.
- Agendamento público com confirmação automática, sem necessidade de aprovação manual.
- Agenda com visualizações por dia, semana e mês, incluindo bloqueios e filtros por profissional.
- Registro histórico de agendamentos, atendimentos, cancelamentos e pagamentos.
- Visão operacional e financeira consolidada no dashboard do proprietário.

---

# 2. Objetivos do sistema

## 2.1. Objetivo geral

Disponibilizar uma plataforma única para gerenciar barbearias e organizar todo o ciclo de agendamento, atendimento e pagamento entre proprietários, barbeiros e clientes.

## 2.2. Objetivos específicos

- Permitir o cadastro e a autenticação de clientes e barbeiros em fluxos separados, com login compartilhado.
- Permitir a criação e a configuração da barbearia pelo proprietário.
- Disponibilizar um catálogo inicial de serviços e permitir a personalização por barbearia.
- Configurar os horários gerais de funcionamento e a disponibilidade individual dos profissionais.
- Oferecer uma agenda centralizada com visualizações diária, semanal e mensal.
- Permitir agendamentos públicos e manuais com confirmação automática.
- Registrar cancelamentos, remarcações, atendimentos, não comparecimentos e pagamentos.
- Disponibilizar indicadores operacionais e financeiros ao proprietário.
- Preservar o histórico de agendamentos, atendimentos e pagamentos.

## 2.3. Métricas de sucesso

| Métrica | Descrição | Meta inicial | Como medir |
| --- | --- | --- | --- |
| Conflitos de agenda | Agendamentos sobrepostos para o mesmo profissional | Zero | Verificação de conflitos registrados |
| Agendamentos sem intervenção manual | Percentual de agendamentos criados pela página pública | A definir com o negócio | Origem registrada no agendamento |
| Ocupação da agenda | Relação entre horários ocupados e horários disponíveis | A definir com o negócio | Agenda e disponibilidade configuradas |
| Registro de pagamentos | Atendimentos finalizados com pagamento registrado | A definir com o negócio | Atendimentos e pagamentos no dashboard |
| Retenção de histórico | Agendamentos históricos preservados após alterações cadastrais ou de serviço | 100% | Auditoria de registros históricos |

As metas ainda não definidas na SPEC estão registradas como pontos em aberto na seção 13.

---

# 3. Perfis de usuários

## 3.1. Proprietário

**Descrição:** barbeiro que criou a barbearia ou que administra o estabelecimento. Pode acumular a atuação de barbeiro.

**Pode:**

- Configurar e editar os dados da barbearia.
- Gerenciar o catálogo de serviços, preços e durações.
- Configurar os horários gerais de funcionamento.
- Gerenciar os vínculos dos profissionais e compartilhar o código de convite.
- Visualizar e gerenciar todos os agendamentos da barbearia.
- Cancelar qualquer agendamento da barbearia.
- Atuar como barbeiro, com agenda, disponibilidade e bloqueios próprios.
- Realizar atendimentos e registrar pagamentos em sua própria atuação.
- Consultar clientes relacionados à barbearia.
- Acompanhar o dashboard e os indicadores.

**Não pode:**

- Cadastrar, editar ou excluir contas de clientes.
- Criar contas de barbeiros diretamente; a entrada ocorre por código de convite.

## 3.2. Barbeiro

**Descrição:** profissional vinculado a uma barbearia por código de convite.

**Pode:**

- Visualizar os serviços ativos da barbearia.
- Configurar a própria disponibilidade semanal.
- Cadastrar folgas, períodos de indisponibilidade e bloqueios pontuais.
- Visualizar e gerenciar a própria agenda.
- Criar agendamentos manuais em sua própria agenda.
- Consultar detalhes dos agendamentos.
- Iniciar e finalizar atendimentos.
- Registrar pagamentos.
- Cancelar agendamentos da própria agenda.
- Sugerir novo horário ao cancelar um agendamento.
- Marcar clientes como não comparecentes.
- Receber notificações internas.

**Não pode:**

- Alterar os dados cadastrais da barbearia.
- Adicionar, editar ou remover serviços.
- Configurar os horários gerais de funcionamento.
- Gerenciar vínculos de outros profissionais.
- Cancelar agendamentos de profissionais que não pertencem à sua agenda.
- Cancelar ou remarcar agendamentos após o início do atendimento.

## 3.3. Cliente

**Descrição:** pessoa que utiliza a plataforma para consultar barbearias e realizar agendamentos.

**Pode:**

- Criar uma conta e editar os próprios dados cadastrais.
- Consultar a página pública de uma barbearia.
- Visualizar serviços e horários disponíveis.
- Realizar agendamentos com confirmação automática.
- Consultar os próprios agendamentos.
- Cancelar ou remarcar agendamentos antes do início do atendimento.
- Aceitar ou rejeitar uma sugestão de novo horário feita pelo barbeiro.
- Receber notificações internas.

**Não pode:**

- Realizar agendamentos sem autenticação.
- Alterar dados de outros usuários.
- Cancelar ou remarcar agendamentos após o início do atendimento.

## 3.4. Matriz de permissões

| Módulo | Proprietário | Barbeiro | Cliente |
| --- | --- | --- | --- |
| Autenticação e Cadastro | Total (própria conta) | Total (própria conta) | Total (própria conta) |
| Empresa (Barbearia) | Total | Leitura | Nenhum |
| Serviços | Total | Leitura | Leitura (serviços ativos) |
| Horários e Disponibilidade | Total (geral e própria) | Total (própria) | Nenhum |
| Agenda e Agendamentos | Total | Parcial (própria agenda) | Parcial (próprios agendamentos) |
| Atendimento e Pagamentos | Parcial (própria atuação) | Parcial (própria agenda) | Nenhum |
| Clientes | Leitura | Parcial (via agendamentos) | Parcial (próprios dados) |
| Profissionais | Total | Parcial (próprios dados) | Nenhum |
| Dashboard | Total | Nenhum | Nenhum |
| Agendamento Público | Leitura | Leitura | Total (próprio agendamento) |
| Configurações | Total | Parcial (própria disponibilidade) | Parcial (próprios dados) |
| Notificações | Leitura (recebe) | Leitura (recebe) | Leitura (recebe) |

---

# 4. Escopo do projeto

## 4.1. Funcionalidades incluídas em V1.0

| Módulo | Funcionalidades | Prioridade |
| --- | --- | --- |
| Autenticação e Cadastro | Cadastro separado de clientes e barbeiros, login compartilhado, criação de barbearia, entrada por código de convite | Essencial |
| Empresa (Barbearia) | Consulta e edição dos dados cadastrais da barbearia | Essencial |
| Serviços | Catálogo inicial, cadastro, edição, remoção, preço e duração | Essencial |
| Horários e Disponibilidade | Horários gerais, disponibilidade semanal, folgas, bloqueios pontuais | Essencial |
| Agenda e Agendamentos | Visualizações diária, semanal e mensal, agendamento manual, cancelamento, remarcação e sugestão de horário | Essencial |
| Atendimento e Pagamentos | Início e fim do atendimento, não comparecimento, registro de pagamento presencial | Essencial |
| Clientes | Listagem e pesquisa de clientes da barbearia, edição dos próprios dados | Essencial |
| Profissionais | Listagem, edição, ativação, desativação e remoção de vínculo | Essencial |
| Dashboard | Indicadores do dia, semana, cancelamentos e recebimentos, próximos agendamentos | Essencial |
| Agendamento Público | Página pública por barbearia e fluxo de agendamento com confirmação automática | Essencial |
| Configurações | Horários gerais, usuários vinculados, código de convite e disponibilidade individual | Essencial |
| Notificações | Notificações internas relacionadas aos agendamentos | Importante |

## 4.2. Funcionalidades fora do escopo de V1.0

- Envio automático de mensagens pelo WhatsApp — previsto para versão futura.
- Integração com gateway de pagamento, pagamento antecipado ou cobrança online — o pagamento da V1.0 é presencial.
- Exigência de CNPJ ou documentos pessoais para cadastro da barbearia — não faz parte do cadastro inicial.
- Agendamento anônimo — o cliente precisa possuir conta.
- Cadastro de clientes pelo proprietário — clientes criam as próprias contas.
- Criação direta de contas de barbeiros pelo proprietário — a entrada ocorre por código de convite.
- Integrações externas de calendário, e-mail ou SMS — não previstas na SPEC.
- Relatórios financeiros avançados e conciliação de pagamentos — não previstos na V1.0.
- Recursos adicionais de configuração além dos descritos — a V1.0 deve manter as configurações simples.

## 4.3. Premissas e dependências

| Tipo | Descrição | Impacto se não se confirmar |
| --- | --- | --- |
| Premissa | O sistema é uma aplicação web acessível por navegador, sem aplicativo móvel nativo na V1.0 | Pode exigir requisitos adicionais de plataforma |
| Premissa | Cada barbearia opera em um único fuso horário, correspondente à sua cidade | Cálculos de horário podem ficar ambíguos |
| Dependência | Verificação do telefone comercial da barbearia | Cadastro pode ficar incompleto ou sem verificação |
| Dependência | Definição do procedimento de verificação do telefone comercial | Requisito permanece pendente de implementação |
| Dependência | Definição da política de retenção e privacidade de dados pessoais | Risco de não conformidade com a LGPD |
| Dependência | Definição das metas dos indicadores de sucesso | Impossibilidade de avaliar resultados do produto |

---

# 5. Jornadas principais

## 5.1. Cadastro de cliente e agendamento público

**Ator:** Cliente
**Pré-condições:** Barbearia configurada, com serviços ativos e horários disponíveis.

1. O cliente acessa a tela inicial e seleciona "Sou cliente".
2. O cliente preenche o cadastro com nome, e-mail, telefone, senha e confirmação de senha.
3. O sistema valida os dados e cria a conta.
4. O cliente acessa a página pública da barbearia.
5. O cliente seleciona o serviço, o profissional, a data e o horário disponível.
6. O cliente informa uma observação opcional e confirma o agendamento.
7. O sistema valida novamente a disponibilidade e confirma automaticamente o agendamento.
8. O agendamento passa a constar na agenda da barbearia, do profissional e na área do cliente.

**Resultado esperado:** Agendamento confirmado, horário ocupado e histórico registrado.
**Exceções:** E-mail já cadastrado, dados inválidos, horário ocupado ou indisponível, cliente não autenticado.

## 5.2. Criação da barbearia pelo proprietário

**Ator:** Barbeiro recém-cadastrado
**Pré-condições:** Conta de barbeiro ativa.

1. O barbeiro escolhe criar uma nova barbearia.
2. O sistema solicita nome, telefone comercial, e-mail comercial, cidade e endereço.
3. O proprietário informa opcionalmente o Instagram.
4. O sistema valida os campos e cria a barbearia.
5. O sistema define o criador como proprietário e gera o código de convite único.
6. O proprietário é direcionado ao dashboard inicial.

**Resultado esperado:** Barbearia criada, proprietário vinculado e código de convite disponível.
**Exceções:** Campos obrigatórios ausentes, e-mail ou telefone inválidos.

## 5.3. Entrada de profissional por código de convite

**Ator:** Barbeiro
**Pré-condições:** Conta de barbeiro ativa e código de convite válido fornecido pelo proprietário.

1. O barbeiro escolhe entrar em uma barbearia existente.
2. O barbeiro informa o código de convite.
3. O sistema valida o código e cria o vínculo com a barbearia.
4. O barbeiro passa a visualizar os serviços e a configurar sua própria disponibilidade.

**Resultado esperado:** Vínculo ativo entre barbeiro e barbearia.
**Exceções:** Código inválido, barbeiro já vinculado, vínculo inativo.

## 5.4. Agendamento manual na agenda

**Ator:** Proprietário ou barbeiro
**Pré-condições:** Cliente cadastrado, profissional ativo e vinculado, serviço ativo.

1. O usuário acessa a agenda e seleciona "+ Novo".
2. O usuário informa cliente, profissional, data, horário de início e serviço.
3. O usuário informa uma observação opcional.
4. O sistema calcula a duração, o horário de término e o valor com base no serviço.
5. O sistema valida funcionamento, disponibilidade, bloqueios e conflitos.
6. O agendamento é confirmado automaticamente e exibido na agenda.

**Resultado esperado:** Agendamento confirmado com serviço, valor e horários corretos.
**Exceções:** Cliente inexistente, profissional inativo, serviço inativo, horário indisponível ou conflitante.

## 5.5. Atendimento e pagamento presencial

**Ator:** Barbeiro
**Pré-condições:** Agendamento confirmado para a data e o profissional.

1. O barbeiro consulta os detalhes do agendamento.
2. Quando o cliente chega, o barbeiro inicia o atendimento.
3. O sistema registra automaticamente o horário real de início.
4. Ao concluir, o barbeiro finaliza o atendimento.
5. O sistema registra automaticamente o horário real de término.
6. O barbeiro registra o valor recebido e a forma de pagamento.
7. O dashboard é atualizado com o valor recebido no mês.

**Resultado esperado:** Atendimento finalizado e pagamento registrado, com horários reais preservados.
**Exceções:** Atendimento não iniciado, cliente não comparecente, valor ou forma de pagamento inválidos.

## 5.6. Cancelamento com sugestão de novo horário

**Ator:** Barbeiro
**Pré-condições:** Agendamento confirmado e ainda não iniciado.

1. O barbeiro solicita o cancelamento de um agendamento da própria agenda.
2. O barbeiro informa o motivo e sugere um novo horário.
3. O sistema cancela o agendamento original e reserva o horário sugerido por 24 horas.
4. O cliente recebe a sugestão e pode aceitar ou rejeitar.
5. Se aceitar, o agendamento é transferido para o horário sugerido.
6. Se rejeitar ou não responder em 24 horas, o horário sugerido é liberado.
7. O agendamento original permanece cancelado quando a sugestão é rejeitada ou expira.

**Resultado esperado:** Cancelamento registrado com autor e data, sugestão tratada e horário liberado ou ocupado corretamente.
**Exceções:** Atendimento já iniciado, motivo ausente, horário sugerido indisponível.

## 5.7. Remarcação pelo cliente

**Ator:** Cliente
**Pré-condições:** Agendamento próprio confirmado e ainda não iniciado.

1. O cliente consulta seus agendamentos.
2. O cliente seleciona a opção de remarcar.
3. O cliente escolhe uma nova data e um novo horário disponível.
4. O sistema valida as regras de funcionamento, disponibilidade, bloqueios e conflitos.
5. O agendamento existente é atualizado e seu histórico é preservado.

**Resultado esperado:** Agendamento remarcado, com histórico anterior mantido.
**Exceções:** Atendimento já iniciado, novo horário indisponível, agendamento cancelado.

---

# 6. Módulos e funcionalidades

## 6.1. Autenticação e Cadastro (`AUTH`)

### Visão geral

Responsável pelo acesso ao sistema, pelo cadastro de clientes e barbeiros, pela criação da barbearia e pela entrada de profissionais por código de convite.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-AUTH-001 | Disponibilizar tela inicial com as opções "Sou cliente" e "Sou barbeiro" | Essencial |
| RF-AUTH-002 | Disponibilizar formulário de cadastro exclusivo para clientes | Essencial |
| RF-AUTH-003 | Disponibilizar formulário de cadastro exclusivo para barbeiros | Essencial |
| RF-AUTH-004 | Disponibilizar tela de login compartilhada entre todos os perfis | Essencial |
| RF-AUTH-005 | Após o cadastro, permitir que o barbeiro escolha criar uma barbearia ou entrar em uma existente | Essencial |
| RF-AUTH-006 | Criar a barbearia e definir automaticamente o criador como proprietário | Essencial |
| RF-AUTH-007 | Permitir que o proprietário atue também como barbeiro, com agenda e disponibilidade próprias | Essencial |
| RF-AUTH-008 | Permitir que o proprietário compartilhe o código de convite da barbearia | Essencial |
| RF-AUTH-009 | Permitir que um barbeiro entre em uma barbearia existente por código de convite | Essencial |
| RF-AUTH-010 | Permitir o cadastro dos dados iniciais da barbearia durante sua criação | Essencial |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-AUTH-001 | O sistema possui os perfis Proprietário, Barbeiro e Cliente | Separação de responsabilidades |
| RN-AUTH-002 | Clientes e barbeiros utilizam fluxos de cadastro separados | Especificação do produto |
| RN-AUTH-003 | O login é compartilhado entre todos os perfis | Simplicidade de acesso |
| RN-AUTH-004 | O cliente precisa possuir conta para realizar agendamentos; não há agendamento anônimo | Controle de identidade e histórico |
| RN-AUTH-005 | O barbeiro pode criar uma barbearia ou entrar em uma existente | Flexibilidade de vínculo |
| RN-AUTH-006 | O barbeiro que cria a barbearia torna-se automaticamente seu proprietário | Definição de responsabilidade |
| RN-AUTH-007 | O proprietário pode acumular a função de barbeiro | Operação do estabelecimento |
| RN-AUTH-008 | O código de convite é único por barbearia e permanece o mesmo ao longo do tempo | Estabilidade do convite |
| RN-AUTH-009 | Somente o proprietário pode compartilhar o código de convite | Controle de entrada |
| RN-AUTH-010 | A saída ou remoção de um barbeiro mantém sua conta e seu histórico, removendo apenas o vínculo | Preservação de dados |
| RN-AUTH-011 | O cadastro inicial da barbearia não exige CNPJ nem documentos pessoais | Redução de barreiras |
| RN-AUTH-012 | O telefone comercial da barbearia deve ser verificado; momento e procedimento serão definidos | Requisito em aberto |
| RN-AUTH-013 | O Instagram da barbearia é opcional | Dado complementar |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Nome | Texto | Sim | Preenchimento obrigatório no cadastro de cliente e barbeiro |
| E-mail | E-mail | Sim | Formato válido e não utilizado por outra conta |
| Telefone | Telefone | Sim | Formato válido no cadastro e na edição |
| Senha | Senha | Sim | Confirmação deve corresponder à senha informada |
| Confirmar senha | Senha | Sim | Igual à senha |
| Nome da barbearia | Texto | Sim | Preenchimento obrigatório na criação |
| Telefone comercial | Telefone | Sim | Formato válido e sujeito a verificação |
| E-mail comercial | E-mail | Sim | Formato válido |
| Cidade | Texto | Sim | Preenchimento obrigatório |
| Endereço | Texto | Sim | Preenchimento obrigatório |
| Instagram | Texto | Não | Campo opcional |
| Código de convite | Texto | Sim | Deve corresponder a uma barbearia existente |

### Restrições e validações

- Todos os campos de cadastro de cliente e barbeiro são obrigatórios.
- O e-mail deve possuir formato válido e não pode estar cadastrado em outra conta.
- A confirmação de senha deve corresponder à senha informada.
- Todos os campos obrigatórios da barbearia devem ser preenchidos na criação.
- O Instagram não é obrigatório e o cadastro não exige CNPJ ou documentos pessoais.
- O código de convite deve corresponder a uma barbearia existente; códigos inválidos são rejeitados.
- A verificação do telefone comercial permanece pendente de definição de momento e procedimento.

### Estados e transições

Não se aplica: a SPEC não define ciclo de vida para contas de usuário além do vínculo profissional, tratado no módulo 6.8.

---

## 6.2. Empresa (Barbearia) (`EMP`)

### Visão geral

Permite ao proprietário consultar e manter as informações cadastrais do estabelecimento.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-EMP-001 | Permitir que o proprietário consulte os dados da barbearia | Essencial |
| RF-EMP-002 | Permitir que o proprietário edite os dados da barbearia | Essencial |
| RF-EMP-003 | Permitir atualizar nome, telefone comercial, e-mail comercial, cidade, endereço e Instagram | Essencial |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-EMP-001 | Cada barbearia possui suas próprias informações cadastrais | Isolamento entre estabelecimentos |
| RN-EMP-002 | Somente o proprietário pode alterar os dados da empresa | Controle de acesso |
| RN-EMP-003 | Barbeiros vinculados não podem alterar as informações do estabelecimento | Controle de acesso |
| RN-EMP-004 | O Instagram é opcional | Dado complementar |
| RN-EMP-005 | O telefone comercial deve passar por verificação, com momento e procedimento a definir | Requisito em aberto |
| RN-EMP-006 | Alterações cadastrais não modificam informações históricas dos agendamentos | Integridade do histórico |
| RN-EMP-007 | Os horários gerais de funcionamento são configurados separadamente | Separação de responsabilidades |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Nome da barbearia | Texto | Sim | Preenchimento obrigatório |
| Telefone comercial | Telefone | Sim | Formato válido e sujeito a verificação |
| E-mail comercial | E-mail | Sim | Formato válido |
| Cidade | Texto | Sim | Preenchimento obrigatório |
| Endereço | Texto | Sim | Preenchimento obrigatório |
| Instagram | Texto | Não | Campo opcional |

### Restrições e validações

- Todos os campos obrigatórios devem estar preenchidos.
- E-mail e telefone comercial devem possuir formato válido.
- Somente o proprietário pode alterar os dados cadastrais.
- Alterações não podem modificar os dados históricos dos agendamentos.
- A verificação do telefone comercial permanece pendente de definição.

### Estados e transições

Não se aplica: os dados da barbearia não possuem ciclo de vida próprio na SPEC.

---

## 6.3. Serviços (`SERV`)

### Visão geral

Mantém o catálogo de serviços de cada barbearia, com nome, duração e preço, e disponibiliza os serviços ativos para o agendamento público.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-SERV-001 | Disponibilizar um catálogo inicial de serviços como modelo para as barbearias | Essencial |
| RF-SERV-002 | Permitir que o proprietário adicione serviços à barbearia | Essencial |
| RF-SERV-003 | Permitir que o proprietário edite serviços cadastrados | Essencial |
| RF-SERV-004 | Permitir que o proprietário remova serviços do catálogo | Essencial |
| RF-SERV-005 | Permitir que o proprietário defina o preço de cada serviço | Essencial |
| RF-SERV-006 | Permitir que o proprietário altere a duração de cada serviço | Essencial |
| RF-SERV-007 | Disponibilizar os serviços ativos para os clientes no agendamento público | Essencial |
| RF-SERV-008 | Permitir que os barbeiros visualizem os serviços disponíveis na barbearia | Essencial |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-SERV-001 | Cada barbearia possui seu próprio catálogo de serviços | Personalização por estabelecimento |
| RN-SERV-002 | O catálogo inicial é um modelo e pode ser personalizado | Flexibilidade de preços e durações |
| RN-SERV-003 | O proprietário é o único responsável por adicionar, editar e remover serviços | Controle de acesso |
| RN-SERV-004 | Barbeiros podem apenas visualizar os serviços | Controle de acesso |
| RN-SERV-005 | Cada serviço possui nome, duração e preço | Dados mínimos do serviço |
| RN-SERV-006 | O serviço Corte + Barba é independente, com preço e duração próprios | Modelo de catálogo |
| RN-SERV-007 | Cada agendamento permite a seleção de apenas um serviço | Regra de agendamento |
| RN-SERV-008 | Remover um serviço o desativa para novos agendamentos, sem excluir registros históricos | Integridade do histórico |
| RN-SERV-009 | Agendamentos antigos preservam nome, duração e preço do serviço no momento da contratação | Integridade do histórico |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Nome | Texto | Sim | Preenchimento obrigatório |
| Duração | Duração | Sim | Valor válido e maior que zero |
| Preço | Monetário | Sim | Valor válido e não negativo |

### Restrições e validações

- Somente o proprietário pode cadastrar, editar ou remover serviços.
- Serviços removidos não aparecem como opção para novos agendamentos.
- A remoção não exclui nem modifica agendamentos históricos.
- Cada agendamento pode possuir apenas um serviço.
- O catálogo inicial contém Corte (40 minutos), Barba (30 minutos) e Corte + Barba (70 minutos).
- O preço não pode ser negativo e a duração deve ser válida.

### Estados e transições

| Estado | Descrição | Transições permitidas |
| --- | --- | --- |
| Ativo | Serviço disponível para novos agendamentos | Inativo |
| Inativo | Serviço removido do catálogo, indisponível para novos agendamentos e preservado no histórico | Nenhuma |

---

## 6.4. Horários e Disponibilidade (`HOR`)

### Visão geral

Define o funcionamento da barbearia, a disponibilidade semanal de cada profissional e os bloqueios pontuais de agenda.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-HOR-001 | Permitir que o proprietário configure os horários gerais de funcionamento da barbearia | Essencial |
| RF-HOR-002 | Permitir a configuração dos horários para cada dia da semana | Essencial |
| RF-HOR-003 | Permitir definir, por dia, horário de abertura, horário de fechamento e dia fechado | Essencial |
| RF-HOR-004 | Permitir que cada barbeiro configure sua disponibilidade semanal | Essencial |
| RF-HOR-005 | Permitir que cada barbeiro cadastre folgas e períodos de indisponibilidade | Essencial |
| RF-HOR-006 | Permitir que cada profissional bloqueie períodos específicos da agenda | Essencial |
| RF-HOR-007 | Permitir informar motivo opcional para o bloqueio | Importante |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-HOR-001 | Os horários gerais pertencem à barbearia e são configurados pelo proprietário | Controle de acesso |
| RN-HOR-002 | A disponibilidade semanal pertence individualmente a cada profissional | Autonomia do profissional |
| RN-HOR-003 | A disponibilidade do profissional deve respeitar os horários gerais da barbearia | Consistência da agenda |
| RN-HOR-004 | Um profissional não pode receber agendamentos fora de sua disponibilidade | Consistência da agenda |
| RN-HOR-005 | Horários semanais são separados de bloqueios pontuais | Modelagem distinta |
| RN-HOR-006 | Bloqueios pontuais impedem novos agendamentos no período | Proteção da agenda |
| RN-HOR-007 | O proprietário também pode configurar sua própria disponibilidade quando atuar como barbeiro | Acúmulo de funções |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Dia da semana | Enumeração | Sim | Dia válido da semana |
| Horário de abertura | Hora | Sim | Anterior ao fechamento |
| Horário de fechamento | Hora | Sim | Posterior à abertura |
| Dia fechado | Booleano | Sim | Define ausência de atendimento no dia |
| Horário de início | Hora | Sim | Dentro do funcionamento da barbearia |
| Horário de término | Hora | Sim | Posterior ao início e dentro do funcionamento |
| Data e hora de início do bloqueio | Data/hora | Sim | Anterior ao término |
| Data e hora de término do bloqueio | Data/hora | Sim | Posterior ao início |
| Motivo do bloqueio | Texto | Não | Campo opcional |

### Restrições e validações

- Os horários de funcionamento são configurados individualmente por dia da semana.
- Dias fechados não disponibilizam horários para agendamento.
- O fechamento deve ser posterior à abertura.
- A disponibilidade do profissional não pode ultrapassar o funcionamento da barbearia.
- O sistema não disponibiliza horários fora da jornada configurada para o profissional.
- O início do bloqueio deve ser anterior ao término.
- O sistema não permite novos agendamentos em períodos bloqueados e valida conflitos.
- Alterar a disponibilidade não modifica automaticamente agendamentos existentes.

### Estados e transições

Não se aplica: horários e bloqueios são configurações, sem ciclo de vida próprio na SPEC.

---

## 6.5. Agenda e Agendamentos (`AGD`)

### Visão geral

Centraliza a visualização e a gestão dos agendamentos, incluindo criação manual, cancelamento, remarcação e sugestão de novo horário.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-AGD-001 | Permitir que proprietário e barbeiros visualizem agendamentos conforme suas permissões | Essencial |
| RF-AGD-002 | Disponibilizar visualizações de agenda por dia, semana e mês | Essencial |
| RF-AGD-003 | Permitir navegar entre períodos anteriores, atuais e futuros | Essencial |
| RF-AGD-004 | Permitir que o proprietário filtre agendamentos por profissional | Importante |
| RF-AGD-005 | Permitir que proprietário ou barbeiro crie agendamentos manualmente pela agenda | Essencial |
| RF-AGD-006 | Disponibilizar a opção "+ Novo" para iniciar um agendamento manual | Essencial |
| RF-AGD-007 | Permitir informar cliente, profissional, data, horário de início, serviço e observação opcional | Essencial |
| RF-AGD-008 | Calcular automaticamente duração, horário de término e valor com base no serviço | Essencial |
| RF-AGD-009 | Permitir consultar os detalhes de cada agendamento | Essencial |
| RF-AGD-010 | Permitir que o cliente consulte os próprios agendamentos | Essencial |
| RF-AGD-011 | Permitir que o cliente cancele um agendamento antes do início | Essencial |
| RF-AGD-012 | Permitir que o cliente remarque um agendamento antes do início | Essencial |
| RF-AGD-013 | Permitir que o barbeiro cancele agendamentos da própria agenda | Essencial |
| RF-AGD-014 | Permitir que o proprietário cancele qualquer agendamento da barbearia | Essencial |
| RF-AGD-015 | Permitir que o barbeiro sugira outro horário ao cancelar um agendamento | Essencial |
| RF-AGD-016 | Permitir que o cliente aceite ou rejeite o horário sugerido | Essencial |
| RF-AGD-017 | Registrar quem realizou o cancelamento e quando ele ocorreu | Essencial |
| RF-AGD-018 | Identificar visualmente agendamentos confirmados, cancelados e bloqueios na agenda | Importante |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-AGD-001 | Agendamentos públicos e manuais são confirmados automaticamente após a criação | Fluxo sem aprovação manual |
| RN-AGD-002 | Todo agendamento está associado a uma barbearia, cliente, profissional e serviço | Integridade do domínio |
| RN-AGD-003 | Cada agendamento possui apenas um serviço | Regra de negócio |
| RN-AGD-004 | O horário de término é calculado pela duração do serviço | Consistência de agenda |
| RN-AGD-005 | O valor corresponde ao preço do serviço no momento da contratação | Preservação comercial |
| RN-AGD-006 | O sistema valida funcionamento, disponibilidade, bloqueios e conflitos | Consistência de agenda |
| RN-AGD-007 | Um horário ocupado não pode receber outro agendamento conflitante | Evita sobreposição |
| RN-AGD-008 | O cliente pode cancelar a qualquer momento antes do início, sem antecedência mínima | Flexibilidade ao cliente |
| RN-AGD-009 | O cliente pode remarcar a qualquer momento antes do início, sem antecedência mínima | Flexibilidade ao cliente |
| RN-AGD-010 | A remarcação atualiza o agendamento existente e preserva seu histórico | Rastreabilidade |
| RN-AGD-011 | O barbeiro pode cancelar agendamentos da própria agenda | Autonomia operacional |
| RN-AGD-012 | O proprietário pode cancelar qualquer agendamento da barbearia | Supervisão do estabelecimento |
| RN-AGD-013 | O cancelamento não exige justificativa, mas registra autor e data | Auditoria |
| RN-AGD-014 | Na sugestão de novo horário, o motivo é obrigatório | Contexto para o cliente |
| RN-AGD-015 | O horário sugerido fica reservado por até 24 horas | Janela de decisão |
| RN-AGD-016 | Se o cliente aceitar, o agendamento é transferido para o horário sugerido | Conclusão da remarcação |
| RN-AGD-017 | Se o cliente rejeitar ou não responder em 24 horas, o horário é liberado | Liberação da agenda |
| RN-AGD-018 | O agendamento original permanece cancelado se a sugestão for rejeitada ou expirar | Consistência do cancelamento |
| RN-AGD-019 | Após o início do atendimento, ninguém pode cancelar nem remarcar | Proteção do atendimento em curso |
| RN-AGD-020 | Agendamentos confirmados são verdes, cancelados são vermelhos e bloqueios são cinzas | Identificação visual |
| RN-AGD-021 | A cor amarela não é utilizada para representar agendamentos pendentes | Padrão visual definido |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Cliente | Referência | Sim | Cliente cadastrado |
| Profissional | Referência | Sim | Vinculado à barbearia e ativo |
| Data | Data | Sim | Data válida |
| Horário de início | Hora | Sim | Dentro do funcionamento e da disponibilidade |
| Serviço | Referência | Sim | Serviço ativo da barbearia |
| Observação | Texto | Não | Campo opcional |
| Novo horário sugerido | Data/hora | Sim | Disponível e dentro das regras de agenda |
| Motivo do cancelamento | Texto | Sim | Obrigatório na sugestão de novo horário |

### Restrições e validações

- O cliente deve possuir conta para realizar agendamentos.
- O profissional deve estar vinculado à barbearia e ativo.
- O serviço deve estar ativo.
- Data e horário devem respeitar o funcionamento da barbearia e a disponibilidade do profissional.
- O sistema impede conflitos entre agendamentos do mesmo profissional e durante bloqueios.
- O horário de término é calculado automaticamente.
- Cancelamentos e remarcações são bloqueados após o início do atendimento.
- O barbeiro não pode alterar agendamentos de profissionais fora da própria agenda.
- Somente o proprietário pode cancelar agendamentos de qualquer profissional da barbearia.
- O motivo é obrigatório quando o barbeiro sugere novo horário.
- O horário sugerido fica reservado por no máximo 24 horas e é liberado se rejeitado ou expirado.
- O histórico do agendamento é preservado em remarcações e alterações de horário.

### Estados e transições

| Estado | Descrição | Transições permitidas |
| --- | --- | --- |
| Confirmado | Agendamento criado e confirmado automaticamente | Em atendimento, Cancelado, Não compareceu |
| Em atendimento | Atendimento iniciado pelo barbeiro | Finalizado |
| Finalizado | Atendimento concluído | Nenhuma |
| Cancelado | Agendamento cancelado por cliente, barbeiro ou proprietário | Nenhuma |
| Não compareceu | Cliente não compareceu ao agendamento | Nenhuma |
| Sugestão pendente | Cancelamento com novo horário reservado por até 24 horas | Transferido para o novo horário, Liberado |

---

## 6.6. Atendimento e Pagamentos (`ATD`)

### Visão geral

Controla a execução do atendimento no dia do agendamento e o registro do pagamento presencial, com estados independentes.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-ATD-001 | Permitir que o barbeiro consulte os detalhes do agendamento | Essencial |
| RF-ATD-002 | Permitir que o barbeiro inicie o atendimento quando o cliente chegar | Essencial |
| RF-ATD-003 | Registrar automaticamente o horário real de início | Essencial |
| RF-ATD-004 | Permitir que o barbeiro finalize o atendimento após sua conclusão | Essencial |
| RF-ATD-005 | Registrar automaticamente o horário real de término | Essencial |
| RF-ATD-006 | Permitir que o barbeiro registre o pagamento do atendimento | Essencial |
| RF-ATD-007 | Permitir pagamento em dinheiro, Pix, cartão de débito ou cartão de crédito | Essencial |
| RF-ATD-008 | Permitir que o barbeiro marque o cliente como não comparecente | Essencial |
| RF-ATD-009 | Apresentar separadamente a situação do atendimento e a situação do pagamento | Essencial |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-ATD-001 | Atendimento e pagamento possuem estados independentes | Controle operacional e financeiro distintos |
| RN-ATD-002 | Um atendimento finalizado pode permanecer com pagamento pendente | Pagamento posterior ao atendimento |
| RN-ATD-003 | O atendimento é iniciado pelo barbeiro quando o cliente chega | Operação presencial |
| RN-ATD-004 | O horário real de início é registrado pelo sistema na inicialização | Precisão do registro |
| RN-ATD-005 | O horário real de término é registrado pelo sistema na finalização | Precisão do registro |
| RN-ATD-006 | O pagamento é realizado presencialmente, após ou ao finalizar o atendimento | Modelo da V1.0 |
| RN-ATD-007 | Não há pagamento antecipado nem gateway de pagamento na primeira versão | Escopo da V1.0 |
| RN-ATD-008 | O valor recebido atualiza o indicador "Recebido no mês" do dashboard | Consolidação financeira |
| RN-ATD-009 | Não comparecimento não é tratado como cancelamento | Distinção de status |
| RN-ATD-010 | Agendamento marcado como não compareceu não pode iniciar nem finalizar atendimento | Integridade de status |
| RN-ATD-011 | O cancelamento não é permitido após o início do atendimento | Proteção do atendimento em curso |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Agendamento | Referência | Sim | Agendamento existente e não cancelado |
| Horário real de início | Data/hora | Sim | Registrado automaticamente pelo sistema |
| Horário real de término | Data/hora | Sim | Registrado automaticamente ao finalizar |
| Valor recebido | Monetário | Sim | Valor válido e não negativo |
| Forma de pagamento | Enumeração | Sim | Dinheiro, Pix, cartão de débito ou cartão de crédito |

### Restrições e validações

- O atendimento só pode ser iniciado quando o cliente comparecer.
- Os horários reais de início e término são registrados automaticamente.
- O atendimento não pode ser finalizado antes de ser iniciado.
- O pagamento deve possuir valor válido e não negativo.
- A forma de pagamento deve ser uma das opções disponíveis.
- Atendimento finalizado pode permanecer com pagamento pendente.
- Agendamentos cancelados não são considerados pagamentos recebidos.
- Não comparecimento não permite executar atendimento.
- Não é permitido cancelar agendamento após o início do atendimento.

### Estados e transições

**Atendimento**

| Estado | Descrição | Transições permitidas |
| --- | --- | --- |
| Não iniciado | Agendamento confirmado e atendimento ainda não iniciado | Em andamento, Não compareceu |
| Em andamento | Atendimento iniciado com horário real registrado | Finalizado |
| Finalizado | Atendimento concluído com horário real de término | Nenhuma |
| Não compareceu | Cliente não compareceu ao agendamento | Nenhuma |

**Pagamento**

| Estado | Descrição | Transições permitidas |
| --- | --- | --- |
| Pendente | Atendimento sem pagamento registrado | Pago |
| Pago | Pagamento registrado com valor e forma | Nenhuma |

---

## 6.7. Clientes (`CLI`)

### Visão geral

Permite ao proprietário consultar os clientes relacionados à barbearia e ao cliente manter os próprios dados cadastrais.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-CLI-001 | Disponibilizar área de consulta dos clientes relacionados à barbearia | Essencial |
| RF-CLI-002 | Permitir que o proprietário visualize clientes que possuem ou já tiveram agendamentos na barbearia | Essencial |
| RF-CLI-003 | Permitir que o proprietário pesquise clientes cadastrados | Importante |
| RF-CLI-004 | Permitir que o cliente consulte os próprios dados cadastrais | Essencial |
| RF-CLI-005 | Permitir que o cliente edite suas informações pessoais | Essencial |
| RF-CLI-006 | Permitir que o cliente atualize nome, e-mail e telefone | Essencial |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-CLI-001 | Clientes criam as próprias contas no sistema | Autoatendimento |
| RN-CLI-002 | O proprietário não pode cadastrar clientes manualmente | Controle de cadastro |
| RN-CLI-003 | O proprietário não pode editar nem excluir contas de clientes | Controle de acesso |
| RN-CLI-004 | A relação do cliente com a barbearia é estabelecida por seus agendamentos | Vínculo por histórico |
| RN-CLI-005 | A listagem considera clientes com agendamentos atuais ou históricos no estabelecimento | Escopo da consulta |
| RN-CLI-006 | Cada cliente edita apenas os próprios dados | Privacidade e controle de acesso |
| RN-CLI-007 | Alterações nos dados pessoais não modificam informações históricas dos agendamentos | Integridade do histórico |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Nome | Texto | Sim | Preenchimento obrigatório |
| E-mail | E-mail | Sim | Formato válido |
| Telefone | Telefone | Sim | Formato válido |

### Restrições e validações

- Nome, e-mail e telefone são obrigatórios e devem possuir valores válidos.
- O cliente só pode editar os próprios dados.
- O proprietário não pode alterar, excluir ou cadastrar contas de clientes.
- A listagem apresenta apenas clientes relacionados à barbearia por agendamentos.
- Alterações cadastrais não modificam os dados históricos dos agendamentos.

### Estados e transições

Não se aplica: a SPEC não define estados para a conta de cliente.

---

## 6.8. Profissionais (`PROF`)

### Visão geral

Permite ao proprietário gerenciar os vínculos dos profissionais com a barbearia, incluindo ativação, desativação e remoção.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-PROF-001 | Disponibilizar área de gerenciamento dos profissionais vinculados à barbearia | Essencial |
| RF-PROF-002 | Permitir que o proprietário visualize a lista de profissionais | Essencial |
| RF-PROF-003 | Apresentar nome, telefone e situação (ativo ou inativo) na listagem | Essencial |
| RF-PROF-004 | Permitir que o proprietário consulte os dados de cada profissional | Essencial |
| RF-PROF-005 | Permitir que o proprietário edite os dados profissionais | Essencial |
| RF-PROF-006 | Permitir que o proprietário ative ou desative profissionais | Essencial |
| RF-PROF-007 | Permitir que o proprietário remova o vínculo de um profissional com a barbearia | Essencial |
| RF-PROF-008 | Disponibilizar o código de convite da barbearia para compartilhamento pelo proprietário | Essencial |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-PROF-001 | Profissionais criam as próprias contas e entram na barbearia por código de convite | Autoatendimento e controle de entrada |
| RN-PROF-002 | Somente o proprietário gerencia os vínculos dos profissionais | Controle de acesso |
| RN-PROF-003 | Profissional desativado não recebe novos agendamentos | Proteção da agenda |
| RN-PROF-004 | A desativação não exclui a conta nem o histórico do profissional | Preservação de dados |
| RN-PROF-005 | A remoção do vínculo não exclui a conta nem os registros históricos da barbearia | Preservação de dados |
| RN-PROF-006 | Cada profissional administra a própria disponibilidade e os próprios bloqueios | Autonomia operacional |
| RN-PROF-007 | O proprietário pode atuar como profissional, com disponibilidade e agenda próprias | Acúmulo de funções |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Nome | Texto | Sim | Exibido na listagem |
| Telefone | Telefone | Sim | Exibido na listagem |
| Situação | Enumeração | Sim | Ativo ou inativo |
| Código de convite | Texto | Sim | Único e permanente por barbearia |

### Restrições e validações

- Somente o proprietário pode ativar, desativar, editar ou remover vínculos.
- Profissionais desativados não recebem novos agendamentos.
- A desativação não exclui a conta do profissional.
- A remoção do vínculo não exclui o histórico de agendamentos e atendimentos.
- O profissional só administra a própria disponibilidade e os próprios bloqueios.
- Profissionais sem vínculo ativo não podem receber novos agendamentos na barbearia.

### Estados e transições

| Estado | Descrição | Transições permitidas |
| --- | --- | --- |
| Ativo | Profissional vinculado e apto a receber agendamentos | Inativo, Vínculo removido |
| Inativo | Profissional vinculado, mas sem novos agendamentos | Ativo, Vínculo removido |
| Vínculo removido | Profissional sem vínculo com a barbearia, com conta e histórico preservados | Novo vínculo por código de convite |

---

## 6.9. Dashboard (`DASH`)

### Visão geral

Apresenta ao proprietário uma visão consolidada dos agendamentos e dos recebimentos da barbearia.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-DASH-001 | Disponibilizar dashboard inicial ao proprietário após a configuração da barbearia | Essencial |
| RF-DASH-002 | Apresentar os indicadores Hoje, Esta semana, Cancelamentos e Recebido no mês | Essencial |
| RF-DASH-003 | Apresentar a lista de próximos agendamentos | Essencial |
| RF-DASH-004 | Apresentar horário, cliente, profissional e status em cada próximo agendamento | Essencial |
| RF-DASH-005 | Apresentar os indicadores mesmo quando não houver movimentações | Essencial |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-DASH-001 | O dashboard é a primeira tela do proprietário após a configuração inicial | Ponto de entrada da gestão |
| RN-DASH-002 | "Hoje" considera os agendamentos da data atual | Definição do indicador |
| RN-DASH-003 | "Esta semana" considera os agendamentos da semana atual | Definição do indicador |
| RN-DASH-004 | "Cancelamentos" considera agendamentos cancelados no mês atual | Definição do indicador |
| RN-DASH-005 | "Recebido no mês" considera exclusivamente pagamentos registrados no mês atual | Definição do indicador |
| RN-DASH-006 | O valor recebido é a soma dos pagamentos efetivamente registrados | Consolidação financeira |
| RN-DASH-007 | Os próximos agendamentos apresentam compromissos futuros da barbearia | Definição da lista |
| RN-DASH-008 | Indicadores sem dados apresentam valor zero | Clareza na ausência de movimentação |
| RN-DASH-009 | Os dados apresentados pertencem à barbearia do proprietário autenticado | Isolamento entre estabelecimentos |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Hoje | Indicador numérico | Sim | Agendamentos da data atual |
| Esta semana | Indicador numérico | Sim | Agendamentos da semana atual |
| Cancelamentos | Indicador numérico | Sim | Agendamentos cancelados no mês atual |
| Recebido no mês | Indicador monetário | Sim | Soma dos pagamentos registrados no mês atual |
| Horário | Hora | Sim | Exibido nos próximos agendamentos |
| Cliente | Referência | Sim | Exibido nos próximos agendamentos |
| Profissional | Referência | Sim | Exibido nos próximos agendamentos |
| Status | Enumeração | Sim | Situação do agendamento |

### Restrições e validações

- Os indicadores consideram os períodos correspondentes.
- Agendamentos cancelados não são contabilizados como pagamentos recebidos.
- O indicador financeiro considera apenas pagamentos registrados.
- O dashboard apresenta zero quando não há dados.
- Os dados pertencem à barbearia do proprietário autenticado.

### Estados e transições

Não se aplica: o dashboard é uma visão consolidada, sem ciclo de vida próprio.

---

## 6.10. Agendamento Público (`PUB`)

### Visão geral

Disponibiliza uma página pública por barbearia para que clientes autenticados escolham serviço, profissional e horário e confirmem o agendamento automaticamente.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-PUB-001 | Disponibilizar uma página pública específica para cada barbearia | Essencial |
| RF-PUB-002 | Apresentar as informações da barbearia e seus serviços disponíveis | Essencial |
| RF-PUB-003 | Permitir o agendamento em etapas: serviço, profissional, data e horário, observação e confirmação | Essencial |
| RF-PUB-004 | Permitir que o cliente visualize os horários disponíveis de cada profissional | Essencial |
| RF-PUB-005 | Permitir que o cliente confirme o agendamento | Essencial |
| RF-PUB-006 | Disponibilizar o agendamento confirmado na agenda da barbearia e do profissional | Essencial |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-PUB-001 | Cada página pública corresponde a uma barbearia específica | Isolamento entre estabelecimentos |
| RN-PUB-002 | O cliente deve estar autenticado para realizar o agendamento | Identificação e histórico |
| RN-PUB-003 | Os dados pessoais do cliente são obtidos da própria conta | Evita redigitação e inconsistência |
| RN-PUB-004 | Cada agendamento possui apenas um serviço | Regra de negócio |
| RN-PUB-005 | Os horários respeitam funcionamento, disponibilidade, bloqueios e agendamentos existentes | Consistência de agenda |
| RN-PUB-006 | Agendamentos públicos são confirmados automaticamente, sem aprovação manual | Fluxo autônomo |
| RN-PUB-007 | O horário selecionado é ocupado pelo agendamento, impedindo reservas conflitantes | Evita sobreposição |
| RN-PUB-008 | A disponibilidade é validada novamente na confirmação | Concorrência de agendamentos |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Serviço | Referência | Sim | Serviço ativo da barbearia |
| Profissional | Referência | Sim | Ativo e vinculado à barbearia |
| Data | Data | Sim | Data válida e disponível |
| Horário | Hora | Sim | Horário disponível |
| Observação | Texto | Não | Campo opcional |
| Dados do cliente | Referência | Sim | Obtidos automaticamente da conta autenticada |

### Restrições e validações

- O cliente deve estar autenticado para concluir o agendamento.
- Serviço e profissional devem estar ativos e vinculados à barbearia.
- O horário deve estar disponível e respeitar funcionamento, disponibilidade e bloqueios.
- O sistema impede conflitos com outros agendamentos.
- A confirmação é automática após a validação.
- O mesmo horário não pode ser reservado simultaneamente por agendamentos conflitantes.

### Estados e transições

O agendamento público nasce no estado **Confirmado**, conforme os estados definidos no módulo 6.5.

---

## 6.11. Configurações (`CONF`)

### Visão geral

Reúne as configurações da barbearia e as configurações individuais de disponibilidade e bloqueios dos profissionais.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-CONF-001 | Disponibilizar área de configurações para o proprietário | Essencial |
| RF-CONF-002 | Permitir a administração dos horários gerais de funcionamento | Essencial |
| RF-CONF-003 | Permitir configurar os horários de cada dia da semana | Essencial |
| RF-CONF-004 | Permitir definir dias em que a barbearia permanecerá fechada | Essencial |
| RF-CONF-005 | Disponibilizar área de consulta dos usuários vinculados à barbearia | Essencial |
| RF-CONF-006 | Permitir que o proprietário consulte e compartilhe o código de convite | Essencial |
| RF-CONF-007 | Permitir que cada barbeiro acesse a configuração da própria disponibilidade semanal | Essencial |
| RF-CONF-008 | Permitir que cada barbeiro administre os próprios bloqueios de agenda | Essencial |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-CONF-001 | Somente o proprietário configura os horários gerais da barbearia | Controle de acesso |
| RN-CONF-002 | Cada barbeiro configura a própria disponibilidade | Autonomia do profissional |
| RN-CONF-003 | A disponibilidade individual respeita os horários gerais | Consistência da agenda |
| RN-CONF-004 | Horários gerais e disponibilidade individual são tratados separadamente | Modelagem distinta |
| RN-CONF-005 | Bloqueios pontuais são administrados na agenda do próprio profissional | Autonomia operacional |
| RN-CONF-006 | O código de convite é único e permanente por barbearia | Estabilidade do convite |
| RN-CONF-007 | Somente o proprietário compartilha o código de convite | Controle de entrada |
| RN-CONF-008 | Novos barbeiros entram por código de convite, não por criação direta de conta | Controle de cadastro |
| RN-CONF-009 | A V1.0 mantém as configurações simples, deixando recursos adicionais para versões futuras | Escopo da versão |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Dia da semana | Enumeração | Sim | Dia válido |
| Horário de abertura | Hora | Sim | Anterior ao fechamento |
| Horário de fechamento | Hora | Sim | Posterior à abertura |
| Dia fechado | Booleano | Sim | Define ausência de atendimento |
| Horário de início | Hora | Sim | Dentro do funcionamento da barbearia |
| Horário de término | Hora | Sim | Posterior ao início e dentro do funcionamento |

### Restrições e validações

- Horários gerais respeitam os dias de funcionamento definidos pelo proprietário.
- O fechamento deve ser posterior à abertura.
- Dias fechados não disponibilizam horários para agendamento.
- A disponibilidade do profissional não pode ultrapassar o horário geral.
- Cada barbeiro administra apenas a própria disponibilidade.
- Somente o proprietário compartilha o código de convite.
- O sistema não permite criação direta de contas de barbeiros pelo proprietário.
- As configurações respeitam as permissões de cada perfil.

### Estados e transições

Não se aplica: configurações não possuem ciclo de vida próprio na SPEC.

---

## 6.12. Notificações (`NOTIF`)

### Visão geral

Disponibiliza notificações internas relacionadas aos agendamentos para os usuários envolvidos.

### Requisitos funcionais

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-NOTIF-001 | Disponibilizar notificações internas relacionadas aos agendamentos | Importante |

### Regras de negócio

| ID | Regra | Origem/Justificativa |
| --- | --- | --- |
| RN-NOTIF-001 | As notificações da primeira versão são internas ao sistema | Escopo da V1.0 |
| RN-NOTIF-002 | O envio automático de mensagens pelo WhatsApp fica para versão futura | Evolução prevista |

### Campos obrigatórios

| Campo | Tipo | Obrigatório | Regra de validação |
| --- | --- | --- | --- |
| Evento relacionado | Referência | Sim | Agendamento ou cancelamento correspondente |
| Destinatário | Referência | Sim | Usuário envolvido no evento |
| Conteúdo | Texto | Sim | Informação mínima do evento |

### Restrições e validações

- As notificações são apenas internas na V1.0.
- Não há envio automático por WhatsApp na V1.0.

### Estados e transições

Não se aplica: a SPEC não define estados para notificações.

---

# 7. Requisitos não funcionais

Os alvos numéricos abaixo são premissas de qualidade propostas para a V1.0 e devem ser validados antes da implementação.

| ID | Categoria | Requisito | Critério mensurável |
| --- | --- | --- | --- |
| RNF-DESEMPENHO-001 | Desempenho | As telas de agenda e agendamento devem responder em tempo adequado ao uso | Carregamento em até 2 segundos no percentil 95 em conexão de banda larga |
| RNF-DESEMPENHO-002 | Desempenho | A consulta de horários disponíveis deve ser rápida | Resposta em até 1 segundo no percentil 95 |
| RNF-DESEMPENHO-003 | Desempenho | A confirmação de agendamento deve concluir sem espera perceptível | Conclusão em até 3 segundos no percentil 95 |
| RNF-SEGURANCA-001 | Segurança | Senhas devem ser armazenadas de forma irreversível | Hash forte com salt; nenhuma senha em texto puro |
| RNF-SEGURANCA-002 | Segurança | O acesso a dados e operações deve respeitar o perfil autenticado | 100% das operações críticas validadas no servidor |
| RNF-SEGURANCA-003 | Segurança | O código de convite não deve ser previsível | Código único, aleatório e não sequencial |
| RNF-CONCORRENCIA-001 | Consistência | Agendamentos simultâneos não podem ocupar o mesmo horário do mesmo profissional | Zero reservas conflitantes confirmadas |
| RNF-DISPONIBILIDADE-001 | Disponibilidade | O sistema deve permanecer disponível durante o horário de operação | 99,5% de disponibilidade mensal, excluindo manutenções planejadas |
| RNF-PRIVACIDADE-001 | Privacidade e LGPD | Dados pessoais devem ser tratados conforme a LGPD e acessíveis apenas ao titular e a quem tem permissão | Acesso restrito por perfil e finalidade |
| RNF-AUDITORIA-001 | Auditoria | Cancelamentos e alterações de agendamento devem ser rastreáveis | Autor, data e hora registrados; histórico preservado |
| RNF-ACESSIBILIDADE-001 | Acessibilidade | As interfaces devem ser utilizáveis por pessoas com deficiência | Conformidade com WCAG 2.1 nível AA nas telas principais |
| RNF-COMPATIBILIDADE-001 | Compatibilidade | O sistema deve funcionar nos navegadores usuais | Últimas duas versões de Chrome, Edge, Firefox e Safari; layout responsivo |
| RNF-USABILIDADE-001 | Usabilidade | O agendamento público deve ser concluído sem suporte | Fluxo de até 5 etapas, com confirmação clara |
| RNF-RETENCAO-001 | Retenção de dados | O histórico de agendamentos, atendimentos e pagamentos deve ser preservado | 100% dos registros históricos mantidos após alterações cadastrais e remoções de serviço ou vínculo |

---

# 8. Critérios de aceite

## 8.1. Autenticação e Cadastro (`AUTH`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-AUTH-001 | RF-AUTH-001 | Dado um visitante na tela inicial, quando acessa o sistema, então visualiza as opções "Sou cliente" e "Sou barbeiro" | Opções disponíveis e funcionais |
| CA-AUTH-002 | RF-AUTH-002, RF-AUTH-003 | Dado um visitante, quando escolhe o perfil de cliente ou barbeiro, então acessa o formulário de cadastro correspondente | Fluxos de cadastro separados |
| CA-AUTH-003 | RF-AUTH-002, RF-AUTH-003 | Dado um cadastro com e-mail já existente ou confirmação de senha divergente, quando tenta concluir, então o sistema rejeita a operação | Nenhuma conta é criada e a mensagem de erro é exibida |
| CA-AUTH-004 | RF-AUTH-004 | Dado um usuário com conta válida, quando informa e-mail e senha na tela compartilhada, então acessa o sistema conforme seu perfil | Sessão autenticada com permissões corretas |
| CA-AUTH-005 | RF-AUTH-005, RF-AUTH-006 | Dado um barbeiro recém-cadastrado, quando escolhe criar uma barbearia e preenche os dados obrigatórios, então a barbearia é criada e ele se torna proprietário | Dashboard inicial disponível ao criador |
| CA-AUTH-006 | RF-AUTH-007 | Dado um proprietário autenticado, quando acessa sua atuação como barbeiro, então possui agenda e disponibilidade próprias | Agenda e disponibilidade independentes disponíveis |
| CA-AUTH-007 | RF-AUTH-008, RF-AUTH-009 | Dado um proprietário com código de convite, quando o compartilha e um barbeiro o utiliza, então o vínculo é criado | Barbeiro vinculado à barbearia |
| CA-AUTH-008 | RF-AUTH-009 | Dado um barbeiro com código inválido, quando tenta entrar em uma barbearia, então o sistema rejeita a operação | Vínculo não é criado |
| CA-AUTH-009 | RF-AUTH-010 | Dado um proprietário em criação de barbearia, quando informa os dados obrigatórios, então o cadastro inicial é concluído sem exigir CNPJ ou documentos | Barbearia criada somente com os campos previstos |

## 8.2. Empresa (`EMP`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-EMP-001 | RF-EMP-001 | Dado um proprietário autenticado, quando acessa os dados da barbearia, então visualiza as informações cadastradas | Dados corretos da própria barbearia |
| CA-EMP-002 | RF-EMP-002, RF-EMP-003 | Dado um proprietário, quando edita um campo obrigatório com valor válido, então a alteração é salva | Dados atualizados |
| CA-EMP-003 | RF-EMP-002, RF-EMP-003 | Dado um e-mail ou telefone comercial inválido, quando o proprietário tenta salvar, então o sistema rejeita a alteração | Dados anteriores preservados |
| CA-EMP-004 | RF-EMP-002, RF-EMP-003 | Dado um barbeiro vinculado, quando tenta alterar os dados da empresa, então o acesso é negado | Somente o proprietário altera o cadastro |
| CA-EMP-005 | RF-EMP-003 | Dado um agendamento histórico, quando o proprietário altera os dados da barbearia, então o agendamento permanece inalterado | Histórico preservado |

## 8.3. Serviços (`SERV`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-SERV-001 | RF-SERV-001 | Dada uma nova barbearia, quando o catálogo inicial é disponibilizado, então contém Corte (40 min), Barba (30 min) e Corte + Barba (70 min) | Catálogo modelo disponível |
| CA-SERV-002 | RF-SERV-002, RF-SERV-005, RF-SERV-006 | Dado um proprietário, quando adiciona um serviço com nome, duração e preço válidos, então o serviço é criado no catálogo | Serviço disponível para configuração |
| CA-SERV-003 | RF-SERV-003 | Dado um serviço existente, quando o proprietário altera nome, duração ou preço, então a alteração é salva | Serviço atualizado |
| CA-SERV-004 | RF-SERV-004, RF-SERV-007 | Dado um serviço removido, quando um novo agendamento é iniciado, então o serviço não aparece como opção | Serviço indisponível para novos agendamentos |
| CA-SERV-005 | RF-SERV-008 | Dado um barbeiro vinculado, quando acessa o catálogo, então visualiza os serviços sem opções de edição | Acesso somente para leitura |
| CA-SERV-006 | RF-SERV-002, RF-SERV-005 | Dado um preço negativo ou duração inválida, quando o proprietário tenta salvar, então o sistema rejeita | Serviço não é criado ou alterado |
| CA-SERV-007 | RF-SERV-004 | Dado um agendamento histórico com serviço removido, quando consultado, então mantém nome, duração e preço originais | Histórico preservado |

## 8.4. Horários e Disponibilidade (`HOR`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-HOR-001 | RF-HOR-001, RF-HOR-002, RF-HOR-003 | Dado um proprietário, quando configura abertura e fechamento por dia da semana, então os horários são salvos individualmente | Configuração semanal aplicada |
| CA-HOR-002 | RF-HOR-003 | Dado um dia marcado como fechado, quando o cliente consulta horários, então nenhum horário é ofertado naquele dia | Dia indisponível para agendamento |
| CA-HOR-003 | RF-HOR-002, RF-HOR-003 | Dado um fechamento anterior à abertura, quando o proprietário tenta salvar, então o sistema rejeita | Configuração inválida não é salva |
| CA-HOR-004 | RF-HOR-004 | Dado um barbeiro, quando configura sua disponibilidade semanal, então os horários são salvos para ele | Disponibilidade individual registrada |
| CA-HOR-005 | RF-HOR-004 | Dado um profissional com disponibilidade fora do funcionamento da barbearia, quando tenta salvar, então o sistema rejeita | Disponibilidade limitada ao funcionamento |
| CA-HOR-006 | RF-HOR-005, RF-HOR-006, RF-HOR-007 | Dado um bloqueio com início anterior ao término e motivo opcional, quando salvo, então impede novos agendamentos no período | Bloqueio ativo e conflitos validados |
| CA-HOR-007 | RF-HOR-006 | Dado um bloqueio com início posterior ao término, quando o profissional tenta salvar, então o sistema rejeita | Bloqueio inválido não é criado |
| CA-HOR-008 | RF-HOR-004 | Dado um agendamento existente, quando a disponibilidade é alterada, então o agendamento não é modificado automaticamente | Histórico preservado |

## 8.5. Agenda e Agendamentos (`AGD`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-AGD-001 | RF-AGD-001, RF-AGD-002 | Dado um usuário autorizado, quando acessa a agenda, então visualiza as opções de dia, semana e mês | Visualizações disponíveis conforme permissão |
| CA-AGD-002 | RF-AGD-001, RF-AGD-003 | Dado um usuário na agenda, quando navega para período anterior ou futuro, então os agendamentos correspondentes são exibidos | Navegação entre períodos funcional |
| CA-AGD-003 | RF-AGD-004 | Dado um proprietário, quando filtra por profissional, então apenas os agendamentos do profissional são exibidos | Filtro aplicado corretamente |
| CA-AGD-004 | RF-AGD-005, RF-AGD-006, RF-AGD-007 | Dado um usuário autorizado, quando seleciona "+ Novo" e informa os campos obrigatórios, então o agendamento manual é criado | Agendamento confirmado na agenda |
| CA-AGD-005 | RF-AGD-008 | Dado um serviço com duração e preço, quando o agendamento é criado, então término e valor são calculados automaticamente | Cálculo consistente com o serviço |
| CA-AGD-006 | RF-AGD-009, RF-AGD-010 | Dado um agendamento, quando cliente ou profissional autorizado consulta os detalhes, então os dados completos são exibidos | Detalhes acessíveis conforme permissão |
| CA-AGD-007 | RF-AGD-011 | Dado um agendamento próprio não iniciado, quando o cliente cancela, então o cancelamento é registrado | Agendamento cancelado |
| CA-AGD-008 | RF-AGD-012 | Dado um agendamento próprio não iniciado, quando o cliente remarca para horário disponível, então o agendamento é atualizado | Histórico anterior preservado |
| CA-AGD-009 | RF-AGD-013, RF-AGD-014 | Dado um agendamento não iniciado, quando o barbeiro cancela um da própria agenda ou o proprietário cancela qualquer um, então o cancelamento é permitido | Permissões de cancelamento respeitadas |
| CA-AGD-010 | RF-AGD-017 | Dado qualquer cancelamento, quando concluído, então o sistema registra autor e data/hora | Auditoria disponível |
| CA-AGD-011 | RF-AGD-015 | Dado um cancelamento por barbeiro, quando não informa motivo ou novo horário, então o sistema impede a sugestão | Motivo obrigatório |
| CA-AGD-012 | RF-AGD-015, RF-AGD-016 | Dado um cancelamento com sugestão, quando o cliente aceita, então o agendamento é transferido para o horário sugerido | Horário original liberado e novo ocupado |
| CA-AGD-013 | RF-AGD-016 | Dado um cancelamento com sugestão, quando o cliente rejeita ou 24 horas expiram, então o horário é liberado | Agendamento original permanece cancelado |
| CA-AGD-014 | RF-AGD-018 | Dado um dia na agenda, quando exibe agendamentos e bloqueios, então confirmados são verdes, cancelados são vermelhos e bloqueios são cinzas | Nenhum agendamento pendente em amarelo |
| CA-AGD-015 | RF-AGD-005, RF-AGD-007 | Dado um horário já ocupado ou bloqueado, quando se tenta criar outro agendamento, então o sistema rejeita | Sem sobreposição de agenda |
| CA-AGD-016 | RF-AGD-011, RF-AGD-012 | Dado um atendimento já iniciado, quando cliente ou profissional tenta cancelar ou remarcar, então o sistema impede | Ação bloqueada após o início |

## 8.6. Atendimento e Pagamentos (`ATD`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-ATD-001 | RF-ATD-001 | Dado um barbeiro, quando consulta um agendamento da própria agenda, então visualiza os detalhes do atendimento | Informações completas |
| CA-ATD-002 | RF-ATD-002, RF-ATD-003 | Dado um agendamento confirmado, quando o barbeiro inicia o atendimento, então o horário real de início é registrado | Atendimento em andamento |
| CA-ATD-003 | RF-ATD-004, RF-ATD-005 | Dado um atendimento em andamento, quando o barbeiro finaliza, então o horário real de término é registrado | Atendimento finalizado |
| CA-ATD-004 | RF-ATD-004 | Dado um atendimento não iniciado, quando o barbeiro tenta finalizar, então o sistema impede | Finalização bloqueada |
| CA-ATD-005 | RF-ATD-006, RF-ATD-007 | Dado um atendimento, quando o barbeiro registra valor válido e forma de pagamento permitida, então o pagamento é salvo | Pagamento registrado |
| CA-ATD-006 | RF-ATD-006, RF-ATD-009 | Dado um atendimento finalizado sem pagamento, quando consultado, então permanece com pagamento pendente | Situações exibidas separadamente |
| CA-ATD-007 | RF-ATD-008 | Dado um agendamento confirmado, quando o barbeiro marca não comparecimento, então o status é registrado sem tratá-lo como cancelamento | Não comparecimento distinto de cancelamento |
| CA-ATD-008 | RF-ATD-008 | Dado um agendamento marcado como não compareceu, quando se tenta iniciar ou finalizar atendimento, então o sistema impede | Ação bloqueada |
| CA-ATD-009 | RF-ATD-006, RF-ATD-007 | Dado valor negativo ou forma de pagamento inválida, quando o barbeiro tenta salvar, então o sistema rejeita | Pagamento não é registrado |
| CA-ATD-010 | RF-ATD-006 | Dado um pagamento registrado, quando o dashboard é consultado, então o valor compõe "Recebido no mês" | Indicador atualizado |

## 8.7. Clientes (`CLI`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-CLI-001 | RF-CLI-001, RF-CLI-002 | Dado um proprietário, quando acessa a área de clientes, então visualiza clientes com agendamentos atuais ou históricos na barbearia | Lista limitada ao vínculo por agendamentos |
| CA-CLI-002 | RF-CLI-003 | Dado um proprietário na lista de clientes, quando pesquisa por um cliente cadastrado, então o resultado correspondente é exibido | Pesquisa funcional |
| CA-CLI-003 | RF-CLI-002 | Dado um cliente sem agendamentos na barbearia, quando o proprietário consulta a lista, então ele não é exibido | Somente clientes relacionados |
| CA-CLI-004 | RF-CLI-004, RF-CLI-005, RF-CLI-006 | Dado um cliente autenticado, quando edita nome, e-mail ou telefone com valores válidos, então os dados são atualizados | Apenas os próprios dados são alterados |
| CA-CLI-005 | RF-CLI-005 | Dado um cliente, quando tenta editar os dados de outro cliente, então o acesso é negado | Controle de acesso |
| CA-CLI-006 | RF-CLI-005 | Dado um agendamento histórico, quando o cliente altera seus dados, então o agendamento permanece inalterado | Histórico preservado |
| CA-CLI-007 | RF-CLI-001, RF-CLI-002 | Dado um proprietário, quando tenta cadastrar, editar ou excluir um cliente, então a operação não é disponibilizada | Cadastro e edição restritos ao cliente |

## 8.8. Profissionais (`PROF`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-PROF-001 | RF-PROF-001, RF-PROF-002, RF-PROF-003 | Dado um proprietário, quando acessa a lista de profissionais, então visualiza nome, telefone e situação | Lista completa dos vinculados |
| CA-PROF-002 | RF-PROF-004, RF-PROF-005 | Dado um profissional vinculado, quando o proprietário consulta ou edita seus dados, então as informações são exibidas ou atualizadas | Dados profissionais mantidos |
| CA-PROF-003 | RF-PROF-006 | Dado um profissional ativo, quando o proprietário o desativa, então ele deixa de receber novos agendamentos | Conta e histórico preservados |
| CA-PROF-004 | RF-PROF-007 | Dado um profissional vinculado, quando o proprietário remove o vínculo, então a conta e o histórico permanecem | Vínculo encerrado |
| CA-PROF-005 | RF-PROF-008 | Dado um proprietário, quando acessa o código de convite, então pode consultá-lo e compartilhá-lo | Código disponível somente ao proprietário |
| CA-PROF-006 | RF-PROF-006, RF-PROF-007 | Dado um profissional, quando tenta gerenciar outro profissional ou o próprio vínculo, então a operação é negada | Somente o proprietário gerencia vínculos |

## 8.9. Dashboard (`DASH`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-DASH-001 | RF-DASH-001 | Dado um proprietário que concluiu a configuração inicial, quando acessa o sistema, então o dashboard é a primeira tela apresentada | Dashboard disponível |
| CA-DASH-002 | RF-DASH-002 | Dado um mês com agendamentos e pagamentos, quando o dashboard é exibido, então os indicadores refletem os períodos correspondentes | Hoje, semana, cancelamentos e recebimentos corretos |
| CA-DASH-003 | RF-DASH-003, RF-DASH-004 | Dado um dashboard, quando há agendamentos futuros, então a lista apresenta horário, cliente, profissional e status | Próximos agendamentos completos |
| CA-DASH-004 | RF-DASH-005 | Dado um dashboard sem movimentações, quando exibido, então os indicadores apresentam zero | Ausência de dados tratada corretamente |
| CA-DASH-005 | RF-DASH-002 | Dado um agendamento cancelado, quando o dashboard calcula os recebimentos, então ele não é somado como pagamento | Apenas pagamentos registrados são contabilizados |
| CA-DASH-006 | RF-DASH-002 | Dado mais de um proprietário autenticado, quando cada um acessa o dashboard, então visualiza apenas dados da própria barbearia | Isolamento entre barbearias |

## 8.10. Agendamento Público (`PUB`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-PUB-001 | RF-PUB-001, RF-PUB-002 | Dado um visitante, quando acessa a página pública de uma barbearia, então visualiza seus dados e serviços ativos | Página específica da barbearia |
| CA-PUB-002 | RF-PUB-003, RF-PUB-004 | Dado um cliente autenticado, quando inicia um agendamento, então percorre serviço, profissional, data e horário, observação e confirmação | Etapas disponíveis na ordem prevista |
| CA-PUB-003 | RF-PUB-005 | Dado um cliente autenticado com dados válidos, quando confirma, então o agendamento é criado automaticamente | Agendamento confirmado sem aprovação manual |
| CA-PUB-004 | RF-PUB-006 | Dado um agendamento público confirmado, quando a agenda da barbearia e do profissional é consultada, então o compromisso aparece | Agendamento visível nas duas agendas |
| CA-PUB-005 | RF-PUB-003 | Dado um visitante não autenticado, quando tenta concluir o agendamento, então o sistema exige autenticação | Agendamento não é criado |
| CA-PUB-006 | RF-PUB-004 | Dado um profissional com disponibilidade e bloqueios, quando o cliente consulta horários, então apenas horários válidos são exibidos | Funcionamento, disponibilidade, bloqueios e conflitos respeitados |
| CA-PUB-007 | RF-PUB-005 | Dado o mesmo horário sendo confirmado por dois clientes simultaneamente, quando ambas as confirmações chegam, então apenas uma é aceita | Sem reserva conflitante |
| CA-PUB-008 | RF-PUB-003 | Dado um serviço inativo ou profissional inativo, quando o cliente tenta agendar, então o sistema impede a confirmação | Apenas serviços e profissionais ativos |

## 8.11. Configurações (`CONF`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-CONF-001 | RF-CONF-001, RF-CONF-002, RF-CONF-003 | Dado um proprietário, quando acessa as configurações e define horários por dia, então a configuração é salva | Horários gerais atualizados |
| CA-CONF-002 | RF-CONF-004 | Dado um dia definido como fechado, quando a agenda é consultada, então não há horários disponíveis nesse dia | Fechamento respeitado |
| CA-CONF-003 | RF-CONF-005, RF-CONF-006 | Dado um proprietário, quando consulta usuários vinculados e o código de convite, então visualiza as informações da barbearia | Consulta disponível |
| CA-CONF-004 | RF-CONF-007 | Dado um barbeiro, quando acessa as configurações, então edita apenas a própria disponibilidade semanal | Disponibilidade individual atualizada |
| CA-CONF-005 | RF-CONF-008 | Dado um barbeiro, quando administra bloqueios, então apenas os próprios bloqueios são afetados | Bloqueios individuais |
| CA-CONF-006 | RF-CONF-002 | Dado um barbeiro, quando tenta alterar os horários gerais, então o acesso é negado | Somente o proprietário configura a barbearia |
| CA-CONF-007 | RF-CONF-006, RF-CONF-007 | Dado um código de convite, quando novas barbearias são criadas, então cada código é único e permanece o mesmo | Código único e permanente |
| CA-CONF-008 | RF-CONF-001, RF-CONF-007 | Dado um proprietário, quando tenta criar diretamente uma conta de barbeiro, então a operação não é disponibilizada | Entrada apenas por código de convite |

## 8.12. Notificações (`NOTIF`)

| ID | Requisitos | Cenário | Resultado esperado |
| --- | --- | --- | --- |
| CA-NOTIF-001 | RF-NOTIF-001 | Dado um evento relacionado a agendamento, quando ocorre, então os usuários envolvidos recebem notificação interna | Notificação disponível no sistema |
| CA-NOTIF-002 | RF-NOTIF-001 | Dado o envio de mensagens, quando a V1.0 está em operação, então nenhum WhatsApp é disparado automaticamente | Apenas notificações internas |

---

# 9. Dados e informações do domínio

## 9.1. Entidades

| Entidade | Descrição | Campos principais | Relacionamentos |
| --- | --- | --- | --- |
| Usuário | Conta de acesso ao sistema | Identificador, nome, e-mail, telefone, senha protegida, perfil | Cliente, barbeiro, notificações |
| Cliente | Usuário que realiza agendamentos | Nome, e-mail, telefone | Agendamentos, notificações |
| Barbeiro | Profissional que atende em uma ou mais barbearias | Nome, telefone, situação | Vínculos, disponibilidade, bloqueios, agendamentos, atendimentos |
| Barbearia | Estabelecimento gerenciado pelo proprietário | Nome, telefone comercial, e-mail comercial, cidade, endereço, Instagram, código de convite | Profissionais, serviços, horários, agendamentos, configurações |
| Vínculo Profissional | Relação entre barbeiro e barbearia | Situação do vínculo, papel do profissional | Barbeiro, barbearia |
| Serviço | Item do catálogo da barbearia | Nome, duração, preço, situação | Barbearia, agendamentos |
| Horário de Funcionamento | Funcionamento geral por dia da semana | Dia, abertura, fechamento, fechado | Barbearia |
| Disponibilidade Semanal | Jornada semanal do profissional | Dia, início, término | Barbeiro, barbearia |
| Bloqueio | Período pontual indisponível | Início, término, motivo opcional | Barbeiro |
| Agendamento | Compromisso entre cliente, profissional e serviço | Data, início, término, valor, observação, origem, status | Barbearia, cliente, barbeiro, serviço |
| Snapshot do Serviço | Dados do serviço preservados no agendamento | Nome, duração, preço no momento da contratação | Agendamento |
| Atendimento | Execução real do agendamento | Início real, término real, status | Agendamento, pagamento |
| Pagamento | Registro financeiro do atendimento | Valor recebido, forma de pagamento, data do registro | Atendimento |
| Sugestão de Horário | Novo horário proposto no cancelamento | Novo início, motivo, expiração, situação | Agendamento |
| Notificação | Aviso interno ao usuário | Tipo, conteúdo, data, situação de leitura | Usuário, agendamento |
| Histórico do Agendamento | Registro de alterações e cancelamentos | Evento, autor, data/hora, dados anteriores | Agendamento |

## 9.2. Ciclo de vida e retenção

- O histórico de agendamentos, atendimentos e pagamentos deve ser preservado mesmo após alterações cadastrais, remoção de serviços ou remoção de vínculos profissionais.
- Serviços removidos tornam-se inativos e permanecem referenciados no histórico.
- Contas de clientes e barbeiros não são excluídas pela remoção de vínculo com a barbearia.
- Agendamentos cancelados permanecem registrados com autor e data do cancelamento.
- Pagamentos registrados permanecem vinculados ao atendimento e ao dashboard do período correspondente.
- A política de retenção, anonimização e exclusão de dados pessoais deve ser definida conforme a LGPD.

---

# 10. Integrações e notificações

## 10.1. Integrações

| Integração | Objetivo | Direção | Criticidade | Observações |
| --- | --- | --- | --- | --- |
| Verificação do telefone comercial | Validar o telefone da barbearia | A definir | Média | Momento e procedimento ainda não definidos na SPEC |
| Notificações internas | Comunicar eventos de agendamento aos usuários | Interna | Média | Única forma de notificação na V1.0 |
| WhatsApp | Envio automático de mensagens | Saída | Baixa na V1.0 | Fora do escopo da primeira versão |
| Gateway de pagamento | Cobrança online | A definir | Baixa na V1.0 | Não previsto na primeira versão; pagamento é presencial |

## 10.2. Notificações

| Evento | Canal | Destinatário | Conteúdo mínimo | Gatilho |
| --- | --- | --- | --- | --- |
| Agendamento confirmado | Interno | Cliente e profissional | Serviço, data, horário, profissional, barbearia | Criação do agendamento |
| Agendamento cancelado | Interno | Cliente e profissional envolvido | Data, horário, autor do cancelamento | Cancelamento |
| Novo horário sugerido | Interno | Cliente | Data, horário sugerido, motivo, prazo de 24 horas | Sugestão feita pelo barbeiro |
| Sugestão aceita | Interno | Profissional e proprietário | Novo horário confirmado | Aceite do cliente |
| Sugestão rejeitada ou expirada | Interno | Profissional e proprietário | Horário liberado | Rejeição ou expiração |
| Atendimento iniciado | Interno | Cliente | Início do atendimento | Início registrado pelo barbeiro |
| Atendimento finalizado | Interno | Cliente | Conclusão do atendimento | Finalização registrada |
| Pagamento registrado | Interno | Cliente | Valor e forma de pagamento | Registro do pagamento |

---

# 11. Riscos e pontos de atenção

| Risco | Impacto | Probabilidade | Mitigação |
| --- | --- | --- | --- |
| Conflito de agendamentos simultâneos | Alto | Média | Revalidar a disponibilidade e reservar o horário de forma atômica na confirmação |
| Verificação do telefone comercial sem procedimento definido | Médio | Alta | Definir momento, canal e regra de verificação antes da implementação |
| Expiração da sugestão de horário não processada | Médio | Média | Rotina de expiração após 24 horas e liberação automática do horário |
| Alteração de disponibilidade afetar agendamentos existentes | Alto | Baixa | Não recalcular nem modificar agendamentos já criados |
| Remoção de serviço ou vínculo apagar histórico | Alto | Baixa | Desativação lógica e preservação de snapshots e registros históricos |
| Pagamento registrado incorretamente impactar o dashboard | Médio | Média | Validar valor e forma; permitir conferência por atendimento |
| Não comparecimento confundido com cancelamento | Médio | Média | Status distinto e bloqueio de atendimento para não comparecentes |
| Acesso indevido a dados de outra barbearia | Alto | Baixa | Isolar consultas por barbearia e validar permissões no servidor |
| Dados pessoais sem política de retenção definida | Alto | Média | Definir política de privacidade e retenção conforme a LGPD |
| Ausência de metas para os indicadores | Baixo | Alta | Validar metas com o negócio antes da operação |
| Dependência de fuso horário único por barbearia | Médio | Baixa | Definir e documentar o fuso de cada estabelecimento |
| Notificações internas sem conteúdo definido | Baixo | Média | Validar os eventos e conteúdos mínimos com os usuários |

---

# 12. Decisões consolidadas e evolução do produto

## 12.1. Decisões fechadas

| ID | Decisão | Justificativa | Data |
| --- | --- | --- | --- |
| DEC-001 | O sistema possui os perfis Proprietário, Barbeiro e Cliente | Separação de responsabilidades | 16/11/2026 |
| DEC-002 | Cadastros de cliente e barbeiro são separados; o login é compartilhado | Simplicidade e clareza de fluxo | 16/11/2026 |
| DEC-003 | O barbeiro que cria a barbearia torna-se proprietário e pode atuar como barbeiro | Definição de responsabilidade e operação | 16/11/2026 |
| DEC-004 | O código de convite é único e permanente por barbearia | Estabilidade do convite | 16/11/2026 |
| DEC-005 | O cadastro da barbearia não exige CNPJ nem documentos pessoais | Redução de barreiras de entrada | 16/11/2026 |
| DEC-006 | Cada agendamento possui apenas um serviço | Simplicidade do fluxo e cálculo | 16/11/2026 |
| DEC-007 | Agendamentos públicos e manuais são confirmados automaticamente | Ausência de aprovação manual | 16/11/2026 |
| DEC-008 | O valor e a duração do serviço são preservados no agendamento | Integridade histórica e comercial | 16/11/2026 |
| DEC-009 | O cliente pode cancelar ou remarcar sem antecedência mínima antes do início | Flexibilidade ao cliente | 16/11/2026 |
| DEC-010 | Cancelamentos registram autor e data, sem exigir justificativa | Auditoria e simplicidade | 16/11/2026 |
| DEC-011 | A sugestão de novo horário reserva o horário por 24 horas | Janela de decisão do cliente | 16/11/2026 |
| DEC-012 | Atendimento e pagamento possuem estados independentes | Controle operacional e financeiro distintos | 16/11/2026 |
| DEC-013 | O pagamento é presencial, sem gateway na V1.0 | Escopo da primeira versão | 16/11/2026 |
| DEC-014 | Não comparecimento não é cancelamento | Distinção operacional | 16/11/2026 |
| DEC-015 | A agenda usa verde para confirmados, vermelho para cancelados e cinza para bloqueios, sem amarelo | Padrão visual definido | 16/11/2026 |
| DEC-016 | A V1.0 terá apenas notificações internas; WhatsApp fica para versão futura | Escopo da primeira versão | 16/11/2026 |

## 12.2. Pontos a especificar no detalhamento

- Procedimento e momento da verificação do telefone comercial da barbearia.
- Política de senha, recuperação de acesso e duração de sessão.
- Definição do fuso horário padrão por barbearia.
- Regras para pagamentos parciais, múltiplas formas de pagamento e troco.
- Critérios para permitir ou impedir a seleção de clientes em agendamentos manuais.
- Conteúdo, agrupamento e marcação de leitura das notificações internas.
- Política de retenção, anonimização e exclusão de dados pessoais.
- Metas dos indicadores de sucesso e dos requisitos não funcionais.
- Comportamento da agenda em viradas de dia, semana e mês.
- Tratamento de agendamentos com cliente ou profissional removido do vínculo.

## 12.3. Evolução futura

| Item | Descrição | Versão prevista |
| --- | --- | --- |
| Envio automático por WhatsApp | Notificar clientes e profissionais automaticamente | Versão futura |
| Pagamento online | Integração com gateway e cobrança antecipada | Versão futura |
| Recursos avançados de configuração | Novas opções de personalização da barbearia | Versão futura |
| Relatórios gerenciais | Análises ampliadas de operação e faturamento | Versão futura |
| Aplicativo móvel nativo | Acesso por aplicativo dedicado | Não definida |

---

# 13. Premissas e questões em aberto

| ID | Tipo | Descrição | Impacto | Responsável por validar |
| --- | --- | --- | --- | --- |
| ABT-001 | Premissa | O produto é uma aplicação web responsiva, sem aplicativo nativo na V1.0 | Requisitos de plataforma e compatibilidade | Produto |
| ABT-002 | Premissa | Cada barbearia opera em um único fuso horário | Cálculo de horários e disponibilidade | Produto |
| ABT-003 | Premissa | As metas numéricas dos requisitos não funcionais são alvos propostos para a V1.0 | Critérios de desempenho e disponibilidade | Produto e Tecnologia |
| ABT-004 | Premissa | As metas dos indicadores de sucesso serão definidas com o negócio | Avaliação de resultados | Produto |
| ABT-005 | Questão | Como e quando o telefone comercial da barbearia será verificado? | Cadastro e confiança dos dados | Produto |
| ABT-006 | Questão | Qual política de senha e recuperação de acesso será adotada? | Segurança e experiência de acesso | Produto e Tecnologia |
| ABT-007 | Questão | Como o sistema deve tratar pagamentos parciais ou com múltiplas formas? | Registro financeiro e dashboard | Produto |
| ABT-008 | Questão | Quais eventos e conteúdos mínimos as notificações internas devem ter? | Comunicação com os usuários | Produto |
| ABT-009 | Questão | Qual é a política de retenção e exclusão de dados pessoais? | Conformidade com a LGPD | Produto e Jurídico |
| ABT-010 | Questão | Como o proprietário seleciona um cliente em um agendamento manual? | Usabilidade da agenda | Produto |
| ABT-011 | Questão | O proprietário deve visualizar a agenda de todos os profissionais ou apenas a própria? | Permissões da agenda | Produto |
| ABT-012 | Questão | O agendamento com sugestão pendente deve ser exibido de qual forma na agenda? | Clareza visual e operação | Produto e Design |

---

# Apêndice A — Glossário

| Termo | Definição |
| --- | --- |
| Agendamento | Compromisso que associa cliente, profissional, serviço, data e horário |
| Atendimento | Execução real do agendamento, com horários reais de início e término |
| Bloqueio | Período pontual em que o profissional não recebe agendamentos |
| Código de convite | Código único e permanente usado pelo barbeiro para entrar em uma barbearia |
| Disponibilidade semanal | Jornada recorrente configurada pelo profissional para cada dia da semana |
| Não comparecimento | Situação em que o cliente não comparece ao agendamento, distinta de cancelamento |
| Proprietário | Barbeiro que criou ou administra a barbearia |
| Snapshot do serviço | Cópia de nome, duração e preço do serviço no momento da contratação |
| Sugestão de horário | Novo horário proposto pelo barbeiro ao cancelar um agendamento, reservado por até 24 horas |
| Vínculo profissional | Relação entre barbeiro e barbearia, com situação ativa ou inativa |

# Apêndice B — Matriz de rastreabilidade

| Objetivo | Módulo | Requisitos | Critérios de aceite |
| --- | --- | --- | --- |
| Centralizar a gestão da barbearia | Empresa, Serviços, Horários, Profissionais, Configurações, Dashboard | RF-EMP-001 a RF-EMP-003; RF-SERV-001 a RF-SERV-008; RF-HOR-001 a RF-HOR-007; RF-PROF-001 a RF-PROF-008; RF-CONF-001 a RF-CONF-008; RF-DASH-001 a RF-DASH-005 | CA-EMP-001 a CA-EMP-005; CA-SERV-001 a CA-SERV-007; CA-HOR-001 a CA-HOR-008; CA-PROF-001 a CA-PROF-006; CA-CONF-001 a CA-CONF-008; CA-DASH-001 a CA-DASH-006 |
| Simplificar o agendamento | Autenticação, Agendamento Público, Agenda, Clientes | RF-AUTH-001 a RF-AUTH-010; RF-PUB-001 a RF-PUB-006; RF-AGD-001 a RF-AGD-018; RF-CLI-001 a RF-CLI-006 | CA-AUTH-001 a CA-AUTH-009; CA-PUB-001 a CA-PUB-008; CA-AGD-001 a CA-AGD-016; CA-CLI-001 a CA-CLI-007 |
| Organizar o relacionamento entre perfis | Autenticação, Clientes, Profissionais, Notificações | RF-AUTH-001 a RF-AUTH-010; RF-CLI-001 a RF-CLI-006; RF-PROF-001 a RF-PROF-008; RF-NOTIF-001 | CA-AUTH-001 a CA-AUTH-009; CA-CLI-001 a CA-CLI-007; CA-PROF-001 a CA-PROF-006; CA-NOTIF-001 a CA-NOTIF-002 |
| Controlar atendimentos e pagamentos | Atendimento e Pagamentos, Agenda, Dashboard | RF-ATD-001 a RF-ATD-009; RF-AGD-009 a RF-AGD-018; RF-DASH-001 a RF-DASH-005 | CA-ATD-001 a CA-ATD-010; CA-AGD-006 a CA-AGD-016; CA-DASH-001 a CA-DASH-006 |
| Preservar histórico e permitir auditoria | Serviços, Agenda, Atendimento, Clientes, Profissionais | RF-SERV-004; RF-SERV-008; RF-AGD-012; RF-AGD-017; RF-ATD-006; RF-CLI-005; RF-PROF-007 | CA-SERV-004; CA-SERV-007; CA-AGD-008; CA-AGD-010; CA-ATD-010; CA-CLI-006; CA-PROF-004 |





