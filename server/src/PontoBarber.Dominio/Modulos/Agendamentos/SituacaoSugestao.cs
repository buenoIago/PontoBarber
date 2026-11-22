namespace PontoBarber.Dominio.Modulos.Agendamentos;

/// <summary>Situações de uma sugestão de novo horário (TDD §8.1).</summary>
public enum SituacaoSugestao
{
    Pendente = 0,
    Aceita = 1,
    Rejeitada = 2,
    Expirada = 3
}
