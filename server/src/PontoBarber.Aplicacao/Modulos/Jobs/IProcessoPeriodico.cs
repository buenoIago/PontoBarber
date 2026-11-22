namespace PontoBarber.Aplicacao.Modulos.Jobs;

/// <summary>
/// Contrato de execução de processos periódicos pertencente à Aplicação.
/// Nenhuma biblioteca de agendamento é referenciada aqui (ADR-027).
/// </summary>
public interface IProcessoPeriodico
{
    string Nome { get; }

    Task<ResultadoProcessamento> ExecutarAsync(DateTime agoraUtc, CancellationToken cancellationToken);
}

/// <summary>Resultado simples e consistente de um processamento em lote.</summary>
public sealed record ResultadoProcessamento(int Quantidade)
{
    public static ResultadoProcessamento Vazio => new(0);
}
