using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;

namespace PontoBarber.Api.Modulos.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(ServicoConta servicoConta) : ControllerBase
{
    [HttpPost("register/customer")]
    public async Task<IResult> CadastrarCliente(
        [FromBody] RegistroRequest request,
        CancellationToken cancellationToken)
    {
        await servicoConta.CadastrarClienteAsync(request, cancellationToken);
        return Results.StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("register/professional")]
    public async Task<IResult> CadastrarBarbeiro(
        [FromBody] RegistroRequest request,
        CancellationToken cancellationToken)
    {
        await servicoConta.CadastrarBarbeiroAsync(request, cancellationToken);
        return Results.StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    public async Task<IResult> Autenticar(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await servicoConta.AutenticarAsync(request, cancellationToken);

        return resultado switch
        {
            ResultadoAutenticacao.Sucesso => Results.NoContent(),
            ResultadoAutenticacao.Bloqueado => Erro(
                StatusCodes.Status423Locked,
                CodigosErro.AutenticacaoBloqueada,
                "Conta bloqueada",
                "Conta temporariamente bloqueada. Tente novamente mais tarde."),
            _ => Erro(
                StatusCodes.Status401Unauthorized,
                CodigosErro.AutenticacaoCredenciaisInvalidas,
                "Não autenticado",
                "E-mail ou senha incorretos.")
        };
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IResult> ObterConta(CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioId is null)
            return Erro(StatusCodes.Status401Unauthorized, CodigosErro.AutenticacaoCredenciaisInvalidas,
                "Não autenticado", "É necessário autenticar para acessar este recurso.");

        var conta = await servicoConta.ObterContaAsync(usuarioId, cancellationToken);
        return Results.Ok(new
        {
            conta.Id,
            conta.NomeCompleto,
            conta.Email,
            conta.Telefone,
            conta.Tipo
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IResult> EncerrarSessao(CancellationToken cancellationToken)
    {
        await servicoConta.EncerrarSessaoAsync(cancellationToken);
        return Results.NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IResult> AlterarSenha(
        [FromBody] AlterarSenhaRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioId is null)
            return Erro(StatusCodes.Status401Unauthorized, CodigosErro.AutenticacaoCredenciaisInvalidas,
                "Não autenticado", "É necessário autenticar para acessar este recurso.");

        await servicoConta.AlterarSenhaAsync(request, usuarioId, cancellationToken);
        return Results.NoContent();
    }

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
