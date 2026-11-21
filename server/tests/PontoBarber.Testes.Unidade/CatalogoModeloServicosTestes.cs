using PontoBarber.Aplicacao.Modulos.Servicos;
using PontoBarber.Dominio.Modulos.Servicos;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>Task 5.1: catálogo modelo, idempotência e isolamento por barbearia.</summary>
[TestClass]
public sealed class CatalogoModeloServicosTestes
{
    private static readonly Guid _barbeariaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _barbeariaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [TestMethod]
    public void Catalogo_TemOitoServicosComDuracoesDefinidasESemPreco()
    {
        Assert.AreEqual(8, CatalogoModeloServicos.Todos.Count);

        var esperado = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Corte social / tradicional"] = 30,
            ["Corte degradê (fade / navalhado)"] = 40,
            ["Corte infantil"] = 30,
            ["Raspagem na máquina ou navalha (cabeça inteira)"] = 20,
            ["Acabamento / Pezinho"] = 15,
            ["Lavagem e finalização (modelagem com pomada ou cera)"] = 20,
            ["Barba simples (máquina e tesoura)"] = 20,
            ["Barba completa (alinhamento com navalha e pós-barba)"] = 30
        };

        foreach (var (nome, duracao) in esperado)
        {
            var modelo = CatalogoModeloServicos.Todos.Single(item => item.Nome == nome);
            Assert.AreEqual(duracao, modelo.DuracaoMinutos);

            var servico = Servico.CriarModelo(_barbeariaA, modelo.Nome, modelo.DuracaoMinutos);
            Assert.AreEqual(_barbeariaA, servico.BarbeariaId);
            Assert.AreEqual(duracao, servico.DuracaoMinutos);
            Assert.IsNull(servico.Preco);
            Assert.IsTrue(servico.Ativo);
            Assert.IsTrue(servico.EhModelo);
            Assert.AreEqual(0, servico.Validar().Count);
        }
    }

    [TestMethod]
    public async Task Aplicar_EmBarbeariaNova_CriaOitoServicos()
    {
        var repositorio = new ServicoRepositorioFake();
        var servico = new ServicoCatalogoModelo(repositorio);

        await servico.AplicarAsync(_barbeariaA, CancellationToken.None);

        Assert.AreEqual(8, repositorio.Quantidade(_barbeariaA));
        Assert.IsTrue(repositorio.Todos(_barbeariaA).All(item => item.Preco is null));
    }

    [TestMethod]
    public async Task Aplicar_SegundaVez_NaoDuplica()
    {
        var repositorio = new ServicoRepositorioFake();
        var servico = new ServicoCatalogoModelo(repositorio);

        await servico.AplicarAsync(_barbeariaA, CancellationToken.None);
        await servico.AplicarAsync(_barbeariaA, CancellationToken.None);

        Assert.AreEqual(8, repositorio.Quantidade(_barbeariaA));
    }

    [TestMethod]
    public async Task Aplicar_EmOutraBarbearia_MantemCatalogosIsolados()
    {
        var repositorio = new ServicoRepositorioFake();
        var servico = new ServicoCatalogoModelo(repositorio);

        await servico.AplicarAsync(_barbeariaA, CancellationToken.None);
        await servico.AplicarAsync(_barbeariaB, CancellationToken.None);

        Assert.AreEqual(8, repositorio.Quantidade(_barbeariaA));
        Assert.AreEqual(8, repositorio.Quantidade(_barbeariaB));
        Assert.IsTrue(repositorio.Todos(_barbeariaA).All(item => item.BarbeariaId == _barbeariaA));
        Assert.IsTrue(repositorio.Todos(_barbeariaB).All(item => item.BarbeariaId == _barbeariaB));
    }

    private sealed class ServicoRepositorioFake : IServicoRepositorio
    {
        private readonly Dictionary<Guid, List<Servico>> _servicos = [];

        public Task<IReadOnlyList<Servico>> ListarPorBarbeariaAsync(
            Guid barbeariaId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Servico>>(
                _servicos.TryGetValue(barbeariaId, out var lista) ? [.. lista] : []);

        public Task<IReadOnlyList<Servico>> ListarAtivosPorBarbeariaAsync(
            Guid barbeariaId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Servico>>([]);

        public Task<Servico?> ObterPorIdAsync(
            Guid barbeariaId,
            Guid servicoId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_servicos.TryGetValue(barbeariaId, out var lista)
                ? lista.FirstOrDefault(item => item.Id == servicoId)
                : null);

        public Task AtualizarAsync(Servico servico, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task AdicionarAsync(IReadOnlyList<Servico> servicos, CancellationToken cancellationToken)
        {
            foreach (var grupo in servicos.GroupBy(item => item.BarbeariaId))
            {
                if (!_servicos.TryGetValue(grupo.Key, out var lista))
                {
                    lista = [];
                    _servicos[grupo.Key] = lista;
                }
                lista.AddRange(grupo);
            }
            return Task.CompletedTask;
        }

        public int Quantidade(Guid barbeariaId) =>
            _servicos.TryGetValue(barbeariaId, out var lista) ? lista.Count : 0;

        public List<Servico> Todos(Guid barbeariaId) =>
            _servicos.TryGetValue(barbeariaId, out var lista) ? lista : [];
    }
}

#pragma warning restore CA1707
