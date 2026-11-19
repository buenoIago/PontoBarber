# Spec Delta

## Purpose

Define como pessoas criam conta, entram no sistema e recebem autorização conforme o perfil. Cliente e barbeiro/proprietário têm cadastros e autenticação distintos: OTP por e-mail para clientes e e-mail/senha para profissionais.

## ADDED Requirements

### Requirement: Escolha de perfil no acesso
A tela inicial SHALL oferecer as opções "Sou cliente" e "Sou barbeiro", conduzindo cada uma ao cadastro correspondente. Cliente e profissional MUST ter fluxos de autenticação separados.

#### Scenario: Visitante escolhe o perfil
- **WHEN** um visitante acessa o sistema
- **THEN** visualiza as duas opções de perfil e o acesso ao fluxo de autenticação correspondente

### Requirement: Cadastro separado de cliente e barbeiro
O sistema SHALL permitir o cadastro de cliente e de barbeiro em fluxos distintos. O cadastro de cliente SHALL exigir nome, e-mail único e telefone obrigatório, sem senha. O cadastro de barbeiro SHALL exigir nome, e-mail, telefone, senha e confirmação de senha. O telefone do cliente MUST ser usado somente como contato para o barbeiro e MUST NOT ser usado para login, autenticação, recuperação de conta ou envio de OTP.

#### Scenario: Cadastro válido de cliente
- **WHEN** um visitante informa nome, e-mail válido e ainda não utilizado e telefone válido
- **THEN** a conta de cliente é criada, com identidade global na plataforma e sem vínculo obrigatório com barbearia

#### Scenario: Cadastro válido de barbeiro
- **WHEN** um visitante informa todos os campos obrigatórios válidos no cadastro de barbeiro
- **THEN** a conta de barbeiro é criada e pode ser usada para autenticação

### Requirement: Validação de cadastro
O sistema MUST recusar cadastro com e-mail já utilizado, e-mail com formato inválido ou campo obrigatório ausente. A confirmação de senha divergente MUST ser recusada no cadastro de barbeiro. Nenhuma conta MUST ser criada nesses casos.

#### Scenario: E-mail já cadastrado
- **WHEN** um visitante tenta concluir um cadastro com e-mail já existente
- **THEN** a operação é recusada e nenhuma conta é criada

#### Scenario: Confirmação de senha divergente
- **WHEN** a confirmação de senha não corresponde à senha informada
- **THEN** a operação é recusada e nenhuma conta é criada

### Requirement: Autenticação por perfil
Barbeiros e proprietários SHALL autenticar com e-mail e senha. Clientes SHALL autenticar exclusivamente com código OTP de 6 dígitos enviado por e-mail, sem senha e sem uso do telefone. Falhas de autenticação MUST receber resposta genérica, sem revelar se o e-mail existe.

#### Scenario: Login válido de profissional
- **WHEN** um barbeiro ou proprietário com conta ativa informa e-mail e senha corretos
- **THEN** a sessão é iniciada e o perfil e os vínculos são retornados

#### Scenario: Autenticação válida de cliente
- **WHEN** um cliente informa e-mail e código OTP válido, não expirado e ainda não utilizado
- **THEN** a conta é criada ou recuperada e a sessão é iniciada por cookie seguro

#### Scenario: Credenciais inválidas
- **WHEN** a senha informada está incorreta ou o código OTP é inválido
- **THEN** a autenticação é recusada com mensagem genérica

### Requirement: Código OTP do cliente
O sistema SHALL gerar código de 6 dígitos com validade de 5 minutos e uso único, armazenado de forma segura e invalidado após a validação. O sistema MUST limitar tentativas de validação e reenvio e MUST NOT revelar se o e-mail informado possui conta.

#### Scenario: Código expirado ou já utilizado
- **WHEN** o código OTP informado expirou ou já foi utilizado
- **THEN** a validação é recusada com mensagem genérica

#### Scenario: Abuso de solicitação ou validação
- **WHEN** os limites de reenvio ou de tentativas de validação são excedidos
- **THEN** novas solicitações ou tentativas são temporariamente recusadas

### Requirement: Bloqueio por tentativas de login
O sistema SHALL bloquear temporariamente a conta após tentativas inválidas repetidas e MUST informar o bloqueio sem revelar dados sensíveis.

#### Scenario: Tentativas inválidas repetidas
- **WHEN** uma conta acumula tentativas inválidas dentro da janela definida
- **THEN** novas tentativas são recusadas com indicação de conta temporariamente bloqueada

### Requirement: Sessão segura
A sessão SHALL usar cookie persistente inacessível a JavaScript, com atributos de segurança e expiração deslizante por perfil: 60 dias de inatividade para clientes e 30 dias para barbeiros e proprietários. MUST NOT haver limite absoluto de duração; cada acesso válido SHALL renovar o prazo do perfil e, após a inatividade, o usuário MUST autenticar novamente. A política de 60 dias MUST ser exclusiva dos clientes. O logout MUST encerrar a sessão imediatamente no servidor.

#### Scenario: Renovação por acesso válido
- **WHEN** um usuário autenticado realiza um acesso válido antes de expirar o prazo de inatividade
- **THEN** o prazo do seu perfil é renovado — 60 dias para cliente, 30 dias para barbeiro ou proprietário

#### Scenario: Inatividade além do prazo do perfil
- **WHEN** um usuário permanece inativo pelo prazo definido para seu perfil
- **THEN** a sessão deixa de ser aceita e o usuário deve autenticar novamente

#### Scenario: Logout
- **WHEN** um usuário autenticado encerra a sessão
- **THEN** o cookie é removido e a sessão deixa de ser aceita pelo servidor imediatamente

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
O barbeiro ou proprietário autenticado SHALL poder alterar a própria senha mediante senha atual válida e confirmação da nova senha. Clientes SHALL NOT possuir senha e não utilizam esse fluxo.

#### Scenario: Alteração válida de senha
- **WHEN** um profissional informa a senha atual correta e a nova senha com confirmação correspondente
- **THEN** a senha é alterada e a sessão permanece válida conforme a política de segurança
