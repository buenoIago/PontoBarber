namespace PontoBarber.Aplicacao.Compartilhado;

/// <summary>
/// Violação de horário com código estável do catálogo (TDD §11.1), mapeada
/// para 422 pelo middleware de ProblemDetails.
/// </summary>
#pragma warning disable CA1710 // Nomenclatura de exceções em português: padrão aprovado
public sealed class ExcecaoHorarioAplicacao : Exception
{
    public ExcecaoHorarioAplicacao(string codigo, string mensagem)
        : base(mensagem)
    {
        Codigo = codigo;
    }

    public string Codigo { get; }
}
#pragma warning restore CA1710
