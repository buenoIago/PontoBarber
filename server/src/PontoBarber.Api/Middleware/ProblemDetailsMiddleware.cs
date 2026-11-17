using Microsoft.EntityFrameworkCore;
using PontoBarber.Api.Errors;
using PontoBarber.Application.Abstractions;

namespace PontoBarber.Api.Middleware;

/// <summary>
/// Converte exceções em ProblemDetails com content-type application/problem+json,
/// código estável, traceId e erros por campo.
/// </summary>
public sealed class ProblemDetailsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (status, code, title, detail, errors) = Map(exception);
            await ProblemResponseWriter.WriteAsync(context, status, code, title, detail, errors);
        }
    }

    private static (int Status, string Code, string Title, string Detail, IReadOnlyDictionary<string, string[]>? Errors)
        Map(Exception exception)
    {
        return exception switch
        {
            ApplicationValidationException validation => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationFailed,
                "Requisição inválida",
                validation.Message,
                validation.Errors),
            ApplicationConflictException conflict => (
                StatusCodes.Status409Conflict,
                conflict.Code,
                "Conflito",
                conflict.Message,
                null),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                ErrorCodes.ConcurrencyConflict,
                "Conflito de concorrência",
                "O registro foi alterado por outra operação.",
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                ErrorCodes.InternalError,
                "Erro interno",
                "Ocorreu um erro inesperado.",
                null)
        };
    }
}
