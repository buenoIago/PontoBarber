using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Notificacoes;
using PontoBarber.Aplicacao.Modulos.Notificacoes.DTOs;

namespace PontoBarber.Api.Modulos.Notificacoes;

public sealed record NotificacaoContract(
    Guid Id,
    Guid BarbeariaId,
    string Tipo,
    Guid? AgendamentoId,
    string Mensagem,
    DateTime CriadoEmUtc,
    DateTime? LidoEmUtc);

public sealed record ListaNotificacoesContract(
    IReadOnlyList<NotificacaoContract> Itens,
    int NaoLidas,
    int Pagina,
    int Tamanho);

[ApiController]
[Tags("Notificações")]
[Route("api/v1/notificacoes")]
public sealed class NotificacoesController(ServicoNotificacoes servico) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IResult> Listar(
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        var resposta = await servico.ListarAsync(pagina, tamanho, usuarioId, cancellationToken);
        return Results.Ok(new ListaNotificacoesContract(
            resposta.Itens.Select(item => new NotificacaoContract(
                item.Id, item.BarbeariaId, item.Tipo, item.AgendamentoId,
                item.Mensagem, item.CriadoEmUtc, item.LidoEmUtc)).ToList(),
            resposta.NaoLidas,
            resposta.Pagina,
            resposta.Tamanho));
    }

    [HttpGet("nao-lidas")]
    [Authorize]
    public async Task<IResult> ContarNaoLidas(CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        return Results.Ok(new { naoLidas = await servico.ContarNaoLidasAsync(usuarioId, cancellationToken) });
    }

    [HttpPost("{id:guid}/ler")]
    [Authorize]
    public async Task<IResult> MarcarComoLida(Guid id, CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        await servico.MarcarComoLidaAsync(id, usuarioId, cancellationToken);
        return Results.NoContent();
    }

    [HttpPost("ler-todas")]
    [Authorize]
    public async Task<IResult> MarcarTodas(CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null) return NaoAutenticado();

        var quantidade = await servico.MarcarTodasAsync(usuarioId, cancellationToken);
        return Results.Ok(new { marcadas = quantidade });
    }

    private string? ObterUsuarioId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

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
