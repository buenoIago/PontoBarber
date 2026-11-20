using Microsoft.EntityFrameworkCore;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

[TestClass]
public class TestesFiltroGlobalTenant : IDisposable
{
    private static readonly Guid _tenantA = Guid.NewGuid();
    private static readonly Guid _tenantB = Guid.NewGuid();

    private readonly ContextoTenantMutavel _contextoTenant = new();
    private readonly ContextoPersistenciaTeste _context;

    public TestesFiltroGlobalTenant()
    {
        var database = $"PontoBarberFiltros_{Guid.NewGuid():N}";
        var stringConexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        var options = new DbContextOptionsBuilder<ContextoPontoBarber>()
            .UseSqlServer(stringConexao)
            .Options;

        _context = new ContextoPersistenciaTeste(options, _contextoTenant);
        _context.Database.EnsureCreated();
        SemTenantAtivo();

        _context.Set<RegistroTenant>().AddRange(
            new RegistroTenant { Id = Guid.NewGuid(), BarbeariaId = _tenantA, Name = "Corte" },
            new RegistroTenant { Id = Guid.NewGuid(), BarbeariaId = _tenantB, Name = "Corte" });
        _context.Set<RegistroGlobal>().Add(
            new RegistroGlobal { Id = Guid.NewGuid(), Name = "CatalogoModelo" });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    [TestMethod]
    public void FiltroGlobal_ComTenantAAtivo_RetornaSomenteRegistrosDeA()
    {
        ComTenantAAtivo();

        var registros = _context.Set<RegistroTenant>().Where(record => record.Name == "Corte").ToList();

        Assert.AreEqual(1, registros.Count);
        foreach (var registro in registros)
            Assert.AreEqual(_tenantA, registro.BarbeariaId);
    }

    [TestMethod]
    public void FiltroGlobal_ComTenantBAtivo_RetornaSomenteRegistrosDeB()
    {
        ComTenantBAtivo();

        var registros = _context.Set<RegistroTenant>().Where(record => record.Name == "Corte").ToList();

        Assert.AreEqual(1, registros.Count);
        foreach (var registro in registros)
            Assert.AreEqual(_tenantB, registro.BarbeariaId);
    }

    [TestMethod]
    public void FiltroGlobal_SemTenantAtivo_NaoRetornaRegistrosMultiTenant()
    {
        SemTenantAtivo();

        Assert.AreEqual(0, _context.Set<RegistroTenant>().ToList().Count);
    }

    [TestMethod]
    public void FiltroGlobal_EntidadeGlobal_PermaneceAcessivelSemTenant()
    {
        SemTenantAtivo();

        var registros = _context.Set<RegistroGlobal>().ToList();

        Assert.AreEqual(1, registros.Count);
        Assert.AreEqual("CatalogoModelo", registros[0].Name);
    }

    [TestMethod]
    public void FiltroGlobal_AlteracaoDoTenantId_NaoPermiteAcessoNemModificacao()
    {
        ComTenantAAtivo();
        var registro = ObterUnico(_context.Set<RegistroTenant>().ToList());
        var idRegistroA = registro.Id;

        registro.BarbeariaId = _tenantB;

        // O registro de A continua invisível para B, mesmo com o valor alterado em memória.
        ComTenantBAtivo();
        Assert.IsFalse(_context.Set<RegistroTenant>().ToList().Any(item => item.Id == idRegistroA));

        // A tentativa de mover o registro para outro tenant é bloqueada.
        Assert.ThrowsExactly<InvalidOperationException>(() => _context.SaveChanges());

        _context.ChangeTracker.Clear();
        ComTenantAAtivo();
        var persistido = ObterUnico(_context.Set<RegistroTenant>().Where(item => item.Id == idRegistroA).ToList());
        Assert.AreEqual(_tenantA, persistido.BarbeariaId);
    }

    [TestMethod]
    public void FiltroGlobal_MesmaInstanciaComContextoTrocado_ReavaliaOTenant()
    {
        ComTenantAAtivo();
        Assert.AreEqual(_tenantA, ObterUnico(_context.Set<RegistroTenant>().ToList()).BarbeariaId);

        ComTenantBAtivo();
        Assert.AreEqual(_tenantB, ObterUnico(_context.Set<RegistroTenant>().ToList()).BarbeariaId);

        SemTenantAtivo();
        Assert.AreEqual(0, _context.Set<RegistroTenant>().ToList().Count);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private static T ObterUnico<T>(List<T> itens)
    {
        Assert.AreEqual(1, itens.Count);
        return itens.First();
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
        public Guid BarbeariaId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class RegistroGlobal
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
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
