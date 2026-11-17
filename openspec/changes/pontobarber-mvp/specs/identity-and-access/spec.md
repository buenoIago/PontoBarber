# Spec Delta

## Purpose

Define como pessoas criam conta, entram no sistema e recebem autorização conforme o perfil, com cadastros separados para cliente e barbeiro e login compartilhado.

## ADDED Requirements

### Requirement: Escolha de perfil no acesso
A tela inicial SHALL oferecer as opções "Sou cliente" e "Sou barbeiro", conduzindo cada uma ao cadastro correspondente, e MUST oferecer uma tela de login compartilhada.

#### Scenario: Visitante escolhe o perfil
- **WHEN** um visitante acessa o sistema
- **THEN** visualiza as duas opções de perfil e o acesso ao login compartilhado

### Requirement: Cadastro separado de cliente e barbeiro
O sistema SHALL permitir o cadastro de cliente e de barbeiro em fluxos distintos, exigindo nome, e-mail, telefone, senha e confirmação de senha em ambos.

#### Scenario: Cadastro válido de cliente
- **WHEN** um visitante informa todos os campos obrigatórios válidos no cadastro de cliente
- **THEN** a conta de cliente é criada e pode ser usada para autenticação

#### Scenario: Cadastro válido de barbeiro
- **WHEN** um visitante informa todos os campos obrigatórios válidos no cadastro de barbeiro
- **THEN** a conta de barbeiro é criada e pode ser usada para autenticação

### Requirement: Validação de cadastro
O sistema MUST recusar cadastro com e-mail já utilizado, e-mail com formato inválido, campo obrigatório ausente ou confirmação de senha divergente. Nenhuma conta MUST ser criada nesses casos.

#### Scenario: E-mail já cadastrado
- **WHEN** um visitante tenta concluir um cadastro com e-mail já existente
- **THEN** a operação é recusada e nenhuma conta é criada

#### Scenario: Confirmação de senha divergente
- **WHEN** a confirmação de senha não corresponde à senha informada
- **THEN** a operação é recusada e nenhuma conta é criada

### Requirement: Login compartilhado
Usuários de qualquer perfil SHALL autenticar com e-mail e senha na mesma tela. Credenciais inválidas MUST receber resposta genérica, sem revelar se o e-mail existe.

#### Scenario: Login válido
- **WHEN** um usuário com conta ativa informa e-mail e senha corretos
- **THEN** a sessão é iniciada e o perfil e os vínculos são retornados

#### Scenario: Credenciais inválidas
- **WHEN** a senha informada está incorreta
- **THEN** a autenticação é recusada com mensagem genérica

### Requirement: Bloqueio por tentativas de login
O sistema SHALL bloquear temporariamente a conta após tentativas inválidas repetidas e MUST informar o bloqueio sem revelar dados sensíveis.

#### Scenario: Tentativas inválidas repetidas
- **WHEN** uma conta acumula tentativas inválidas dentro da janela definida
- **THEN** novas tentativas são recusadas com indicação de conta temporariamente bloqueada

### Requirement: Sessão segura
A sessão SHALL usar cookie inacessível a JavaScript, com atributos de segurança, expiração deslizante e absoluta. O logout MUST encerrar a sessão no servidor.

#### Scenario: Logout
- **WHEN** um usuário autenticado encerra a sessão
- **THEN** o cookie é removido e a sessão deixa de ser aceita pelo servidor

#### Scenario: Consulta do próprio perfil
- **WHEN** um usuário autenticado consulta seus dados de sessão
- **THEN** recebe perfil, vínculos ativos e permissões correspondentes

### Requirement: Proteção de requisições autenticadas
Requisições autenticadas que alteram estado SHALL exigir token antiforgery válido. Requisições sem token ou com token inválido MUST ser recusadas.

#### Scenario: Requisição sem token antiforgery
- **WHEN** uma requisição autenticada de criação, alteração ou remoção chega sem token antiforgery válido
- **THEN** a operação é recusada sem alterar dados

### Requirement: Autorização por perfil e por recurso
O sistema SHALL autorizar cada operação pelo perfil e pelo recurso envolvido, impedindo que um usuário acesse ou altere recursos de outro.

#### Scenario: Barbeiro tenta alterar dados da empresa
- **WHEN** um barbeiro vinculado tenta editar os dados cadastrais da barbearia
- **THEN** a operação é recusada

#### Scenario: Cliente tenta acessar dados de outro cliente
- **WHEN** um cliente tenta consultar ou editar dados de outro cliente
- **THEN** o acesso é negado

### Requirement: Alteração de senha
O usuário autenticado SHALL poder alterar a própria senha mediante senha atual válida e confirmação da nova senha.

#### Scenario: Alteração válida de senha
- **WHEN** o usuário informa a senha atual correta e a nova senha com confirmação correspondente
- **THEN** a senha é alterada e a sessão permanece válida conforme a política de segurança
