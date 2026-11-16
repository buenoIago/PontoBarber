# 1. Arquitetura e estrutura da solução

## 1.1. Visão geral

O PontoBarber será desenvolvido como uma aplicação web composta por um backend em ASP.NET Core e um frontend em Angular.

O backend será responsável pelo processamento das requisições, aplicação das regras de negócio, autenticação, autorização, persistência e integração com serviços externos.

O frontend será responsável pela interface de usuário, navegação, apresentação dos dados e comunicação com a API.

A solução será mantida em um único repositório, contendo os projetos do backend e a aplicação frontend.

A arquitetura do backend será organizada em quatro camadas independentes:

- API;
- Aplicação;
- Domínio;
- Infraestrutura.

Cada camada será mantida em seu próprio projeto `.csproj`, reunido por uma solução no formato `.slnx`.

## 1.2. Tecnologias principais

| Componente | Tecnologia | Estado |
|---|---|---|
| Backend | C# / ASP.NET Core | Confirmado |
| Frontend | Angular / TypeScript | Confirmado |
| Banco de dados | Microsoft SQL Server | Confirmado |
| ORM | Entity Framework Core | Confirmado |
| Comunicação | API REST | Confirmado |
| Solução .NET | `.slnx` | Confirmado |
| Estilização | Bootstrap 5 / SCSS | Confirmado |
| Ícones | Bootstrap Icons | Confirmado |
| Containers | Docker | Confirmado |
| Orquestração local | Docker Compose | Confirmado |

As versões específicas das tecnologias deverão ser verificadas antes da criação dos projetos, considerando compatibilidade entre SDK, runtime, dependências e ferramentas.

## 1.3. Estrutura do repositório

A estrutura inicial será organizada da seguinte maneira:

```text
PontoBarber/
│
├── PontoBarber.slnx
│
├── src/
│   ├── Api/
│   │   └── Api.csproj
│   │
│   ├── Aplicacao/
│   │   └── Aplicacao.csproj
│   │
│   ├── Dominio/
│   │   └── Dominio.csproj
│   │
│   └── Infraestrutura/
│       └── Infraestrutura.csproj
│
├── frontend/
│   └── pontobarber-web/
│       ├── src/
│       ├── angular.json
│       ├── package.json
│       └── tsconfig.json
│
├── docker-compose.yml
├── .gitignore
└── README.md
```

A estrutura poderá receber novos arquivos e diretórios conforme as necessidades técnicas forem definidas, sem alterar desnecessariamente a separação principal dos projetos.

## 1.4. Camada API

**Projeto:** `Api`

**Responsabilidade:** disponibilizar os recursos do sistema por meio de endpoints HTTP.

A API será a porta de entrada das requisições provenientes do frontend e de eventuais integrações externas.

Suas responsabilidades incluem:

- Disponibilizar endpoints REST.
- Receber e interpretar requisições HTTP.
- Encaminhar operações para a camada de Aplicação.
- Retornar respostas HTTP apropriadas.
- Configurar autenticação e autorização.
- Configurar middlewares e tratamento global de exceções.
- Registrar dependências necessárias para inicialização da aplicação.
- Disponibilizar documentação da API por Swagger / OpenAPI.
- Configurar versionamento dos endpoints.

A camada API não deverá concentrar regras de negócio. Seu papel será intermediar a comunicação HTTP e a camada de Aplicação.

## 1.5. Camada Aplicação

**Projeto:** `Aplicacao`

**Responsabilidade:** coordenar os casos de uso e operações disponibilizados pelo sistema.

Essa camada deverá organizar a execução das funcionalidades, coordenando o domínio e as abstrações necessárias para realizar cada operação.

Suas responsabilidades incluem:

- Implementar os casos de uso da aplicação.
- Coordenar operações envolvendo diferentes entidades.
- Definir serviços de aplicação.
- Definir DTOs de entrada e saída.
- Organizar operações de cadastro, consulta, atualização e remoção.
- Coordenar o acesso aos repositórios por meio de contratos.
- Aplicar validações relacionadas aos casos de uso.
- Garantir que as operações respeitem as regras estabelecidas pelo domínio.

A camada de Aplicação não deverá depender diretamente de detalhes de infraestrutura, como implementações concretas do Entity Framework Core ou conexões com o SQL Server.

## 1.6. Camada Domínio

**Projeto:** `Dominio`

**Responsabilidade:** concentrar os conceitos e as regras fundamentais do negócio.

Essa camada representa o núcleo do PontoBarber e deverá permanecer independente de tecnologias externas.

Suas responsabilidades incluem:

- Definir entidades do domínio.
- Representar os conceitos centrais do negócio.
- Implementar regras e comportamentos das entidades.
- Definir contratos e abstrações necessários ao domínio.
- Garantir invariantes relacionadas às operações de negócio.

As regras que determinam o comportamento do sistema deverão permanecer protegidas contra alterações indevidas provenientes de detalhes de interface, banco de dados ou infraestrutura.

O domínio não deverá depender de Angular, ASP.NET Core, Entity Framework Core ou SQL Server.

## 1.7. Camada Infraestrutura

**Projeto:** `Infraestrutura`

**Responsabilidade:** implementar os mecanismos técnicos necessários para a execução da aplicação.

Suas responsabilidades incluem:

- Configurar e disponibilizar o Entity Framework Core.
- Implementar o acesso ao SQL Server.
- Implementar os repositórios.
- Configurar o `DbContext`.
- Configurar o mapeamento das entidades.
- Implementar integrações externas.
- Configurar mecanismos técnicos de persistência.
- Disponibilizar implementações concretas dos contratos definidos nas camadas apropriadas.

As configurações das entidades do Entity Framework Core serão organizadas em classes próprias utilizando `IEntityTypeConfiguration`.

A Infraestrutura deverá implementar os contratos definidos pelas camadas internas, evitando que detalhes de persistência sejam espalhados pelo restante da aplicação.

## 1.8. Dependências entre camadas

A organização deverá respeitar a separação de responsabilidades e evitar dependências circulares.

A direção proposta das dependências será:

```text
Api
 ├── Aplicacao
 └── Infraestrutura

Infraestrutura
 ├── Aplicacao
 └── Dominio

Aplicacao
 └── Dominio

Dominio
 └── Sem dependências dos demais projetos
```

A API será responsável por configurar a composição da aplicação, incluindo a injeção de dependência e o registro das implementações de infraestrutura.

O projeto de Domínio deverá permanecer independente das demais camadas.

A composição definitiva dos projetos e suas referências deverá ser validada durante a criação da solução.

## 1.9. Injeção de dependência

A aplicação utilizará o sistema nativo de injeção de dependência do ASP.NET Core.

Os serviços e repositórios deverão ser registrados por meio de extensões de configuração próprias, mantendo a inicialização organizada.

A API será responsável por compor as dependências necessárias para execução da aplicação.

A definição dos tempos de vida (`Singleton`, `Scoped` e `Transient`) deverá respeitar a responsabilidade e o ciclo de vida de cada serviço.

## 1.10. Frontend Angular

O frontend será desenvolvido em Angular com TypeScript, utilizando componentes standalone.

A organização interna seguirá o padrão de desenvolvimento já utilizado pelo responsável pelo projeto, preservando a separação dos arquivos de componentes e a organização das funcionalidades.

A aplicação deverá utilizar:

- Angular Router para navegação.
- `HttpClient` para comunicação com a API.
- Reactive Forms para formulários.
- SCSS para estilização.
- Bootstrap 5 para componentes e estrutura visual.
- Bootstrap Icons para ícones.

A aplicação deverá consumir exclusivamente os recursos disponibilizados pela API para operações que envolvam dados persistidos e regras de negócio.

A estratégia específica de autenticação no Angular, interceptors, guards e armazenamento dos tokens será detalhada na seção de autenticação.

## 1.11. Comunicação entre frontend e backend

A comunicação entre Angular e ASP.NET Core ocorrerá por meio de requisições HTTP utilizando uma API REST.

Os recursos serão organizados sob o prefixo `/api/v1/`.

Exemplos de rotas:

```text
/api/v1/auth
/api/v1/barbearias
/api/v1/agendamentos
/api/v1/servicos
```

As operações deverão utilizar métodos HTTP apropriados e contratos definidos para requisições e respostas.

O backend será responsável por validar as informações recebidas, executar as regras de negócio e retornar os resultados das operações.

## 1.12. Containers e ambiente de desenvolvimento

O projeto utilizará Docker e Docker Compose para padronizar a execução dos serviços necessários ao desenvolvimento.

A configuração inicial deverá contemplar o SQL Server e a API, conforme a estratégia de execução escolhida.

As configurações específicas de cada ambiente deverão ser mantidas separadas, evitando o versionamento de credenciais e segredos.

Os detalhes de construção das imagens, volumes, redes, persistência e inicialização dos serviços serão definidos na seção de infraestrutura.

## 1.13. Decisões ainda pendentes

Os seguintes detalhes não foram definidos nesta etapa:

| ID | Assunto | Situação |
|---|---|---|
| ARQ-01 | Versões exatas do .NET e Angular | Pendente |
| ARQ-02 | Nome definitivo dos namespaces | Pendente |
| ARQ-03 | Organização interna de cada projeto | Pendente |
| ARQ-04 | Definição dos contratos de repositório e serviços | Pendente |
| ARQ-05 | Estratégia de transações e unidade de trabalho | Pendente |
| ARQ-06 | Estratégia de composição e registro de dependências | Pendente |
| ARQ-07 | Organização detalhada das funcionalidades Angular | Pendente |
| ARQ-08 | Estratégia de execução local do frontend e backend com Docker | Pendente |

As pendências deverão ser discutidas antes de serem tratadas como decisões definitivas.

# 2. Arquitetura de autenticação e autorização

## 2.1. Visão geral

O PontoBarber utilizará ASP.NET Core Identity em conjunto com autenticação JWT (JSON Web Token) para gerenciar a identidade dos usuários e proteger os recursos disponibilizados pela API.

O Identity será responsável pelo gerenciamento das contas, credenciais e informações de identidade dos usuários.

A autenticação das requisições à API será realizada por meio de tokens JWT, permitindo que o frontend Angular acesse os recursos protegidos mediante apresentação de um token válido.

A autorização será baseada em políticas e permissões, respeitando os diferentes perfis de acesso e os vínculos dos usuários com as barbearias.

O sistema deverá contemplar três perfis principais:

- Proprietário;
- Barbeiro;
- Cliente.

A autenticação e a autorização deverão ser tratadas no backend, garantindo que as regras de acesso não dependam exclusivamente dos controles implementados no frontend.

## 2.2. Registro central de decisões

| ID | Tema | Decisão | Estado |
|---|---|---|---|
| AUTH-01 | Gerenciamento de identidade | ASP.NET Core Identity | Confirmada |
| AUTH-02 | Autenticação da API | JWT | Confirmada |
| AUTH-03 | Autorização | Policies e permissões | Confirmada |
| AUTH-04 | Perfis | Proprietário, barbeiro e cliente | Confirmada |
| AUTH-05 | Segurança dos endpoints | Validação no backend | Confirmada |
| AUTH-06 | Vínculo com barbearia | Considerar o contexto da barbearia nas autorizações | Confirmada |
| AUTH-07 | Assinatura JWT | Algoritmo e configuração | Pendente |
| AUTH-08 | Expiração dos tokens | Tempo de validade | Pendente |
| AUTH-09 | Renovação de sessão | Estratégia de refresh token | Pendente |
| AUTH-10 | Armazenamento no frontend | Local de armazenamento dos tokens | Pendente |
| AUTH-11 | Recuperação de senha | Fluxo e mecanismo de recuperação | Pendente |
| AUTH-12 | Verificação de e-mail | Estratégia de confirmação de endereço | Pendente |
| AUTH-13 | Bloqueio de contas | Política para tentativas de autenticação inválidas | Pendente |
| AUTH-14 | Estrutura das permissões | Organização definitiva das policies | Pendente |

## 2.3. ASP.NET Core Identity

O ASP.NET Core Identity será utilizado para gerenciar as contas dos usuários.

A implementação ficará integrada à camada de Infraestrutura, mantendo os detalhes de persistência e gerenciamento de identidade fora do domínio.

As responsabilidades do Identity incluem:

- Cadastro de usuários.
- Gerenciamento de credenciais.
- Hash e verificação de senhas.
- Consulta de usuários.
- Atualização de informações de identidade.
- Gerenciamento de papéis.
- Controle de bloqueio de contas, conforme configuração.
- Suporte aos mecanismos de recuperação de credenciais.

As informações de identidade deverão ser persistidas no SQL Server por meio do Entity Framework Core.

### 2.3.1. Identificador dos usuários

O tipo de identificador utilizado pelo usuário do Identity ainda deverá ser definido.

A escolha deverá considerar a compatibilidade com as entidades do domínio, relacionamentos, tabelas de identidade e referências entre usuários e barbearias.

**Estado:** pendente de definição.

### 2.3.2. Separação entre identidade e domínio

A identidade técnica do usuário não deverá concentrar todas as informações e regras de negócio do PontoBarber.

As informações específicas do domínio deverão permanecer organizadas em suas respectivas entidades.

Essa separação deverá permitir que o gerenciamento de credenciais seja realizado pelo Identity sem transformar suas classes internas no centro do modelo de negócio.

## 2.4. Autenticação JWT

A API utilizará tokens JWT para autenticar as requisições realizadas pelo frontend.

Após uma autenticação bem-sucedida, o backend deverá emitir um token contendo as informações necessárias para identificar o usuário e validar sua sessão.

Nas requisições protegidas, o frontend deverá enviar o token no cabeçalho HTTP:

```http
Authorization: Bearer {token}
```

O middleware de autenticação do ASP.NET Core será responsável por validar o token recebido antes de permitir o acesso aos endpoints protegidos.

### 2.4.1. Responsabilidades do mecanismo JWT

- Emitir tokens após autenticação válida.
- Identificar o usuário autenticado.
- Validar a assinatura do token.
- Verificar sua validade temporal.
- Rejeitar tokens inválidos ou expirados.
- Disponibilizar as claims necessárias ao processo de autorização.

### 2.4.2. Claims

O token poderá conter informações de identificação e autorização do usuário.

A definição definitiva das claims deverá considerar:

- Identificador do usuário.
- Identificação do perfil.
- Identificação da barbearia, quando aplicável.
- Informações necessárias para aplicação das políticas de autorização.

Não deverão ser incluídas informações pessoais ou dados de negócio desnecessários no token.

A inclusão de `BarbeariaId` deverá considerar os diferentes contextos de acesso do sistema, especialmente usuários que podem possuir vínculos com diferentes estabelecimentos ou atuar em contextos distintos.

**Estado:** estrutura definitiva das claims pendente.

### 2.4.3. Configuração de segurança

A implementação deverá manter as chaves e informações utilizadas para assinatura dos tokens fora do código-fonte.

As configurações deverão ser fornecidas por mecanismos seguros de configuração, respeitando a separação entre os ambientes.

O algoritmo de assinatura, a duração dos tokens e a estratégia de renovação serão definidos em decisões técnicas específicas.

## 2.5. Perfis de acesso

O PontoBarber possuirá três perfis principais de usuários.

### 2.5.1. Proprietário

O proprietário será responsável pela administração da barbearia.

Suas permissões deverão contemplar:

- Gerenciamento das informações da barbearia.
- Gerenciamento dos serviços.
- Gerenciamento dos profissionais.
- Configuração dos horários gerais.
- Consulta e administração da agenda.
- Consulta dos clientes relacionados à barbearia.
- Administração dos agendamentos.
- Consulta das informações financeiras disponibilizadas pelo sistema.

O proprietário também poderá atuar como barbeiro, possuindo sua própria disponibilidade e agenda de atendimentos.

Essa condição deverá ser considerada na modelagem de autorização, evitando que a função administrativa impeça o proprietário de utilizar as funcionalidades destinadas aos profissionais.

### 2.5.2. Barbeiro

O barbeiro será responsável pelas atividades relacionadas à sua atuação profissional.

Suas permissões deverão contemplar:

- Consulta da própria agenda.
- Gerenciamento da própria disponibilidade.
- Criação de bloqueios na própria agenda.
- Consulta dos agendamentos relacionados à sua atuação.
- Gerenciamento dos atendimentos permitidos.
- Registro das informações de pagamento conforme as permissões estabelecidas.
- Cancelamento dos agendamentos de sua responsabilidade.

O barbeiro não deverá possuir acesso automático às funcionalidades administrativas exclusivas do proprietário.

### 2.5.3. Cliente

O cliente utilizará o sistema para realizar e acompanhar seus agendamentos.

Suas permissões deverão contemplar:

- Cadastro e autenticação.
- Consulta dos serviços disponibilizados pelas barbearias.
- Consulta de profissionais e horários disponíveis.
- Criação de agendamentos.
- Consulta dos próprios agendamentos.
- Cancelamento dos próprios agendamentos, respeitando as regras do negócio.
- Remarcação dos próprios agendamentos, respeitando as regras do negócio.
- Aceitação ou rejeição de horários sugeridos pelo profissional.
- Atualização das próprias informações cadastrais.

O cliente não poderá acessar informações administrativas ou dados particulares de outros clientes.

## 2.6. Autorização baseada em políticas

A API utilizará o sistema de autorização do ASP.NET Core por meio de policies.

As políticas deverão representar as permissões necessárias para acessar determinadas funcionalidades e executar operações específicas.

A autorização deverá considerar mais do que a simples autenticação do usuário.

Uma requisição poderá ser realizada por um usuário autenticado e ainda assim ser negada quando ele não possuir a permissão necessária.

A implementação deverá considerar:

- Perfil do usuário.
- Permissão exigida pela operação.
- Vínculo com a barbearia.
- Propriedade do recurso solicitado.
- Situação da conta e do vínculo profissional.
- Regras específicas do domínio.

### 2.6.1. Separação entre perfil e permissão

Os perfis representam as funções principais dos usuários dentro do sistema.

As permissões representam as operações que cada usuário poderá executar.

Essa distinção permitirá que o sistema evolua sem depender exclusivamente de verificações simples de perfil.

Por exemplo, possuir o perfil de barbeiro não deverá permitir que um profissional altere os serviços cadastrados pela barbearia.

Da mesma forma, possuir o perfil de proprietário deverá permitir operações administrativas somente dentro do contexto da barbearia à qual ele está associado.

A estrutura definitiva de policies e permissões ainda deverá ser detalhada.

## 2.7. Autorização e isolamento entre barbearias

A autorização deverá trabalhar em conjunto com a estratégia de multi-tenancy definida para o projeto.

O acesso aos dados de uma barbearia deverá considerar o vínculo do usuário com aquele estabelecimento.

O backend deverá validar o contexto de acesso antes de executar operações que envolvam dados pertencentes a uma barbearia.

Não será suficiente verificar apenas se o usuário possui determinado perfil.

Exemplo:

Um proprietário autenticado não poderá consultar, alterar ou remover serviços pertencentes a outra barbearia apenas por possuir permissões administrativas.

Da mesma maneira, um barbeiro deverá acessar somente os recursos permitidos dentro do estabelecimento ao qual está vinculado.

A proteção deverá ser aplicada tanto nas operações de consulta quanto nas operações de criação, atualização e remoção.

A implementação detalhada do isolamento será aprofundada na seção específica de multi-tenancy.

## 2.8. Fluxo de autenticação

O fluxo geral de autenticação será:

1. O usuário informa suas credenciais no frontend Angular.
2. O frontend encaminha as informações para o endpoint de autenticação.
3. A API recebe a requisição.
4. O Identity verifica as credenciais informadas.
5. Caso a autenticação seja válida, a API gera um JWT.
6. O token é retornado ao frontend.
7. O frontend utiliza o token nas requisições protegidas.
8. A API valida o token recebido antes de processar a operação.

Caso as credenciais sejam inválidas, a API deverá retornar uma resposta apropriada, sem expor informações internas ou detalhes sensíveis da autenticação.

## 2.9. Proteção dos endpoints

Os endpoints deverão ser classificados de acordo com a necessidade de autenticação.

Existirão recursos públicos e recursos protegidos.

Os recursos públicos poderão ser acessados sem autenticação, conforme as regras definidas no PRD.

Os recursos protegidos deverão exigir um usuário autenticado e, quando necessário, autorização específica.

A implementação deverá utilizar os mecanismos nativos do ASP.NET Core para proteção dos endpoints.

A ausência de token válido deverá resultar em resposta HTTP `401 Unauthorized`.

A presença de um usuário autenticado sem permissão para determinada operação deverá resultar em resposta HTTP `403 Forbidden`.

## 2.10. Integração com Angular

O frontend Angular será responsável por apresentar os formulários de autenticação e encaminhar as credenciais à API.

A aplicação deverá organizar os mecanismos de autenticação e controle de navegação sem substituir as validações realizadas pelo backend.

A integração deverá contemplar:

- Serviço responsável pela comunicação com os endpoints de autenticação.
- Interceptor HTTP para inclusão do token nas requisições protegidas.
- Guards para controle de navegação.
- Tratamento de respostas relacionadas à autenticação.
- Encerramento da sessão quando necessário.

Os guards e controles visuais serão utilizados para melhorar a experiência de navegação, mas não serão considerados mecanismos de segurança suficientes.

A autorização efetiva permanecerá sob responsabilidade da API.

## 2.11. Tratamento de falhas

O mecanismo de autenticação deverá tratar adequadamente as seguintes situações:

| Situação | Comportamento esperado |
|---|---|
| Credenciais inválidas | Rejeitar autenticação |
| Token ausente em endpoint protegido | Retornar HTTP 401 |
| Token inválido | Retornar HTTP 401 |
| Token expirado | Retornar HTTP 401 |
| Usuário sem permissão | Retornar HTTP 403 |
| Acesso a recurso de outra barbearia | Negar operação |
| Conta bloqueada | Impedir autenticação |
| Vínculo profissional inativo | Impedir operações incompatíveis com o vínculo |

O tratamento deverá evitar a exposição de informações internas, credenciais, tokens ou detalhes de implementação.

## 2.12. Segurança e boas práticas

A implementação deverá observar os seguintes cuidados:

- Nunca armazenar senhas em texto puro.
- Utilizar os mecanismos de hash fornecidos pelo Identity.
- Não armazenar chaves de assinatura no código-fonte.
- Não registrar tokens ou credenciais nos logs.
- Validar a autenticação no backend.
- Validar permissões em operações protegidas.
- Impedir acesso indevido entre barbearias.
- Não confiar em informações de autorização enviadas pelo frontend.
- Evitar incluir informações sensíveis nas claims.
- Utilizar HTTPS nos ambientes de implantação.

A estratégia de armazenamento e renovação dos tokens deverá ser definida antes da implementação definitiva da sessão no frontend.

## 2.13. Testes e verificações

A implementação deverá possuir testes que verifiquem o comportamento dos mecanismos de autenticação e autorização.

As verificações mínimas deverão contemplar:

| Cenário | Resultado esperado |
|---|---|
| Login com credenciais válidas | Autenticação realizada |
| Login com credenciais inválidas | Autenticação rejeitada |
| Requisição sem token | Acesso negado em endpoint protegido |
| Requisição com token inválido | Acesso negado |
| Requisição com token expirado | Acesso negado |
| Usuário sem permissão | Operação rejeitada |
| Proprietário acessando recurso de outra barbearia | Acesso negado |
| Barbeiro tentando executar operação administrativa não permitida | Acesso negado |
| Cliente tentando acessar dados de outro cliente | Acesso negado |
| Barbeiro com vínculo inativo tentando realizar operação restrita | Operação rejeitada |

Os testes deverão verificar não apenas a existência de um token válido, mas também o comportamento das regras de autorização e isolamento.

A estratégia de implementação e organização desses testes será detalhada na seção de qualidade e verificação.

## 2.14. Decisões pendentes

Antes da implementação definitiva, deverão ser discutidos e registrados:

1. Tipo de identificador dos usuários.
2. Algoritmo e configuração de assinatura JWT.
3. Tempo de expiração do access token.
4. Utilização ou não de refresh tokens.
5. Estratégia de armazenamento dos tokens no Angular.
6. Estrutura definitiva das claims.
7. Organização das roles e policies.
8. Estratégia de recuperação de senha.
9. Verificação de e-mail.
10. Política de bloqueio por tentativas inválidas.
11. Tratamento de sessões e encerramento de autenticação.

Esses detalhes não deverão ser presumidos como decisões aprovadas apenas por constarem como possibilidades técnicas neste documento.

# 3. Arquitetura Multi-Tenant

## 3.1. Objetivo

O PontoBarber será desenvolvido como uma aplicação **multi-tenant**, permitindo que diversas barbearias utilizem a mesma plataforma, mantendo seus dados e operações isolados.

Cada barbearia representa um tenant independente. Embora compartilhem a mesma aplicação, infraestrutura e banco de dados, seus dados operacionais não poderão ser acessados ou modificados por usuários vinculados a outras barbearias.

A arquitetura deverá garantir que o isolamento seja aplicado em todas as operações relevantes, incluindo:

- Consultas e listagens.
- Visualização de registros individuais.
- Criação e alteração de registros.
- Exclusão e desativação de registros.
- Relacionamentos entre entidades.
- Agendamentos, serviços e profissionais.
- Consultas do dashboard e relatórios.
- Operações administrativas e processos executados em segundo plano.

O isolamento deverá ser garantido pelo backend e pela camada de persistência. O frontend não será considerado uma barreira de segurança.

## 3.2. Estratégia de Multi-tenancy

A estratégia definida para o MVP será **banco de dados compartilhado com isolamento lógico por `BarbeariaId`**.

Todas as barbearias utilizarão a mesma instância do SQL Server e a mesma estrutura de tabelas. Os registros pertencentes a uma barbearia serão identificados por meio de uma chave que permita determinar a qual tenant pertencem.

Exemplo conceitual:

| BarbeariaId | Nome da barbearia | Cliente |
|---|---|---|
| 1 | Barbearia Central | João |
| 1 | Barbearia Central | Pedro |
| 2 | Barbearia Premium | Carlos |
| 2 | Barbearia Premium | Lucas |

Um usuário autorizado na Barbearia Central não poderá consultar os registros da Barbearia Premium, mesmo que conheça o identificador de algum registro pertencente a ela.

A separação será realizada por regras de aplicação, contexto de tenant, consultas controladas e mecanismos de integridade no banco de dados.

### 3.2.1. Responsabilidade do BarbeariaId

O identificador `BarbeariaId` será utilizado para determinar o tenant ao qual uma operação pertence.

Nas entidades que representam dados operacionais de uma barbearia, esse identificador deverá estar presente ou ser obtido por meio de um relacionamento cuja integridade esteja garantida.

Exemplos de dados associados a uma barbearia:

- Serviços cadastrados.
- Horários gerais de funcionamento.
- Vínculos entre profissionais e barbearias.
- Disponibilidade dos profissionais.
- Bloqueios de agenda.
- Agendamentos.
- Registros de atendimento.
- Pagamentos.
- Configurações comerciais.

Nem todas as entidades precisarão necessariamente possuir essa coluna. A modelagem deverá considerar a natureza dos dados e sua abrangência.

Por exemplo, uma conta de usuário representa uma identidade na plataforma e não deve ser automaticamente duplicada para cada barbearia. Um mesmo cliente poderá possuir agendamentos em diferentes estabelecimentos, enquanto seus dados cadastrais continuarão associados à sua conta.

A definição de quais entidades possuirão `BarbeariaId` diretamente deverá ser registrada na modelagem do domínio.

## 3.3. Classificação dos dados

Para evitar associações incorretas entre dados globais e dados específicos de cada barbearia, as entidades serão classificadas conforme sua abrangência.

| Classificação | Descrição | Exemplos |
|---|---|---|
| Global | Dados que existem independentemente de uma barbearia. | Conta de usuário e identidade. |
| Tenant-scoped | Dados pertencentes exclusivamente a uma barbearia. | Serviços, horários e configurações comerciais. |
| Associativo | Dados que representam vínculos entre entidades globais e uma barbearia. | Vínculo de profissional com barbearia. |
| Transacional | Dados de operações realizadas dentro de uma barbearia. | Agendamentos, atendimentos e pagamentos. |

Essa classificação deverá orientar a criação das entidades, seus relacionamentos, índices e regras de autorização.

### 3.3.1. Contas de usuários

As contas serão administradas pelo ASP.NET Core Identity.

A identidade de um usuário não será considerada pertencente exclusivamente a uma barbearia. O vínculo com um estabelecimento será representado separadamente.

Essa separação permite atender aos fluxos definidos no PRD:

- Um barbeiro poderá criar sua própria conta.
- Um barbeiro poderá ingressar em uma barbearia por meio de um código de convite.
- Um barbeiro poderá deixar uma barbearia sem perder sua conta.
- A remoção de um vínculo não deverá apagar o histórico de operações.
- O proprietário também poderá atuar como profissional e possuir sua própria agenda.

A possibilidade de uma mesma conta manter vínculos simultâneos com múltiplas barbearias ainda deverá ser definida na modelagem de identidade e autorização.

### 3.3.2. Clientes

Os clientes terão contas próprias na plataforma.

O cadastro do cliente será global, enquanto sua relação com cada barbearia será estabelecida por meio de seus agendamentos e respectivos históricos.

Dessa forma:

- O cliente poderá utilizar a mesma conta em diferentes barbearias.
- Seus dados cadastrais não precisarão ser duplicados por estabelecimento.
- Cada barbearia visualizará somente os clientes relacionados às suas operações.
- Uma alteração nos dados cadastrais do cliente não deverá modificar informações históricas dos agendamentos.

A consulta de clientes pela administração da barbearia deverá considerar exclusivamente os registros relacionados ao tenant autenticado.

### 3.3.3. Serviços

Cada barbearia possuirá seu próprio catálogo de serviços.

O catálogo inicial definido no PRD funcionará como referência para a configuração inicial:

- Corte.
- Barba.
- Corte + Barba.

Os serviços efetivamente utilizados serão associados à barbearia, permitindo que cada estabelecimento defina seus próprios preços e durações.

A alteração ou desativação de um serviço em uma barbearia não deverá afetar serviços de outros estabelecimentos.

Os agendamentos históricos deverão preservar as informações relevantes do serviço contratado, mesmo quando ele for posteriormente desativado ou removido do catálogo.

A estratégia de instanciação dos serviços-modelo durante a criação da barbearia deverá ser definida na modelagem de domínio e persistência.

## 3.4. Contexto do Tenant

A aplicação deverá possuir um mecanismo centralizado para identificar a barbearia na qual determinada operação está sendo executada.

Esse mecanismo será denominado **Tenant Context**.

O contexto deverá disponibilizar o identificador da barbearia ativa para os componentes que precisam executar operações específicas de um tenant.

Representação conceitual:

```csharp
public interface IContextoBarbearia
{
    Guid? BarbeariaId { get; }
}
```

O tipo definitivo do identificador deverá ser padronizado durante a definição das entidades e da identidade.

A interface acima representa uma proposta de contrato, não uma implementação já definida.

### 3.4.1. Origem do contexto

O contexto da barbearia deverá ser estabelecido pelo backend a partir da identidade autenticada e dos vínculos válidos do usuário.

O frontend poderá indicar a barbearia que deseja acessar quando houver suporte a múltiplos vínculos, mas essa informação deverá ser validada pelo servidor.

O backend deverá verificar:

1. Se o usuário está autenticado.
2. Se existe um vínculo válido entre o usuário e a barbearia solicitada.
3. Se o vínculo está ativo.
4. Se o usuário possui o perfil ou as permissões necessárias para executar a operação.
5. Se o recurso solicitado pertence à barbearia identificada.

O `BarbeariaId` recebido diretamente de uma requisição não poderá ser considerado confiável sem validação.

### 3.4.2. Usuários sem contexto de barbearia

Nem todas as operações exigirão um tenant ativo.

Exemplos:

- Cadastro de usuário.
- Login.
- Recuperação de senha.
- Consulta de informações globais da conta.
- Fluxo inicial de criação de barbearia.
- Ingresso em uma barbearia por código de convite.

Essas operações deverão possuir tratamento específico, sem assumir a existência de um `BarbeariaId`.

Após a identificação de uma barbearia e a validação do vínculo, as operações específicas do estabelecimento deverão utilizar o contexto correspondente.

O comportamento para usuários vinculados a mais de uma barbearia deverá ser definido antes da implementação desse fluxo.

## 3.5. Isolamento na camada de aplicação

O isolamento entre barbearias deverá ser aplicado nas operações de aplicação, independentemente da interface que originou a requisição.

Os serviços responsáveis pelas operações deverão utilizar o contexto validado e respeitar as regras de acesso do tenant.

Exemplo conceitual:

```csharp
public async Task<Resultado> ConsultarAgendamentosAsync(
    CancellationToken cancellationToken)
{
    var barbeariaId = contextoBarbearia.BarbeariaId;

    if (barbeariaId is null)
        return Resultado.Falha("Barbearia não identificada.");

    var agendamentos = await repositorio
        .ConsultarPorBarbeariaAsync(
            barbeariaId.Value,
            cancellationToken);

    return Resultado.Sucesso(agendamentos);
}
```

O exemplo demonstra a responsabilidade de utilizar o contexto validado. O contrato definitivo dos serviços, repositórios e resultados será definido nas seções correspondentes.

### 3.5.1. Regras obrigatórias

Toda operação que envolva dados de uma barbearia deverá respeitar as seguintes regras:

- Não consultar registros de outro tenant.
- Não permitir alterações em registros de outro tenant.
- Não permitir exclusões de registros de outro tenant.
- Não associar registros de barbearias diferentes.
- Não permitir que o cliente determine livremente o tenant de uma operação.
- Validar o pertencimento dos recursos antes de executar ações.
- Preservar a integridade dos relacionamentos.
- Aplicar as mesmas restrições em consultas individuais e listagens.

A proteção não poderá existir somente na tela ou na listagem principal. Uma requisição direta à API também deverá respeitar o isolamento.

## 3.6. Isolamento na camada de persistência

A camada de Infraestrutura deverá implementar mecanismos que reduzam a possibilidade de consultas ou gravações incorretas.

O Entity Framework Core será utilizado como ORM e deverá ser configurado para trabalhar com o contexto da barbearia.

### 3.6.1. Consultas por tenant

Os repositórios responsáveis por dados específicos de uma barbearia deverão considerar o `BarbeariaId` nas consultas.

Exemplo conceitual:

```csharp
var agendamentos = await dbContext.Agendamentos
    .Where(agendamento =>
        agendamento.BarbeariaId == barbeariaId)
    .ToListAsync(cancellationToken);
```

A filtragem deverá ocorrer no banco de dados, evitando carregar registros de outros estabelecimentos para filtrá-los posteriormente em memória.

### 3.6.2. Global Query Filters

O uso de filtros globais do Entity Framework Core será avaliado como uma camada adicional de proteção.

Esses filtros permitem configurar condições aplicadas automaticamente às consultas de determinadas entidades.

Exemplo conceitual:

```csharp
builder.HasQueryFilter(
    entidade => entidade.BarbeariaId == contexto.BarbeariaId);
```

A implementação real deverá considerar o ciclo de vida do contexto, a disponibilidade do tenant e as entidades que realmente possuem escopo por barbearia.

Os filtros globais não serão considerados a única proteção de segurança.

Operações que utilizarem mecanismos como `IgnoreQueryFilters()` deverão ser restritas, justificadas e testadas. O mesmo cuidado deverá ser aplicado a consultas SQL diretas e operações que não passam pelos filtros convencionais do ORM.

**Estado:** proposta técnica pendente de validação.

### 3.6.3. Gravação de dados

A gravação de registros deverá garantir que o tenant informado corresponda ao contexto validado.

O backend não deverá aceitar um `BarbeariaId` arbitrário enviado pelo frontend para definir a propriedade de um registro.

Quando aplicável, o identificador deverá ser preenchido a partir do contexto da barbearia.

Também deverá ser avaliada a implementação de validações centralizadas durante o processo de persistência, utilizando interceptors ou mecanismos equivalentes do Entity Framework Core.

O objetivo é impedir que uma falha em um serviço ou repositório permita salvar dados associados ao tenant incorreto.

**Estado:** mecanismo definitivo pendente.

## 3.7. Integridade dos relacionamentos

O isolamento deverá ser garantido também pelos relacionamentos entre entidades.

Não será suficiente validar apenas a entidade principal de uma operação. Todas as referências utilizadas deverão pertencer ao mesmo contexto de barbearia, quando aplicável.

Exemplo:

Um agendamento da Barbearia A não poderá utilizar:

- Um profissional vinculado exclusivamente à Barbearia B.
- Um serviço cadastrado na Barbearia B.
- Um bloqueio de agenda pertencente à Barbearia B.

Essa validação deverá ocorrer na camada de aplicação e, sempre que viável, ser reforçada por restrições no banco de dados.

### 3.7.1. Chaves estrangeiras compostas

Será avaliada a utilização de chaves estrangeiras compostas que incluam o `BarbeariaId` em relacionamentos tenant-scoped.

Exemplo conceitual:

```text
Agendamento
    BarbeariaId
    ProfissionalId

ProfissionalBarbearia
    BarbeariaId
    ProfissionalId
```

Uma restrição composta poderá garantir que a associação entre as entidades pertença ao mesmo estabelecimento.

A aplicação dessa estratégia deverá considerar a modelagem real dos vínculos, evitando duplicação desnecessária de informações.

**Estado:** proposta pendente de definição por entidade.

### 3.7.2. Unicidade

As regras de unicidade deverão considerar o escopo de cada informação.

Uma informação poderá ser:

- Única globalmente.
- Única dentro de uma barbearia.
- Não sujeita a uma regra de unicidade.

Quando a unicidade for específica do tenant, os índices deverão considerar o `BarbeariaId`.

O código de convite, por exemplo, deverá ser único e permanente por barbearia, conforme definido no PRD.

As demais regras de unicidade deverão ser avaliadas individualmente durante a modelagem das entidades.

## 3.8. Operações de ingresso e vínculo

O ingresso de um profissional em uma barbearia deverá respeitar o fluxo definido no PRD.

O código de convite será único e permanente para cada barbearia. Somente o proprietário poderá compartilhá-lo com funcionários.

O código será utilizado para localizar a barbearia e iniciar o processo de ingresso, mas não substituirá a autenticação e a autorização.

Fluxo conceitual:

1. O profissional realiza login ou cadastro.
2. Informa o código de convite.
3. O backend localiza a barbearia correspondente.
4. O backend valida a existência da barbearia e as condições de ingresso.
5. O sistema cria o vínculo entre o profissional e a barbearia.
6. O profissional passa a poder acessar as operações permitidas pelo seu vínculo.

A remoção de um profissional deverá encerrar o vínculo com a barbearia, sem excluir sua conta ou os registros históricos relacionados às operações anteriores.

A desativação de um profissional deverá impedir novos agendamentos, respeitando o histórico já existente.

## 3.9. Segurança e prevenção de vazamento

A arquitetura deverá adotar uma postura de negação por padrão.

Quando uma operação não conseguir comprovar que o usuário possui acesso ao tenant e ao recurso solicitado, ela não deverá prosseguir.

Devem ser considerados os seguintes cenários:

| Situação | Comportamento esperado |
|---|---|
| Usuário sem autenticação acessa recurso protegido | Bloquear acesso. |
| Usuário autenticado sem vínculo com a barbearia | Negar acesso. |
| Vínculo desativado | Negar operações que dependam de vínculo ativo. |
| Usuário tenta consultar registro de outro tenant | Impedir a consulta. |
| Usuário tenta alterar registro de outro tenant | Impedir a alteração. |
| Usuário tenta excluir registro de outro tenant | Impedir a exclusão. |
| Recurso referencia entidade de outro tenant | Impedir a associação. |
| Contexto da barbearia ausente em operação tenant-scoped | Impedir a execução. |

A escolha entre retornar `403 Forbidden` ou `404 Not Found` para recursos de outros tenants deverá ser definida na política de tratamento de erros. Essa decisão deverá considerar a necessidade de não revelar a existência de registros pertencentes a outros estabelecimentos.

### 3.9.1. Proteção em consultas relacionadas

Consultas que utilizem `Include`, projeções, subconsultas ou carregamento de relacionamentos também deverão respeitar o tenant.

A filtragem da entidade principal não poderá ser considerada suficiente quando houver possibilidade de carregar ou expor informações relacionadas de outro estabelecimento.

### 3.9.2. Logs e informações sensíveis

Os logs deverão permitir identificar a barbearia relacionada a uma operação quando houver contexto de tenant.

Entretanto, não deverão registrar senhas, tokens de autenticação ou informações sensíveis desnecessárias.

Mensagens de erro também não deverão expor dados de outros estabelecimentos.

### 3.9.3. Cache e processos futuros

Caso sejam implementados mecanismos de cache, relatórios, exportações ou processamento em segundo plano, as operações deverão considerar o tenant como parte de seu contexto.

As chaves de cache deverão evitar colisões entre barbearias.

Processos em segundo plano deverão receber explicitamente o contexto necessário para executar suas operações, sem depender de informações de uma requisição HTTP anterior.

Esses mecanismos deverão ser tratados como requisitos de segurança nas futuras evoluções da aplicação.

## 3.10. Auditoria

Operações importantes deverão possuir rastreabilidade.

A auditoria deverá permitir identificar:

- Qual usuário executou a operação.
- Qual barbearia estava relacionada à operação.
- Qual recurso foi afetado.
- Qual ação foi realizada.
- Quando a operação ocorreu.

Essa rastreabilidade será especialmente importante para alterações em agendamentos, cancelamentos, vínculos profissionais, configurações e registros financeiros.

A auditoria deverá preservar o contexto do tenant e não poderá permitir que um usuário consulte registros de auditoria pertencentes a outra barbearia.

A estrutura definitiva da auditoria, incluindo entidade, campos, retenção e estratégia de armazenamento, será detalhada na seção de persistência e histórico.

## 3.11. Testes de isolamento

O isolamento entre barbearias será considerado um requisito crítico de segurança.

Os testes deverão verificar não apenas os fluxos normais, mas também tentativas de acesso indevido por meio de requisições manipuladas.

A estratégia de testes deverá incluir cenários com pelo menos duas barbearias independentes, identificadas como Barbearia A e Barbearia B.

| Cenário | Resultado esperado |
|---|---|
| Consultar agendamentos da própria barbearia | Retornar somente registros autorizados. |
| Consultar agendamentos de outra barbearia | Impedir o acesso. |
| Consultar registro por identificador de outro tenant | Impedir a consulta. |
| Alterar registro de outro tenant | Impedir a alteração. |
| Excluir registro de outro tenant | Impedir a exclusão. |
| Criar agendamento com serviço de outro tenant | Rejeitar a operação. |
| Criar agendamento com profissional de outro tenant | Rejeitar a operação. |
| Consultar clientes | Retornar somente clientes relacionados à barbearia. |
| Consultar dashboard | Considerar somente dados do tenant ativo. |
| Utilizar vínculo desativado | Impedir acesso às operações protegidas pelo vínculo. |
| Executar consulta com relacionamentos | Não retornar dados de outros tenants. |
| Salvar registro com `BarbeariaId` manipulado | Rejeitar ou sobrescrever com o contexto validado, conforme a operação. |
| Executar consulta sem contexto de barbearia | Impedir operações tenant-scoped. |
| Utilizar filtros globais ignorados indevidamente | Garantir que outras proteções impeçam vazamentos. |

Os testes de integração deverão utilizar um banco de dados de teste compatível com os recursos de persistência utilizados pela aplicação, evitando depender exclusivamente de mocks para validar o isolamento real.

Também deverão ser testados os relacionamentos e as restrições de integridade implementadas no SQL Server.

## 3.12. Riscos técnicos

Os principais riscos relacionados à arquitetura multi-tenant são:

| Risco | Consequência | Medida preventiva |
|---|---|---|
| Consulta sem filtro de tenant | Vazamento de dados entre barbearias. | Contexto centralizado, filtros e testes. |
| Confiança em `BarbeariaId` enviado pelo frontend | Acesso indevido a outro estabelecimento. | Resolver e validar o contexto no backend. |
| Relacionamento entre entidades de tenants diferentes | Corrupção da integridade dos dados. | Validações e restrições de banco. |
| Uso incorreto de filtros globais | Consultas sem isolamento. | Restringir bypass e adicionar testes. |
| Vínculo profissional desativado ainda autorizado | Acesso indevido à barbearia. | Validar o estado do vínculo nas operações. |
| Cache sem identificação do tenant | Exposição de dados entre estabelecimentos. | Incluir tenant na chave de cache. |
| Processamento em segundo plano sem contexto | Operações executadas no tenant incorreto. | Propagar contexto explicitamente. |
| Exclusão de vínculos apagando histórico | Perda de rastreabilidade. | Preservar registros históricos e relacionamentos necessários. |

## 3.13. Registro de decisões e pendências

As decisões abaixo registram o que já foi estabelecido e o que ainda precisa de definição técnica.

| ID | Tema | Decisão ou estado |
|---|---|---|
| TEN-01 | Estratégia multi-tenant | **Definido:** banco compartilhado com isolamento lógico por `BarbeariaId`. |
| TEN-02 | Identificação do tenant | **Definido:** o backend deverá validar o contexto da barbearia antes de executar operações tenant-scoped. |
| TEN-03 | Origem do contexto | **Definido:** o contexto deverá ser derivado da identidade autenticada e de vínculos válidos, não de um identificador arbitrário enviado pelo frontend. |
| TEN-04 | Contas de usuários | **Definido:** identidade global, com vínculos independentes às barbearias. |
| TEN-05 | Dados dos clientes | **Definido:** conta global; relacionamento com barbearias por meio das operações e dos agendamentos. |
| TEN-06 | Serviços | **Definido:** catálogo próprio por barbearia, com preços e durações independentes. |
| TEN-07 | Filtros globais do EF Core | **Proposta:** avaliar utilização como proteção adicional. |
| TEN-08 | Validação de gravações | **Pendente:** definir mecanismo centralizado para garantir o tenant correto na persistência. |
| TEN-09 | Integridade entre tenants | **Proposta:** avaliar chaves estrangeiras compostas e restrições de banco. |
| TEN-10 | Unicidade | **Pendente:** definir regras globais e por barbearia para cada entidade. |
| TEN-11 | Múltiplos vínculos | **Pendente:** definir se uma mesma conta poderá manter vínculos simultâneos com diferentes barbearias e como será selecionado o contexto ativo. |
| TEN-12 | Identificador do tenant | **Pendente:** padronizar o tipo definitivo de `BarbeariaId` na modelagem. |
| TEN-13 | Tratamento de acesso indevido | **Pendente:** definir quando retornar `403` ou `404`. |
| TEN-14 | Auditoria | **Definido:** operações importantes deverão registrar usuário, barbearia e momento da ação. A implementação permanece pendente. |
| TEN-15 | Contexto em tarefas futuras | **Definido:** processos em segundo plano deverão receber contexto explícito do tenant. |

## 3.14. Critérios de conclusão

A arquitetura multi-tenant será considerada implementada quando:

1. Cada barbearia possuir isolamento lógico dos seus dados.
2. O backend identificar e validar o tenant das operações protegidas.
3. Consultas e alterações respeitarem o contexto da barbearia.
4. Relacionamentos não permitirem associações indevidas entre tenants.
5. A persistência possuir mecanismos de proteção contra gravações no tenant incorreto.
6. O fluxo de vínculo de profissionais respeitar as regras de ingresso e remoção.
7. Os dados históricos forem preservados conforme as regras do produto.
8. Os testes de integração comprovarem o isolamento entre pelo menos duas barbearias.
9. Nenhuma operação tenant-scoped depender exclusivamente de validações realizadas pelo frontend.

A implementação deverá manter o isolamento como uma responsabilidade estrutural da aplicação, e não como uma regra isolada de cada tela ou endpoint.

# 4. Arquitetura de Persistência e Acesso a Dados

## 4.1. Objetivo

Esta seção define a arquitetura responsável pela persistência e recuperação dos dados do PontoBarber, estabelecendo os padrões de comunicação entre a aplicação e o banco de dados.

A persistência deverá garantir:

- Integridade dos dados armazenados.
- Consistência das operações de negócio.
- Isolamento entre barbearias.
- Preservação de informações históricas.
- Controle de concorrência em operações simultâneas.
- Organização e manutenção da estrutura do banco.
- Facilidade de evolução do modelo de dados.
- Rastreabilidade das alterações relevantes.

A camada de persistência será implementada utilizando **Microsoft SQL Server** e **Entity Framework Core**, conforme definido nas decisões técnicas do projeto.

## 4.2. Banco de dados

O banco de dados principal será o Microsoft SQL Server.

A aplicação utilizará uma única estrutura de banco compartilhada entre as barbearias, respeitando a estratégia multi-tenant definida na seção anterior.

O banco deverá armazenar informações relacionadas a:

- Usuários e autenticação.
- Barbearias.
- Vínculos de profissionais.
- Serviços.
- Horários de funcionamento.
- Disponibilidade e bloqueios.
- Clientes e agendamentos.
- Atendimentos.
- Pagamentos.
- Notificações.
- Auditoria e histórico.

A organização das tabelas deverá refletir o domínio da aplicação, evitando estruturas genéricas que dificultem a aplicação das regras de negócio.

### 4.2.1. Convenções do banco

As seguintes convenções deverão orientar a modelagem:

| Elemento | Convenção |
|---|---|
| Tabelas | Nomes representativos das entidades. |
| Colunas | Nomes claros e consistentes. |
| Chaves primárias | Identificadores únicos. |
| Chaves estrangeiras | Relacionamentos explícitos entre entidades. |
| Índices | Criados conforme padrões de consulta e regras de unicidade. |
| Datas | Utilizar tipos adequados para data e horário. |
| Valores monetários | Utilizar tipos decimais com precisão definida. |
| Campos obrigatórios | Restrições `NOT NULL` quando aplicável. |
| Relacionamentos | Integridade referencial garantida pelo banco sempre que possível. |

A convenção definitiva de nomes, incluindo pluralização, idioma dos objetos e padrão de identificação, deverá ser padronizada antes da criação das entidades.

### 4.2.2. Identificadores

O tipo dos identificadores deverá ser consistente entre as entidades e seus relacionamentos.

A escolha entre `Guid` e outros tipos de identificadores deverá considerar:

- Integração com ASP.NET Core Identity.
- Facilidade de geração de identificadores.
- Desempenho dos índices.
- Necessidade de identificação externa.
- Consistência entre entidades.

A definição do tipo de `BarbeariaId` deverá acompanhar a decisão sobre os identificadores das demais entidades.

**Estado:** pendente de padronização definitiva.

## 4.3. Entity Framework Core

O Entity Framework Core será utilizado como ORM da aplicação.

Sua responsabilidade será realizar o mapeamento entre as entidades do domínio e as estruturas relacionais do SQL Server.

O EF Core será utilizado para:

- Consultar registros.
- Inserir entidades.
- Atualizar informações.
- Remover ou desativar registros.
- Gerenciar relacionamentos.
- Controlar alterações rastreadas.
- Executar transações.
- Aplicar migrations.
- Garantir a persistência das operações.

O acesso ao banco deverá ocorrer por meio da camada de Infraestrutura, evitando que regras de persistência sejam espalhadas pela API ou pela camada de domínio.

### 4.3.1. DbContext

A aplicação possuirá um contexto principal de persistência, responsável por representar a sessão de trabalho com o banco de dados.

Esse contexto será responsável por:

- Expor os conjuntos de entidades persistidas.
- Configurar o mapeamento das entidades.
- Controlar o rastreamento das alterações.
- Executar operações de persistência.
- Integrar as configurações de isolamento multi-tenant.
- Integrar o ASP.NET Core Identity.

O nome definitivo do contexto será definido durante a implementação.

### 4.3.2. Configuração das entidades

O mapeamento das entidades deverá ser realizado por classes próprias utilizando `IEntityTypeConfiguration<T>`.

Essa abordagem evita concentrar todas as configurações dentro do `DbContext`.

Exemplo conceitual:

```csharp
public class AgendamentoConfiguration
    : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(
        EntityTypeBuilder<Agendamento> builder)
    {
        builder.HasKey(agendamento => agendamento.Id);

        builder.Property(agendamento => agendamento.Observacao)
            .HasMaxLength(500);
    }
}
```

Cada configuração deverá ser responsável por definir os aspectos relacionais da entidade correspondente, incluindo:

- Chave primária.
- Propriedades obrigatórias.
- Tipos e tamanhos das colunas.
- Precisão de valores decimais.
- Relacionamentos.
- Chaves estrangeiras.
- Índices.
- Restrições de unicidade.
- Conversões de tipos, quando necessárias.

As regras de domínio não deverão ser transferidas para essas classes. O mapeamento define como os dados são armazenados; as regras de negócio permanecem nas camadas responsáveis pelo domínio e pela aplicação.

## 4.4. Repository Pattern

O projeto utilizará Repository Pattern para organizar o acesso aos dados.

As interfaces dos repositórios ficarão nas camadas apropriadas da aplicação, enquanto suas implementações ficarão na Infraestrutura.

O objetivo é evitar que os serviços de aplicação dependam diretamente dos detalhes de implementação do Entity Framework Core.

### 4.4.1. Responsabilidades dos repositórios

Os repositórios deverão encapsular operações de persistência relacionadas às entidades ou agregados do domínio.

Responsabilidades previstas:

- Consultar entidades.
- Recuperar registros por identificador.
- Consultar registros com critérios específicos.
- Adicionar entidades.
- Atualizar entidades.
- Remover entidades quando permitido.
- Carregar relacionamentos necessários.
- Aplicar critérios de isolamento por barbearia.

Os repositórios não deverão concentrar regras de negócio que pertencem aos serviços de aplicação ou ao domínio.

### 4.4.2. Interfaces e implementações

A separação deverá respeitar o sentido das dependências definido na arquitetura:

```text
Aplicacao
    └── Interfaces dos repositórios

Infraestrutura
    └── Implementações dos repositórios
            └── Entity Framework Core
```

A camada de Aplicação conhecerá os contratos necessários para executar suas operações, sem depender da implementação concreta de persistência.

A Infraestrutura implementará esses contratos utilizando o SQL Server e o EF Core.

### 4.4.3. Repositórios específicos

A implementação deverá priorizar repositórios que representem necessidades reais do domínio.

Não será obrigatório criar um repositório para cada classe ou tabela, nem utilizar um repositório genérico apenas para padronizar operações CRUD.

A escolha deverá considerar:

- Complexidade das consultas.
- Responsabilidades da entidade ou agregado.
- Necessidade de carregamento de relacionamentos.
- Regras de isolamento multi-tenant.
- Necessidade de operações específicas.

Consultas mais complexas poderão utilizar métodos específicos, evitando que os serviços precisem conhecer detalhes do banco.

## 4.5. Serviços de aplicação e persistência

Os serviços da camada de Aplicação serão responsáveis por coordenar as operações necessárias para executar os casos de uso.

Eles poderão utilizar um ou mais repositórios para consultar ou modificar informações.

Exemplo conceitual de fluxo:

```text
Controller
    |
    v
ServicoAgendamento
    |
    +----> RepositorioAgendamento
    |
    +----> RepositorioServico
    |
    +----> RepositorioProfissional
    |
    v
Persistência
    |
    v
SQL Server
```

O serviço deverá garantir que as validações necessárias ocorram antes da confirmação da operação.

Exemplos:

- Validar se o serviço pertence à barbearia.
- Validar se o profissional está vinculado e ativo.
- Validar se o horário está dentro do funcionamento.
- Validar se não existe bloqueio.
- Validar se não existe outro agendamento conflitante.
- Garantir que os registros relacionados pertençam ao mesmo tenant.

A camada de aplicação deverá coordenar essas verificações sem transferir a responsabilidade das regras de negócio para os controllers.

## 4.6. Unidade de trabalho e transações

As operações que envolvem múltiplas alterações relacionadas deverão manter consistência transacional.

Quando um caso de uso precisar modificar diferentes entidades, as alterações deverão ser confirmadas de forma atômica sempre que fizerem parte da mesma operação de negócio.

Exemplo:

Ao confirmar uma remarcação, o sistema poderá precisar:

1. Validar o novo horário.
2. Atualizar a data e o horário do agendamento.
3. Preservar as informações históricas necessárias.
4. Registrar a alteração para auditoria.
5. Confirmar a operação.

Se uma etapa obrigatória falhar, a operação não deverá deixar o banco em um estado parcialmente atualizado.

O EF Core disponibiliza mecanismos de transação e persistência por meio do `DbContext`.

Será avaliada a adoção de um contrato explícito de Unit of Work para coordenar as operações dos repositórios.

**Estado:** pendente de definição entre utilização direta do `DbContext.SaveChangesAsync()` e implementação de uma abstração explícita de Unit of Work.

A decisão deverá evitar abstrações redundantes e preservar a clareza da arquitetura.

## 4.7. Concorrência e integridade dos agendamentos

O controle de concorrência será uma das responsabilidades mais importantes da persistência do PontoBarber.

O sistema não poderá permitir que duas solicitações confirmem simultaneamente o mesmo horário para o mesmo profissional quando os períodos forem conflitantes.

A validação realizada exclusivamente pela interface não será suficiente.

Duas requisições poderão consultar o mesmo horário como disponível e tentar confirmá-lo praticamente ao mesmo tempo.

Por isso, o backend deverá implementar proteção contra reservas simultâneas.

### 4.7.1. Validação de conflitos

A validação deverá considerar o intervalo completo do agendamento, não apenas seu horário inicial.

Exemplo:

```text
Agendamento existente:
14:00 ───────── 14:40

Nova solicitação:
14:30 ───────── 15:10

Resultado:
Conflito de horário.
```

O sistema deverá considerar conflitos quando os intervalos se sobrepuserem para o mesmo profissional.

Também deverão ser considerados:

- Duração do serviço.
- Horário de funcionamento da barbearia.
- Disponibilidade semanal do profissional.
- Bloqueios pontuais.
- Agendamentos já existentes.
- Status dos agendamentos.
- Reservas temporárias de horários sugeridos, conforme as regras do produto.

### 4.7.2. Proteção no backend e no banco

A aplicação deverá garantir que a verificação e a confirmação do agendamento não permitam uma condição de corrida.

Serão avaliadas estratégias como:

- Transações com nível de isolamento adequado.
- Bloqueios de registros ou intervalos durante a confirmação.
- Estratégias de concorrência otimista ou pessimista.
- Restrições adicionais no banco quando aplicáveis.

Uma simples consulta seguida de inserção sem proteção transacional não será considerada suficiente.

A estratégia escolhida deverá ser compatível com o SQL Server e comprovada por testes de integração concorrentes.

**Estado:** requisito definido; mecanismo técnico pendente de implementação e validação.

### 4.7.3. Concorrência otimista em outras entidades

Também deverá ser avaliada a utilização de controle de concorrência otimista para alterações simultâneas em registros que possam ser modificados por diferentes usuários.

O SQL Server oferece o tipo `rowversion`, que poderá ser utilizado pelo EF Core como token de concorrência.

A aplicação deverá avaliar sua utilização em entidades relevantes, especialmente quando atualizações simultâneas puderem causar perda de informações.

**Estado:** proposta técnica pendente.

## 4.8. Preservação de histórico

A persistência deverá respeitar as regras históricas definidas no PRD.

A exclusão de informações operacionais não deverá apagar registros necessários para compreender operações realizadas anteriormente.

Exemplos:

- A desativação de um serviço não deverá modificar agendamentos antigos.
- A remoção do vínculo de um profissional não deverá apagar seu histórico de atendimentos.
- A alteração dos dados cadastrais de um cliente não deverá modificar informações históricas dos agendamentos.
- O cancelamento deverá preservar o registro do agendamento.
- Uma remarcação deverá atualizar o agendamento existente, preservando o histórico necessário.
- Os pagamentos registrados deverão permanecer associados aos respectivos atendimentos.

### 4.8.1. Exclusão lógica

A utilização de exclusão lógica deverá ser avaliada para entidades cujo histórico precise ser preservado.

O mecanismo poderá utilizar um indicador de situação, como `Ativo`, ou outro estado adequado ao significado da entidade.

Não deverá ser aplicado indiscriminadamente a todas as tabelas.

A escolha deverá considerar:

- Regras de negócio.
- Necessidade de preservação histórica.
- Relacionamentos existentes.
- Consultas e relatórios.
- Regras de retenção de dados.

### 4.8.2. Snapshot de informações históricas

Os agendamentos deverão preservar as informações relevantes do serviço contratado no momento da reserva.

Isso inclui, no mínimo, as informações exigidas pelo PRD para preservar o histórico quando um serviço for posteriormente removido ou alterado:

- Nome do serviço.
- Duração.
- Valor.

A modelagem deverá definir se essas informações serão armazenadas diretamente no agendamento, em uma entidade histórica associada ou por outra estratégia que garanta a preservação dos dados originais.

A solução escolhida não poderá depender exclusivamente dos dados atuais do catálogo de serviços.

## 4.9. Migrations e evolução do banco

A evolução da estrutura do banco deverá ser controlada por migrations do Entity Framework Core.

As migrations serão utilizadas para registrar alterações estruturais realizadas durante o desenvolvimento.

Exemplos:

- Criação de tabelas.
- Alteração de colunas.
- Inclusão de índices.
- Criação de relacionamentos.
- Alteração de restrições.
- Inclusão de novas entidades.
- Modificações necessárias para novas funcionalidades.

Cada migration deverá representar uma alteração identificável e rastreável na estrutura do banco.

### 4.9.1. Versionamento

As migrations deverão ser mantidas no controle de versão junto ao código-fonte.

Não será permitido depender exclusivamente de alterações manuais no banco de dados para manter os ambientes sincronizados.

As alterações deverão ser revisadas antes de serem aplicadas em ambientes compartilhados ou de produção.

### 4.9.2. Aplicação das migrations

O processo de aplicação das migrations deverá considerar o ambiente de execução.

No ambiente local de desenvolvimento, deverá existir um fluxo automatizado para preparar o banco de dados e aplicar migrations pendentes antes de a aplicação começar a aceitar requisições.

Em ambientes de produção, a estratégia de aplicação deverá ser controlada e documentada, evitando alterações inesperadas na estrutura do banco durante a inicialização de múltiplas instâncias da API.

A execução de migrations em produção deverá considerar:

- Backup e recuperação.
- Compatibilidade entre versões da aplicação.
- Tempo de execução das alterações.
- Impacto sobre dados existentes.
- Possibilidade de rollback ou correção por nova migration.

A estratégia definitiva de implantação será aprofundada na seção de infraestrutura e deploy.

## 4.10. Inicialização e dados iniciais

A aplicação poderá precisar de dados iniciais para funcionar corretamente em ambiente de desenvolvimento.

Esses dados deverão ser criados por um processo controlado, separado da lógica normal de cadastro de usuários e barbearias.

O provisionamento deverá ser idempotente: executar o processo novamente não poderá gerar duplicações ou inconsistências.

Exemplos de dados que poderão ser provisionados:

- Serviços-modelo iniciais.
- Dados sintéticos para desenvolvimento.
- Configurações necessárias para testes locais.

O provisionamento de dados não deverá criar informações reais de clientes ou operações.

A criação de usuários de teste e credenciais deverá respeitar as regras de segurança e permanecer restrita aos ambientes apropriados.

A estratégia de dados iniciais deverá ser compatível com o bootstrap local e com o ambiente Docker definido para o projeto.

## 4.11. Desempenho e otimização das consultas

As consultas deverão ser projetadas considerando o volume de dados esperado e a necessidade de isolamento entre barbearias.

A aplicação deverá evitar operações desnecessárias e carregamento excessivo de entidades.

Diretrizes:

- Utilizar projeções para retornar somente os campos necessários.
- Utilizar `AsNoTracking()` em consultas somente de leitura quando apropriado.
- Evitar carregamento desnecessário de relacionamentos.
- Evitar consultas repetitivas que provoquem problemas de N+1.
- Criar índices para consultas frequentes.
- Aplicar filtros de tenant diretamente nas consultas.
- Implementar paginação em listagens potencialmente extensas.
- Evitar carregar históricos completos em telas que não precisam dessas informações.
- Utilizar consultas assíncronas com os métodos apropriados do EF Core.

### 4.11.1. Consultas da agenda

A agenda deverá ser otimizada para consultar os agendamentos de um período específico.

A consulta deverá considerar:

- Barbearia.
- Profissional, quando houver filtro.
- Data inicial.
- Data final.
- Status relevante para a visualização.

Não deverá carregar todos os agendamentos históricos para montar uma visualização diária ou semanal.

### 4.11.2. Dashboard

As informações do dashboard deverão ser obtidas por consultas específicas, evitando carregar entidades completas apenas para calcular indicadores.

Os indicadores deverão considerar os períodos definidos no PRD:

- Agendamentos do dia.
- Agendamentos da semana.
- Cancelamentos do mês.
- Valores recebidos no mês.
- Próximos agendamentos.

Todas as consultas deverão respeitar o tenant ativo.

## 4.12. Configuração e segurança da conexão

A string de conexão do SQL Server deverá ser configurada por meio de mecanismos externos ao código-fonte.

A aplicação não deverá armazenar credenciais diretamente em arquivos versionados.

Serão utilizados mecanismos de configuração compatíveis com ASP.NET Core, Docker e os ambientes de implantação.

Diretrizes:

- Utilizar variáveis de ambiente para configuração local via Docker.
- Manter arquivos `.env` fora do controle de versão.
- Utilizar User Secrets quando apropriado no desenvolvimento local.
- Utilizar armazenamento seguro de segredos no ambiente de produção.
- Evitar registrar strings de conexão completas nos logs.
- Utilizar credenciais com permissões mínimas necessárias.
- Não utilizar credenciais administrativas para a conexão normal da aplicação.

A configuração deverá permitir que a aplicação utilize diferentes bancos ou instâncias conforme o ambiente, sem alterações no código-fonte.

## 4.13. Testes da persistência

A camada de persistência deverá possuir testes que verifiquem o comportamento real do mapeamento e das operações no banco.

Os testes não deverão depender exclusivamente de mocks para validar relacionamentos, migrations, transações e restrições do SQL Server.

A estratégia deverá considerar um banco de dados de teste compatível com o banco utilizado pela aplicação.

### 4.13.1. Testes de mapeamento

Verificar:

- Criação correta das tabelas.
- Chaves primárias.
- Chaves estrangeiras.
- Índices.
- Restrições de unicidade.
- Campos obrigatórios.
- Precisão de valores monetários.
- Tipos de dados.
- Relacionamentos entre entidades.

### 4.13.2. Testes de repositórios

Verificar:

- Inclusão de registros.
- Consulta por identificador.
- Consulta por critérios.
- Atualização de registros.
- Exclusão ou desativação.
- Carregamento de relacionamentos.
- Paginação.
- Aplicação do isolamento multi-tenant.

### 4.13.3. Testes transacionais

Verificar:

- Confirmação de operações completas.
- Rollback em caso de falha.
- Consistência entre múltiplas alterações.
- Comportamento diante de erros de persistência.
- Ausência de registros parcialmente atualizados.

### 4.13.4. Testes de concorrência

Verificar:

- Duas solicitações tentando reservar o mesmo horário.
- Solicitações simultâneas com intervalos conflitantes.
- Solicitações simultâneas para profissionais diferentes.
- Concorrência entre agendamentos e bloqueios de agenda.
- Concorrência entre alterações do mesmo registro.

O sistema deverá impedir reservas conflitantes, preservando a integridade dos dados.

### 4.13.5. Testes de migrations

Verificar:

- Aplicação das migrations em banco vazio.
- Evolução de um banco já existente.
- Preservação dos dados durante alterações estruturais.
- Consistência entre o modelo atual e a estrutura persistida.

## 4.14. Registro de decisões e pendências

| ID | Tema | Decisão ou estado |
|---|---|---|
| PER-01 | Banco de dados | **Definido:** Microsoft SQL Server. |
| PER-02 | ORM | **Definido:** Entity Framework Core. |
| PER-03 | Contexto de persistência | **Definido:** utilização de `DbContext` para controlar a sessão de persistência. |
| PER-04 | Configuração das entidades | **Definido:** classes próprias com `IEntityTypeConfiguration<T>`. |
| PER-05 | Repository Pattern | **Definido:** interfaces nas camadas apropriadas e implementações na Infraestrutura. |
| PER-06 | Serviços de aplicação | **Definido:** responsáveis por coordenar os casos de uso e as operações de persistência. |
| PER-07 | Unit of Work | **Pendente:** definir se haverá abstração explícita ou utilização direta do `DbContext`. |
| PER-08 | Transações | **Definido:** operações relacionadas deverão manter consistência transacional. |
| PER-09 | Concorrência de agendamentos | **Definido:** impedir reservas simultâneas conflitantes no backend e na persistência. |
| PER-10 | Estratégia de concorrência | **Pendente:** definir transações, isolamento e mecanismos de proteção no SQL Server. |
| PER-11 | Concorrência otimista | **Proposta:** avaliar `rowversion` nas entidades relevantes. |
| PER-12 | Histórico de agendamentos | **Definido:** preservar informações relevantes dos serviços e operações históricas. |
| PER-13 | Exclusão lógica | **Pendente:** definir quais entidades utilizarão desativação ou exclusão lógica. |
| PER-14 | Migrations | **Definido:** utilizar migrations do Entity Framework Core versionadas no repositório. |
| PER-15 | Identificadores | **Pendente:** padronizar o tipo de chave primária e estrangeira. |
| PER-16 | Convenções de nomes | **Pendente:** definir padrão de nomenclatura de tabelas, colunas e propriedades. |
| PER-17 | Dados iniciais | **Definido:** provisionamento local deverá ser controlado e idempotente. |
| PER-18 | Testes de persistência | **Definido:** testar operações e integridade com banco compatível com SQL Server. |
| PER-19 | Configuração de conexão | **Definido:** credenciais e strings de conexão não deverão ser versionadas no código-fonte. |
| PER-20 | Estratégia de consultas | **Definido:** priorizar consultas específicas, projeções, paginação e índices adequados. |

## 4.15. Critérios de conclusão

A arquitetura de persistência será considerada implementada quando:

1. O SQL Server estiver integrado à aplicação por meio do Entity Framework Core.
2. As entidades estiverem mapeadas por configurações independentes.
3. Os repositórios implementarem os contratos definidos pela camada de Aplicação.
4. As operações respeitarem o isolamento multi-tenant.
5. As transações garantirem consistência das operações relacionadas.
6. O controle de concorrência impedir reservas conflitantes.
7. Os dados históricos forem preservados conforme as regras do produto.
8. As migrations permitirem criar e evoluir o banco de forma controlada.
9. A configuração de conexão estiver protegida e separada do código-fonte.
10. Os testes de persistência comprovarem o comportamento esperado no SQL Server.

A camada de persistência deverá funcionar como uma infraestrutura confiável para as regras de negócio do PontoBarber, garantindo que os dados armazenados permaneçam consistentes, íntegros e isolados entre as barbearias.