# Tasks

## 1. Fundação da solução

- [x] 1.1 Criar `PontoBarber.slnx` com projetos `Dominio`, `Aplicacao`, `Infra`, `WebApp` e testes correspondentes; verificar `dotnet build` verde e que `Dominio` não referencia EF Core, ASP.NET Core, Identity ou biblioteca de agendamento
- [x] 1.2 Configurar `Directory.Build.props`, nullable, analisadores, tratamento de warnings como erro e convenções de nomenclatura; verificar build sem warnings
- [x] 1.3 Configurar `DbContext`, convenções de `TenantId`, UTC em `datetime2(3)` e configurações por `IEntityTypeConfiguration`; verificar teste que rejeita entidade de negócio sem `TenantId`
- [x] 1.4 Implementar resolução de tenant ativo pela rota e validação contra vínculo do usuário; verificar testes negativos de rota com barbearia sem vínculo
- [x] 1.5 Implementar filtro global por tenant e testes que comprovem isolamento entre duas barbearias com dados homônimos
- [x] 1.6 Implementar contrato de erro `application/problem+json` com código estável, `traceId` e erros por campo; verificar testes de conflito e de validação
- [x] 1.7 Implementar health checks de vivacidade e prontidão com falha de prontidão quando a persistência não responde; verificar teste de integração com banco indisponível
- [x] 1.8 Configurar Serilog, OpenTelemetry com exportador OTLP e correlação de requisição, usuário e barbearia; verificar teste de que senha, cookie e e-mail/telefone completos não aparecem nos logs
- [ ] 1.9 Criar pipeline do GitHub Actions com restore, build, testes, análise e verificação de migrations pendentes, sem exigir provedor de nuvem; verificar execução verde em ambiente limpo
- [ ] 1.10 Criar `Dockerfile` da API e da web com Nginx e build multi-stage; verificar imagem construída localmente e health check respondendo
- [ ] 1.11 Documentar em `docs/` o comando de execução local, migrations e o contrato de erros; verificar que os comandos documentados executam como escritos

## 2. Fundação visual

- [ ] 2.1 Configurar Bootstrap 5 com importação seletiva por SCSS, tokens de tema, Bootstrap Icons e orçamento de bundle; verificar build e medição do bundle inicial dentro do orçamento
- [ ] 2.2 Criar `shared/ui` com layout, botão, campo de formulário, tabela, estado vazio e diálogo com foco, rótulos e navegação por teclado; verificar testes de componente e axe sem violação crítica
- [ ] 2.3 Configurar cliente HTTP com interceptação de erro, antiforgery e correlação; verificar teste de que erro da API é exibido de forma compreensível
- [ ] 2.4 Criar testes E2E e auditoria axe no pipeline; verificar que uma violação crítica reprova a execução

## 3. Identidade e acesso

- [ ] 3.1 Configurar ASP.NET Core Identity, política de senha e cookie com atributos de segurança; verificar teste de criação de usuário e de cookie inacessível a JavaScript
- [ ] 3.2 Implementar cadastro de cliente e cadastro de barbeiro em fluxos distintos; verificar testes de cadastro válido, e-mail duplicado e confirmação divergente
- [ ] 3.3 Implementar login compartilhado com mensagem genérica e bloqueio por tentativas; verificar testes de credencial inválida e de bloqueio temporário
- [ ] 3.4 Implementar `/auth/me`, logout e alteração de senha; verificar testes de sessão encerrada após logout e de alteração com senha atual válida
- [ ] 3.5 Aplicar antiforgery em métodos que alteram estado e autorização por perfil e recurso; verificar testes de requisição sem token e de acesso cruzado entre perfis
- [ ] 3.6 Implementar telas de escolha de perfil, cadastro, login e dados da conta; verificar E2E dos três fluxos e axe sem violação crítica

## 4. Barbearia e profissionais

- [ ] 4.1 Implementar criação de barbearia com validação, fuso IANA e geração de código de convite; verificar testes de criação válida, campo inválido e código único
- [ ] 4.2 Implementar consulta e edição dos dados da barbearia restritas ao proprietário; verificar testes de edição válida, tentativa de barbeiro e preservação de agendamentos históricos
- [ ] 4.3 Implementar entrada por código de convite com rejeição de código inválido e de vínculo duplicado; verificar testes dos três cenários
- [ ] 4.4 Implementar gestão de vínculos: ativar, desativar, editar dados profissionais e remover, preservando conta e histórico; verificar testes de cada operação e de tentativa sobre vínculo de outro profissional
- [ ] 4.5 Garantir que o proprietário tenha vínculo de profissional com agenda própria; verificar teste de acesso simultâneo às permissões administrativas e à agenda
- [ ] 4.6 Implementar telas de empresa, convite e profissionais; verificar E2E de criação de barbearia, entrada por convite e desativação de profissional

## 5. Catálogo de serviços

- [ ] 5.1 Implementar seed do catálogo modelo (Corte 40 min, Barba 30 min, Corte + Barba 70 min) sem preços fixos; verificar teste de disponibilidade do modelo em barbearia nova
- [ ] 5.2 Implementar criação e edição de serviços restritas ao proprietário, com validação de preço não negativo e duração; verificar testes de criação válida, valor inválido e tentativa de barbeiro
- [ ] 5.3 Implementar remoção lógica e consulta por perfil, com cliente vendo apenas serviços ativos; verificar testes de serviço removido fora da oferta e de histórico preservado com nome, duração e preço originais
- [ ] 5.4 Implementar tela de catálogo com criação, edição e remoção; verificar E2E do fluxo do proprietário e leitura sem opções de edição para barbeiro

## 6. Disponibilidade e horários

- [ ] 6.1 Implementar horários gerais de funcionamento por dia da semana com validação de abertura e fechamento; verificar testes de configuração válida, período invertido e dia fechado
- [ ] 6.2 Implementar disponibilidade semanal do profissional contida no funcionamento; verificar testes de janela válida, janela fora do funcionamento e preservação de agendamentos existentes
- [ ] 6.3 Implementar bloqueios pontuais com validação de período e tratamento de conflito com agendamento existente; verificar testes dos três cenários
- [ ] 6.4 Implementar cálculo de horários disponíveis considerando funcionamento, disponibilidade, bloqueios e agendamentos, com limite de 90 dias e fuso da barbearia; verificar testes de horário livre, fora da jornada e data além do limite
- [ ] 6.5 Aplicar autorização das configurações por perfil; verificar testes de barbeiro tentando alterar horários gerais e bloqueio de outro profissional
- [ ] 6.6 Implementar telas de horários, disponibilidade e bloqueios; verificar E2E e axe sem violação crítica

## 7. Agendamento e agenda

- [ ] 7.1 Implementar criação de agendamento com snapshot de nome, duração e preço e cálculo de término e valor; verificar testes de cálculo e de preservação após alteração do serviço
- [ ] 7.2 Implementar confirmação em transação com lock por profissional/data, revalidação de disponibilidade e índice único; verificar teste de concorrência com duas confirmações simultâneas aceitando apenas uma
- [ ] 7.3 Implementar agendamento público autenticado; verificar testes de confirmação válida, visitante não autenticado, serviço inativo e profissional inativo
- [ ] 7.4 Implementar agendamento manual pela agenda com autorização por profissional; verificar testes de criação válida e de barbeiro tentando agendar para profissional fora da própria agenda
- [ ] 7.5 Implementar consulta de agendamentos com autorização por proprietário, profissional e titular; verificar testes de cliente acessando agendamento de outro cliente
- [ ] 7.6 Implementar agenda em dia, semana e mês com filtro por profissional e cores de confirmado, cancelado e bloqueio; verificar testes de componente das visualizações e E2E de alternância
- [ ] 7.7 Implementar fluxo de agendamento público no frontend com seleção de serviço, profissional, data e horário; verificar E2E completo e axe sem violação crítica

## 8. Ciclo de vida do agendamento

- [ ] 8.1 Implementar cancelamento por cliente, barbeiro e proprietário com registro de autor e momento; verificar testes de cada perfil e de cancelamento recusado após o início
- [ ] 8.2 Implementar remarcação para horário disponível preservando histórico; verificar testes de remarcação válida, horário indisponível e manutenção do horário original em caso de recusa
- [ ] 8.3 Implementar sugestão de novo horário com motivo obrigatório e reserva de até 24 horas; verificar testes de sugestão sem motivo, aceite, rejeição e expiração
- [ ] 8.4 Implementar histórico append-only com eventos, autor e momento em ordem cronológica; verificar teste de que eventos não podem ser alterados ou removidos pela aplicação
- [ ] 8.5 Implementar telas de cancelamento, remarcação e histórico; verificar E2E dos fluxos e axe sem violação crítica

## 9. Processos periódicos

- [ ] 9.1 Definir o contrato de execução de processos na `Aplicacao` e o registro em `JobRuns` com início, fim, resultado e quantidade; verificar teste de registro em execução bem-sucedida e em falha
- [ ] 9.2 Implementar adaptador de disparo provisório apenas em `Infra`, sem escolher biblioteca definitiva, e documentar a pendência do ADR-027; verificar que `Dominio` e `Aplicacao` não referenciam biblioteca de agendamento
- [ ] 9.3 Implementar expiração de sugestões com `UPDATE` condicional e notificação aos envolvidos; verificar teste de expiração e de execução repetida sem liberação ou notificação duplicada
- [ ] 9.4 Implementar fechamento de agendamentos vencidos como não comparecimento; verificar teste de agendamento vencido e de reprocessamento sem alteração de status
- [ ] 9.5 Implementar expurgo de registros de idempotência por data de expiração; verificar teste de remoção de expirado e preservação de vigente
- [ ] 9.6 Documentar os processos e a limitação de durabilidade enquanto o ADR-027 estiver pendente; verificar que o documento lista os quatro processos e seus gatilhos

## 10. Atendimento e pagamentos

- [ ] 10.1 Implementar início e fim do atendimento com registro automático dos horários reais e recusa para status inválido; verificar testes de início válido, início em cancelado, fim válido e fim sem início
- [ ] 10.2 Implementar não comparecimento como status distinto de cancelamento, impedindo início e fim; verificar testes dos três cenários
- [ ] 10.3 Implementar registro de pagamento com valor e forma permitidos, situação independente do atendimento e recusa de duplicidade; verificar testes de pagamento válido, valor inválido, forma inválida, duplicidade e atendimento finalizado sem pagamento
- [ ] 10.4 Implementar telas de atendimento e pagamento; verificar E2E do ciclo completo e axe sem violação crítica

## 11. Clientes

- [ ] 11.1 Implementar relação do cliente por agendamentos atuais ou históricos; verificar testes de cliente com agendamento aparecendo e cliente sem agendamento ausente
- [ ] 11.2 Implementar listagem, pesquisa e consulta de clientes restritas ao proprietário; verificar testes de pesquisa, barbeiro sem acesso e cliente de outra barbearia
- [ ] 11.3 Implementar consulta e edição dos próprios dados pelo cliente com validação de e-mail duplicado; verificar testes de edição válida, acesso a outro cliente e preservação do histórico
- [ ] 11.4 Garantir ausência de cadastro, edição ou exclusão de clientes pelo estabelecimento; verificar testes de que as operações não estão expostas na API
- [ ] 11.5 Implementar telas de clientes para o proprietário e de perfil para o cliente; verificar E2E e axe sem violação crítica

## 12. Dashboard

- [ ] 12.1 Implementar consulta agregada dos indicadores Hoje, Esta semana, Cancelamentos e Recebido no mês no fuso da barbearia; verificar testes de período, de semana ISO e de cálculo na virada do dia
- [ ] 12.2 Garantir que cancelamentos não entram em recebimentos e que indicadores sem dados retornam zero; verificar testes específicos
- [ ] 12.3 Implementar lista de próximos agendamentos com horário, cliente, profissional e situação; verificar testes de lista preenchida e vazia
- [ ] 12.4 Aplicar isolamento por barbearia e autorização de proprietário; verificar testes com dois proprietários e com barbeiro tentando acessar
- [ ] 12.5 Implementar dashboard como primeira tela após a configuração inicial; verificar E2E de acesso e axe sem violação crítica

## 13. Notificações internas

- [ ] 13.1 Implementar notificação para os eventos de criação, cancelamento, sugestão, aceite, rejeição, expiração, início, fim e pagamento, com tipo, conteúdo, destinatário e barbearia; verificar testes de geração por tipo
- [ ] 13.2 Garantir idempotência por evento e destinatário; verificar teste de reprocessamento sem notificação duplicada
- [ ] 13.3 Implementar consulta por polling com contagem de não lidas, marcação individual e marcação de todas; verificar testes de consulta restrita ao destinatário e de marcação negada para outro usuário
- [ ] 13.4 Implementar reconciliação de eventos sem notificação; verificar testes de evento reconciliado e de evento já notificado sem duplicidade
- [ ] 13.5 Implementar central de notificações no frontend com polling e marcação de leitura; verificar E2E e axe sem violação crítica
- [ ] 13.6 Verificar ausência de canais externos; verificar teste de que nenhum envio de WhatsApp, e-mail ou SMS é disparado por evento

## 14. Integração e endurecimento

- [ ] 14.1 Executar testes de carga nos fluxos de agenda, dashboard e agendamento e comparar com as metas do TDD; verificar relatório com p95 e taxa de erro
- [ ] 14.2 Executar auditoria de acessibilidade em todas as telas principais e corrigir violações; verificar relatório axe e checklist manual de teclado
- [ ] 14.3 Executar revisão de segurança: isolamento por tenant, CSRF, autorização por recurso, segredos e dependências; verificar ausência de achados críticos ou altos
- [ ] 14.4 Executar restauração de backup em ambiente isolado e registrar RTO e RPO; verificar relatório de restauração com dados íntegros
- [ ] 14.5 Revisar logs e traces quanto a dados sensíveis e confirmar exportação OTLP configurável; verificar amostra de telemetria sem senha, cookie ou contato completo
- [ ] 14.6 Atualizar `docs/` com contratos finais, processos periódicos e limitações conhecidas; verificar que a documentação corresponde ao comportamento implementado

## 15. Produção

- [ ] 15.1 Registrar a decisão de hospedagem e provedor (ADR-028) antes do go-live; verificar ADR aprovado e ambiente de staging provisionado
- [ ] 15.2 Registrar a decisão de scheduler (ADR-027) e substituir o adaptador provisório; verificar ADR aprovado e processos executando com registro de execução
- [ ] 15.3 Aplicar migrations e seed de catálogo modelo em staging e executar smoke tests; verificar checklist de homologação assinado
- [ ] 15.4 Configurar gerenciador de segredos, backup, alertas e runbooks do ambiente escolhido; verificar teste de alerta e execução do runbook
- [ ] 15.5 Publicar em produção com aprovação manual e smoke tests; verificar revisão saudável e checklist de go-live concluído

## Workflow follow-up

- Revisar a change completa antes de iniciar a implementação, conforme solicitado.
- Arquivar a change após a implementação e a validação das capabilities.
