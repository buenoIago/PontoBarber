using PontoBarber.Aplicacao.Modulos.Jobs.Processos;

namespace PontoBarber.Aplicacao.Modulos.Jobs;

/// <summary>
/// Catálogo dos processos disponíveis para o adaptador de disparo e para a
/// API. Não conhece tecnologia de agendamento (ADR-027).
/// </summary>
public sealed class RegistroProcessosPeriodicos(
    ExpirarSugestoesProcesso expirarSugestoes,
    FecharVencidosProcesso fecharVencidos,
    ExpurgarIdempotenciaProcesso expurgarIdempotencia)
{
    public IReadOnlyList<IProcessoPeriodico> Processos { get; } =
        [expirarSugestoes, fecharVencidos, expurgarIdempotencia];

    public IProcessoPeriodico? Obter(string nome) =>
        Processos.FirstOrDefault(processo =>
            string.Equals(processo.Nome, nome, StringComparison.OrdinalIgnoreCase));
}
