namespace PontoBarber.Aplicacao.Compartilhado;

/// <summary>Falha de autenticação com resposta genérica (401).</summary>
#pragma warning disable CA1710 // Nomenclatura de exceções em português: padrão aprovado para o PontoBarber
public sealed class ExcecaoAutenticacaoAplicacao : Exception
{
    public ExcecaoAutenticacaoAplicacao(string codigo, string mensagem)
        : base(mensagem)
    {
        Codigo = codigo;
    }

    public string Codigo { get; }
}
#pragma warning restore CA1710
