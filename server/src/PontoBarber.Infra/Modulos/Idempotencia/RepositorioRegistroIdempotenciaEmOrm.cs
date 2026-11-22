using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Modulos.Idempotencia;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Idempotencia;

public sealed class RepositorioRegistroIdempotenciaEmOrm(ContextoPontoBarber contexto)
    : IRegistroIdempotenciaRepositorio
{
    /// <summary>
    /// Exclusão direta no banco: não carrega registros em memória e é segura
    /// sob repetição porque afeta somente os vencidos.
    /// </summary>
    public Task<int> ExpurgarExpiradosAsync(DateTime agoraUtc, CancellationToken cancellationToken) =>
        contexto.Database.ExecuteSqlRawAsync(
            "DELETE FROM [RegistrosIdempotencia] WHERE [ExpiraEmUtc] <= {0}",
            new object[] { agoraUtc },
            cancellationToken);
}
