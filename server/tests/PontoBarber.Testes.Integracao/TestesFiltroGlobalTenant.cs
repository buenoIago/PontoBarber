using Microsoft.EntityFrameworkCore;
using PontoBarber.Aplicacao.Abstracoes;
using PontoBarber.Dominio.Comum;
using PontoBarber.Infra.Persistencia;

namespace PontoBarber.Testes.Integracao;

public class TestesFiltroGlobalTenant : IDisposable
{
    private static readonly Guid _tenantA = Guid.NewGuid();
    private static readonly Guid _tenantB = Guid.NewGuid();

    private readonly string _connectionString;
    private readonly ContextoTenantMutavel _contextoTenant = new();
    private readonly ContextoPersistenciaTeste _context;

    public TestesFiltroGlobalTenant()
    {
        var database = $"PontoBarberFiltros_{Guid.NewGuid():N}";
        _connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={database};Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<ContextoPontoBarber>()
            .UseSqlServer(_connectionString)
            .Options;

        _context = new ContextoPersistenciaTeste(options, _contextoTenant);
        _context.Database.EnsureCreated();
        SemTenantAtivo();

        _context.Set<RegistroTenant>().AddRange(
            new RegistroTenant { Id = Guid.NewGuid(), TenantId = _tenantA, Name = "Corte" },
            new RegistroTenant { Id = Guid.NewGuid(), TenantId = _tenantB, Name = "Corte" });
        _context.Set<RegistroGlobal>().Add(
            new RegistroGlobal { Id = Guid.NewGuid(), Name = "CatalogoModelo" });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    [Fact]
    public void FiltroGlobal_ComTenantAAtivo_RetornaSomenteRegistrosDeA()
    {
        ComTenantAAtivo();

        var registros = _context.Set<RegistroTenant>().Where(record => record.Name == "Corte").ToList();

        Assert.Single(registros);
        Assert.All(registros, record => Assert.Equal(_tenantA, record.TenantId));
    }

    [Fact]
    public void FiltroGlobal_ComTenantBAtivo_RetornaSomenteRegistrosDeB()
    {
        ComTenantBAtivo();

        var registros = _context.Set<RegistroTenant>().Where(record => record.Name == "Corte").ToList();

        Assert.Single(registros);
        Assert.All(registros, record => Assert.Equal(_tenantB, record.TenantId));
    }

    [Fact]
    public void FiltroGlobal_SemTenantAtivo_NaoRetornaRegistrosMultiTenant()
    {
        SemTenantAtivo();

        Assert.Empty(_context.Set<RegistroTenant>().ToList());
    }

    [Fact]
    public void FiltroGlobal_EntidadeGlobal_PermaneceAcessivelSemTenant()
    {
        SemTenantAtivo();

        var registros = _context.Set<RegistroGlobal>().ToList();

        Assert.Single(registros);
        Assert.Equal("CatalogoModelo", registros[0].Name);
    }

    [Fact]
    public void FiltroGlobal_AlteracaoDoTenantId_NaoPermiteAcessoNemModificacao()
    {
        ComTenantAAtivo();
        var registro = Assert.Single(_context.Set<RegistroTenant>().ToList());
        var idRegistroA = registro.Id;

        registro.TenantId = _tenantB;

        // O registro de A continua invisível para B, mesmo com o valor alterado em memória.
        ComTenantBAtivo();
        Assert.DoesNotContain(_context.Set<RegistroTenant>().ToList(), item => item.Id == idRegistroA);

        // A tentativa de mover o registro para outro tenant é bloqueada.
        Assert.Throws<InvalidOperationException>(() => _context.SaveChanges());

        _context.ChangeTracker.Clear();
        ComTenantAAtivo();
        var persistido = Assert.Single(_context.Set<RegistroTenant>().Where(item => item.Id == idRegistroA).ToList());
        Assert.Equal(_tenantA, persistido.TenantId);
    }

    [Fact]
    public void FiltroGlobal_MesmaInstanciaComContextoTrocado_ReavaliaOTenant()
    {
        ComTenantAAtivo();
        Assert.Equal(_tenantA, Assert.Single(_context.Set<RegistroTenant>().ToList()).TenantId);

        ComTenantBAtivo();
        Assert.Equal(_tenantB, Assert.Single(_context.Set<RegistroTenant>().ToList()).TenantId);

        SemTenantAtivo();
        Assert.Empty(_context.Set<RegistroTenant>().ToList());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ComTenantAAtivo() => _contextoTenant.TenantId = _tenantA;

    private void ComTenantBAtivo() => _contextoTenant.TenantId = _tenantB;

    private void SemTenantAtivo() => _contextoTenant.TenantId = null;

    private sealed class ContextoTenantMutavel : IContextoTenant
    {
        public Guid? TenantId { get; set; }
    }

    public sealed class RegistroTenant : IPertencenteTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class RegistroGlobal : IEntidade
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ContextoPersistenciaTeste(
        DbContextOptions<ContextoPontoBarber> options,
        IContextoTenant contextoTenant) : ContextoPontoBarber(options, contextoTenant)
    {
        protected override void OnModelCreating(ModelBuilder construtorModelo)
        {
            base.OnModelCreating(construtorModelo);
            construtorModelo.Entity<RegistroTenant>();
            construtorModelo.Entity<RegistroGlobal>();
            AplicarFiltrosTenant(construtorModelo);
        }
    }
}
