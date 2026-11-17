namespace PontoBarber.Application.Abstractions;

/// <summary>
/// Conflito de regra de negócio mapeado para um código estável do catálogo.
/// </summary>
public sealed class ApplicationConflictException : Exception
{
    public ApplicationConflictException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
