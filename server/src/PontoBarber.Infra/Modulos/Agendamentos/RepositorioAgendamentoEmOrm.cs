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

        var falhaLock = await AdquirirLocksAsync(conexao, transacaoDb, [chaveLock], cancellationToken);
        if (falhaLock is not null)
        {
            await transacao.RollbackAsync(cancellationToken);
            return falhaLock == ResultadoCicloVida.Indisponivel
                ? ResultadoConfirmacao.Indisponivel
                : ResultadoConfirmacao.Conflito;
        }

        try
        {
            if (await ExisteConflitoInternoAsync(
                    agendamento.BarbeariaId,
                    agendamento.ProfissionalId,
                    agendamento.IniciaEmUtc,
                    agendamento.TerminaEmUtc,
                    DateTime.UtcNow,
                    null,
                    cancellationToken))
                return ResultadoConfirmacao.Conflito;

            var agora = DateTime.UtcNow;
            contexto.Set<Agendamento>().Add(agendamento);
            RegistrarEvento(agendamento.BarbeariaId, agendamento.Id, TipoEventoHistorico.Criacao, null, agora,
                detalhes: $"Origem: {agendamento.Origem}. Snapshot: {agendamento.NomeServicoSnapshot}.");
            RegistrarEvento(agendamento.BarbeariaId, agendamento.Id, TipoEventoHistorico.Confirmacao, null, agora,
                novoInicioUtc: agendamento.IniciaEmUtc, detalhes: "Confirmação automática na criação.");

            await contexto.SaveChangesAsync(cancellationToken);
            await LiberarLocksAsync(conexao, transacaoDb, [chaveLock], cancellationToken);
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
        CancellationToken cancellationToken) =>
        await ExisteConflitoInternoAsync(
            barbeariaId, profissionalId, inicioUtc, fimUtc, DateTime.UtcNow, null, cancellationToken);

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

    public async Task<ResultadoCicloVida> CancelarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string chaveLock,
        string autorUsuarioId,
        string? motivo,
        DateTime agoraUtc,
        CancellationToken cancellationToken) =>
        await ExecutarAsync([chaveLock], async () =>
        {
            var agendamento = await ObterAsync(barbeariaId, agendamentoId, cancellationToken);
            if (agendamento is null) return ResultadoCicloVida.NaoEncontrado;
            if (agendamento.Situacao != SituacaoAgendamento.Confirmado)
                return ResultadoCicloVida.EstadoInvalido;
            if (agoraUtc >= agendamento.IniciaEmUtc)
                return ResultadoCicloVida.JaIniciado;

            var inicioAnterior = agendamento.IniciaEmUtc;
            agendamento.Cancelar(autorUsuarioId, motivo, agoraUtc);
            RegistrarEvento(barbeariaId, agendamentoId, TipoEventoHistorico.Cancelamento, autorUsuarioId,
                agoraUtc, inicioAnterior, detalhes: motivo);
            return ResultadoCicloVida.Sucesso;
        }, cancellationToken);

    public async Task<ResultadoCicloVida> RemarcarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        DateTime novoInicioUtc,
        IReadOnlyList<string> chavesLock,
        DateTime agoraUtc,
        CancellationToken cancellationToken) =>
        await ExecutarAsync(chavesLock, async () =>
        {
            var agendamento = await ObterAsync(barbeariaId, agendamentoId, cancellationToken);
            if (agendamento is null) return ResultadoCicloVida.NaoEncontrado;
            if (agendamento.Situacao != SituacaoAgendamento.Confirmado)
                return ResultadoCicloVida.EstadoInvalido;
            if (agoraUtc >= agendamento.IniciaEmUtc)
                return ResultadoCicloVida.JaIniciado;
            if (novoInicioUtc.Kind != DateTimeKind.Utc || novoInicioUtc <= agoraUtc)
                return ResultadoCicloVida.EstadoInvalido;

            var inicioAnterior = agendamento.IniciaEmUtc;
            var novoFim = novoInicioUtc.AddMinutes(agendamento.DuracaoSnapshot);
            if (await ExisteConflitoInternoAsync(
                    barbeariaId, agendamento.ProfissionalId, novoInicioUtc, novoFim,
                    agoraUtc, agendamentoId, cancellationToken))
                return ResultadoCicloVida.Conflito;

            agendamento.Remarcar(novoInicioUtc, agoraUtc);
            RegistrarEvento(barbeariaId, agendamentoId, TipoEventoHistorico.Remarcacao, null, agoraUtc,
                inicioAnterior, novoInicioUtc);
            return ResultadoCicloVida.Sucesso;
        }, cancellationToken);

    public async Task<ResultadoCicloVida> SugerirAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        DateTime inicioSugeridoUtc,
        string motivo,
        string autorUsuarioId,
        string chaveLock,
        DateTime agoraUtc,
        CancellationToken cancellationToken) =>
        await ExecutarAsync([chaveLock], async () =>
        {
            var agendamento = await ObterAsync(barbeariaId, agendamentoId, cancellationToken);
            if (agendamento is null) return ResultadoCicloVida.NaoEncontrado;
            if (agendamento.Situacao != SituacaoAgendamento.Confirmado)
                return ResultadoCicloVida.EstadoInvalido;
            if (agoraUtc >= agendamento.IniciaEmUtc)
                return ResultadoCicloVida.JaIniciado;
            if (inicioSugeridoUtc.Kind != DateTimeKind.Utc || inicioSugeridoUtc <= agoraUtc)
                return ResultadoCicloVida.EstadoInvalido;
            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoCicloVida.EstadoInvalido;

            var fimSugerido = inicioSugeridoUtc.AddMinutes(agendamento.DuracaoSnapshot);
            if (await ExisteConflitoInternoAsync(
                    barbeariaId, agendamento.ProfissionalId, inicioSugeridoUtc, fimSugerido,
                    agoraUtc, agendamentoId, cancellationToken))
                return ResultadoCicloVida.Conflito;

            var inicioAnterior = agendamento.IniciaEmUtc;
            agendamento.Cancelar(autorUsuarioId, motivo, agoraUtc);
            var sugestao = SugestaoHorario.Criar(
                barbeariaId, agendamentoId, agendamento.ProfissionalId, inicioSugeridoUtc,
                agendamento.DuracaoSnapshot, motivo, autorUsuarioId, agoraUtc);
            contexto.Set<SugestaoHorario>().Add(sugestao);

            RegistrarEvento(barbeariaId, agendamentoId, TipoEventoHistorico.Cancelamento, autorUsuarioId,
                agoraUtc, inicioAnterior, detalhes: motivo);
            RegistrarEvento(barbeariaId, agendamentoId, TipoEventoHistorico.SugestaoCriada, autorUsuarioId,
                agoraUtc, detalhes: $"Válida até {sugestao.ExpiraEmUtc:O}. Motivo: {motivo}");
            return ResultadoCicloVida.Sucesso;
        }, cancellationToken);

    public async Task<ResultadoCicloVida> ResponderSugestaoAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        Guid sugestaoId,
        bool aceitar,
        IReadOnlyList<string> chavesLock,
        DateTime agoraUtc,
        CancellationToken cancellationToken) =>
        await ExecutarAsync(chavesLock, async () =>
        {
            var sugestao = await contexto.Set<SugestaoHorario>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.Id == sugestaoId &&
                                              item.AgendamentoId == agendamentoId &&
                                              item.BarbeariaId == barbeariaId,
                    cancellationToken);
            if (sugestao is null) return ResultadoCicloVida.NaoEncontrado;
            if (sugestao.Situacao != SituacaoSugestao.Pendente)
                return ResultadoCicloVida.EstadoInvalido;
            if (sugestao.EstaExpirada(agoraUtc))
                return ResultadoCicloVida.EstadoInvalido;

            var agendamento = await ObterAsync(barbeariaId, agendamentoId, cancellationToken);
            if (agendamento is null) return ResultadoCicloVida.NaoEncontrado;

            var inicioAnterior = agendamento.IniciaEmUtc;
            if (aceitar)
            {
                if (await ExisteConflitoInternoAsync(
                        barbeariaId, agendamento.ProfissionalId, sugestao.InicioSugeridoUtc,
                        sugestao.FimSugeridoUtc, agoraUtc, agendamentoId, cancellationToken))
                    return ResultadoCicloVida.Conflito;

                agendamento.Reativar(sugestao.InicioSugeridoUtc, agoraUtc);
                sugestao.Aceitar(agoraUtc);
                RegistrarEvento(barbeariaId, agendamentoId, TipoEventoHistorico.SugestaoAceita,
                    sugestao.AutorUsuarioId, agoraUtc, inicioAnterior, sugestao.InicioSugeridoUtc);
            }
            else
            {
                sugestao.Rejeitar(agoraUtc);
                RegistrarEvento(barbeariaId, agendamentoId, TipoEventoHistorico.SugestaoRejeitada,
                    sugestao.AutorUsuarioId, agoraUtc, detalhes: "Sugestão rejeitada pelo cliente.");
            }

            return ResultadoCicloVida.Sucesso;
        }, cancellationToken);

    public async Task<int> ExpirarSugestoesAsync(DateTime agoraUtc, CancellationToken cancellationToken)
    {
        var expiradas = await contexto.Set<SugestaoHorario>()
            .IgnoreQueryFilters()
            .Where(sugestao => sugestao.Situacao == SituacaoSugestao.Pendente &&
                               sugestao.ExpiraEmUtc <= agoraUtc)
            .ToListAsync(cancellationToken);
        if (expiradas.Count == 0)
            return 0;

        foreach (var sugestao in expiradas)
        {
            sugestao.Expirar(agoraUtc);
            RegistrarEvento(sugestao.BarbeariaId, sugestao.AgendamentoId, TipoEventoHistorico.SugestaoExpirada,
                sugestao.AutorUsuarioId, agoraUtc, detalhes: "Reserva liberada por expiração.");
        }

        await contexto.SaveChangesAsync(cancellationToken);
        return expiradas.Count;
    }

    public async Task<IReadOnlyList<HistoricoAgendamento>> ListarHistoricoAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        CancellationToken cancellationToken) =>
        await contexto.Set<HistoricoAgendamento>()
            .IgnoreQueryFilters()
            .Where(evento => evento.BarbeariaId == barbeariaId && evento.AgendamentoId == agendamentoId)
            .OrderBy(evento => evento.OcorridoEmUtc)
            .ThenBy(evento => evento.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SugestaoHorario>> ListarSugestoesAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        CancellationToken cancellationToken) =>
        await contexto.Set<SugestaoHorario>()
            .IgnoreQueryFilters()
            .Where(sugestao => sugestao.BarbeariaId == barbeariaId && sugestao.AgendamentoId == agendamentoId)
            .OrderByDescending(sugestao => sugestao.ExpiraEmUtc)
            .ToListAsync(cancellationToken);

    public Task<SugestaoHorario?> ObterSugestaoAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        Guid sugestaoId,
        CancellationToken cancellationToken) =>
        contexto.Set<SugestaoHorario>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(sugestao => sugestao.Id == sugestaoId &&
                                              sugestao.AgendamentoId == agendamentoId &&
                                              sugestao.BarbeariaId == barbeariaId,
                cancellationToken);

    private Task<Agendamento?> ObterAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        CancellationToken cancellationToken) =>
        contexto.Set<Agendamento>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                agendamento => agendamento.Id == agendamentoId && agendamento.BarbeariaId == barbeariaId,
                cancellationToken);

    private void RegistrarEvento(
        Guid barbeariaId,
        Guid agendamentoId,
        TipoEventoHistorico tipo,
        string? autor,
        DateTime ocorridoEmUtc,
        DateTime? inicioAnterior = null,
        DateTime? novoInicioUtc = null,
        string? detalhes = null) =>
        contexto.Set<HistoricoAgendamento>().Add(HistoricoAgendamento.Criar(
            barbeariaId, agendamentoId, tipo, autor, ocorridoEmUtc, inicioAnterior, novoInicioUtc, detalhes));

    private async Task<bool> ExisteConflitoInternoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        DateTime inicioUtc,
        DateTime fimUtc,
        DateTime agoraUtc,
        Guid? ignorarAgendamentoId,
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

        if (await contexto.Set<Agendamento>()
                .IgnoreQueryFilters()
                .AnyAsync(agendamento => agendamento.BarbeariaId == barbeariaId &&
                                         agendamento.ProfissionalId == profissionalId &&
                                         agendamento.Situacao == SituacaoAgendamento.Confirmado &&
                                         agendamento.Id != ignorarAgendamentoId &&
                                         agendamento.IniciaEmUtc < fimUtc &&
                                         agendamento.TerminaEmUtc > inicioUtc,
                          cancellationToken))
            return true;

        // Reserva temporária: uma sugestão pendente e não vencida ocupa o
        // horário. Sugestões vencidas não bloqueiam mesmo se o job atrasou.
        return await contexto.Set<SugestaoHorario>()
            .IgnoreQueryFilters()
            .AnyAsync(sugestao => sugestao.BarbeariaId == barbeariaId &&
                                  sugestao.ProfissionalId == profissionalId &&
                                  sugestao.AgendamentoId != ignorarAgendamentoId &&
                                  sugestao.Situacao == SituacaoSugestao.Pendente &&
                                  sugestao.ExpiraEmUtc > agoraUtc &&
                                  sugestao.InicioSugeridoUtc < fimUtc &&
                                  sugestao.FimSugeridoUtc > inicioUtc,
                      cancellationToken);
    }

    private async Task<ResultadoCicloVida> ExecutarAsync(
        IReadOnlyList<string> chavesLock,
        Func<Task<ResultadoCicloVida>> operacao,
        CancellationToken cancellationToken)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);
        var conexao = (SqlConnection)contexto.Database.GetDbConnection();
        var transacaoDb = (SqlTransaction)transacao.GetDbTransaction();

        var falhaLock = await AdquirirLocksAsync(conexao, transacaoDb, chavesLock, cancellationToken);
        if (falhaLock is not null)
        {
            await transacao.RollbackAsync(cancellationToken);
            return falhaLock.Value;
        }

        try
        {
            var resultado = await operacao();
            if (resultado != ResultadoCicloVida.Sucesso)
            {
                await transacao.RollbackAsync(cancellationToken);
                return resultado;
            }

            await contexto.SaveChangesAsync(cancellationToken);
            await LiberarLocksAsync(conexao, transacaoDb, chavesLock, cancellationToken);
            await transacao.CommitAsync(cancellationToken);
            return resultado;
        }
        catch (DbUpdateException)
        {
            await transacao.RollbackAsync(cancellationToken);
            return ResultadoCicloVida.Conflito;
        }
    }

    private static async Task<ResultadoCicloVida?> AdquirirLocksAsync(
        SqlConnection conexao,
        SqlTransaction transacao,
        IEnumerable<string> chavesLock,
        CancellationToken cancellationToken)
    {
        // Ordem determinística evita deadlock quando a operação envolve
        // duas datas (remarcação entre dias diferentes).
        foreach (var chave in chavesLock.Distinct(StringComparer.Ordinal)
                     .OrderBy(chave => chave, StringComparer.Ordinal))
        {
            using var comando = conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.CommandType = CommandType.StoredProcedure;
            comando.CommandText = "sp_getapplock";
            comando.Parameters.AddWithValue("@Resource", chave);
            comando.Parameters.AddWithValue("@LockMode", "Exclusive");
            comando.Parameters.AddWithValue("@LockOwner", "Transaction");
            comando.Parameters.AddWithValue("@LockTimeout", 5000);
            var resultado = new SqlParameter("@resultado", SqlDbType.Int)
            {
                Direction = ParameterDirection.ReturnValue
            };
            comando.Parameters.Add(resultado);
            await comando.ExecuteNonQueryAsync(cancellationToken);

            var codigo = (int)resultado.Value!;
            if (codigo < 0)
                return codigo == -1 ? ResultadoCicloVida.Indisponivel : ResultadoCicloVida.Conflito;
        }

        return null;
    }

    private static async Task LiberarLocksAsync(
        SqlConnection conexao,
        SqlTransaction transacao,
        IEnumerable<string> chavesLock,
        CancellationToken cancellationToken)
    {
        foreach (var chave in chavesLock.Distinct(StringComparer.Ordinal)
                     .OrderByDescending(chave => chave, StringComparer.Ordinal))
        {
            using var comando = conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.CommandType = CommandType.StoredProcedure;
            comando.CommandText = "sp_releaseapplock";
            comando.Parameters.AddWithValue("@Resource", chave);
            comando.Parameters.AddWithValue("@LockOwner", "Transaction");
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
