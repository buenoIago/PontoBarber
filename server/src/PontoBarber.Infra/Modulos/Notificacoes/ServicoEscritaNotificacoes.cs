using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Notificacoes;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Notificacoes;

/// <summary>
/// Escrita compartilhada de notificações na mesma transação do evento de
/// negócio. A unicidade por (EventId, DestinatarioUsuarioId) é verificada antes
/// do insert e reforçada pelo índice único do banco. Não faz SaveChanges: o
/// chamador persiste tudo em uma única transação.
/// </summary>
public sealed class ServicoEscritaNotificacoes(ContextoPontoBarber contexto)
{
    public async Task<IReadOnlyList<Notificacao>> AdicionarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        Guid eventId,
        TipoNotificacao tipo,
        DateTime agoraUtc,
        CancellationToken cancellationToken)
    {
        var agendamento = await contexto.Set<Agendamento>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == agendamentoId && item.BarbeariaId == barbeariaId,
                cancellationToken);
        if (agendamento is null) return [];

        var profissional = await contexto.Set<PerfilProfissional>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == agendamento.ProfissionalId, cancellationToken);

        var vinculoDono = await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.BarbeariaId == barbeariaId &&
                        item.Cargo == CargoProfissional.Proprietario,
                cancellationToken);

        string? donoUsuarioId = null;
        if (vinculoDono is not null)
        {
            var dono = await contexto.Set<PerfilProfissional>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == vinculoDono.ProfissionalId, cancellationToken);
            donoUsuarioId = dono?.UsuarioId;
        }

        var existentes = (await contexto.Set<Notificacao>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(item => item.EventId == eventId)
                .Select(item => item.DestinatarioUsuarioId)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var novas = NotificacoesPorEvento.Criar(
                barbeariaId,
                agendamentoId,
                eventId,
                tipo,
                agendamento.ClienteId,
                profissional?.UsuarioId,
                donoUsuarioId,
                agendamento.NomeServicoSnapshot,
                agoraUtc)
            .Where(item => !existentes.Contains(item.DestinatarioUsuarioId))
            .ToList();

        if (novas.Count > 0)
            contexto.Set<Notificacao>().AddRange(novas);

        return novas;
    }
}
