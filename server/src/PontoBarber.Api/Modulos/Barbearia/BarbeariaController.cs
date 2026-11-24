using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Aplicacao.Modulos.Profissionais;

namespace PontoBarber.Api.Modulos.Barbearia;

/// <summary>
/// Criação, consulta e edição da barbearia e ingresso por convite (TDD §10.2).
/// O usuário e o tenant são derivados da sessão e do vínculo, nunca do corpo.
/// </summary>
[ApiController]
[Route("api/v1/barbearias")]
public sealed class BarbeariaController(
    ServicoBarbearia servico,
    ServicoProfissional servicoProfissional) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Criar(
        [FromBody] CriarBarbeariaContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.CriarAsync(
            BarbeariaContracts.ParaRequest(contrato), usuarioId, cancellationToken);
        return Results.Created($"/api/v1/barbearias/{resposta.Id}", BarbeariaContracts.DaResponse(resposta));
    }

    [HttpGet("{barbeariaId:guid}")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Obter(Guid barbeariaId, CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.ObterAsync(barbeariaId, usuarioId, cancellationToken);
        return Results.Ok(BarbeariaContracts.DaResponse(resposta));
    }

    /// <summary>
    /// Leitura usada pelo fluxo de agendamento público: o cliente autenticado
    /// precisa do fuso da barbearia e da lista de profissionais ativos, sem
    /// acessar a gestão de vínculos (Task 7.7).
    /// </summary>
    [HttpGet("{barbeariaId:guid}/agendamento-publico")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaCliente)]
    public async Task<IResult> ObterDadosAgendamentoPublico(
        Guid barbeariaId, CancellationToken cancellationToken) =>
        Results.Ok(await servicoProfissional.ObterDadosParaAgendamentoPublicoAsync(
            barbeariaId, cancellationToken));

    [HttpPut("{barbeariaId:guid}")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Atualizar(
        Guid barbeariaId,
        [FromBody] AtualizarBarbeariaContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servico.AtualizarAsync(
            barbeariaId, BarbeariaContracts.ParaRequest(contrato), usuarioId, cancellationToken);
        return Results.Ok(BarbeariaContracts.DaResponse(resposta));
    }

    [HttpPost("entrar")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Entrar(
        [FromBody] EntrarBarbeariaContract contrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
            return NaoAutenticado();

        var resposta = await servicoProfissional.EntrarPorConviteAsync(
            BarbeariaContracts.ParaRequest(contrato), usuarioId, cancellationToken);
        return Results.Ok(BarbeariaContracts.DaResponse(resposta));
    }

    private string? ObterUsuarioId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private IResult NaoAutenticado() =>
        Erro(
            StatusCodes.Status401Unauthorized,
            CodigosErro.AutenticacaoCredenciaisInvalidas,
            "Não autenticado",
            "É necessário autenticar para acessar este recurso.");

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
