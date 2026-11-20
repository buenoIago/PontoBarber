using Microsoft.AspNetCore.Antiforgery;
using PontoBarber.Api.Compartilhado.Erros;
using PontoBarber.Aplicacao.Compartilhado;

namespace PontoBarber.Api.Modulos.Auth;

/// <summary>
/// Emite o request token antiforgery em um cookie legível pelo cliente e valida
/// o header em toda requisição que altera estado (TDD §11.1/§12.3).
/// <para>
/// O cookie token permanece protegido no cookie do ASP.NET Core
/// (<see cref="ConfiguracaoAuth.CookieCsrf"/>). O par
/// <c>cookieToken:requestToken</c> é publicado em
/// <see cref="ConfiguracaoAuth.CookieRequestToken"/> para ser devolvido em
/// <see cref="ConfiguracaoAuth.HeaderCsrf"/>.
/// </para>
/// <para>
/// Como a política de cookie é <c>SecurePolicy.Always</c>, nenhum token é
/// emitido em requisições HTTP. Requisições HTTP que alteram estado são
/// rejeitadas; endpoints de health check em HTTP seguem sem antiforgery.
/// </para>
/// </summary>
public sealed class AntiforgeryMiddleware(
    RequestDelegate proximo,
    IAntiforgery antiforgery)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        if (!contexto.Request.IsHttps)
        {
            // Emitir cookie Secure em HTTP lança no CheckSSLConfig do ASP.NET Core.
            // Requisições seguras seguem sem token; as que alteram estado falham fechado.
            if (MetodoAlterEstado(contexto.Request.Method))
            {
                await EscreverFalhaAsync(
                    contexto,
                    "A proteção antiforgery exige uma requisição HTTPS.",
                    "Token ausente ou inválido.");
                return;
            }

            await proximo(contexto);
            return;
        }

        var tokens = antiforgery.GetAndStoreTokens(contexto);

        contexto.Response.Cookies.Append(
            ConfiguracaoAuth.CookieRequestToken,
            tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });

        if (!MetodoAlterEstado(contexto.Request.Method))
        {
            await proximo(contexto);
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(contexto);
        }
        catch (AntiforgeryValidationException)
        {
            await EscreverFalhaAsync(
                contexto,
                "Token antiforgery ausente ou inválido.",
                "Token ausente ou inválido.");
            return;
        }

        await proximo(contexto);
    }

    private static bool MetodoAlterEstado(string metodo) =>
        !HttpMethods.IsGet(metodo) &&
        !HttpMethods.IsHead(metodo) &&
        !HttpMethods.IsOptions(metodo) &&
        !HttpMethods.IsTrace(metodo);

    private static Task EscreverFalhaAsync(HttpContext contexto, string detalhe, string mensagemCampo) =>
        EscritorProblemDetails.EscreverAsync(
            contexto,
            StatusCodes.Status400BadRequest,
            CodigosErro.ValidacaoFalhou,
            "Requisição inválida",
            detalhe,
            new Dictionary<string, string[]> { ["antiForgeryToken"] = [mensagemCampo] });
}
