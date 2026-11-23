using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Modulos.Notificacoes;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Notificacoes;

/// <summary>
/// As notificações não têm rota com barbeariaId: o limite de acesso é o
/// DestinatarioUsuarioId autenticado, usado como predicado explícito em todas
/// as consultas (justificativa para ignorar o filtro global de tenant).
/// </summary>
public sealed class RepositorioNotificacaoEmOrm(ContextoPontoBarber contexto) : INotificacaoRepositorio
{
    public async Task<IReadOnlyList<Notificacao>> ListarAsync(
        string destinatarioUsuarioId, int pagina, int tamanho, CancellationToken cancellationToken) =>
        await contexto.Set<Notificacao>().IgnoreQueryFilters()
            .Where(item => item.DestinatarioUsuarioId == destinatarioUsuarioId)
            .OrderByDescending(item => item.CriadoEmUtc)
            .ThenByDescending(item => item.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken);

    public Task<int> ContarNaoLidasAsync(
        string destinatarioUsuarioId, CancellationToken cancellationToken) =>
        contexto.Set<Notificacao>().IgnoreQueryFilters()
            .CountAsync(
                item => item.DestinatarioUsuarioId == destinatarioUsuarioId && item.LidoEmUtc == null,
                cancellationToken);

    public Task<Notificacao?> ObterAsync(
        string destinatarioUsuarioId, Guid id, CancellationToken cancellationToken) =>
        contexto.Set<Notificacao>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                item => item.Id == id && item.DestinatarioUsuarioId == destinatarioUsuarioId,
                cancellationToken);

    public async Task<int> MarcarComoLidaAsync(
        string destinatarioUsuarioId, Guid id, DateTime agoraUtc, CancellationToken cancellationToken)
    {
        var notificacao = await ObterAsync(destinatarioUsuarioId, id, cancellationToken);
        if (notificacao is null || notificacao.LidoEmUtc is not null) return 0;

        notificacao.MarcarComoLida(destinatarioUsuarioId, agoraUtc);
        await contexto.SaveChangesAsync(cancellationToken);
        return 1;
    }

    public async Task<int> MarcarTodasAsync(
        string destinatarioUsuarioId, DateTime agoraUtc, CancellationToken cancellationToken)
    {
        var pendentes = await contexto.Set<Notificacao>().IgnoreQueryFilters()
            .Where(item => item.DestinatarioUsuarioId == destinatarioUsuarioId && item.LidoEmUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var notificacao in pendentes)
            notificacao.MarcarComoLida(destinatarioUsuarioId, agoraUtc);

        if (pendentes.Count > 0)
            await contexto.SaveChangesAsync(cancellationToken);

        return pendentes.Count;
    }
}
