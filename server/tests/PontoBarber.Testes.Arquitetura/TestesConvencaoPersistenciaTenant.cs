using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PontoBarber.Dominio.Comum;
using PontoBarber.Infra.Persistencia;

namespace PontoBarber.Testes.Arquitetura;

public class TestesConvencaoPersistenciaTenant
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
        var entidades = typeof(IEntidade).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => typeof(IEntidade).IsAssignableFrom(type)
                || type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)?.PropertyType == typeof(Guid))
            .ToList();

        var semTenant = entidades
            .Where(type => !typeof(IPertencenteTenant).IsAssignableFrom(type) && !_entidadesGlobais.Contains(type.Name))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            semTenant.Count == 0,
            $"Entidades de negócio sem IPertencenteTenant/TenantId: {string.Join(", ", semTenant)}");
    }

    [Fact]
    public void ConvencoesPersistencia_ComEntidadeTenant_ConfiguraTenantIdEDataUtc()
    {
        using var contexto = new ContextoTeste();

        var tipoEntidade = contexto.Model.FindEntityType(typeof(EntidadeTenantTeste))!;
        var tenantId = tipoEntidade.FindProperty(nameof(IPertencenteTenant.TenantId))!;
        var criadoEmUtc = tipoEntidade.FindProperty(nameof(EntidadeTenantTeste.CreatedAtUtc))!;
        var opcionalUtc = tipoEntidade.FindProperty(nameof(EntidadeTenantTeste.OptionalAtUtc))!;

        Assert.False(tenantId.IsNullable);
        Assert.Equal("TenantId", tenantId.GetColumnName());
        Assert.Equal("datetime2(3)", criadoEmUtc.GetColumnType());
        Assert.Equal("datetime2(3)", opcionalUtc.GetColumnType());
    }

    [Fact]
    public void ConvencoesPersistencia_ComPropriedadeSemSufixoUtc_RejeitaMapeamento()
    {
        using var contexto = new ContextoSemSufixoTeste();

        var excecao = Assert.Throws<InvalidOperationException>(() => { _ = contexto.Model; });

        Assert.Contains("CreatedAt", excecao.Message, StringComparison.Ordinal);
        Assert.Contains("Utc", excecao.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConvencoesPersistencia_ComInstanteUtc_NormalizaKindNaLeituraEEscrita()
    {
        using var contexto = new ContextoTeste();

        var tipoEntidade = contexto.Model.FindEntityType(typeof(EntidadeTenantTeste))!;
        var criadoEmUtc = tipoEntidade.FindProperty(nameof(EntidadeTenantTeste.CreatedAtUtc))!;
        var opcionalUtc = tipoEntidade.FindProperty(nameof(EntidadeTenantTeste.OptionalAtUtc))!;

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

    public sealed class EntidadeTenantTeste : IPertencenteTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? OptionalAtUtc { get; set; }
    }

    private sealed class ContextoTeste : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=PontoBarberProbe;Trusted_Connection=True;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder construtorModelo)
        {
            base.OnModelCreating(construtorModelo);
            construtorModelo.Entity<EntidadeTenantTeste>();
            ConvencoesPersistencia.Aplicar(construtorModelo);
        }
    }

    public sealed class EntidadeDataLocalTeste
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class ContextoSemSufixoTeste : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=PontoBarberProbe;Trusted_Connection=True;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder construtorModelo)
        {
            base.OnModelCreating(construtorModelo);
            construtorModelo.Entity<EntidadeDataLocalTeste>();
            ConvencoesPersistencia.Aplicar(construtorModelo);
        }
    }
}
