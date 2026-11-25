using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Profissionais;

namespace PontoBarber.Api.Modulos.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    ServicoConta servicoConta,
    ServicoProfissional servicoProfissional) : ControllerBase
{
    [HttpPost("register/customer")]
    public async Task<IResult> CadastrarCliente(
        [FromBody] RegistroClienteRequest request,
        CancellationToken cancellationToken)
    {
        await servicoConta.CadastrarClienteAsync(request, cancellationToken);
        return Results.StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("register/professional")]
    public async Task<IResult> CadastrarBarbeiro(
        [FromBody] RegistroProfissionalRequest request,
        CancellationToken cancellationToken)
    {
        await servicoConta.CadastrarBarbeiroAsync(request, cancellationToken);
        return Results.StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("otp/request")]
    public async Task<IResult> SolicitarOtp(
        [FromBody] SolicitarOtpRequest request,
        CancellationToken cancellationToken)
    {
        await servicoConta.SolicitarOtpAsync(request, cancellationToken);
        // Resposta genérica: não revela se o e-mail possui conta.
        return Results.Accepted(value: new { mensagem = "Se a conta existir, um código foi enviado." });
    }

    [HttpPost("otp/verify")]
    public async Task<IResult> VerificarOtp(
        [FromBody] VerificarOtpRequest request,
        CancellationToken cancellationToken)
    {
        await servicoConta.VerificarOtpAsync(request, cancellationToken);
        return Results.NoContent();
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
        var vinculos = conta.Tipo == ServicoConta.TipoBarbeiro
            ? await servicoProfissional.ObterVinculosAtivosAsync(usuarioId, cancellationToken)
            : [];

        // Contrato já previsto em TDD §10.2 e na spec identity-and-access:
        // perfil, vínculos ativos e permissões. Sem convite ou dados alheios.
        return Results.Ok(new
        {
            conta.Id,
            conta.NomeCompleto,
            conta.Email,
            conta.Telefone,
            conta.Tipo,
            vinculos = vinculos.Select(vinculo => new
            {
                vinculo.Id,
                vinculo.BarbeariaId,
                vinculo.ProfissionalId,
                vinculo.Cargo,
                vinculo.Situacao
            }).ToList()
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
