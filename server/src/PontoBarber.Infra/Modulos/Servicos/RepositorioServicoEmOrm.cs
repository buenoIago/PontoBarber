using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Servicos;

public sealed class RepositorioServicoEmOrm(ContextoPontoBarber contexto) : IServicoRepositorio
{
    public async Task<IReadOnlyList<Servico>> ListarPorBarbeariaAsync(
        Guid barbeariaId,
        CancellationToken cancellationToken)
    {
        // O seed pode ocorrer fora de uma requisição com tenant ativo. O
        // predicato por BarbeariaId é explícito e garante o isolamento.
        return await contexto.Set<Servico>()
            .IgnoreQueryFilters()
            .Where(servico => servico.BarbeariaId == barbeariaId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Servico>> ListarAtivosPorBarbeariaAsync(
        Guid barbeariaId,
        CancellationToken cancellationToken) =>
        await contexto.Set<Servico>()
            .IgnoreQueryFilters()
            .Where(servico => servico.BarbeariaId == barbeariaId && servico.Ativo)
            .ToListAsync(cancellationToken);

    public Task<Servico?> ObterPorIdAsync(
        Guid barbeariaId,
        Guid servicoId,
        CancellationToken cancellationToken) =>
        // Predicato explícito por BarbeariaId: um serviço de outro tenant nunca
        // é devolvido, mesmo que o identificador seja conhecido.
        contexto.Set<Servico>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                servico => servico.Id == servicoId && servico.BarbeariaId == barbeariaId,
                cancellationToken);

    public async Task AdicionarAsync(IReadOnlyList<Servico> servicos, CancellationToken cancellationToken)
    {
        if (servicos.Count == 0)
            return;

        contexto.Set<Servico>().AddRange(servicos);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Servico servico, CancellationToken cancellationToken) =>
        await contexto.SaveChangesAsync(cancellationToken);
}
