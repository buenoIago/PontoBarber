using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Profissionais;

namespace PontoBarber.Api.Modulos.Profissionais;

/// <summary>Disponibilidade semanal do profissional (TDD §10.2).</summary>
[ApiController]
[Route("api/v1/barbearias/{barbeariaId:guid}/profissionais/{id:guid}/disponibilidade")]
public sealed class DisponibilidadeController(GestaoDisponibilidadeSemanal servico) : ControllerBase
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

        return Results.Ok(await servico.ListarAsync(barbeariaId, id, usuarioId, cancellationToken));
    }

    [HttpPut]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Salvar(
        Guid barbeariaId,
        Guid id,
        [FromBody] IReadOnlyList<JanelaDisponibilidadeContract> contratos,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        return Results.Ok(await servico.SalvarAsync(
            barbeariaId,
            id,
            DisponibilidadeContracts.ParaRequest(contratos),
            usuarioId,
            cancellationToken));
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
