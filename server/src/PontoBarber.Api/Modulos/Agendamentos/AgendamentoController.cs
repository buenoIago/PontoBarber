using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Agendamentos;

namespace PontoBarber.Api.Modulos.Agendamentos;

[ApiController]
[Route("api/v1/barbearias/{barbeariaId:guid}/agendamentos")]
public sealed class AgendamentoController(ServicoAgendamentos servico) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Listar(
        Guid barbeariaId,
        [FromQuery] DateOnly de,
        [FromQuery] DateOnly ate,
        [FromQuery] Guid? profissionalId,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.ListarAsync(
            barbeariaId, de, ate, profissionalId, usuarioId, cancellationToken);
        return Results.Ok(resposta.Select(AgendamentoContracts.DaResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IResult> Obter(
        Guid barbeariaId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        return Results.Ok(AgendamentoContracts.DaResponse(
            await servico.ObterAsync(barbeariaId, id, usuarioId, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> CriarManual(
        Guid barbeariaId,
        [FromBody] CriarAgendamentoContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        return Results.Created(
            $"/api/v1/barbearias/{barbeariaId}/agendamentos",
            AgendamentoContracts.DaResponse(await servico.CriarManualAsync(
                barbeariaId, AgendamentoContracts.ParaRequest(contrato), usuarioId, cancellationToken)));
    }

    [HttpPost("public")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaCliente)]
    public async Task<IResult> CriarPublico(
        Guid barbeariaId,
        [FromBody] CriarAgendamentoContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        return Results.Created(
            $"/api/v1/barbearias/{barbeariaId}/agendamentos",
            AgendamentoContracts.DaResponse(await servico.CriarPublicoAsync(
                barbeariaId, AgendamentoContracts.ParaRequest(contrato), usuarioId, cancellationToken)));
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
