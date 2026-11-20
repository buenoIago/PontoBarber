namespace PontoBarber.Aplicacao.Compartilhado;

/// <summary>
/// Recurso de negócio inexistente ou inacessível (HTTP 404).
/// </summary>
#pragma warning disable CA1710 // Nomenclatura de exceções em português: padrão aprovado
public sealed class ExcecaoNaoEncontradoAplicacao : Exception
{
    public ExcecaoNaoEncontradoAplicacao(string codigo, string mensagem)
        : base(mensagem)
    {
        Codigo = codigo;
    }

    public string Codigo { get; }
}
#pragma warning restore CA1710
