using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Infra.Compartilhado.Orm;

/// <summary>
/// Contexto de persistência do PontoBarber. O filtro global por tenant é
/// aplicado por convenção a toda entidade que implementa IPertencenteTenant.
/// </summary>
public class ContextoPontoBarber(
    DbContextOptions<ContextoPontoBarber> options,
    IContextoTenant contextoTenant) : IdentityDbContext<Usuario>(options)
{
    private static readonly MethodInfo _fabricaEntidades = typeof(ModelBuilder)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(method => method.Name == nameof(ModelBuilder.Entity)
            && method.IsGenericMethodDefinition
            && method.GetParameters().Length == 0);

    private static readonly MethodInfo _configuradorFiltroTenant = typeof(ContextoPontoBarber)
        .GetMethod(nameof(ConfigurarFiltroTenant), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly IContextoTenant _contextoTenant = contextoTenant;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ContextoPontoBarber).Assembly);
        ConvencoesPersistencia.Aplicar(builder);
        AplicarFiltrosTenant(builder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GarantirTenantIdImutavel();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        GarantirTenantIdImutavel();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Aplica o filtro a cada entidade IPertencenteTenant. O valor do tenant é lido
    /// do IContextoTenant da instância atual do contexto em cada consulta,
    /// nunca capturado durante a construção do modelo.
    /// </summary>
    protected void AplicarFiltrosTenant(ModelBuilder construtorModelo)
    {
        foreach (var tipoEntidade in construtorModelo.Model.GetEntityTypes().ToList())
        {
            if (!typeof(IPertencenteTenant).IsAssignableFrom(tipoEntidade.ClrType))
                continue;

            var builder = _fabricaEntidades.MakeGenericMethod(tipoEntidade.ClrType)
                .Invoke(construtorModelo, null)!;

            _configuradorFiltroTenant.MakeGenericMethod(tipoEntidade.ClrType)
                .Invoke(this, [builder]);
        }
    }

    private void ConfigurarFiltroTenant<TEntidade>(EntityTypeBuilder<TEntidade> builder)
        where TEntidade : class, IPertencenteTenant
    {
        builder.HasQueryFilter(entidade => entidade.BarbeariaId == _contextoTenant.TenantId);
    }

    private void GarantirTenantIdImutavel()
    {
        foreach (var entry in ChangeTracker.Entries<IPertencenteTenant>())
        {
            if (entry.State == EntityState.Modified &&
                entry.Property(nameof(IPertencenteTenant.BarbeariaId)).IsModified)
            {
                throw new InvalidOperationException(
                    "O BarbeariaId de uma entidade não pode ser alterado após a criação.");
            }
        }
    }
}
