using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;

namespace PontoBarber.Api.Modulos.Barbearia;

/// <summary>Funcionamento semanal da barbearia (TDD §10.2).</summary>
[ApiController]
[Route("api/v1/barbearias/{barbeariaId:guid}/horarios-funcionamento")]
public sealed class HorarioFuncionamentoController(GestaoHorariosFuncionamento servico) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Listar(Guid barbeariaId, CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        return Results.Ok(await servico.ListarAsync(barbeariaId, usuarioId, cancellationToken));
    }

    [HttpPut]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Salvar(
        Guid barbeariaId,
        [FromBody] IReadOnlyList<DiaFuncionamentoContract> contratos,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        return Results.Ok(await servico.SalvarAsync(
            barbeariaId, HorarioFuncionamentoContracts.ParaRequest(contratos), usuarioId, cancellationToken));
    }

    private string? ObterUsuarioId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private IResult NaoAutenticado() =>
        Results.Problem(
            title: "Não autenticado",
            detail: "É necessário autenticar para acessar este recurso.",
            statusCode: StatusCodes.Status401Unauthorized,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = CodigosErro.AutenticacaoCredenciaisInvalidas,
                ["traceId"] = HttpContext.TraceIdentifier
            });
}
