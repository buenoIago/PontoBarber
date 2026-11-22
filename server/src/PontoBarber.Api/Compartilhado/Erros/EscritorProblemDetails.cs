using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PontoBarber.Api.Compartilhado.Erros;

/// <summary>
/// Escrita centralizada de respostas RFC 7807 usando o <see cref="ProblemDetails"/>
/// nativo do ASP.NET Core, com <c>code</c>, <c>traceId</c> e <c>errors</c> em
/// <see cref="ProblemDetails.Extensions"/> conforme ADR-035.
/// </summary>
internal static class EscritorProblemDetails
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public static async Task EscreverAsync(
        HttpContext contexto,
        int status,
        string codigo,
        string titulo,
        string? detalhe,
        IReadOnlyDictionary<string, string[]>? erros = null,
        Action<HttpResponse>? configurarResposta = null)
    {
        if (contexto.Response.HasStarted)
            return;

        var problema = new ProblemDetails
        {
            Type = "about:blank",
            Title = titulo,
            Status = status,
            Detail = detalhe,
            Instance = contexto.Request.Path.Value ?? string.Empty
        };

        problema.Extensions["code"] = codigo;
        problema.Extensions["traceId"] = contexto.TraceIdentifier;
        problema.Extensions["errors"] = erros ?? new Dictionary<string, string[]>();

        // O conversor nativo omite "detail" quando nulo; o contrato do ADR-017
        // exige a propriedade presente com valor null.
        if (detalhe is null)
            problema.Extensions["detail"] = null;

        contexto.Response.Clear();
        configurarResposta?.Invoke(contexto.Response);
        contexto.Response.StatusCode = status;
        contexto.Response.ContentType = "application/problem+json";
        await contexto.Response.WriteAsync(JsonSerializer.Serialize(problema, _options));
    }
}
