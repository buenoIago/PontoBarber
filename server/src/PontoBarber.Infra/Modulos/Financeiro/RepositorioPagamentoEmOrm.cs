using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Dominio.Modulos.Idempotencia;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Financeiro;

public sealed class RepositorioPagamentoEmOrm(ContextoPontoBarber contexto) : IPagamentoRepositorio
{
    public Task<Pagamento?> ObterPorAgendamentoAsync(
        Guid barbeariaId, Guid agendamentoId, CancellationToken cancellationToken) =>
        contexto.Set<Pagamento>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                pagamento => pagamento.BarbeariaId == barbeariaId &&
                             pagamento.AgendamentoId == agendamentoId,
                cancellationToken);

    public Task<RegistroIdempotencia?> ObterIdempotenciaAsync(
        string usuarioId, string chave, CancellationToken cancellationToken) =>
        contexto.Set<RegistroIdempotencia>()
            .SingleOrDefaultAsync(
                registro => registro.UsuarioId == usuarioId && registro.Chave == chave,
                cancellationToken);

    public async Task RemoverIdempotenciaExpiradaAsync(
        RegistroIdempotencia registro, CancellationToken cancellationToken)
    {
        contexto.Set<RegistroIdempotencia>().Remove(registro);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<ResultadoPagamento> RegistrarAsync(
        Pagamento pagamento,
        RegistroIdempotencia registro,
        string autorUsuarioId,
        CancellationToken cancellationToken)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            contexto.Set<Pagamento>().Add(pagamento);
            contexto.Set<RegistroIdempotencia>().Add(registro);
            contexto.Set<HistoricoAgendamento>().Add(HistoricoAgendamento.Criar(
                pagamento.BarbeariaId,
                pagamento.AgendamentoId,
                TipoEventoHistorico.PagamentoRegistrado,
                autorUsuarioId,
                pagamento.RegistradoEmUtc,
                detalhes: $"Valor {pagamento.Valor:F2}; forma {pagamento.Metodo}."));
            await contexto.SaveChangesAsync(cancellationToken);
            await transacao.CommitAsync(cancellationToken);
            return ResultadoPagamento.Sucesso;
        }
        catch (DbUpdateException)
        {
            await transacao.RollbackAsync(cancellationToken);
            return ResultadoPagamento.Duplicado;
        }
    }
}
