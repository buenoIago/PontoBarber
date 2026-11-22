using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Agendamentos;

/// <summary>
/// Sugestão de novo horário com reserva temporária de 24 horas (TDD §5.5.2).
/// A reserva é considerada pela verificação de conflito enquanto Pendente e
/// não expirada, de modo que um atraso do job não permite aceite indevido.
/// </summary>
public sealed class SugestaoHorario : EntidadeBase<SugestaoHorario>, IPertencenteTenant
{
    public const int HorasValidade = 24;

    private SugestaoHorario()
    {
    }

    public Guid BarbeariaId { get; private set; }
    public Guid AgendamentoId { get; private set; }
    public Guid ProfissionalId { get; private set; }
    public DateTime InicioSugeridoUtc { get; private set; }
    public DateTime FimSugeridoUtc { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public SituacaoSugestao Situacao { get; private set; }
    public DateTime ExpiraEmUtc { get; private set; }
    public DateTime? RespondidoEmUtc { get; private set; }
    public string AutorUsuarioId { get; private set; } = string.Empty;

    public static SugestaoHorario Criar(
        Guid barbeariaId,
        Guid agendamentoId,
        Guid profissionalId,
        DateTime inicioSugeridoUtc,
        int duracaoMinutos,
        string motivo,
        string autorUsuarioId,
        DateTime agoraUtc)
    {
        var sugestao = new SugestaoHorario
        {
            BarbeariaId = barbeariaId,
            AgendamentoId = agendamentoId,
            ProfissionalId = profissionalId,
            InicioSugeridoUtc = inicioSugeridoUtc,
            FimSugeridoUtc = inicioSugeridoUtc.AddMinutes(duracaoMinutos),
            Motivo = motivo?.Trim() ?? string.Empty,
            Situacao = SituacaoSugestao.Pendente,
            ExpiraEmUtc = agoraUtc.AddHours(HorasValidade),
            AutorUsuarioId = autorUsuarioId
        };

        var erros = sugestao.Validar();
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Não foi possível criar a sugestão: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        return sugestao;
    }

    public bool EstaExpirada(DateTime agoraUtc) =>
        Situacao == SituacaoSugestao.Pendente && ExpiraEmUtc <= agoraUtc;

    public void Aceitar(DateTime agoraUtc)
    {
        GarantirPendenteNaoExpirada(agoraUtc);
        Situacao = SituacaoSugestao.Aceita;
        RespondidoEmUtc = agoraUtc;
    }

    public void Rejeitar(DateTime agoraUtc)
    {
        GarantirPendenteNaoExpirada(agoraUtc);
        Situacao = SituacaoSugestao.Rejeitada;
        RespondidoEmUtc = agoraUtc;
    }

    public void Expirar(DateTime agoraUtc)
    {
        if (Situacao != SituacaoSugestao.Pendente || !EstaExpirada(agoraUtc))
            throw new InvalidOperationException("Somente uma sugestão pendente e vencida pode expirar.");

        Situacao = SituacaoSugestao.Expirada;
        RespondidoEmUtc = agoraUtc;
    }

    private void GarantirPendenteNaoExpirada(DateTime agoraUtc)
    {
        if (Situacao != SituacaoSugestao.Pendente)
            throw new InvalidOperationException("A sugestão já foi respondida.");
        if (EstaExpirada(agoraUtc))
            throw new InvalidOperationException("A sugestão expirou.");
    }

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (BarbeariaId == Guid.Empty)
            erros.Add(new ErroValidacao("barbeariaId", "Campo obrigatório."));
        if (AgendamentoId == Guid.Empty)
            erros.Add(new ErroValidacao("agendamentoId", "Campo obrigatório."));
        if (ProfissionalId == Guid.Empty)
            erros.Add(new ErroValidacao("profissionalId", "Campo obrigatório."));
        if (InicioSugeridoUtc.Kind != DateTimeKind.Utc || FimSugeridoUtc.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("periodo", "Informe o período em UTC."));
        if (FimSugeridoUtc <= InicioSugeridoUtc)
            erros.Add(new ErroValidacao("fimSugeridoUtc", "O término deve ser posterior ao início."));
        if (string.IsNullOrWhiteSpace(Motivo))
            erros.Add(new ErroValidacao("motivo", "Campo obrigatório."));
        else if (Motivo.Length > 500)
            erros.Add(new ErroValidacao("motivo", "Máximo de 500 caracteres."));
        if (string.IsNullOrWhiteSpace(AutorUsuarioId))
            erros.Add(new ErroValidacao("autorUsuarioId", "Campo obrigatório."));

        return erros;
    }

    /// <summary>
    /// Atualização genérica só aceita valores idênticos; transições passam
    /// por <see cref="Aceitar"/>, <see cref="Rejeitar"/> ou <see cref="Expirar"/>.
    /// </summary>
    public override void Atualizar(SugestaoHorario entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (Id != entidadeAtualizada.Id ||
            BarbeariaId != entidadeAtualizada.BarbeariaId ||
            AgendamentoId != entidadeAtualizada.AgendamentoId ||
            ProfissionalId != entidadeAtualizada.ProfissionalId ||
            InicioSugeridoUtc != entidadeAtualizada.InicioSugeridoUtc ||
            FimSugeridoUtc != entidadeAtualizada.FimSugeridoUtc ||
            Motivo != entidadeAtualizada.Motivo ||
            Situacao != entidadeAtualizada.Situacao ||
            ExpiraEmUtc != entidadeAtualizada.ExpiraEmUtc ||
            RespondidoEmUtc != entidadeAtualizada.RespondidoEmUtc ||
            AutorUsuarioId != entidadeAtualizada.AutorUsuarioId)
            throw new InvalidOperationException("A sugestão não pode ser alterada por atualização genérica.");
    }
}
