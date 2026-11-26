using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Profissionais;

namespace PontoBarber.Api.Modulos.Profissionais;

/// <summary>Bloqueios pontuais da agenda do profissional (TDD §10.2).</summary>
[ApiController]
[Tags("Bloqueios")]
[Route("api/v1/barbearias/{barbeariaId:guid}/profissionais/{id:guid}/blocks")]
public sealed class BloqueioController(GestaoBloqueios servico) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Listar(
        Guid barbeariaId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.ListarAsync(barbeariaId, id, usuarioId, cancellationToken);
        return Results.Ok(resposta.Select(BloqueioContracts.DaResponse).ToList());
    }

    [HttpPost]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Criar(
        Guid barbeariaId,
        Guid id,
        [FromBody] CriarBloqueioContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.CriarAsync(
            barbeariaId, id, BloqueioContracts.ParaRequest(contrato), usuarioId, cancellationToken);
        return Results.Created(
            $"/api/v1/barbearias/{barbeariaId}/profissionais/{id}/blocks/{resposta.Id}",
            BloqueioContracts.DaResponse(resposta));
    }

    [HttpDelete("{blockId:guid}")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Remover(
        Guid barbeariaId,
        Guid id,
        Guid blockId,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        await servico.RemoverAsync(barbeariaId, id, blockId, usuarioId, cancellationToken);
        return Results.NoContent();
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
