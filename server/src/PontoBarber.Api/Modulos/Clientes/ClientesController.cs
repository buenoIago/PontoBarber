using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Clientes;

namespace PontoBarber.Api.Modulos.Clientes;

/// <summary>
/// Somente leitura: a relação é derivada dos agendamentos. Não existem
/// endpoints administrativos de cadastro, edição ou exclusão de clientes
/// (CA-CLI-007).
/// </summary>
[ApiController]
[Tags("Clientes")]
[Route("api/v1/barbearias/{barbeariaId:guid}/clientes")]
public sealed class ClientesController(ServicoClientes servico) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Listar(
        Guid barbeariaId,
        [FromQuery] string? busca,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioId is null) return NaoAutenticado();

        var resposta = await servico.ListarAsync(
            barbeariaId, busca, pagina, tamanho, usuarioId, cancellationToken);
        return Results.Ok(new ListaClientesContract(
            resposta.Itens.Select(ClienteContracts.DaResponse).ToList(),
            resposta.Total,
            resposta.Pagina,
            resposta.Tamanho));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = ConfiguracaoAuth.PolíticaProfissional)]
    public async Task<IResult> Obter(
        Guid barbeariaId,
        string id,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioId is null) return NaoAutenticado();

        return Results.Ok(ClienteContracts.DaResponse(
            await servico.ObterAsync(barbeariaId, id, usuarioId, cancellationToken)));
    }

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
