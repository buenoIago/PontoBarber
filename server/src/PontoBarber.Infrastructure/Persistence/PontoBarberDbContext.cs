using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Application.Abstractions;
using PontoBarber.Domain.Common;

namespace PontoBarber.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistência do PontoBarber. O filtro global por tenant é
/// aplicado por convenção a toda entidade que implementa ITenantOwned.
/// </summary>
public class PontoBarberDbContext(
    DbContextOptions<PontoBarberDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    private static readonly MethodInfo _entityFactory = typeof(ModelBuilder)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(method => method.Name == nameof(ModelBuilder.Entity)
            && method.IsGenericMethodDefinition
            && method.GetParameters().Length == 0);

    private static readonly MethodInfo _tenantFilterConfigurator = typeof(PontoBarberDbContext)
        .GetMethod(nameof(ConfigureTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly ITenantContext _tenantContext = tenantContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PontoBarberDbContext).Assembly);
        PersistenceConventions.Apply(modelBuilder);
        ApplyTenantFilters(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureTenantIdIsImmutable();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureTenantIdIsImmutable();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Aplica o filtro a cada entidade ITenantOwned. O valor do tenant é lido
    /// do ITenantContext da instância atual do contexto em cada consulta,
    /// nunca capturado durante a construção do modelo.
    /// </summary>
    protected void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (!typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
                continue;

            var builder = _entityFactory.MakeGenericMethod(entityType.ClrType)
                .Invoke(modelBuilder, null)!;

            _tenantFilterConfigurator.MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [builder]);
        }
    }

    private void ConfigureTenantFilter<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ITenantOwned
    {
        builder.HasQueryFilter(entity => entity.TenantId == _tenantContext.TenantId);
    }

    private void EnsureTenantIdIsImmutable()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State == EntityState.Modified &&
                entry.Property(nameof(ITenantOwned.TenantId)).IsModified)
            {
                throw new InvalidOperationException(
                    "O TenantId de uma entidade não pode ser alterado após a criação.");
            }
        }
    }
}
