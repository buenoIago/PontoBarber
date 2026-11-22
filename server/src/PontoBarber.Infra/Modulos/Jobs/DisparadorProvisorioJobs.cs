using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PontoBarber.Aplicacao.Modulos.Jobs;

namespace PontoBarber.Infra.Modulos.Jobs;

/// <summary>
/// Adaptador provisório de disparo: executa os processos registrados em
/// intervalos fixos, em processo, usando apenas recursos do .NET.
/// <para>
/// Não é um scheduler definitivo (ADR-027 pendente): não sobrevive a
/// reinicializações, não coordena instâncias e não possui atraso persistido.
/// A segurança vem da idempotência dos processos e do registro em
/// <c>ExecucoesJob</c>. Substituir este adaptador não exige mudança nas regras
/// de negócio, apenas na infraestrutura de disparo.
/// </para>
/// </summary>
public sealed partial class DisparadorProvisorioJobs(
    IServiceScopeFactory escopoFabrica,
    IConfiguration configuracao,
    ILogger<DisparadorProvisorioJobs> logger) : BackgroundService
{
    private readonly TimeSpan _intervalo =
        TimeSpan.FromMinutes(Math.Max(1, configuracao.GetValue("Jobs:IntervaloMinutos", 5)));

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Falha no processo periódico {Processo}: {Tipo}. O registro de execução foi mantido.")]
    private static partial void LogFalha(ILogger logger, string processo, string tipo);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(_intervalo);

        while (!stoppingToken.IsCancellationRequested &&
               await temporizador.WaitForNextTickAsync(stoppingToken))
        {
            using var escopo = escopoFabrica.CreateScope();
            var registro = escopo.ServiceProvider.GetRequiredService<RegistroProcessosPeriodicos>();
            var servico = escopo.ServiceProvider.GetRequiredService<ServicoExecucaoJobs>();

            foreach (var processo in registro.Processos)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    await servico.ExecutarAsync(processo, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception excecao)
                {
                    LogFalha(logger, processo.Nome, excecao.GetType().Name);
                }
            }
        }
    }
}
