using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Agendamentos;

public sealed class RepositorioAgendamentoEmOrm(ContextoPontoBarber contexto) : IAgendamentoRepositorio
{
    public async Task<ResultadoConfirmacao> ConfirmarAsync(
        Agendamento agendamento,
        string chaveLock,
        CancellationToken cancellationToken)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);

        var conexao = (SqlConnection)contexto.Database.GetDbConnection();
        var transacaoDb = (SqlTransaction)transacao.GetDbTransaction();

        using var comandoLock = conexao.CreateCommand();
        comandoLock.Transaction = transacaoDb;
        comandoLock.CommandType = CommandType.StoredProcedure;
        comandoLock.CommandText = "sp_getapplock";
        comandoLock.Parameters.AddWithValue("@Resource", chaveLock);
        comandoLock.Parameters.AddWithValue("@LockMode", "Exclusive");
        comandoLock.Parameters.AddWithValue("@LockOwner", "Transaction");
        comandoLock.Parameters.AddWithValue("@LockTimeout", 5000);
        var resultado = new SqlParameter("@resultado", SqlDbType.Int)
        {
            Direction = ParameterDirection.ReturnValue
        };
        comandoLock.Parameters.Add(resultado);
        await comandoLock.ExecuteNonQueryAsync(cancellationToken);

        var codigoLock = (int)resultado.Value!;
        if (codigoLock < 0)
        {
            await transacao.RollbackAsync(cancellationToken);

            // sp_getapplock: -1 = timeout, -2 = cancelado, -3 = vítima de
            // deadlock, -999 = erro de parâmetro. Apenas o timeout é uma
            // indisponibilidade temporária passível de nova tentativa.
            return codigoLock == -1
                ? ResultadoConfirmacao.Indisponivel
                : ResultadoConfirmacao.Conflito;
        }

        try
        {
            if (await ExisteConflitoAsync(
                    agendamento.BarbeariaId,
                    agendamento.ProfissionalId,
                    agendamento.IniciaEmUtc,
                    agendamento.TerminaEmUtc,
                    cancellationToken))
                return ResultadoConfirmacao.Conflito;

            contexto.Set<Agendamento>().Add(agendamento);
            await contexto.SaveChangesAsync(cancellationToken);
            using var comandoRelease = conexao.CreateCommand();
            comandoRelease.Transaction = transacaoDb;
            comandoRelease.CommandType = CommandType.StoredProcedure;
            comandoRelease.CommandText = "sp_releaseapplock";
            comandoRelease.Parameters.AddWithValue("@Resource", chaveLock);
            comandoRelease.Parameters.AddWithValue("@LockOwner", "Transaction");
            await comandoRelease.ExecuteNonQueryAsync(cancellationToken);
            await transacao.CommitAsync(cancellationToken);
            return ResultadoConfirmacao.Sucesso;
        }
        catch (DbUpdateException)
        {
            await transacao.RollbackAsync(cancellationToken);
            return ResultadoConfirmacao.Conflito;
        }
    }

    public async Task<bool> ExisteConflitoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        DateTime inicioUtc,
        DateTime fimUtc,
        CancellationToken cancellationToken)
    {
        if (await contexto.Set<BloqueioAgenda>()
                .IgnoreQueryFilters()
                .AnyAsync(bloqueio => bloqueio.BarbeariaId == barbeariaId &&
                                      bloqueio.ProfissionalId == profissionalId &&
                                      bloqueio.IniciaEmUtc < fimUtc &&
                                      bloqueio.TerminaEmUtc > inicioUtc,
                          cancellationToken))
            return true;

        return await contexto.Set<Agendamento>()
            .IgnoreQueryFilters()
            .AnyAsync(agendamento => agendamento.BarbeariaId == barbeariaId &&
                                     agendamento.ProfissionalId == profissionalId &&
                                     agendamento.Situacao == SituacaoAgendamento.Confirmado &&
                                     agendamento.IniciaEmUtc < fimUtc &&
                                     agendamento.TerminaEmUtc > inicioUtc,
                     cancellationToken);
    }

    public async Task<IReadOnlyList<Agendamento>> ListarAtivosPorPeriodoAsync(
        Guid barbeariaId,
        IReadOnlyCollection<Guid> profissionalIds,
        DateTime inicioUtc,
        DateTime fimUtc,
        CancellationToken cancellationToken) =>
        await contexto.Set<Agendamento>()
            .IgnoreQueryFilters()
            .Where(agendamento => agendamento.BarbeariaId == barbeariaId &&
                                  agendamento.Situacao == SituacaoAgendamento.Confirmado &&
                                  profissionalIds.Contains(agendamento.ProfissionalId) &&
                                  agendamento.IniciaEmUtc < fimUtc &&
                                  agendamento.TerminaEmUtc > inicioUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Agendamento>> ListarPorBarbeariaAsync(
        Guid barbeariaId,
        DateTime inicioUtc,
        DateTime fimUtc,
        Guid? profissionalId,
        string? clienteId,
        CancellationToken cancellationToken) =>
        await contexto.Set<Agendamento>()
            .IgnoreQueryFilters()
            .Where(agendamento => agendamento.BarbeariaId == barbeariaId &&
                                  agendamento.IniciaEmUtc >= inicioUtc &&
                                  agendamento.IniciaEmUtc < fimUtc &&
                                  (!profissionalId.HasValue || agendamento.ProfissionalId == profissionalId.Value) &&
                                  (clienteId == null || agendamento.ClienteId == clienteId))
            .OrderBy(agendamento => agendamento.IniciaEmUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Agendamento>> ListarDoClienteAsync(
        string clienteId,
        DateTime inicioUtc,
        DateTime fimUtc,
        CancellationToken cancellationToken) =>
        await contexto.Set<Agendamento>()
            .IgnoreQueryFilters()
            .Where(agendamento => agendamento.ClienteId == clienteId &&
                                  agendamento.IniciaEmUtc >= inicioUtc &&
                                  agendamento.IniciaEmUtc < fimUtc)
            .OrderBy(agendamento => agendamento.IniciaEmUtc)
            .ToListAsync(cancellationToken);

    public Task<Agendamento?> ObterPorIdAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        CancellationToken cancellationToken) =>
        contexto.Set<Agendamento>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                agendamento => agendamento.Id == agendamentoId && agendamento.BarbeariaId == barbeariaId,
                cancellationToken);
}
