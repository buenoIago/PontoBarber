namespace PontoBarber.Dominio.Modulos.Agendamentos;

/// <summary>Situações usadas no MVP; estados de atendimento pertencem à seção 8.</summary>
public enum SituacaoAgendamento
{
    Confirmado = 0,
    Cancelado = 1,
    NaoCompareceu = 2,
    EmAndamento = 3,
    Concluido = 4
}
