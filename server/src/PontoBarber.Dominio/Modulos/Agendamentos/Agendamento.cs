using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Agendamentos;

/// <summary>
/// Agendamento com snapshot do serviço contratado (TDD §8.1/ADR-023). Os
/// dados originais não mudam quando o catálogo é editado ou desativado.
/// </summary>
public sealed class Agendamento : EntidadeBase<Agendamento>, IPertencenteTenant
{
    private Agendamento()
    {
    }

    public Guid BarbeariaId { get; private set; }
    public string ClienteId { get; private set; } = string.Empty;
    public Guid ProfissionalId { get; private set; }
    public Guid ServicoId { get; private set; }
    public DateTime IniciaEmUtc { get; private set; }
    public DateTime TerminaEmUtc { get; private set; }
    public string NomeServicoSnapshot { get; private set; } = string.Empty;
    public int DuracaoSnapshot { get; private set; }
    public decimal PrecoSnapshot { get; private set; }
    public string? Observacoes { get; private set; }
    public OrigemAgendamento Origem { get; private set; }
    public SituacaoAgendamento Situacao { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Agendamento Criar(
        Guid barbeariaId,
        string clienteId,
        Guid profissionalId,
        Guid servicoId,
        DateTime iniciaEmUtc,
        string nomeServico,
        int duracaoMinutos,
        decimal preco,
        string? observacoes,
        OrigemAgendamento origem)
    {
        var agendamento = new Agendamento
        {
            BarbeariaId = barbeariaId,
            ClienteId = clienteId,
            ProfissionalId = profissionalId,
            ServicoId = servicoId,
            IniciaEmUtc = iniciaEmUtc,
            TerminaEmUtc = iniciaEmUtc.AddMinutes(duracaoMinutos),
            NomeServicoSnapshot = nomeServico,
            DuracaoSnapshot = duracaoMinutos,
            PrecoSnapshot = preco,
            Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim(),
            Origem = origem,
            Situacao = SituacaoAgendamento.Confirmado
        };

        var erros = agendamento.Validar();
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Não foi possível criar o agendamento: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        return agendamento;
    }

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (BarbeariaId == Guid.Empty)
            erros.Add(new ErroValidacao("barbeariaId", "Campo obrigatório."));
        if (string.IsNullOrWhiteSpace(ClienteId))
            erros.Add(new ErroValidacao("clienteId", "Campo obrigatório."));
        if (ProfissionalId == Guid.Empty)
            erros.Add(new ErroValidacao("profissionalId", "Campo obrigatório."));
        if (ServicoId == Guid.Empty)
            erros.Add(new ErroValidacao("servicoId", "Campo obrigatório."));
        if (IniciaEmUtc.Kind != DateTimeKind.Utc || TerminaEmUtc.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("periodo", "Informe o período em UTC."));
        if (TerminaEmUtc <= IniciaEmUtc)
            erros.Add(new ErroValidacao("terminaEmUtc", "O término deve ser posterior ao início."));
        if (string.IsNullOrWhiteSpace(NomeServicoSnapshot))
            erros.Add(new ErroValidacao("nomeServicoSnapshot", "Campo obrigatório."));
        if (DuracaoSnapshot <= 0)
            erros.Add(new ErroValidacao("duracaoSnapshot", "Informe uma duração válida."));
        if (PrecoSnapshot < 0)
            erros.Add(new ErroValidacao("precoSnapshot", "O preço não pode ser negativo."));

        return erros;
    }

    /// <summary>
    /// Dados contratados são históricos: alteração genérica só é permitida
    /// para valores idênticos. Transições de situação pertencem à seção 8.
    /// </summary>
    public override void Atualizar(Agendamento entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (Id != entidadeAtualizada.Id ||
            BarbeariaId != entidadeAtualizada.BarbeariaId ||
            ClienteId != entidadeAtualizada.ClienteId ||
            ProfissionalId != entidadeAtualizada.ProfissionalId ||
            ServicoId != entidadeAtualizada.ServicoId ||
            IniciaEmUtc != entidadeAtualizada.IniciaEmUtc ||
            TerminaEmUtc != entidadeAtualizada.TerminaEmUtc ||
            NomeServicoSnapshot != entidadeAtualizada.NomeServicoSnapshot ||
            DuracaoSnapshot != entidadeAtualizada.DuracaoSnapshot ||
            PrecoSnapshot != entidadeAtualizada.PrecoSnapshot ||
            Origem != entidadeAtualizada.Origem ||
            Situacao != entidadeAtualizada.Situacao)
            throw new InvalidOperationException("Os dados contratados de um agendamento não podem ser alterados.");
    }
}
