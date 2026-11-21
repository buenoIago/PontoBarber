using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Servicos;

namespace PontoBarber.Api.Modulos.Servicos;

/// <summary>
/// Criação e edição de serviços (Task 5.2). O tenant vem da rota e o cargo do
/// vínculo ativo do usuário; nenhum identificador é aceito do corpo.
/// </summary>
[ApiController]
[Route("api/v1/barbearias/{barbeariaId:guid}/servicos")]
public sealed class ServicoController(GestaoServicos servico) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IResult> Listar(Guid barbeariaId, CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.ListarAsync(barbeariaId, usuarioId, cancellationToken);
        return Results.Ok(resposta.Select(ServicoContracts.DaResponse).ToList());
    }

    [HttpPost]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Criar(
        Guid barbeariaId,
        [FromBody] CriarServicoContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.CriarAsync(
            barbeariaId, ServicoContracts.ParaRequest(contrato), usuarioId, cancellationToken);
        return Results.Created(
            $"/api/v1/barbearias/{barbeariaId}/servicos/{resposta.Id}", ServicoContracts.DaResponse(resposta));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Atualizar(
        Guid barbeariaId,
        Guid id,
        [FromBody] AtualizarServicoContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.AtualizarAsync(
            barbeariaId, id, ServicoContracts.ParaRequest(contrato), usuarioId, cancellationToken);
        return Results.Ok(ServicoContracts.DaResponse(resposta));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Remover(
        Guid barbeariaId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        await servico.RemoverAsync(barbeariaId, id, usuarioId, cancellationToken);
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
