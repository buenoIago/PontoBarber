using Microsoft.AspNetCore.Antiforgery;
using PontoBarber.Api.Compartilhado.Erros;
using PontoBarber.Aplicacao.Compartilhado;

namespace PontoBarber.Api.Modulos.Auth;

/// <summary>
/// Emite o cookie antiforgery e valida o header em toda requisição que
/// altera estado (TDD §11.1/§12.3). Requisições seguras apenas recebem o cookie.
/// </summary>
public sealed class AntiforgeryMiddleware(
    RequestDelegate proximo,
    IAntiforgery antiforgery)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        antiforgery.GetAndStoreTokens(contexto);

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
            await EscritorProblemDetails.EscreverAsync(
                contexto,
                StatusCodes.Status400BadRequest,
                CodigosErro.ValidacaoFalhou,
                "Requisição inválida",
                "Token antiforgery ausente ou inválido.",
                new Dictionary<string, string[]> { ["antiForgeryToken"] = ["Token ausente ou inválido."] });
            return;
        }

        await proximo(contexto);
    }

    private static bool MetodoAlterEstado(string metodo) =>
        !HttpMethods.IsGet(metodo) &&
        !HttpMethods.IsHead(metodo) &&
        !HttpMethods.IsOptions(metodo) &&
        !HttpMethods.IsTrace(metodo);
}
