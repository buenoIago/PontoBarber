# Processos periódicos

Este documento descreve os processos periódicos do PontoBarber, seus gatilhos,
o registro de execução e as limitações da infraestrutura provisória enquanto o
**ADR-027 (estratégia de execução de processos periódicos)** permanece pendente.

## Processos documentados

A TDD §12.3 lista seis rotinas. As quatro abaixo são as que compõem o escopo
desta etapa (tasks.md 9.3 a 9.6 e a reconciliação prevista na seção 13):

| Processo | Finalidade | Gatilho previsto (TDD §12.3) | Situação nesta etapa |
| --- | --- | --- | --- |
| `ExpireTimeSlotSuggestionsJob` | Marca sugestões vencidas como `Expirada`, libera a reserva temporária e notifica cliente, profissional e proprietário | Cron a cada 5 minutos (UTC) | Implementado |
| `CloseOverdueAppointmentsJob` | Fecha como `NaoCompareceu` agendamentos confirmados cujo início ocorreu há mais de 2 horas | Cron a cada 15 minutos (UTC) | Implementado |
| `PurgeIdempotencyRecordsJob` | Remove registros de idempotência cuja expiração já passou | Cron diário às 03:00 UTC | Implementado |
| `ReconcileNotificationsJob` | Reprocessa eventos que ainda não geraram notificação, por `EventId` | Cron a cada 10 minutos (UTC) | Implementado na Seção 13 |

As rotinas `WarmDashboardJob` e `HealthCheckJob` também constam da TDD §12.3,
mas não fazem parte das quatro rotinas exigidas por esta etapa e não foram
introduzidas apenas para completar a lista.

### Detalhes dos processos implementados

- **Expiração de sugestões:** `UPDATE` condicional sobre sugestões `Pendente`
  com `ExpiraEmUtc <= agora`, em lotes de até 500. A validade de 24 horas
  também é verificada no aceite, independentemente do job: uma sugestão
  vencida não pode ser aceita mesmo que a execução periódica esteja atrasada.
  Reservas vencidas deixam de ser consideradas conflito imediatamente.
- **Não comparecimento:** somente agendamentos `Confirmado` com início há mais
  de 2 horas são elegíveis. Cancelados, futuros e em outros estados não são
  alterados. A transição de domínio e o evento `NaoComparecimento` no histórico
  são gravados na mesma transação, com autor técnico `sistema`.
- **Expurgo de idempotência:** exclusão direta no banco
  (`DELETE ... WHERE ExpiraEmUtc <= @agora`), sem carregar registros em memória.
  A estrutura oficial é `RegistrosIdempotencia` (TDD §8.1/ADR-021); não existe
  outra estrutura equivalente no projeto.
- **Reconciliação de notificações:** o histórico do agendamento é o outbox
  leve. O job lê eventos em lotes de 500 e cria somente as notificações
  ausentes para cada destinatário, usando a chave `(EventId, DestinatarioUsuarioId)`.
  O `sp_getapplock` por job serializa execuções concorrentes; reprocessamentos
  não duplicam notificações nem efeitos de negócio.

## Contrato de execução

O contrato `IProcessoPeriodico` pertence à **Aplicação** e expõe somente `Nome`
e `ExecutarAsync(agoraUtc, cancellationToken)`, com `ResultadoProcessamento`
contendo a quantidade efetivamente processada. Nenhuma biblioteca de
agendamento é referenciada por Domínio ou Aplicação.

Cada execução é registrada em `ExecucoesJob`:

| Campo | Conteúdo |
| --- | --- |
| `Id` | Identificador da execução |
| `NomeJob` | Identificador do processo |
| `IniciadoEmUtc` | Início em UTC |
| `FinalizadoEmUtc` | Fim em UTC (somente quando concluída ou falha) |
| `Resultado` | `EmExecucao`, `Sucesso` ou `Falha` |
| `QuantidadeProcessados` | Registros efetivamente alterados/removidos |
| `Erro` | Somente o tipo da exceção em caso de falha; sem mensagens ou dados de negócio |

`ServicoExecucaoJobs` cria o registro no início e o conclui ou marca como falho
ao final. A falha do processo não impede o registro do resultado; uma falha no
registro da conclusão é propagada e nunca é reportada como sucesso. Execuções
incompletas permanecem como `EmExecucao` — nunca são gravadas como concluídas.

## Adaptador provisório de disparo

`DisparadorProvisorioJobs` (em **Infra**) é um `BackgroundService` que executa
os processos registrados em intervalos fixos configuráveis
(`Jobs:IntervaloMinutos`, padrão 5), criando um escopo de DI por tick. Usa
apenas recursos do .NET, sem biblioteca de scheduler.

**Não é um scheduler definitivo.** Não sobrevive a reinicializações, não
coordina a execução entre instâncias, não persiste atrasos e não oferece
garantia de continuidade enquanto a API estiver indisponível. A substituição
do adaptador não altera as regras de negócio: basta registrar outro mecanismo
que chame `ServicoExecucaoJobs` com os processos de `RegistroProcessosPeriodicos`.

## Idempotência, concorrência e falhas

- Todos os processos usam transações curtas e lotes de até 500 registros.
- Expiração e fechamento adquirem `sp_getapplock` por processo no SQL Server;
  cancelamentos e aceites usam o lock por profissional/data da seção 7.
  Não há locks em memória.
- Notificações são gravadas na mesma transação da expiração, com chave única
  por `(EventId, DestinatarioUsuarioId)`; reprocessamentos não duplicam
  notificações, eventos de histórico nem efeitos de negócio.
- Uma falha parcial permite reprocessamento seguro: o estado de cada registro
  é revalidado antes da transição e a quantidade registrada é a efetivamente
  processada naquela execução.

## Limitações enquanto o ADR-027 estiver pendente

A garantia de que a sugestão será expirada em até 24 horas **não pode ser
afirmada**: depende de a API estar ativa e de o adaptador provisório executar.
A regra de negócio permanece protegida pelo bloqueio de aceite vencido e pela
exclusão de reservas vencidas dos conflitos, que não dependem do job. Para
durabilidade e coordenação entre instâncias é necessário aprovar o ADR-027 e
substituir o adaptador, conforme a task 15.2 do `tasks.md`.
