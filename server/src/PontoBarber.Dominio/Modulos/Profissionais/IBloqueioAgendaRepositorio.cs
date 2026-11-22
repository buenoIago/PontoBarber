namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>Porta de persistência dos bloqueios pontuais da agenda.</summary>
public interface IBloqueioAgendaRepositorio
{
    Task<IReadOnlyList<BloqueioAgenda>> ListarAsync(
        Guid barbeariaId,
        Guid profissionalId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BloqueioAgenda>> ListarPeriodoAsync(
        Guid barbeariaId,
        DateTime inicioUtc,
        DateTime fimUtc,
        CancellationToken cancellationToken);

    Task<BloqueioAgenda?> ObterPorIdAsync(
        Guid barbeariaId,
        Guid bloqueioId,
        CancellationToken cancellationToken);

    Task AdicionarAsync(BloqueioAgenda bloqueio, CancellationToken cancellationToken);

    Task RemoverAsync(BloqueioAgenda bloqueio, CancellationToken cancellationToken);
}
