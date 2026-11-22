using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Agendamentos;

/// <summary>
/// Evento imutável do ciclo de vida do agendamento (TDD §8.1/§8.5). Não existe
/// operação de atualização ou remoção: a aplicação apenas acrescenta eventos.
/// </summary>
public sealed class HistoricoAgendamento : EntidadeBase<HistoricoAgendamento>, IPertencenteTenant
{
    private HistoricoAgendamento()
    {
    }

    public Guid BarbeariaId { get; private set; }
    public Guid AgendamentoId { get; private set; }
    public TipoEventoHistorico TipoEvento { get; private set; }
    public string? AtorUsuarioId { get; private set; }
    public DateTime OcorridoEmUtc { get; private set; }
    public DateTime? InicioAnteriorUtc { get; private set; }
    public DateTime? NovoInicioUtc { get; private set; }
    public string? Detalhes { get; private set; }

    public static HistoricoAgendamento Criar(
        Guid barbeariaId,
        Guid agendamentoId,
        TipoEventoHistorico tipoEvento,
        string? atorUsuarioId,
        DateTime ocorridoEmUtc,
        DateTime? inicioAnteriorUtc = null,
        DateTime? novoInicioUtc = null,
        string? detalhes = null)
    {
        var evento = new HistoricoAgendamento
        {
            BarbeariaId = barbeariaId,
            AgendamentoId = agendamentoId,
            TipoEvento = tipoEvento,
            AtorUsuarioId = string.IsNullOrWhiteSpace(atorUsuarioId) ? null : atorUsuarioId.Trim(),
            OcorridoEmUtc = ocorridoEmUtc,
            InicioAnteriorUtc = inicioAnteriorUtc,
            NovoInicioUtc = novoInicioUtc,
            Detalhes = string.IsNullOrWhiteSpace(detalhes) ? null : detalhes.Trim()
        };

        var erros = evento.Validar();
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Não foi possível registrar o evento: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        return evento;
    }

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (BarbeariaId == Guid.Empty)
            erros.Add(new ErroValidacao("barbeariaId", "Campo obrigatório."));
        if (AgendamentoId == Guid.Empty)
            erros.Add(new ErroValidacao("agendamentoId", "Campo obrigatório."));
        if (OcorridoEmUtc.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("ocorridoEmUtc", "Informe o momento em UTC."));
        if (Detalhes?.Length > 500)
            erros.Add(new ErroValidacao("detalhes", "Máximo de 500 caracteres."));

        return erros;
    }

    public override void Atualizar(HistoricoAgendamento entidadeAtualizada) =>
        throw new InvalidOperationException("Eventos do histórico são imutáveis.");
}
