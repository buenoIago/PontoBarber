using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;

namespace PontoBarber.Api.Modulos.Barbearia;

/// <summary>
/// Criação da barbearia pelo profissional autenticado (TDD §10.2).
/// O proprietário é derivado da sessão, nunca do corpo da requisição.
/// </summary>
[ApiController]
[Route("api/v1/barbearias")]
public sealed class BarbeariaController(ServicoBarbearia servico) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Criar(
        [FromBody] CriarBarbeariaContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioId is null)
            return Erro(
                StatusCodes.Status401Unauthorized,
                CodigosErro.AutenticacaoCredenciaisInvalidas,
                "Não autenticado",
                "É necessário autenticar para acessar este recurso.");

        var resposta = await servico.CriarAsync(
            BarbeariaContracts.ParaRequest(contrato), usuarioId, cancellationToken);
        return Results.Created($"/api/v1/barbearias/{resposta.Id}", BarbeariaContracts.DaResponse(resposta));
    }

    private IResult Erro(int status, string codigo, string titulo, string detalhe) =>
        Results.Problem(
            title: titulo,
            detail: detalhe,
            statusCode: status,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = codigo,
                ["traceId"] = HttpContext.TraceIdentifier
            });
}
