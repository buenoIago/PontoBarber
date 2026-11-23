namespace PontoBarber.Dominio.Modulos.Notificacoes;

/// <summary>Leitura e marcação de leitura das notificações do próprio usuário.</summary>
public interface INotificacaoRepositorio
{
    Task<IReadOnlyList<Notificacao>> ListarAsync(
        string destinatarioUsuarioId, int pagina, int tamanho, CancellationToken cancellationToken);

    Task<int> ContarNaoLidasAsync(string destinatarioUsuarioId, CancellationToken cancellationToken);

    Task<Notificacao?> ObterAsync(
        string destinatarioUsuarioId, Guid id, CancellationToken cancellationToken);

    Task<int> MarcarComoLidaAsync(
        string destinatarioUsuarioId, Guid id, DateTime agoraUtc, CancellationToken cancellationToken);

    Task<int> MarcarTodasAsync(
        string destinatarioUsuarioId, DateTime agoraUtc, CancellationToken cancellationToken);
}

/// <summary>Reconciliação do outbox leve: eventos sem notificação.</summary>
public interface IReconciliacaoNotificacoes
{
    Task<int> ReconciliarAsync(DateTime agoraUtc, CancellationToken cancellationToken);
}
