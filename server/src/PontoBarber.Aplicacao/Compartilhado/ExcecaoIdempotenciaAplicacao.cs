namespace PontoBarber.Aplicacao.Compartilhado;

/// <summary>Chave de idempotência ausente ou divergente (ADR-021).</summary>
#pragma warning disable CA1710 // Nomenclatura de exceções em português: padrão aprovado para o PontoBarber
public sealed class ExcecaoIdempotenciaAplicacao : Exception
{
    public ExcecaoIdempotenciaAplicacao(string codigo, string mensagem)
        : base(mensagem)
    {
        Codigo = codigo;
    }

    public string Codigo { get; }
}
#pragma warning restore CA1710
