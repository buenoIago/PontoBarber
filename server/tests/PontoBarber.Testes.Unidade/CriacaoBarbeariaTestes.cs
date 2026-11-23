using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;
using PontoBarber.Aplicacao.Modulos.Servicos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>
/// Regras da Task 4.1 sem dependência de banco: validação, fuso IANA,
/// proprietário/vínculo e unicidade do código de convite.
/// </summary>
[TestClass]
public sealed class CriacaoBarbeariaTestes
{
    private const string _usuarioId = "11111111-1111-1111-1111-111111111111";

    [TestMethod]
    public async Task Criar_ComDadosValidos_CriaBarbeariaComProprietarioEVinculoOwner()
    {
        var repositorio = new RepositorioFake();
        var servico = new ServicoBarbearia(new IdentidadeFake(), repositorio, new ProfissionalFake(), NovoServicoCatalogo());

        var resposta = await servico.CriarAsync(RequisicaoValida(), _usuarioId, CancellationToken.None);

        Assert.AreEqual("Barbearia Central", resposta.Nome);
        Assert.AreEqual("America/Sao_Paulo", resposta.FusoHorarioId);
        Assert.IsNotNull(resposta.CodigoConvite);
        Assert.AreEqual(8, resposta.CodigoConvite!.Length);
        Assert.IsNotNull(repositorio.Barbearia);
        Assert.AreEqual(resposta.Id, repositorio.Barbearia!.Id);
        Assert.AreEqual(repositorio.Barbearia.CodigoConvite, resposta.CodigoConvite);
        Assert.IsNotNull(repositorio.Proprietario);
        Assert.AreEqual(_usuarioId, repositorio.Proprietario!.UsuarioId);
        Assert.AreEqual(repositorio.Proprietario.Id, repositorio.Barbearia.IdProfissionalProprietario);
        Assert.IsNotNull(repositorio.Vinculo);
        Assert.AreEqual(CargoProfissional.Proprietario, repositorio.Vinculo!.Cargo);
        Assert.AreEqual(StatusVinculo.Ativo, repositorio.Vinculo.Situacao);
        Assert.AreEqual(repositorio.Barbearia.Id, repositorio.Vinculo.BarbeariaId);
    }

    [TestMethod]
    public async Task Criar_ComPerfilProfissionalExistente_ReaproveitaSemDuplicar()
    {
        var perfilExistente = PerfilProfissional.Criar(_usuarioId, "Profissional Existente", "11999999999");
        var repositorio = new RepositorioFake();
        var profissionais = new ProfissionalFake { PerfilExistente = perfilExistente };
        var servico = new ServicoBarbearia(new IdentidadeFake(), repositorio, profissionais, NovoServicoCatalogo());

        var resposta = await servico.CriarAsync(RequisicaoValida(), _usuarioId, CancellationToken.None);

        Assert.IsNull(repositorio.PerfilNovo);
        Assert.AreEqual(perfilExistente.Id, repositorio.Vinculo!.ProfissionalId);
        Assert.AreEqual(perfilExistente.Id, repositorio.Barbearia!.IdProfissionalProprietario);
        Assert.AreEqual(resposta.Id, repositorio.Vinculo.BarbeariaId);
        Assert.AreEqual(CargoProfissional.Proprietario, repositorio.Vinculo.Cargo);
        Assert.AreEqual(StatusVinculo.Ativo, repositorio.Vinculo.Situacao);
    }

    [TestMethod]
    public async Task Criar_ComCamposInvalidos_RetornaErrosPorCampo()
    {
        var repositorio = new RepositorioFake();
        var servico = new ServicoBarbearia(new IdentidadeFake(), repositorio, new ProfissionalFake(), NovoServicoCatalogo());
        var request = RequisicaoValida() with
        {
            Nome = "  ",
            EmailComercial = "invalido",
            TelefoneComercial = "123",
            FusoHorarioId = "Marte/Fobos"
        };

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.CriarAsync(request, _usuarioId, CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("nome"));
        Assert.IsTrue(excecao.Erros.ContainsKey("emailComercial"));
        Assert.IsTrue(excecao.Erros.ContainsKey("telefoneComercial"));
        Assert.IsTrue(excecao.Erros.ContainsKey("fusoHorarioId"));
        Assert.IsNull(repositorio.Barbearia);
    }

    [TestMethod]
    public async Task Criar_ComFusoLocalDoWindows_RejeitaPorNaoSerIana()
    {
        var servico = new ServicoBarbearia(new IdentidadeFake(), new RepositorioFake(), new ProfissionalFake(), NovoServicoCatalogo());
        var request = RequisicaoValida() with { FusoHorarioId = "E. South America Standard Time" };

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.CriarAsync(request, _usuarioId, CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("fusoHorarioId"));
    }

    [TestMethod]
    public async Task Criar_QuandoCodigoConviteJaExiste_GeraOutroCodigo()
    {
        var repositorio = new RepositorioFake { CodigoExisteNasPrimeirasChamadas = 1 };
        var servico = new ServicoBarbearia(new IdentidadeFake(), repositorio, new ProfissionalFake(), NovoServicoCatalogo());

        var resposta = await servico.CriarAsync(RequisicaoValida(), _usuarioId, CancellationToken.None);

        Assert.AreEqual(2, repositorio.ConsultasCodigo);
        Assert.AreEqual(resposta.CodigoConvite, repositorio.Barbearia!.CodigoConvite);
    }

    [TestMethod]
    public async Task Criar_QuandoNaoConsegueCodigoUnico_RetornaConflito()
    {
        var repositorio = new RepositorioFake { CodigoExisteSempre = true };
        var servico = new ServicoBarbearia(new IdentidadeFake(), repositorio, new ProfissionalFake(), NovoServicoCatalogo());

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoConflitoAplicacao>(
            () => servico.CriarAsync(RequisicaoValida(), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantConviteConflito, excecao.Codigo);
        Assert.IsNull(repositorio.Barbearia);
    }

    [TestMethod]
    public async Task Criar_ComPerfilCliente_NaoCriaBarbearia()
    {
        var repositorio = new RepositorioFake();
        var identidade = new IdentidadeFake { Tipo = ServicoConta.TipoCliente };
        var servico = new ServicoBarbearia(identidade, repositorio, new ProfissionalFake(), NovoServicoCatalogo());

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.CriarAsync(RequisicaoValida(), _usuarioId, CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("usuario"));
        Assert.IsNull(repositorio.Barbearia);
    }

    [TestMethod]
    public async Task Criar_ComDadosValidos_AplicaCatalogoModeloNaNovaBarbearia()
    {
        var repositorioServicos = new ServicoRepositorioFake();
        var servico = new ServicoBarbearia(
            new IdentidadeFake(), new RepositorioFake(), new ProfissionalFake(),
            new ServicoCatalogoModelo(repositorioServicos));

        var resposta = await servico.CriarAsync(RequisicaoValida(), _usuarioId, CancellationToken.None);

        Assert.AreEqual(8, repositorioServicos.Quantidade(resposta.Id));
        Assert.IsTrue(repositorioServicos.Todos(resposta.Id).All(item =>
            item.BarbeariaId == resposta.Id && item.Ativo && item.EhModelo && item.Preco is null));
    }

    private static ServicoCatalogoModelo NovoServicoCatalogo(IServicoRepositorio? repositorio = null) =>
        new(repositorio ?? new ServicoRepositorioFake());

    private static CriarBarbeariaRequest RequisicaoValida() => new(
        "Barbearia Central",
        "(11) 99999-9999",
        "contato@barbearia.com",
        "São Paulo",
        "Rua Central, 100",
        "@barbeariacentral",
        "America/Sao_Paulo");

    private sealed class RepositorioFake : IBarbeariaRepositorio
    {
        private int _consultas;

        public int CodigoExisteNasPrimeirasChamadas { get; init; }
        public bool CodigoExisteSempre { get; init; }
        public int ConsultasCodigo { get; private set; }
        public Barbearia? Barbearia { get; private set; }
        public PerfilProfissional? Proprietario { get; private set; }
        public PerfilProfissional? PerfilNovo { get; private set; }
        public VinculoProfissional? Vinculo { get; private set; }

        public Task<bool> CodigoConviteExisteAsync(string codigo, CancellationToken cancellationToken)
        {
            ConsultasCodigo++;
            _consultas++;
            return Task.FromResult(CodigoExisteSempre || _consultas <= CodigoExisteNasPrimeirasChamadas);
        }

        public Task CriarAsync(
            Barbearia barbearia,
            PerfilProfissional? perfilNovo,
            VinculoProfissional vinculo,
            CancellationToken cancellationToken)
        {
            Barbearia = barbearia;
            PerfilNovo = perfilNovo;
            Proprietario = perfilNovo;
            Vinculo = vinculo;
            return Task.CompletedTask;
        }

        public Task<Barbearia?> ObterPorIdAsync(Guid barbeariaId, CancellationToken cancellationToken) =>
            Task.FromResult(Barbearia);

        public Task<Barbearia?> ObterPorCodigoConviteAsync(string codigo, CancellationToken cancellationToken) =>
            Task.FromResult(Barbearia);

        public Task AtualizarAsync(Barbearia barbearia, CancellationToken cancellationToken)
        {
            Barbearia = barbearia;
            return Task.CompletedTask;
        }
    }

    private sealed class ProfissionalFake : IProfissionalRepositorio
    {
        public PerfilProfissional? PerfilExistente { get; init; }

        public Task<PerfilProfissional?> ObterPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken) =>
            Task.FromResult(PerfilExistente);

        public Task<PerfilProfissional?> ObterPorIdAsync(Guid profissionalId, CancellationToken cancellationToken) =>
            Task.FromResult<PerfilProfissional?>(null);

        public Task<VinculoProfissional?> ObterVinculoAsync(
            Guid barbeariaId,
            Guid profissionalId,
            CancellationToken cancellationToken) =>
            Task.FromResult<VinculoProfissional?>(null);

        public Task<bool> PossuiVinculoAtivoAsync(
            Guid usuarioId,
            Guid barbeariaId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<ProfissionalVinculo>> ListarProfissionaisAsync(
            Guid barbeariaId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProfissionalVinculo>>([]);

        public Task AdicionarVinculoAsync(
            PerfilProfissional? perfilNovo,
            VinculoProfissional vinculo,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<VinculoProfissional?> ObterVinculoParaAtualizacaoAsync(
            Guid barbeariaId,
            Guid profissionalId,
            CancellationToken cancellationToken) =>
            Task.FromResult<VinculoProfissional?>(null);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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
            Task.FromResult<IReadOnlyList<Servico>>(
                _servicos.TryGetValue(barbeariaId, out var lista) ? [.. lista.Where(item => item.Ativo)] : []);

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

    private sealed class IdentidadeFake : IIdentidade
    {
        public string Tipo { get; init; } = ServicoConta.TipoBarbeiro;

        public Task<string> CriarUsuarioAsync(DadosRegistro dados, string tipoUsuario, CancellationToken cancellationToken) =>
            Task.FromResult(_usuarioId);

        public Task<ResultadoAutenticacao> AutenticarAsync(string email, string senha, CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoAutenticacao.Sucesso);

        public Task<DadosConta?> ObterContaAsync(string usuarioId, CancellationToken cancellationToken) =>
            Task.FromResult<DadosConta?>(new DadosConta(
                usuarioId, "Profissional Teste", "profissional@exemplo.com", "11999999999", Tipo));

        public Task EncerrarSessaoAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<ResultadoAtualizacaoPerfil> AtualizarPerfilAsync(
            string usuarioId, string nomeCompleto, string email, string? telefone,
            CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoAtualizacaoPerfil.Sucesso);
        public Task<ResultadoAlteracaoSenha> AlterarSenhaAsync(
            string usuarioId,
            string senhaAtual,
            string novaSenha,
            CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoAlteracaoSenha.Sucesso);
    }
}

#pragma warning restore CA1707
