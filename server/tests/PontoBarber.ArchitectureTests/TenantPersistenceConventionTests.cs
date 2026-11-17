using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PontoBarber.Domain.Common;
using PontoBarber.Infrastructure.Persistence;

namespace PontoBarber.ArchitectureTests;

public class TenantPersistenceConventionTests
{
    // Exceções registradas no TDD §8.1 e §11: identidades e perfis globais,
    // a raiz do tenant (cujo Id é o TenantId) e registros operacionais.
    private static readonly HashSet<string> _entidadesGlobais = new(StringComparer.Ordinal)
    {
        "User",
        "CustomerProfile",
        "ProfessionalProfile",
        "Barbershop",
        "IdempotencyRecord",
        "JobRun"
    };

    [Fact]
    public void EntidadesDeNegocio_SemTenantId_SaoRejeitadas()
    {
        var entidades = typeof(IEntity).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => typeof(IEntity).IsAssignableFrom(type)
                || type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)?.PropertyType == typeof(Guid))
            .ToList();

        var semTenant = entidades
            .Where(type => !typeof(ITenantOwned).IsAssignableFrom(type) && !_entidadesGlobais.Contains(type.Name))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            semTenant.Count == 0,
            $"Entidades de negócio sem ITenantOwned/TenantId: {string.Join(", ", semTenant)}");
    }

    [Fact]
    public void PersistenceConventions_ComEntidadeTenant_ConfiguraTenantIdEDataUtc()
    {
        using var context = new ProbeDbContext();

        var entityType = context.Model.FindEntityType(typeof(ProbeTenantEntity))!;
        var tenantId = entityType.FindProperty(nameof(ITenantOwned.TenantId))!;
        var criadoEmUtc = entityType.FindProperty(nameof(ProbeTenantEntity.CreatedAtUtc))!;
        var opcionalUtc = entityType.FindProperty(nameof(ProbeTenantEntity.OptionalAtUtc))!;

        Assert.False(tenantId.IsNullable);
        Assert.Equal("TenantId", tenantId.GetColumnName());
        Assert.Equal("datetime2(3)", criadoEmUtc.GetColumnType());
        Assert.Equal("datetime2(3)", opcionalUtc.GetColumnType());
    }

    [Fact]
    public void PersistenceConventions_ComPropriedadeSemSufixoUtc_RejeitaMapeamento()
    {
        using var context = new ProbeContextSemSufixo();

        var exception = Assert.Throws<InvalidOperationException>(() => { _ = context.Model; });

        Assert.Contains("CreatedAt", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Utc", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PersistenceConventions_ComInstanteUtc_NormalizaKindNaLeituraEEscrita()
    {
        using var context = new ProbeDbContext();

        var entityType = context.Model.FindEntityType(typeof(ProbeTenantEntity))!;
        var criadoEmUtc = entityType.FindProperty(nameof(ProbeTenantEntity.CreatedAtUtc))!;
        var opcionalUtc = entityType.FindProperty(nameof(ProbeTenantEntity.OptionalAtUtc))!;

        var conversor = criadoEmUtc.GetValueConverter()!;
        var local = DateTime.SpecifyKind(new DateTime(2026, 11, 17, 10, 30, 0), DateTimeKind.Local);

        var escrito = (DateTime)conversor.ConvertToProvider(local)!;
        Assert.Equal(DateTimeKind.Utc, escrito.Kind);
        Assert.Equal(local.ToUniversalTime(), escrito);

        var lido = (DateTime)conversor.ConvertFromProvider(
            new DateTime(2026, 11, 17, 13, 30, 0, DateTimeKind.Unspecified))!;
        Assert.Equal(DateTimeKind.Utc, lido.Kind);
        Assert.Equal(13, lido.Hour);

        var conversorOpcional = opcionalUtc.GetValueConverter()!;
        Assert.Null(conversorOpcional.ConvertToProvider(null));

        var opcionalEscrito = (DateTime?)conversorOpcional.ConvertToProvider(local);
        Assert.Equal(DateTimeKind.Utc, opcionalEscrito!.Value.Kind);
    }

    public sealed class ProbeTenantEntity : ITenantOwned
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? OptionalAtUtc { get; set; }
    }

    private sealed class ProbeDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=PontoBarberProbe;Trusted_Connection=True;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ProbeTenantEntity>();
            PersistenceConventions.Apply(modelBuilder);
        }
    }

    public sealed class ProbeDataLocalEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class ProbeContextSemSufixo : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=PontoBarberProbe;Trusted_Connection=True;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ProbeDataLocalEntity>();
            PersistenceConventions.Apply(modelBuilder);
        }
    }
}
