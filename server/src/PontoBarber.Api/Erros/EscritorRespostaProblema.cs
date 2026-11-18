using System.Text.Json;

namespace PontoBarber.Api.Erros;

/// <summary>
/// Escrita centralizada de respostas RFC 7807 para erros que não passam
/// por exceção, como os status de autenticação e autorização.
/// </summary>
internal static class EscritorRespostaProblema
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public static async Task EscreverAsync(
        HttpContext contexto,
        int status,
        string codigo,
        string titulo,
        string detalhe,
        IReadOnlyDictionary<string, string[]>? erros = null)
    {
        if (contexto.Response.HasStarted)
            return;

        var problema = new RespostaProblema
        {
            Status = status,
            Code = codigo,
            Title = titulo,
            Detail = detalhe,
            Instance = contexto.Request.Path.Value ?? string.Empty,
            TraceId = contexto.TraceIdentifier,
            Errors = erros ?? new Dictionary<string, string[]>()
        };

        contexto.Response.Clear();
        contexto.Response.StatusCode = status;
        contexto.Response.ContentType = "application/problem+json";
        await contexto.Response.WriteAsync(JsonSerializer.Serialize(problema, _options));
    }
}
