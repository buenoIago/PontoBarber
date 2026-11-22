namespace PontoBarber.Dominio.Modulos.Agendamentos;

/// <summary>Eventos do ciclo de vida registrados no histórico append-only.</summary>
public enum TipoEventoHistorico
{
    Criacao = 0,
    Confirmacao = 1,
    Cancelamento = 2,
    Remarcacao = 3,
    SugestaoCriada = 4,
    SugestaoAceita = 5,
    SugestaoRejeitada = 6,
    SugestaoExpirada = 7,
    NaoComparecimento = 8
}
