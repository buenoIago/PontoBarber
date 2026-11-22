namespace PontoBarber.Aplicacao.Compartilhado;

/// <summary>
/// Indisponibilidade temporária mapeada para 503 com Retry-After (TDD §5.5.1).
/// </summary>
#pragma warning disable CA1710 // Nomenclatura de exceções em português: padrão aprovado para o PontoBarber
public sealed class ExcecaoIndisponibilidadeAplicacao : Exception
{
    public ExcecaoIndisponibilidadeAplicacao(string codigo, string mensagem)
        : base(mensagem)
    {
        Codigo = codigo;
    }

    public string Codigo { get; }
}
#pragma warning restore CA1710
