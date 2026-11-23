using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Dashboard;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Infra.Modulos.Dashboard;

public sealed class RepositorioDashboardEmOrm(ContextoPontoBarber contexto) : IDashboardRepositorio
{
    public Task<int> ContarAgendamentosAsync(
        Guid barbeariaId,
        IntervaloUtc periodo,
        IReadOnlyList<SituacaoAgendamento> situacoes,
        CancellationToken cancellationToken) =>
        contexto.Set<Agendamento>()
            .CountAsync(
                agendamento => agendamento.BarbeariaId == barbeariaId &&
                               agendamento.IniciaEmUtc >= periodo.InicioUtc &&
                               agendamento.IniciaEmUtc < periodo.FimUtc &&
                               situacoes.Contains(agendamento.Situacao),
                cancellationToken);

    public Task<int> ContarCancelamentosAsync(
        Guid barbeariaId,
        IntervaloUtc periodo,
        CancellationToken cancellationToken) =>
        contexto.Set<Agendamento>()
            .CountAsync(
                agendamento => agendamento.BarbeariaId == barbeariaId &&
                               agendamento.Situacao == SituacaoAgendamento.Cancelado &&
                               agendamento.CanceladoEmUtc >= periodo.InicioUtc &&
                               agendamento.CanceladoEmUtc < periodo.FimUtc,
                cancellationToken);

    public async Task<decimal> SomarPagamentosAsync(
        Guid barbeariaId,
        IntervaloUtc periodo,
        CancellationToken cancellationToken) =>
        await contexto.Set<Pagamento>()
            .Where(pagamento => pagamento.BarbeariaId == barbeariaId &&
                                pagamento.RegistradoEmUtc >= periodo.InicioUtc &&
                                pagamento.RegistradoEmUtc < periodo.FimUtc)
            .SumAsync(pagamento => (decimal?)pagamento.Valor, cancellationToken) ?? 0m;

    public async Task<IReadOnlyList<ProximoAgendamento>> ListarProximosAsync(
        Guid barbeariaId,
        DateTime agoraUtc,
        int limite,
        CancellationToken cancellationToken)
    {
        var futuros = contexto.Set<Agendamento>()
            .Where(agendamento => agendamento.BarbeariaId == barbeariaId &&
                                  agendamento.Situacao == SituacaoAgendamento.Confirmado &&
                                  agendamento.IniciaEmUtc > agoraUtc);

        var consulta = from agendamento in futuros
                       join cliente in contexto.Set<Usuario>()
                           on agendamento.ClienteId equals cliente.Id
                       join profissional in contexto.Set<PerfilProfissional>()
                           on agendamento.ProfissionalId equals profissional.Id
                       orderby agendamento.IniciaEmUtc
                       select new ProximoAgendamento(
                           agendamento.Id,
                           agendamento.IniciaEmUtc,
                           cliente.FullName,
                           profissional.NomeCompleto,
                           agendamento.Situacao);

        return await consulta.Take(limite).ToListAsync(cancellationToken);
    }
}
