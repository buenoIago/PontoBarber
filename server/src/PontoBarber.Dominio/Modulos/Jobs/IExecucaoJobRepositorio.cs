namespace PontoBarber.Dominio.Modulos.Jobs;

/// <summary>Persistência do registro de execução dos processos periódicos.</summary>
public interface IExecucaoJobRepositorio
{
    Task IniciarAsync(ExecucaoJob execucao, CancellationToken cancellationToken);

    Task ConcluirAsync(Guid execucaoId, int quantidade, DateTime finalizadoEmUtc, CancellationToken cancellationToken);

    Task FalharAsync(Guid execucaoId, string erroResumo, DateTime finalizadoEmUtc, CancellationToken cancellationToken);
}
