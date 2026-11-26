using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Dashboard;
using PontoBarber.Aplicacao.Modulos.Dashboard.DTOs;

namespace PontoBarber.Api.Modulos.Dashboard;

public sealed record ProximoAgendamentoContract(
    Guid Id,
    DateTime IniciaEmUtc,
    string NomeCliente,
    string NomeProfissional,
    string Situacao);

public sealed record DashboardContract(
    int Hoje,
    int EstaSemana,
    int Cancelamentos,
    decimal RecebidoNoMes,
    IReadOnlyList<ProximoAgendamentoContract> Proximos);

[ApiController]
[Tags("Painel")]
[Route("api/v1/barbearias/{barbeariaId:guid}/dashboard")]
public sealed class DashboardController(ServicoDashboard servico) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Obter(Guid barbeariaId, CancellationToken cancellationToken)
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

        var resposta = await servico.ObterAsync(barbeariaId, usuarioId, cancellationToken);
        return Results.Ok(new DashboardContract(
            resposta.Hoje,
            resposta.EstaSemana,
            resposta.Cancelamentos,
            resposta.RecebidoNoMes,
            resposta.Proximos.Select(item => new ProximoAgendamentoContract(
                item.Id, item.IniciaEmUtc, item.NomeCliente, item.NomeProfissional, item.Situacao)).ToList()));
    }
}
