namespace PontoBarber.Api.Compartilhado.Erros;

/// <summary>
/// Representação serializada de um RFC 7807 ProblemDetails com os campos
/// adicionais exigidos pelo ADR-017: code, traceId e errors.
/// </summary>
internal sealed record RespostaProblema
{
    public string Type { get; init; } = "about:blank";
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string? Detail { get; init; }
    public string Instance { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } =
        new Dictionary<string, string[]>();
}
