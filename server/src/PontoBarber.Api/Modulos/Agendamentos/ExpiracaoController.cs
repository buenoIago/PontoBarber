using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Aplicacao.Modulos.Jobs;
using PontoBarber.Aplicacao.Modulos.Jobs.Processos;

namespace PontoBarber.Api.Modulos.Agendamentos;

/// <summary>
/// Acionamento manual dos processos periódicos. Toda execução passa pelo
/// serviço de registro em <c>ExecucoesJob</c>, seja disparada aqui ou pelo
/// adaptador provisório em Infra.
/// </summary>
[ApiController]
[Route("api/v1/jobs")]
public sealed class ExpiracaoController(
    RegistroProcessosPeriodicos registro,
    ServicoExecucaoJobs servico) : ControllerBase
{
    [HttpPost("expirar-sugestoes")]
    [Authorize]
    public Task<IResult> Expirar(CancellationToken cancellationToken) =>
        Executar(ExpirarSugestoesProcesso.NomeJob, cancellationToken);

    [HttpPost("fechar-vencidos")]
    [Authorize]
    public Task<IResult> FecharVencidos(CancellationToken cancellationToken) =>
        Executar(FecharVencidosProcesso.NomeJob, cancellationToken);

    [HttpPost("expurgar-idempotencia")]
    [Authorize]
    public Task<IResult> Expurgar(CancellationToken cancellationToken) =>
        Executar(ExpurgarIdempotenciaProcesso.NomeJob, cancellationToken);

    private async Task<IResult> Executar(string nome, CancellationToken cancellationToken)
    {
        var processo = registro.Obter(nome);
        if (processo is null)
            return Results.NotFound();

        var resultado = await servico.ExecutarAsync(processo, cancellationToken);
        // "expiradas" é mantido como alias de "processados" pelo contrato da seção 8.
        return Results.Ok(new
        {
            processo = processo.Nome,
            processados = resultado.Quantidade,
            expiradas = resultado.Quantidade
        });
    }
}
