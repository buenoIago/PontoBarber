namespace PontoBarber.Dominio.Modulos.Agendamentos;

/// <summary>Resultado da confirmação transacional de um agendamento.</summary>
public enum ResultadoConfirmacao
{
    Sucesso,
    Conflito,
    Indisponivel
}

/// <summary>Porta de persistência e reserva atômica de agendamentos.</summary>
public interface IAgendamentoRepositorio
{
    Task<ResultadoConfirmacao> ConfirmarAsync(
        Agendamento agendamento,
        string chaveLock,
        CancellationToken cancellationToken);

    Task<bool> ExisteConflitoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        DateTime inicioUtc,
        DateTime fimUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Agendamento>> ListarAtivosPorPeriodoAsync(
        Guid barbeariaId,
        IReadOnlyCollection<Guid> profissionalIds,
        DateTime inicioUtc,
        DateTime fimUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Agendamento>> ListarPorBarbeariaAsync(
        Guid barbeariaId,
        DateTime inicioUtc,
        DateTime fimUtc,
        Guid? profissionalId,
        string? clienteId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Agendamento>> ListarDoClienteAsync(
        string clienteId,
        DateTime inicioUtc,
        DateTime fimUtc,
        CancellationToken cancellationToken);

    Task<Agendamento?> ObterPorIdAsync(Guid barbeariaId, Guid agendamentoId, CancellationToken cancellationToken);
}
