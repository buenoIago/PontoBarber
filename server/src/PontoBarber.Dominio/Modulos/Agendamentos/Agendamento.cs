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
    public DateTime? CanceladoEmUtc { get; private set; }
    public string? CanceladoPorUsuarioId { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public DateTime? InicioRealUtc { get; private set; }
    public DateTime? FimRealUtc { get; private set; }
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
        if (Situacao == SituacaoAgendamento.Cancelado &&
            (CanceladoEmUtc is null || string.IsNullOrWhiteSpace(CanceladoPorUsuarioId)))
            erros.Add(new ErroValidacao("cancelamento", "Autor e momento do cancelamento são obrigatórios."));
        if (Situacao == SituacaoAgendamento.Confirmado &&
            (CanceladoEmUtc is not null || CanceladoPorUsuarioId is not null))
            erros.Add(new ErroValidacao("cancelamento", "Um agendamento confirmado não pode ter dados de cancelamento."));
        if (InicioRealUtc is not null && InicioRealUtc.Value.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("inicioRealUtc", "Informe o início real em UTC."));
        if (FimRealUtc is not null && FimRealUtc.Value.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("fimRealUtc", "Informe o fim real em UTC."));
        if (InicioRealUtc is not null && FimRealUtc is not null && FimRealUtc <= InicioRealUtc)
            erros.Add(new ErroValidacao("fimRealUtc", "O fim real deve ser posterior ao início real."));

        return erros;
    }

    /// <summary>
    /// Cancela o agendamento, registrando autor, momento e motivo. Somente a
    /// transição Confirmado → Cancelado antes do início é permitida.
    /// </summary>
    public void Cancelar(string autorUsuarioId, string? motivo, DateTime agoraUtc)
    {
        if (Situacao != SituacaoAgendamento.Confirmado)
            throw new InvalidOperationException("Somente um agendamento confirmado pode ser cancelado.");
        if (agoraUtc >= IniciaEmUtc)
            throw new InvalidOperationException("Não é possível cancelar após o início do agendamento.");
        if (string.IsNullOrWhiteSpace(autorUsuarioId))
            throw new InvalidOperationException("O autor do cancelamento é obrigatório.");

        Situacao = SituacaoAgendamento.Cancelado;
        CanceladoEmUtc = agoraUtc;
        CanceladoPorUsuarioId = autorUsuarioId;
        MotivoCancelamento = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
    }

    /// <summary>
    /// Move o agendamento para outro horário preservando duração, preço e
    /// snapshots. Os dados contratados permanecem intactos.
    /// </summary>
    public void Remarcar(DateTime novoInicioUtc, DateTime agoraUtc)
    {
        GarantirPeriodoAlteravel(agoraUtc);
        if (novoInicioUtc.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("Informe o novo horário em UTC.");

        IniciaEmUtc = novoInicioUtc;
        TerminaEmUtc = novoInicioUtc.AddMinutes(DuracaoSnapshot);
    }

    /// <summary>
    /// Fecha um agendamento vencido como não comparecimento. TDD §12.3:
    /// elegível somente confirmado com início há mais de 2 horas.
    /// </summary>
    public void RegistrarNaoComparecimento(DateTime agoraUtc)
    {
        if (agoraUtc < IniciaEmUtc.AddHours(2))
            throw new InvalidOperationException("O agendamento ainda não venceu para não comparecimento.");

        AplicarNaoComparecimento();
    }

    /// <summary>
    /// Registro manual pelo barbeiro (RF-ATD-008): usa a mesma transição de
    /// domínio do job, sem duplicar regra, mas sem exigir o prazo de 2 horas.
    /// </summary>
    public void RegistrarNaoComparecimentoManual(DateTime agoraUtc)
    {
        if (agoraUtc.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("Informe o momento em UTC.");

        AplicarNaoComparecimento();
    }

    private void AplicarNaoComparecimento()
    {
        if (Situacao != SituacaoAgendamento.Confirmado)
            throw new InvalidOperationException("Somente um agendamento confirmado pode virar não comparecimento.");

        Situacao = SituacaoAgendamento.NaoCompareceu;
    }

    /// <summary>Inicia o atendimento registrando o horário real em UTC.</summary>
    public void Iniciar(DateTime agoraUtc)
    {
        if (Situacao != SituacaoAgendamento.Confirmado)
            throw new InvalidOperationException("Somente um agendamento confirmado pode ser iniciado.");
        if (agoraUtc.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("Informe o início em UTC.");
        if (InicioRealUtc is not null)
            throw new InvalidOperationException("O atendimento já foi iniciado.");

        InicioRealUtc = agoraUtc;
        Situacao = SituacaoAgendamento.EmAndamento;
    }

    /// <summary>Conclui somente um atendimento em andamento.</summary>
    public void Concluir(DateTime agoraUtc)
    {
        if (Situacao != SituacaoAgendamento.EmAndamento)
            throw new InvalidOperationException("Somente um atendimento em andamento pode ser concluído.");
        if (agoraUtc.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("Informe o fim em UTC.");
        if (InicioRealUtc is null)
            throw new InvalidOperationException("Não é possível concluir sem início registrado.");
        if (agoraUtc < InicioRealUtc)
            throw new InvalidOperationException("O fim não pode ser anterior ao início real.");

        FimRealUtc = agoraUtc;
        Situacao = SituacaoAgendamento.Concluido;
    }

    /// <summary>
    /// Reativa um agendamento cancelado ao aceitar uma sugestão de horário.
    /// </summary>
    public void Reativar(DateTime novoInicioUtc, DateTime agoraUtc)
    {
        if (Situacao != SituacaoAgendamento.Cancelado)
            throw new InvalidOperationException("Somente um agendamento cancelado pode ser reativado.");
        if (novoInicioUtc.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("Informe o novo horário em UTC.");
        if (agoraUtc >= novoInicioUtc)
            throw new InvalidOperationException("Não é possível aceitar um horário que já passou.");

        IniciaEmUtc = novoInicioUtc;
        TerminaEmUtc = novoInicioUtc.AddMinutes(DuracaoSnapshot);
        Situacao = SituacaoAgendamento.Confirmado;
        CanceladoEmUtc = null;
        CanceladoPorUsuarioId = null;
        MotivoCancelamento = null;
    }

    private void GarantirPeriodoAlteravel(DateTime agoraUtc)
    {
        if (Situacao != SituacaoAgendamento.Confirmado)
            throw new InvalidOperationException("Somente um agendamento confirmado pode ser remarcado.");
        if (agoraUtc >= IniciaEmUtc)
            throw new InvalidOperationException("Não é possível remarcar após o início do agendamento.");
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
            Situacao != entidadeAtualizada.Situacao ||
            CanceladoEmUtc != entidadeAtualizada.CanceladoEmUtc ||
            CanceladoPorUsuarioId != entidadeAtualizada.CanceladoPorUsuarioId ||
            MotivoCancelamento != entidadeAtualizada.MotivoCancelamento ||
            InicioRealUtc != entidadeAtualizada.InicioRealUtc ||
            FimRealUtc != entidadeAtualizada.FimRealUtc)
            throw new InvalidOperationException("Os dados contratados de um agendamento não podem ser alterados.");
    }
}
