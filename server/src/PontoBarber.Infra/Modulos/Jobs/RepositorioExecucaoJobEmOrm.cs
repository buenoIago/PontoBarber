using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Dominio.Modulos.Jobs;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Jobs;

public sealed class RepositorioExecucaoJobEmOrm(ContextoPontoBarber contexto) : IExecucaoJobRepositorio
{
    public async Task IniciarAsync(ExecucaoJob execucao, CancellationToken cancellationToken)
    {
        contexto.Set<ExecucaoJob>().Add(execucao);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ConcluirAsync(
        Guid execucaoId, int quantidade, DateTime finalizadoEmUtc, CancellationToken cancellationToken)
    {
        var execucao = await ObterAsync(execucaoId, cancellationToken);
        execucao.Concluir(quantidade, finalizadoEmUtc);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task FalharAsync(
        Guid execucaoId, string erroResumo, DateTime finalizadoEmUtc, CancellationToken cancellationToken)
    {
        var execucao = await ObterAsync(execucaoId, cancellationToken);
        execucao.Falhar(erroResumo, finalizadoEmUtc);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task<ExecucaoJob> ObterAsync(Guid execucaoId, CancellationToken cancellationToken) =>
        await contexto.Set<ExecucaoJob>().SingleAsync(
            execucao => execucao.Id == execucaoId, cancellationToken);
}
