namespace PontoBarber.Dominio.Modulos.Agendamentos;

/// <summary>Resultado das operações transacionais do ciclo de vida.</summary>
public enum ResultadoCicloVida
{
    Sucesso,
    NaoEncontrado,
    EstadoInvalido,
    JaIniciado,
    Conflito,
    Indisponivel
}
