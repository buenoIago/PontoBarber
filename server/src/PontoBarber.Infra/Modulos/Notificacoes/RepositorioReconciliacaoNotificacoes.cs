using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Notificacoes;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Notificacoes;

/// <summary>
/// Reconcilia o outbox leve: lê eventos do histórico do agendamento e cria as
/// notificações ausentes. Reprocessamentos e execuções concorrentes não duplicam
/// efeitos por causa da chave (EventId, DestinatarioUsuarioId) e do lock de job.
/// </summary>
public sealed class RepositorioReconciliacaoNotificacoes(
    ContextoPontoBarber contexto,
    ServicoEscritaNotificacoes escrita) : IReconciliacaoNotificacoes
{
    private const string _chaveLock = "pb:job:ReconcileNotificationsJob";

    public async Task<int> ReconciliarAsync(DateTime agoraUtc, CancellationToken cancellationToken)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);
        var conexao = contexto.Database.GetDbConnection();
        var transacaoDb = transacao.GetDbTransaction();

        if (!await LockTransacionalPostgres.AdquirirAsync(
                conexao, transacaoDb, _chaveLock, cancellationToken))
        {
            await transacao.RollbackAsync(cancellationToken);
            return 0;
        }

        try
        {
            var eventos = await contexto.Set<HistoricoAgendamento>()
                .IgnoreQueryFilters()
                // Somente eventos ainda não reconciliados; sem este filtro a
                // consulta reaplica sempre os mesmos 500 primeiros registros.
                .Where(evento => !contexto.Set<Notificacao>()
                    .IgnoreQueryFilters()
                    .Any(notificacao => notificacao.EventId == evento.Id))
                .OrderBy(evento => evento.OcorridoEmUtc)
                .ThenBy(evento => evento.Id)
                .Take(500)
                .ToListAsync(cancellationToken);

            var reconciliados = 0;
            foreach (var evento in eventos)
            {
                var tipo = NotificacoesPorEvento.Mapear(evento.TipoEvento);
                if (tipo is null) continue;

                var novas = await escrita.AdicionarAsync(
                    evento.BarbeariaId,
                    evento.AgendamentoId,
                    evento.Id,
                    tipo.Value,
                    evento.OcorridoEmUtc,
                    cancellationToken);
                if (novas.Count > 0) reconciliados++;
            }

            await contexto.SaveChangesAsync(cancellationToken);
            await transacao.CommitAsync(cancellationToken);
            return reconciliados;
        }
        catch (DbUpdateException)
        {
            await transacao.RollbackAsync(cancellationToken);
            return 0;
        }
    }
}
