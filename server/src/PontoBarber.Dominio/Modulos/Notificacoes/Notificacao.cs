using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Notificacoes;

/// <summary>
/// Notificação interna usada como destino idempotente do outbox leve descrito
/// no TDD §12.1. Aplicação e API de notificações pertencem à seção 13; aqui
/// existe apenas a persistência necessária aos processos periódicos.
/// </summary>
public sealed class Notificacao : EntidadeBase<Notificacao>, IPertencenteTenant
{
    private Notificacao()
    {
    }

    public Guid BarbeariaId { get; private set; }
    public string DestinatarioUsuarioId { get; private set; } = string.Empty;
    public TipoNotificacao Tipo { get; private set; }
    public Guid? AgendamentoId { get; private set; }
    public string Mensagem { get; private set; } = string.Empty;
    public Guid EventId { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime? LidoEmUtc { get; private set; }

    public static Notificacao Criar(
        Guid barbeariaId,
        string destinatarioUsuarioId,
        TipoNotificacao tipo,
        Guid? agendamentoId,
        string mensagem,
        Guid eventId,
        DateTime criadoEmUtc)
    {
        var notificacao = new Notificacao
        {
            BarbeariaId = barbeariaId,
            DestinatarioUsuarioId = destinatarioUsuarioId,
            Tipo = tipo,
            AgendamentoId = agendamentoId,
            Mensagem = mensagem?.Trim() ?? string.Empty,
            EventId = eventId,
            CriadoEmUtc = criadoEmUtc
        };

        var erros = notificacao.Validar();
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Não foi possível criar a notificação: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        return notificacao;
    }

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (BarbeariaId == Guid.Empty)
            erros.Add(new ErroValidacao("barbeariaId", "Campo obrigatório."));
        if (string.IsNullOrWhiteSpace(DestinatarioUsuarioId))
            erros.Add(new ErroValidacao("destinatarioUsuarioId", "Campo obrigatório."));
        if (string.IsNullOrWhiteSpace(Mensagem))
            erros.Add(new ErroValidacao("mensagem", "Campo obrigatório."));
        else if (Mensagem.Length > 500)
            erros.Add(new ErroValidacao("mensagem", "Máximo de 500 caracteres."));
        if (EventId == Guid.Empty)
            erros.Add(new ErroValidacao("eventId", "Campo obrigatório."));
        if (CriadoEmUtc.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("criadoEmUtc", "Informe a data em UTC."));

        return erros;
    }

    public override void Atualizar(Notificacao entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (Id != entidadeAtualizada.Id ||
            BarbeariaId != entidadeAtualizada.BarbeariaId ||
            DestinatarioUsuarioId != entidadeAtualizada.DestinatarioUsuarioId ||
            Tipo != entidadeAtualizada.Tipo ||
            AgendamentoId != entidadeAtualizada.AgendamentoId ||
            Mensagem != entidadeAtualizada.Mensagem ||
            EventId != entidadeAtualizada.EventId ||
            CriadoEmUtc != entidadeAtualizada.CriadoEmUtc ||
            LidoEmUtc != entidadeAtualizada.LidoEmUtc)
            throw new InvalidOperationException("Os dados de uma notificação não podem ser alterados.");
    }
}
