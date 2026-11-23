using PontoBarber.Dominio.Modulos.Agendamentos;

namespace PontoBarber.Dominio.Modulos.Dashboard;

/// <summary>Consultas agregadas e projetadas, sempre restritas à barbearia.</summary>
public interface IDashboardRepositorio
{
    Task<int> ContarAgendamentosAsync(
        Guid barbeariaId,
        IntervaloUtc periodo,
        IReadOnlyList<SituacaoAgendamento> situacoes,
        CancellationToken cancellationToken);

    Task<int> ContarCancelamentosAsync(
        Guid barbeariaId,
        IntervaloUtc periodo,
        CancellationToken cancellationToken);

    Task<decimal> SomarPagamentosAsync(
        Guid barbeariaId,
        IntervaloUtc periodo,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProximoAgendamento>> ListarProximosAsync(
        Guid barbeariaId,
        DateTime agoraUtc,
        int limite,
        CancellationToken cancellationToken);
}
