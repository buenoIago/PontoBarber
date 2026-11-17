using System.Text.Json;

namespace PontoBarber.Api.Errors;

/// <summary>
/// Escrita centralizada de respostas RFC 7807 para erros que não passam
/// por exceção, como os status de autenticação e autorização.
/// </summary>
internal static class ProblemResponseWriter
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(
        HttpContext context,
        int status,
        string code,
        string title,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted)
            return;

        var problem = new ProblemResponse
        {
            Status = status,
            Code = code,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path.Value ?? string.Empty,
            TraceId = context.TraceIdentifier,
            Errors = errors ?? new Dictionary<string, string[]>()
        };

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, _options));
    }
}
