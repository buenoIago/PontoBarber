namespace PontoBarber.Aplicacao.Compartilhado;

/// <summary>
/// Falha de validação com erros por campo.
/// </summary>
#pragma warning disable CA1710 // Nomenclatura de exceções em português: padrão aprovado para o PontoBarber
public sealed class ExcecaoValidacaoAplicacao : Exception
{
    public ExcecaoValidacaoAplicacao(IReadOnlyDictionary<string, string[]> erros)
        : base("A requisição contém campos inválidos.")
    {
        Erros = erros;
    }

    public IReadOnlyDictionary<string, string[]> Erros { get; }
}
#pragma warning restore CA1710
