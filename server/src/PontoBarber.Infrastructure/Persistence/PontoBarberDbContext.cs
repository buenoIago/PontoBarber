using Microsoft.EntityFrameworkCore;

namespace PontoBarber.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistência do PontoBarber. Os filtros globais por tenant
/// pertencem à tarefa 1.5 e não são aplicados nesta etapa.
/// </summary>
public sealed class PontoBarberDbContext(DbContextOptions<PontoBarberDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PontoBarberDbContext).Assembly);
        PersistenceConventions.Apply(modelBuilder);
    }
}
