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

    Task<ResultadoCicloVida> CancelarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string chaveLock,
        string autorUsuarioId,
        string? motivo,
        DateTime agoraUtc,
        CancellationToken cancellationToken);

    Task<ResultadoCicloVida> IniciarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string chaveLock,
        string autorUsuarioId,
        DateTime agoraUtc,
        CancellationToken cancellationToken);

    Task<ResultadoCicloVida> ConcluirAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string chaveLock,
        string autorUsuarioId,
        DateTime agoraUtc,
        CancellationToken cancellationToken);

    Task<ResultadoCicloVida> RegistrarAusenciaAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string chaveLock,
        string autorUsuarioId,
        DateTime agoraUtc,
        CancellationToken cancellationToken);

    Task<ResultadoCicloVida> RemarcarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        DateTime novoInicioUtc,
        IReadOnlyList<string> chavesLock,
        DateTime agoraUtc,
        CancellationToken cancellationToken);

    Task<ResultadoCicloVida> SugerirAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        DateTime inicioSugeridoUtc,
        string motivo,
        string autorUsuarioId,
        string chaveLock,
        DateTime agoraUtc,
        CancellationToken cancellationToken);

    Task<ResultadoCicloVida> ResponderSugestaoAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        Guid sugestaoId,
        bool aceitar,
        IReadOnlyList<string> chavesLock,
        DateTime agoraUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marca como expiradas as sugestões vencidas e registra, na mesma
    /// transação, as notificações idempotentes por EventId. Idempotente.
    /// </summary>
    Task<IReadOnlyList<SugestaoExpiradaInfo>> ExpirarSugestoesAsync(
        DateTime agoraUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Fecha como não comparecimento os agendamentos confirmados vencidos há
    /// mais de 2 horas, em lotes, com evento de histórico na mesma transação.
    /// </summary>
    Task<int> FecharVencidosAsync(DateTime agoraUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<HistoricoAgendamento>> ListarHistoricoAsync(
        Guid barbeariaId, Guid agendamentoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SugestaoHorario>> ListarSugestoesAsync(
        Guid barbeariaId, Guid agendamentoId, CancellationToken cancellationToken);

    Task<SugestaoHorario?> ObterSugestaoAsync(
        Guid barbeariaId, Guid agendamentoId, Guid sugestaoId, CancellationToken cancellationToken);
}
