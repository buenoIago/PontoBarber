using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Profissionais;

public sealed class RepositorioBloqueioAgendaEmOrm(ContextoPontoBarber contexto)
    : IBloqueioAgendaRepositorio
{
    public async Task<IReadOnlyList<BloqueioAgenda>> ListarAsync(
        Guid barbeariaId,
        Guid profissionalId,
        CancellationToken cancellationToken) =>
        // Predicato explícito por BarbeariaId e ProfissionalId.
        await contexto.Set<BloqueioAgenda>()
            .IgnoreQueryFilters()
            .Where(bloqueio => bloqueio.BarbeariaId == barbeariaId && bloqueio.ProfissionalId == profissionalId)
            .OrderBy(bloqueio => bloqueio.IniciaEmUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BloqueioAgenda>> ListarPeriodoAsync(
        Guid barbeariaId,
        DateTime inicioUtc,
        DateTime fimUtc,
        CancellationToken cancellationToken) =>
        await contexto.Set<BloqueioAgenda>()
            .IgnoreQueryFilters()
            .Where(bloqueio => bloqueio.BarbeariaId == barbeariaId &&
                               bloqueio.IniciaEmUtc < fimUtc &&
                               bloqueio.TerminaEmUtc > inicioUtc)
            .ToListAsync(cancellationToken);

    public Task<BloqueioAgenda?> ObterPorIdAsync(
        Guid barbeariaId,
        Guid bloqueioId,
        CancellationToken cancellationToken) =>
        contexto.Set<BloqueioAgenda>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                bloqueio => bloqueio.Id == bloqueioId && bloqueio.BarbeariaId == barbeariaId,
                cancellationToken);

    public async Task AdicionarAsync(BloqueioAgenda bloqueio, CancellationToken cancellationToken)
    {
        contexto.Set<BloqueioAgenda>().Add(bloqueio);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverAsync(BloqueioAgenda bloqueio, CancellationToken cancellationToken)
    {
        contexto.Set<BloqueioAgenda>().Remove(bloqueio);
        await contexto.SaveChangesAsync(cancellationToken);
    }
}
