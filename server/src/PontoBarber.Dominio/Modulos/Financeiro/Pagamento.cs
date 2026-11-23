using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Financeiro;

/// <summary>
/// Pagamento presencial do atendimento (TDD §8.1). A situação financeira é
/// independente da situação do atendimento: um atendimento pode ser concluído
/// sem pagamento e registrar um pagamento não altera o estado do atendimento.
/// </summary>
public sealed class Pagamento : EntidadeBase<Pagamento>, IPertencenteTenant
{
    private Pagamento()
    {
    }

    public Guid BarbeariaId { get; private set; }
    public Guid AgendamentoId { get; private set; }
    public decimal Valor { get; private set; }
    public FormaPagamento Metodo { get; private set; }
    public DateTime RegistradoEmUtc { get; private set; }
    public string RegistradoPorUsuarioId { get; private set; } = string.Empty;

    public static Pagamento Criar(
        Guid barbeariaId,
        Guid agendamentoId,
        decimal valor,
        FormaPagamento metodo,
        DateTime registradoEmUtc,
        string registradoPorUsuarioId)
    {
        var pagamento = new Pagamento
        {
            BarbeariaId = barbeariaId,
            AgendamentoId = agendamentoId,
            Valor = valor,
            Metodo = metodo,
            RegistradoEmUtc = registradoEmUtc,
            RegistradoPorUsuarioId = registradoPorUsuarioId
        };

        var erros = pagamento.Validar();
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Não foi possível registrar o pagamento: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        return pagamento;
    }

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (BarbeariaId == Guid.Empty)
            erros.Add(new ErroValidacao("barbeariaId", "Campo obrigatório."));
        if (AgendamentoId == Guid.Empty)
            erros.Add(new ErroValidacao("agendamentoId", "Campo obrigatório."));
        if (Valor < 0)
            erros.Add(new ErroValidacao("valor", "O valor não pode ser negativo."));
        else if (decimal.Round(Valor, 2) != Valor)
            erros.Add(new ErroValidacao("valor", "Informe no máximo duas casas decimais."));
        if (!Enum.IsDefined(Metodo))
            erros.Add(new ErroValidacao("metodo", "Forma de pagamento inválida."));
        if (RegistradoEmUtc.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("registradoEmUtc", "Informe o momento em UTC."));
        if (string.IsNullOrWhiteSpace(RegistradoPorUsuarioId))
            erros.Add(new ErroValidacao("registradoPorUsuarioId", "Campo obrigatório."));

        return erros;
    }

    public override void Atualizar(Pagamento entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (Id != entidadeAtualizada.Id ||
            BarbeariaId != entidadeAtualizada.BarbeariaId ||
            AgendamentoId != entidadeAtualizada.AgendamentoId ||
            Valor != entidadeAtualizada.Valor ||
            Metodo != entidadeAtualizada.Metodo ||
            RegistradoEmUtc != entidadeAtualizada.RegistradoEmUtc ||
            RegistradoPorUsuarioId != entidadeAtualizada.RegistradoPorUsuarioId)
            throw new InvalidOperationException("Um pagamento registrado não pode ser alterado.");
    }
}
