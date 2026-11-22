namespace PontoBarber.Dominio.Modulos.Idempotencia;

/// <summary>Persistência da estrutura oficial de idempotência (ADR-021).</summary>
public interface IRegistroIdempotenciaRepositorio
{
    /// <summary>Remove em uma única operação somente os registros vencidos.</summary>
    Task<int> ExpurgarExpiradosAsync(DateTime agoraUtc, CancellationToken cancellationToken);
}
