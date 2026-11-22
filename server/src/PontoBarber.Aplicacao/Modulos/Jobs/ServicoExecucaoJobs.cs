using PontoBarber.Dominio.Modulos.Jobs;

namespace PontoBarber.Aplicacao.Modulos.Jobs;

/// <summary>
/// Executa processos periódicos e registra início, conclusão ou falha em
/// <c>ExecucoesJob</c>. A falha do processo nunca impede o registro do
/// resultado, e uma falha no registro nunca é confundida com sucesso.
/// </summary>
public sealed class ServicoExecucaoJobs(IExecucaoJobRepositorio repositorio)
{
    public async Task<ResultadoProcessamento> ExecutarAsync(
        IProcessoPeriodico processo,
        CancellationToken cancellationToken)
    {
        var iniciadoEmUtc = DateTime.UtcNow;
        var execucao = ExecucaoJob.Iniciar(processo.Nome, iniciadoEmUtc);
        await repositorio.IniciarAsync(execucao, cancellationToken);

        ResultadoProcessamento resultado;
        try
        {
            resultado = await processo.ExecutarAsync(iniciadoEmUtc, cancellationToken);
        }
        catch (Exception excecao)
        {
            await RegistrarFalhaSeguraAsync(execucao.Id, excecao, cancellationToken);
            throw;
        }

        // Se o registro da conclusão falhar, a exceção é propagada: a operação
        // não é reportada como concluída quando não há evidência persistida.
        await repositorio.ConcluirAsync(
            execucao.Id, resultado.Quantidade, DateTime.UtcNow, cancellationToken);
        return resultado;
    }

    private async Task RegistrarFalhaSeguraAsync(
        Guid execucaoId, Exception excecao, CancellationToken cancellationToken)
    {
        try
        {
            await repositorio.FalharAsync(
                execucaoId, ResumoSeguro(excecao), DateTime.UtcNow, cancellationToken);
        }
        catch
        {
            // Não mascarar a exceção original do processo.
        }
    }

    /// <summary>
    /// Registra somente o tipo da exceção: mensagens podem conter dados de
    /// negócio e não são necessárias para identificar a falha.
    /// </summary>
    private static string ResumoSeguro(Exception excecao) => excecao.GetType().Name;
}
