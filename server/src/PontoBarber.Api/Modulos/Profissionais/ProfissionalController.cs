using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Profissionais;

namespace PontoBarber.Api.Modulos.Profissionais;

/// <summary>
/// Gestão de vínculos profissionais (Task 4.4 / TDD §10.2). O proprietário é
/// identificado pelo vínculo ativo na barbearia da rota, nunca pelo corpo.
/// </summary>
[ApiController]
[Route("api/v1/barbearias/{barbeariaId:guid}/profissionais")]
public sealed class ProfissionalController(ServicoProfissional servico) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Listar(Guid barbeariaId, CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.ListarProfissionaisAsync(barbeariaId, usuarioId, cancellationToken);
        return Results.Ok(resposta.Select(ProfissionalContracts.DaResponse).ToList());
    }

    [HttpPut("{profissionalId:guid}")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> AtualizarDados(
        Guid barbeariaId,
        Guid profissionalId,
        [FromBody] AtualizarProfissionalContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.AtualizarDadosAsync(
            barbeariaId,
            profissionalId,
            ProfissionalContracts.ParaRequest(contrato),
            usuarioId,
            cancellationToken);

        return Results.Ok(ProfissionalContracts.DaResponse(resposta));
    }

    [HttpPatch("{profissionalId:guid}/status")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> AlterarStatus(
        Guid barbeariaId,
        Guid profissionalId,
        [FromBody] AlterarStatusProfissionalContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.AlterarStatusAsync(
            barbeariaId,
            profissionalId,
            ProfissionalContracts.ParaRequest(contrato),
            usuarioId,
            cancellationToken);

        return Results.Ok(ProfissionalContracts.DaResponse(resposta));
    }

    [HttpDelete("{profissionalId:guid}")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> RemoverVinculo(
        Guid barbeariaId,
        Guid profissionalId,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        await servico.RemoverVinculoAsync(barbeariaId, profissionalId, usuarioId, cancellationToken);
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
