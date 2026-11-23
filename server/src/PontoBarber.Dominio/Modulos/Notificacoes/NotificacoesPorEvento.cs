using PontoBarber.Dominio.Modulos.Agendamentos;

namespace PontoBarber.Dominio.Modulos.Notificacoes;

/// <summary>
/// Regras puras de mapeamento de eventos do ciclo de vida para notificações
/// internas. Os destinatários são os usuários envolvidos no agendamento, não
/// todos os usuários da barbearia (CA-NOTIF-001).
/// </summary>
public static class NotificacoesPorEvento
{
    public static TipoNotificacao? Mapear(TipoEventoHistorico evento) => evento switch
    {
        TipoEventoHistorico.Criacao or TipoEventoHistorico.Confirmacao => TipoNotificacao.AgendamentoCriado,
        TipoEventoHistorico.Cancelamento => TipoNotificacao.AgendamentoCancelado,
        TipoEventoHistorico.SugestaoCriada => TipoNotificacao.SugestaoCriada,
        TipoEventoHistorico.SugestaoAceita => TipoNotificacao.SugestaoAceita,
        TipoEventoHistorico.SugestaoRejeitada => TipoNotificacao.SugestaoRejeitada,
        TipoEventoHistorico.SugestaoExpirada => TipoNotificacao.SugestaoExpirada,
        TipoEventoHistorico.ServicoIniciado => TipoNotificacao.ServicoIniciado,
        TipoEventoHistorico.ServicoConcluido => TipoNotificacao.ServicoConcluido,
        TipoEventoHistorico.PagamentoRegistrado => TipoNotificacao.PagamentoRegistrado,
        _ => null
    };

    public static IReadOnlyList<Notificacao> Criar(
        Guid barbeariaId,
        Guid agendamentoId,
        Guid eventId,
        TipoNotificacao tipo,
        string clienteId,
        string? profissionalUsuarioId,
        string? donoUsuarioId,
        string nomeServico,
        DateTime agoraUtc)
    {
        var destinatarios = new HashSet<string>(StringComparer.Ordinal);

        switch (tipo)
        {
            case TipoNotificacao.SugestaoCriada:
                Adicionar(destinatarios, clienteId);
                break;
            case TipoNotificacao.SugestaoAceita:
                Adicionar(destinatarios, clienteId);
                Adicionar(destinatarios, profissionalUsuarioId);
                Adicionar(destinatarios, donoUsuarioId);
                break;
            case TipoNotificacao.SugestaoRejeitada:
                Adicionar(destinatarios, profissionalUsuarioId);
                Adicionar(destinatarios, donoUsuarioId);
                break;
            case TipoNotificacao.ServicoIniciado:
            case TipoNotificacao.ServicoConcluido:
            case TipoNotificacao.PagamentoRegistrado:
                Adicionar(destinatarios, clienteId);
                Adicionar(destinatarios, donoUsuarioId);
                break;
            default:
                Adicionar(destinatarios, clienteId);
                Adicionar(destinatarios, profissionalUsuarioId);
                Adicionar(destinatarios, donoUsuarioId);
                break;
        }

        var mensagem = Mensagem(tipo, nomeServico);
        return destinatarios
            .Select(destinatario => Notificacao.Criar(
                barbeariaId, destinatario, tipo, agendamentoId, mensagem, eventId, agoraUtc))
            .ToList();
    }

    private static void Adicionar(HashSet<string> destinatarios, string? usuarioId)
    {
        if (!string.IsNullOrWhiteSpace(usuarioId))
            destinatarios.Add(usuarioId);
    }

    private static string Mensagem(TipoNotificacao tipo, string nomeServico) => tipo switch
    {
        TipoNotificacao.AgendamentoCriado => $"Novo agendamento confirmado: {nomeServico}.",
        TipoNotificacao.AgendamentoCancelado => $"Agendamento cancelado: {nomeServico}.",
        TipoNotificacao.SugestaoCriada => $"Um novo horário foi sugerido para {nomeServico}.",
        TipoNotificacao.SugestaoAceita => $"A sugestão de horário de {nomeServico} foi aceita.",
        TipoNotificacao.SugestaoRejeitada => $"A sugestão de horário de {nomeServico} foi rejeitada.",
        TipoNotificacao.SugestaoExpirada => "O horário sugerido expirou e a reserva temporária foi liberada.",
        TipoNotificacao.ServicoIniciado => $"O atendimento de {nomeServico} foi iniciado.",
        TipoNotificacao.ServicoConcluido => $"O atendimento de {nomeServico} foi concluído.",
        TipoNotificacao.PagamentoRegistrado => $"Pagamento registrado para {nomeServico}.",
        _ => "Atualização no agendamento."
    };
}
