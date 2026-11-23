namespace PontoBarber.Aplicacao.Modulos.Notificacoes.DTOs;

public sealed record NotificacaoResponse(
    Guid Id,
    Guid BarbeariaId,
    string Tipo,
    Guid? AgendamentoId,
    string Mensagem,
    DateTime CriadoEmUtc,
    DateTime? LidoEmUtc);

public sealed record ListaNotificacoesResponse(
    IReadOnlyList<NotificacaoResponse> Itens,
    int NaoLidas,
    int Pagina,
    int Tamanho);
