using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Servicos;
using PontoBarber.Aplicacao.Modulos.Servicos.DTOs;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>Task 5.2: criação e edição do catálogo com permissões por cargo.</summary>
[TestClass]
public sealed class GestaoServicosTestes
{
    private const string _usuarioId = "11111111-1111-1111-1111-111111111111";
    private static readonly Guid _barbeariaId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [TestMethod]
    public async Task Criar_ComProprietario_AdicionaServicoComPrecoEDuracao()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario);

        var resposta = await servico.CriarAsync(
            _barbeariaId,
            new CriarServicoRequest("Corte premium", 45, 80m),
            _usuarioId,
            CancellationToken.None);

        Assert.AreEqual("Corte premium", resposta.Nome);
        Assert.AreEqual(45, resposta.DuracaoMinutos);
        Assert.AreEqual(80m, resposta.Preco);
        Assert.IsTrue(resposta.Ativo);
        Assert.IsFalse(resposta.EhModelo);
        Assert.AreEqual(2, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Criar_ComPrecoNegativo_RejeitaSemPersistir()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.CriarAsync(
                _barbeariaId, new CriarServicoRequest("Corte", 30, -1m), _usuarioId, CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("preco"));
        Assert.AreEqual(1, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Criar_ComDuracaoInvalida_RejeitaSemPersistir()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.CriarAsync(
                _barbeariaId, new CriarServicoRequest("Corte", 0, 50m), _usuarioId, CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("duracaoMinutos"));
        Assert.AreEqual(1, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Criar_ComBarbeiro_NegaAcesso()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Barbeiro);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.CriarAsync(
                _barbeariaId, new CriarServicoRequest("Corte", 30, 50m), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.AreEqual(1, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Atualizar_ComProprietario_EditaNomePrecoDuracaoEDisponibilidade()
    {
        var (servico, _, existente) = CriarServico(CargoProfissional.Proprietario);

        var resposta = await servico.AtualizarAsync(
            _barbeariaId, existente.Id,
            new AtualizarServicoRequest("Corte premium", 50, 90m, false),
            _usuarioId, CancellationToken.None);

        Assert.AreEqual("Corte premium", resposta.Nome);
        Assert.AreEqual(50, resposta.DuracaoMinutos);
        Assert.AreEqual(90m, resposta.Preco);
        Assert.IsFalse(resposta.Ativo);
    }

    [TestMethod]
    public async Task Atualizar_ComBarbeiro_EditaSomentePrecoEDuracao()
    {
        var (servico, _, existente) = CriarServico(CargoProfissional.Barbeiro);

        var resposta = await servico.AtualizarAsync(
            _barbeariaId, existente.Id,
            new AtualizarServicoRequest(null, 35, 65m, null),
            _usuarioId, CancellationToken.None);

        Assert.AreEqual("Corte", resposta.Nome);
        Assert.AreEqual(35, resposta.DuracaoMinutos);
        Assert.AreEqual(65m, resposta.Preco);
        Assert.IsTrue(resposta.Ativo);
    }

    [TestMethod]
    public async Task Atualizar_ComBarbeiro_TentaAlterarNome_NegaAcesso()
    {
        var (servico, _, existente) = CriarServico(CargoProfissional.Barbeiro);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.AtualizarAsync(
                _barbeariaId, existente.Id,
                new AtualizarServicoRequest("Outro nome", null, null, null),
                _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.AreEqual("Corte", existente.Nome);
    }

    [TestMethod]
    public async Task Atualizar_ComBarbeiro_TentaAlterarDisponibilidade_NegaAcesso()
    {
        var (servico, _, existente) = CriarServico(CargoProfissional.Barbeiro);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.AtualizarAsync(
                _barbeariaId, existente.Id,
                new AtualizarServicoRequest(null, null, null, false),
                _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.IsTrue(existente.Ativo);
    }

    [TestMethod]
    public async Task Atualizar_ComServicoDeOutraBarbearia_NaoEncontrado()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario);
        var servicoDeOutraBarbearia = repositorio.AdicionarServicoEmOutraBarbearia();

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoNaoEncontradoAplicacao>(
            () => servico.AtualizarAsync(
                _barbeariaId, servicoDeOutraBarbearia,
                new AtualizarServicoRequest(null, null, 10m, null),
                _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNaoEncontrado, excecao.Codigo);
        Assert.AreEqual(1, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Remover_ComProprietario_DesativaSemExcluirRegistro()
    {
        var (servico, repositorio, existente) = CriarServico(CargoProfissional.Proprietario);

        await servico.RemoverAsync(_barbeariaId, existente.Id, _usuarioId, CancellationToken.None);

        Assert.IsFalse(existente.Ativo);
        Assert.AreEqual(1, repositorio.Quantidade(_barbeariaId));
        Assert.AreEqual("Corte", existente.Nome);
        Assert.AreEqual(30, existente.DuracaoMinutos);
        Assert.AreEqual(50m, existente.Preco);
    }

    [TestMethod]
    public async Task Remover_ComBarbeiro_NegaAcesso()
    {
        var (servico, repositorio, existente) = CriarServico(CargoProfissional.Barbeiro);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.RemoverAsync(_barbeariaId, existente.Id, _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.IsTrue(existente.Ativo);
        Assert.AreEqual(1, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Remover_ComServicoDeOutraBarbearia_NaoEncontrado()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario);
        var idExterno = repositorio.AdicionarServicoEmOutraBarbearia();

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoNaoEncontradoAplicacao>(
            () => servico.RemoverAsync(_barbeariaId, idExterno, _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNaoEncontrado, excecao.Codigo);
    }

    [TestMethod]
    public async Task Listar_ComCliente_RetornaSomenteServicosAtivos()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Barbeiro, ServicoConta.TipoCliente);
        var inativo = repositorio.AdicionarServicoInativo();

        var lista = await servico.ListarAsync(_barbeariaId, _usuarioId, CancellationToken.None);

        Assert.AreEqual(1, lista.Count);
        Assert.IsFalse(lista.Any(item => item.Id == inativo));
        Assert.IsTrue(lista.All(item => item.Ativo));
    }

    [TestMethod]
    public async Task Listar_ComBarbeiro_RetornaCatalogoCompletoDaBarbearia()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Barbeiro);
        repositorio.AdicionarServicoInativo();

        var lista = await servico.ListarAsync(_barbeariaId, _usuarioId, CancellationToken.None);

        Assert.AreEqual(2, lista.Count);
        Assert.IsTrue(lista.All(item => item.BarbeariaId == _barbeariaId));
    }

    [TestMethod]
    public async Task Listar_NaoIncluiServicosDeOutraBarbearia()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario);
        var idExterno = repositorio.AdicionarServicoEmOutraBarbearia();

        var lista = await servico.ListarAsync(_barbeariaId, _usuarioId, CancellationToken.None);

        Assert.AreEqual(1, lista.Count);
        Assert.IsFalse(lista.Any(item => item.Id == idExterno));
    }

    private static (GestaoServicos Servico, ServicoRepositorioFake Repositorio, Servico Existente)
        CriarServico(CargoProfissional cargo, string tipoConta = ServicoConta.TipoBarbeiro)
    {
        var perfil = PerfilProfissional.Criar(_usuarioId, "Profissional Teste", "11999999999");
        var vinculo = cargo == CargoProfissional.Proprietario
            ? VinculoProfissional.Criar(_barbeariaId, perfil.Id)
            : VinculoProfissional.CriarBarbeiro(_barbeariaId, perfil.Id);

        var existente = Servico.Criar(_barbeariaId, "Corte", 30, 50m);
        var repositorio = new ServicoRepositorioFake(existente);
        return (
            new GestaoServicos(
                repositorio, new ProfissionalRepositorioFake(perfil, vinculo), new IdentidadeFake(tipoConta)),
            repositorio,
            existente);
    }

    private sealed class ServicoRepositorioFake(Servico existente) : IServicoRepositorio
    {
        private readonly Dictionary<Guid, List<Servico>> _servicos = new()
        {
            [existente.BarbeariaId] = [existente]
        };

        public Task<IReadOnlyList<Servico>> ListarPorBarbeariaAsync(
            Guid barbeariaId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Servico>>(
                _servicos.TryGetValue(barbeariaId, out var lista) ? [.. lista] : []);

        public Task<IReadOnlyList<Servico>> ListarAtivosPorBarbeariaAsync(
            Guid barbeariaId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Servico>>(
                _servicos.TryGetValue(barbeariaId, out var lista)
                    ? [.. lista.Where(item => item.Ativo)]
                    : []);

        public Task<Servico?> ObterPorIdAsync(
            Guid barbeariaId,
            Guid servicoId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_servicos.TryGetValue(barbeariaId, out var lista)
                ? lista.FirstOrDefault(item => item.Id == servicoId)
                : null);

        public Task AdicionarAsync(
            IReadOnlyList<Servico> servicos,
            CancellationToken cancellationToken)
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

        public Task AtualizarAsync(
            Servico servico,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Guid AdicionarServicoInativo()
        {
            var inativo = Servico.Criar(_barbeariaId, "Corte inativo", 30, 40m);
            inativo.Remover();
            _servicos[_barbeariaId].Add(inativo);
            return inativo.Id;
        }

        public Guid AdicionarServicoEmOutraBarbearia()
        {
            var outro = Servico.Criar(Guid.NewGuid(), "Servico exterior", 30, 50m);
            _servicos[outro.BarbeariaId] = [outro];
            return outro.Id;
        }

        public int Quantidade(Guid barbeariaId) =>
            _servicos.TryGetValue(barbeariaId, out var lista) ? lista.Count : 0;
    }

    private sealed class ProfissionalRepositorioFake(
        PerfilProfissional perfil,
        VinculoProfissional vinculo) : IProfissionalRepositorio
    {
        public Task<PerfilProfissional?> ObterPorUsuarioAsync(string usuarioId, CancellationToken ct) =>
            Task.FromResult<PerfilProfissional?>(perfil);

        public Task<PerfilProfissional?> ObterPorIdAsync(Guid profissionalId, CancellationToken ct) =>
            Task.FromResult<PerfilProfissional?>(perfil.Id == profissionalId ? perfil : null);

        public Task<VinculoProfissional?> ObterVinculoAsync(
            Guid barbeariaId, Guid profissionalId, CancellationToken ct) =>
            Task.FromResult(vinculo.BarbeariaId == barbeariaId && vinculo.ProfissionalId == profissionalId
                ? vinculo
                : null);

        public Task<bool> PossuiVinculoAtivoAsync(Guid usuarioId, Guid barbeariaId, CancellationToken ct) =>
            Task.FromResult(vinculo.BarbeariaId == barbeariaId);

        public Task<IReadOnlyList<ProfissionalVinculo>> ListarProfissionaisAsync(
            Guid barbeariaId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ProfissionalVinculo>>([new(perfil, vinculo)]);

        public Task AdicionarVinculoAsync(
            PerfilProfissional? perfilNovo, VinculoProfissional vinculoNovo, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<VinculoProfissional?> ObterVinculoParaAtualizacaoAsync(
            Guid barbeariaId, Guid profissionalId, CancellationToken ct) =>
            Task.FromResult<VinculoProfissional?>(vinculo);

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class IdentidadeFake(string tipo) : IIdentidade
    {
        public Task<string> CriarUsuarioAsync(DadosRegistro dados, string tipoUsuario, CancellationToken ct) =>
            Task.FromResult(_usuarioId);

        public Task<ResultadoAutenticacao> AutenticarAsync(string email, string senha, CancellationToken ct) =>
            Task.FromResult(ResultadoAutenticacao.Sucesso);

        public Task<DadosConta?> ObterContaAsync(string usuarioId, CancellationToken ct) =>
            Task.FromResult<DadosConta?>(new DadosConta(
                usuarioId, "Conta Teste", "conta@exemplo.com", "11999999999", tipo));

        public Task EncerrarSessaoAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<ResultadoAlteracaoSenha> AlterarSenhaAsync(
            string usuarioId, string senhaAtual, string novaSenha, CancellationToken ct) =>
            Task.FromResult(ResultadoAlteracaoSenha.Sucesso);
    }
}

#pragma warning restore CA1707
