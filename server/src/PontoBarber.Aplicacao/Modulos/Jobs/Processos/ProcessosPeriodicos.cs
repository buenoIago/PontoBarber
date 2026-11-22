using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Idempotencia;

namespace PontoBarber.Aplicacao.Modulos.Jobs.Processos;

/// <summary>Expira sugestões vencidas e registra as notificações em lote.</summary>
public sealed class ExpirarSugestoesProcesso(IAgendamentoRepositorio repositorio) : IProcessoPeriodico
{
    public const string NomeJob = "ExpireTimeSlotSuggestionsJob";

    public string Nome => NomeJob;

    public async Task<ResultadoProcessamento> ExecutarAsync(
        DateTime agoraUtc, CancellationToken cancellationToken)
    {
        var expiradas = await repositorio.ExpirarSugestoesAsync(agoraUtc, cancellationToken);
        return new ResultadoProcessamento(expiradas.Count);
    }
}

/// <summary>Fecha como não comparecimento os agendamentos vencidos há mais de 2 h.</summary>
public sealed class FecharVencidosProcesso(IAgendamentoRepositorio repositorio) : IProcessoPeriodico
{
    public const string NomeJob = "CloseOverdueAppointmentsJob";

    public string Nome => NomeJob;

    public async Task<ResultadoProcessamento> ExecutarAsync(
        DateTime agoraUtc, CancellationToken cancellationToken)
    {
        var quantidade = await repositorio.FecharVencidosAsync(agoraUtc, cancellationToken);
        return new ResultadoProcessamento(quantidade);
    }
}

/// <summary>Remove registros de idempotência vencidos por operação direta no banco.</summary>
public sealed class ExpurgarIdempotenciaProcesso(IRegistroIdempotenciaRepositorio repositorio) : IProcessoPeriodico
{
    public const string NomeJob = "PurgeIdempotencyRecordsJob";

    public string Nome => NomeJob;

    public async Task<ResultadoProcessamento> ExecutarAsync(
        DateTime agoraUtc, CancellationToken cancellationToken)
    {
        var quantidade = await repositorio.ExpurgarExpiradosAsync(agoraUtc, cancellationToken);
        return new ResultadoProcessamento(quantidade);
    }
}
