namespace PontoBarber.Dominio.Modulos.Jobs;

/// <summary>Estados simples e consistentes de uma execução de processo.</summary>
public enum ResultadoExecucaoJob
{
    EmExecucao = 0,
    Sucesso = 1,
    Falha = 2
}
