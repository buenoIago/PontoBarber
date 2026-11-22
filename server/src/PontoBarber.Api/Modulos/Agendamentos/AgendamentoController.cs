using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Agendamentos;
using PontoBarber.Aplicacao.Modulos.Agendamentos.DTOs;

namespace PontoBarber.Api.Modulos.Agendamentos;

[ApiController]
[Route("api/v1/barbearias/{barbeariaId:guid}/agendamentos")]
public sealed class AgendamentoController(
    ServicoAgendamentos servico,
    ServicoCicloVidaAgendamentos cicloVida) : ControllerBase
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

    [HttpPost("{id:guid}/cancelar")]
    [Authorize]
    public async Task<IResult> Cancelar(
        Guid barbeariaId,
        Guid id,
        [FromBody] CancelarAgendamentoContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        return Results.Ok(AgendamentoContracts.DaResponse(await cicloVida.CancelarAsync(
            barbeariaId, id,
            new CancelarAgendamentoRequest(contrato.Motivo), usuarioId, cancellationToken)));
    }

    [HttpPost("{id:guid}/remarcar")]
    [Authorize]
    public async Task<IResult> Remarcar(
        Guid barbeariaId,
        Guid id,
        [FromBody] RemarcarAgendamentoContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        return Results.Ok(AgendamentoContracts.DaResponse(await cicloVida.RemarcarAsync(
            barbeariaId, id,
            new RemarcarAgendamentoRequest(contrato.IniciaEmUtc), usuarioId, cancellationToken)));
    }

    [HttpPost("{id:guid}/sugestoes")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Sugerir(
        Guid barbeariaId,
        Guid id,
        [FromBody] SugerirHorarioContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        return Results.Created(
            $"/api/v1/barbearias/{barbeariaId}/agendamentos/{id}/sugestoes",
            AgendamentoContracts.DaResponse(await cicloVida.SugerirAsync(
                barbeariaId, id,
                new SugerirHorarioRequest(contrato.IniciaEmUtc, contrato.Motivo),
                usuarioId, cancellationToken)));
    }

    [HttpGet("{id:guid}/sugestoes")]
    [Authorize]
    public async Task<IResult> ListarSugestoes(
        Guid barbeariaId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        var resposta = await cicloVida.ListarSugestoesAsync(barbeariaId, id, usuarioId, cancellationToken);
        return Results.Ok(resposta.Select(AgendamentoContracts.DaResponse).ToList());
    }

    [HttpPost("{id:guid}/sugestoes/{sugestaoId:guid}/aceitar")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaCliente)]
    public async Task<IResult> Aceitar(
        Guid barbeariaId,
        Guid id,
        Guid sugestaoId,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        return Results.Ok(AgendamentoContracts.DaResponse(await cicloVida.AceitarAsync(
            barbeariaId, id, sugestaoId, usuarioId, cancellationToken)));
    }

    [HttpPost("{id:guid}/sugestoes/{sugestaoId:guid}/rejeitar")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaCliente)]
    public async Task<IResult> Rejeitar(
        Guid barbeariaId,
        Guid id,
        Guid sugestaoId,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        return Results.Ok(AgendamentoContracts.DaResponse(await cicloVida.RejeitarAsync(
            barbeariaId, id, sugestaoId, usuarioId, cancellationToken)));
    }

    [HttpGet("{id:guid}/historico")]
    [Authorize]
    public async Task<IResult> ListarHistorico(
        Guid barbeariaId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        var resposta = await cicloVida.ListarHistoricoAsync(barbeariaId, id, usuarioId, cancellationToken);
        return Results.Ok(resposta.Select(AgendamentoContracts.DaResponse).ToList());
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
