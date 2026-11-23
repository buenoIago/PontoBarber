using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Notificacoes.DTOs;
using PontoBarber.Dominio.Modulos.Notificacoes;

namespace PontoBarber.Aplicacao.Modulos.Notificacoes;

/// <summary>
/// Consulta e leitura das notificações do próprio usuário. A identidade vem
/// sempre da sessão autenticada, nunca de parâmetros enviados pelo cliente.
/// </summary>
public sealed class ServicoNotificacoes(
    INotificacaoRepositorio repositorio,
    IIdentidade identidade)
{
    private const int _tamanhoPadrao = 20;
    private const int _tamanhoMaximo = 100;

    public async Task<ListaNotificacoesResponse> ListarAsync(
        int? pagina,
        int? tamanho,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirAutenticadoAsync(usuarioId, cancellationToken);

        var paginaEfetiva = Math.Max(1, pagina ?? 1);
        var tamanhoEfetivo = Math.Clamp(tamanho ?? _tamanhoPadrao, 1, _tamanhoMaximo);

        var itens = await repositorio.ListarAsync(
            usuarioId, paginaEfetiva, tamanhoEfetivo, cancellationToken);
        var naoLidas = await repositorio.ContarNaoLidasAsync(usuarioId, cancellationToken);

        return new ListaNotificacoesResponse(
            itens.Select(ParaResponse).ToList(), naoLidas, paginaEfetiva, tamanhoEfetivo);
    }

    public async Task<int> ContarNaoLidasAsync(string usuarioId, CancellationToken cancellationToken)
    {
        await ExigirAutenticadoAsync(usuarioId, cancellationToken);
        return await repositorio.ContarNaoLidasAsync(usuarioId, cancellationToken);
    }

    public async Task MarcarComoLidaAsync(
        Guid id, string usuarioId, CancellationToken cancellationToken)
    {
        await ExigirAutenticadoAsync(usuarioId, cancellationToken);

        var quantidade = await repositorio.MarcarComoLidaAsync(
            usuarioId, id, DateTime.UtcNow, cancellationToken);
        if (quantidade == 0)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Notificação não encontrada para este usuário.");
    }

    public async Task<int> MarcarTodasAsync(string usuarioId, CancellationToken cancellationToken)
    {
        await ExigirAutenticadoAsync(usuarioId, cancellationToken);
        return await repositorio.MarcarTodasAsync(usuarioId, DateTime.UtcNow, cancellationToken);
    }

    private async Task ExigirAutenticadoAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null)
            throw new ExcecaoAutorizacaoAplicacao(
                CodigosErro.AutenticacaoNegada, "Usuário não autenticado.");
    }

    private static NotificacaoResponse ParaResponse(Notificacao notificacao) =>
        new(
            notificacao.Id,
            notificacao.BarbeariaId,
            notificacao.Tipo.ToString(),
            notificacao.AgendamentoId,
            notificacao.Mensagem,
            notificacao.CriadoEmUtc,
            notificacao.LidoEmUtc);
}
