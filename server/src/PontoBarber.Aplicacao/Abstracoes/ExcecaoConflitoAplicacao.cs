namespace PontoBarber.Aplicacao.Abstracoes;

/// <summary>
/// Conflito de regra de negócio mapeado para um código estável do catálogo.
/// </summary>
#pragma warning disable CA1710 // Nomenclatura de exceções em português: padrão aprovado para o PontoBarber
public sealed class ExcecaoConflitoAplicacao : Exception
{
    public ExcecaoConflitoAplicacao(string codigo, string mensagem)
        : base(mensagem)
    {
        Codigo = codigo;
    }

    public string Codigo { get; }
}
#pragma warning restore CA1710
