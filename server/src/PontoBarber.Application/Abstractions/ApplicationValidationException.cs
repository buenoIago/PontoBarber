namespace PontoBarber.Application.Abstractions;

/// <summary>
/// Falha de validação com erros por campo.
/// </summary>
public sealed class ApplicationValidationException : Exception
{
    public ApplicationValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("A requisição contém campos inválidos.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
