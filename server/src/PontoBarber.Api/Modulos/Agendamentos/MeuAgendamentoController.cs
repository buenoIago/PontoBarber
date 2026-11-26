using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Agendamentos;

namespace PontoBarber.Api.Modulos.Agendamentos;

[ApiController]
[Tags("Meus Agendamentos")]
[Route("api/v1/minha-conta/agendamentos")]
public sealed class MeuAgendamentoController(ServicoAgendamentos servico) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaCliente)]
    public async Task<IResult> Listar(
        [FromQuery] DateOnly de,
        [FromQuery] DateOnly ate,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioId is null)
            return Results.Problem(
                title: "Não autenticado",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = CodigosErro.AutenticacaoCredenciaisInvalidas,
                    ["traceId"] = HttpContext.TraceIdentifier
                });

        var resposta = await servico.ListarDoClienteAsync(usuarioId, de, ate, cancellationToken);
        return Results.Ok(resposta.Select(AgendamentoContracts.DaResponse).ToList());
    }
}
