using Microsoft.EntityFrameworkCore;
using PontoBarber.Api.Compartilhado.Erros;
using PontoBarber.Aplicacao.Compartilhado;

namespace PontoBarber.Api.Compartilhado.Http;

/// <summary>
/// Converte exceções em ProblemDetails com content-type application/problem+json,
/// código estável, traceId e erros por campo.
/// </summary>
public sealed class ProblemDetailsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await next(contexto);
        }
        catch (Exception excecao) when (!contexto.Response.HasStarted)
        {
            var (status, codigo, titulo, detalhe, erros) = Mapear(excecao);
            await EscritorProblemDetails.EscreverAsync(contexto, status, codigo, titulo, detalhe, erros);
        }
    }

    private static (int Status, string Codigo, string Titulo, string Detalhe, IReadOnlyDictionary<string, string[]>? Erros)
        Mapear(Exception excecao)
    {
        return excecao switch
        {
            ExcecaoValidacaoAplicacao validacao => (
                StatusCodes.Status400BadRequest,
                CodigosErro.ValidacaoFalhou,
                "Requisição inválida",
                validacao.Message,
                validacao.Erros),
            ExcecaoConflitoAplicacao conflito => (
                StatusCodes.Status409Conflict,
                conflito.Codigo,
                "Conflito",
                conflito.Message,
                null),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                CodigosErro.ConcorrenciaConflito,
                "Conflito de concorrência",
                "O registro foi alterado por outra operação.",
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                CodigosErro.ErroInterno,
                "Erro interno",
                "Ocorreu um erro inesperado.",
                null)
        };
    }
}
