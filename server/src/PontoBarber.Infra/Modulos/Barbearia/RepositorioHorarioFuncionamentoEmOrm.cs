using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Barbearia;

public sealed class RepositorioHorarioFuncionamentoEmOrm(ContextoPontoBarber contexto)
    : IHorarioFuncionamentoRepositorio
{
    public async Task<IReadOnlyList<HorarioFuncionamento>> ListarPorBarbeariaAsync(
        Guid barbeariaId,
        CancellationToken cancellationToken) =>
        // Predicato explícito por BarbeariaId: a configuração é sempre da rota.
        await contexto.Set<HorarioFuncionamento>()
            .IgnoreQueryFilters()
            .Where(horario => horario.BarbeariaId == barbeariaId)
            .ToListAsync(cancellationToken);

    public async Task AdicionarAsync(
        IReadOnlyList<HorarioFuncionamento> horarios,
        CancellationToken cancellationToken)
    {
        if (horarios.Count == 0)
            return;

        contexto.Set<HorarioFuncionamento>().AddRange(horarios);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) =>
        contexto.SaveChangesAsync(cancellationToken);
}
