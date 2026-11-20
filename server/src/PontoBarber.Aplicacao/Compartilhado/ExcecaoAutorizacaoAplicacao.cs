namespace PontoBarber.Aplicacao.Compartilhado;

/// <summary>
/// Acesso negado a um recurso por perfil ou vínculo insuficiente (HTTP 403).
/// </summary>
#pragma warning disable CA1710 // Nomenclatura de exceções em português: padrão aprovado
public sealed class ExcecaoAutorizacaoAplicacao : Exception
{
    public ExcecaoAutorizacaoAplicacao(string codigo, string mensagem)
        : base(mensagem)
    {
        Codigo = codigo;
    }

    public string Codigo { get; }
}
#pragma warning restore CA1710
