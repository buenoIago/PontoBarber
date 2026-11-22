using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Profissionais;

public sealed class RepositorioDisponibilidadeSemanalEmOrm(ContextoPontoBarber contexto)
    : IDisponibilidadeSemanalRepositorio
{
    public async Task<IReadOnlyList<DisponibilidadeSemanal>> ListarAsync(
        Guid barbeariaId,
        Guid profissionalId,
        CancellationToken cancellationToken) =>
        // Predicato explícito por BarbeariaId e ProfissionalId.
        await contexto.Set<DisponibilidadeSemanal>()
            .IgnoreQueryFilters()
            .Where(janela => janela.BarbeariaId == barbeariaId && janela.ProfissionalId == profissionalId)
            .ToListAsync(cancellationToken);

    public async Task AdicionarAsync(
        IReadOnlyList<DisponibilidadeSemanal> janelas,
        CancellationToken cancellationToken)
    {
        if (janelas.Count == 0)
            return;

        contexto.Set<DisponibilidadeSemanal>().AddRange(janelas);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverAsync(
        IReadOnlyList<DisponibilidadeSemanal> janelas,
        CancellationToken cancellationToken)
    {
        if (janelas.Count == 0)
            return;

        contexto.Set<DisponibilidadeSemanal>().RemoveRange(janelas);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) =>
        contexto.SaveChangesAsync(cancellationToken);
}
