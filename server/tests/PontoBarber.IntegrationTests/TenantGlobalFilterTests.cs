using Microsoft.EntityFrameworkCore;
using PontoBarber.Application.Abstractions;
using PontoBarber.Domain.Common;
using PontoBarber.Infrastructure.Persistence;

namespace PontoBarber.IntegrationTests;

public class TenantGlobalFilterTests : IDisposable
{
    private static readonly Guid _tenantA = Guid.NewGuid();
    private static readonly Guid _tenantB = Guid.NewGuid();

    private readonly string _connectionString;
    private readonly MutableTenantContext _tenantContext = new();
    private readonly TestTenantDbContext _context;

    public TenantGlobalFilterTests()
    {
        var database = $"PontoBarberFiltros_{Guid.NewGuid():N}";
        _connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={database};Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<PontoBarberDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        _context = new TestTenantDbContext(options, _tenantContext);
        _context.Database.EnsureCreated();
        SemTenantAtivo();

        _context.Set<TenantRecord>().AddRange(
            new TenantRecord { Id = Guid.NewGuid(), TenantId = _tenantA, Name = "Corte" },
            new TenantRecord { Id = Guid.NewGuid(), TenantId = _tenantB, Name = "Corte" });
        _context.Set<GlobalRecord>().Add(
            new GlobalRecord { Id = Guid.NewGuid(), Name = "CatalogoModelo" });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    [Fact]
    public void FiltroGlobal_ComTenantAAtivo_RetornaSomenteRegistrosDeA()
    {
        ComTenantAAtivo();

        var registros = _context.Set<TenantRecord>().Where(record => record.Name == "Corte").ToList();

        Assert.Single(registros);
        Assert.All(registros, record => Assert.Equal(_tenantA, record.TenantId));
    }

    [Fact]
    public void FiltroGlobal_ComTenantBAtivo_RetornaSomenteRegistrosDeB()
    {
        ComTenantBAtivo();

        var registros = _context.Set<TenantRecord>().Where(record => record.Name == "Corte").ToList();

        Assert.Single(registros);
        Assert.All(registros, record => Assert.Equal(_tenantB, record.TenantId));
    }

    [Fact]
    public void FiltroGlobal_SemTenantAtivo_NaoRetornaRegistrosMultiTenant()
    {
        SemTenantAtivo();

        Assert.Empty(_context.Set<TenantRecord>().ToList());
    }

    [Fact]
    public void FiltroGlobal_EntidadeGlobal_PermaneceAcessivelSemTenant()
    {
        SemTenantAtivo();

        var registros = _context.Set<GlobalRecord>().ToList();

        Assert.Single(registros);
        Assert.Equal("CatalogoModelo", registros[0].Name);
    }

    [Fact]
    public void FiltroGlobal_AlteracaoDoTenantId_NaoPermiteAcessoNemModificacao()
    {
        ComTenantAAtivo();
        var registro = Assert.Single(_context.Set<TenantRecord>().ToList());
        var idRegistroA = registro.Id;

        registro.TenantId = _tenantB;

        // O registro de A continua invisível para B, mesmo com o valor alterado em memória.
        ComTenantBAtivo();
        Assert.DoesNotContain(_context.Set<TenantRecord>().ToList(), item => item.Id == idRegistroA);

        // A tentativa de mover o registro para outro tenant é bloqueada.
        Assert.Throws<InvalidOperationException>(() => _context.SaveChanges());

        _context.ChangeTracker.Clear();
        ComTenantAAtivo();
        var persistido = Assert.Single(_context.Set<TenantRecord>().Where(item => item.Id == idRegistroA).ToList());
        Assert.Equal(_tenantA, persistido.TenantId);
    }

    [Fact]
    public void FiltroGlobal_MesmaInstanciaComContextoTrocado_ReavaliaOTenant()
    {
        ComTenantAAtivo();
        Assert.Equal(_tenantA, Assert.Single(_context.Set<TenantRecord>().ToList()).TenantId);

        ComTenantBAtivo();
        Assert.Equal(_tenantB, Assert.Single(_context.Set<TenantRecord>().ToList()).TenantId);

        SemTenantAtivo();
        Assert.Empty(_context.Set<TenantRecord>().ToList());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ComTenantAAtivo() => _tenantContext.TenantId = _tenantA;

    private void ComTenantBAtivo() => _tenantContext.TenantId = _tenantB;

    private void SemTenantAtivo() => _tenantContext.TenantId = null;

    private sealed class MutableTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
    }

    public sealed class TenantRecord : ITenantOwned
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class GlobalRecord : IEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestTenantDbContext(
        DbContextOptions<PontoBarberDbContext> options,
        ITenantContext tenantContext) : PontoBarberDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TenantRecord>();
            modelBuilder.Entity<GlobalRecord>();
            ApplyTenantFilters(modelBuilder);
        }
    }
}
