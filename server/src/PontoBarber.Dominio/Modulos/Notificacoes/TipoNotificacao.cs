namespace PontoBarber.Dominio.Modulos.Notificacoes;

/// <summary>Tipos de notificação interna previstos no TDD §8.1.</summary>
public enum TipoNotificacao
{
    SugestaoExpirada = 0,
    AgendamentoCriado = 1,
    AgendamentoCancelado = 2,
    SugestaoCriada = 3,
    SugestaoAceita = 4,
    SugestaoRejeitada = 5,
    ServicoIniciado = 6,
    ServicoConcluido = 7,
    PagamentoRegistrado = 8
}
