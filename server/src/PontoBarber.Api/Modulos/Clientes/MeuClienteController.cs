using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Clientes;

namespace PontoBarber.Api.Modulos.Clientes;

[ApiController]
[Route("api/v1/me/customer-profile")]
public sealed class MeuClienteController(ServicoClientes servico) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaCliente)]
    public async Task<IResult> Obter(CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioId is null) return NaoAutenticado();

        return Results.Ok(ClienteContracts.DaResponse(
            await servico.ObterPerfilAsync(usuarioId, cancellationToken)));
    }

    [HttpPut]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaCliente)]
    public async Task<IResult> Atualizar(
        [FromBody] AtualizarPerfilClienteContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioId is null) return NaoAutenticado();

        return Results.Ok(ClienteContracts.DaResponse(await servico.AtualizarPerfilAsync(
            ClienteContracts.ParaRequest(contrato), usuarioId, cancellationToken)));
    }

    private IResult NaoAutenticado() =>
        Results.Problem(
            title: "Não autenticado",
            statusCode: StatusCodes.Status401Unauthorized,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = CodigosErro.AutenticacaoCredenciaisInvalidas,
                ["traceId"] = HttpContext.TraceIdentifier
            });
}
