using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Profissionais;
using PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>
/// Task 4.3 — ingresso de profissional por código de convite.
/// </summary>
[TestClass]
public sealed class IngressoPorConviteTestes
{
    private const string _usuarioId = "11111111-1111-1111-1111-111111111111";
    private const string _codigo = "ABC23456";
    private static readonly string[] _propriedadesEsperadas = ["CodigoConvite"];

    [TestMethod]
    public async Task Entrar_ComCodigoValido_CriaVinculoDeBarbeiroParaOProprioUsuario()
    {
        var (servico, barbearias, profissionais) = CriarServico();
        var perfil = PerfilProfissional.Criar(_usuarioId, "Profissional Teste", "11999999999");
        profissionais.Perfil = perfil;

        var resposta = await servico.EntrarPorConviteAsync(
            new EntrarBarbeariaRequest(_codigo), _usuarioId, CancellationToken.None);

        Assert.AreEqual("Barbeiro", resposta.Cargo);
        Assert.AreEqual("Ativo", resposta.Situacao);
        Assert.AreEqual(perfil.Id, resposta.ProfissionalId);
        Assert.AreEqual(barbearias.Barbearia!.Id, resposta.BarbeariaId);
        Assert.IsNotNull(profissionais.VinculoAdicionado);
        Assert.AreEqual(CargoProfissional.Barbeiro, profissionais.VinculoAdicionado.Cargo);
        Assert.AreEqual(StatusVinculo.Ativo, profissionais.VinculoAdicionado.Situacao);
        Assert.IsNull(profissionais.PerfilAdicionado);
    }

    [TestMethod]
    public async Task Entrar_ComCodigoInexistente_RetornaNaoEncontrado()
    {
        var (servico, barbearias, _) = CriarServico();
        barbearias.Barbearia = null;

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoNaoEncontradoAplicacao>(
            () => servico.EntrarPorConviteAsync(
                new EntrarBarbeariaRequest("XXXXXXXX"), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNaoEncontrado, excecao.Codigo);
    }

    [TestMethod]
    public async Task Entrar_ComVinculoInativo_NaoDuplicaNemReativa()
    {
        var (servico, _, profissionais) = CriarServico();
        var perfil = PerfilProfissional.Criar(_usuarioId, "Profissional Teste", "11999999999");
        profissionais.Perfil = perfil;
        var inativo = VinculoProfissional.CriarBarbeiro(Guid.NewGuid(), perfil.Id);
        typeof(VinculoProfissional).GetProperty(nameof(VinculoProfissional.Situacao))!
            .SetValue(inativo, StatusVinculo.Inativo);
        profissionais.Vinculo = inativo;

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoConflitoAplicacao>(
            () => servico.EntrarPorConviteAsync(
                new EntrarBarbeariaRequest(_codigo), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantConviteConflito, excecao.Codigo);
        Assert.IsNull(profissionais.VinculoAdicionado);
        Assert.AreEqual(StatusVinculo.Inativo, inativo.Situacao);
    }

    [TestMethod]
    public async Task Entrar_SemPerfil_CriaPerfilDoUsuarioAutenticado()
    {
        var (servico, _, profissionais) = CriarServico();

        await servico.EntrarPorConviteAsync(
            new EntrarBarbeariaRequest(_codigo), _usuarioId, CancellationToken.None);

        Assert.IsNotNull(profissionais.PerfilAdicionado);
        Assert.AreEqual(_usuarioId, profissionais.PerfilAdicionado!.UsuarioId);
        Assert.AreEqual("Profissional Teste", profissionais.PerfilAdicionado.NomeCompleto);
        Assert.AreEqual(profissionais.PerfilAdicionado.Id, profissionais.VinculoAdicionado!.ProfissionalId);
    }

    [TestMethod]
    public async Task Entrar_ComPerfilCliente_NegaAcesso()
    {
        var (servico, _, _) = CriarServico(tipo: ServicoConta.TipoCliente);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.EntrarPorConviteAsync(
                new EntrarBarbeariaRequest(_codigo), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.AutenticacaoNegada, excecao.Codigo);
    }

    [TestMethod]
    public async Task Entrar_ComCodigoVazio_Rejeita()
    {
        var (servico, _, _) = CriarServico();

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.EntrarPorConviteAsync(
                new EntrarBarbeariaRequest(" "), _usuarioId, CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("codigoConvite"));
    }

    [TestMethod]
    public async Task Entrar_ConcorrenciaNaInsercao_RetornaConflitoSemDuplicar()
    {
        var (servico, _, profissionais) = CriarServico();
        profissionais.ConflitoAoAdicionar = true;

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoConflitoAplicacao>(
            () => servico.EntrarPorConviteAsync(
                new EntrarBarbeariaRequest(_codigo), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantConviteConflito, excecao.Codigo);
    }

    [TestMethod]
    public void Requisicao_NaoExpoeCampoDeCargo()
    {
        var propriedades = typeof(EntrarBarbeariaRequest).GetProperties().Select(p => p.Name).ToList();

        CollectionAssert.AreEqual(_propriedadesEsperadas, propriedades);
        Assert.AreEqual(CargoProfissional.Barbeiro, VinculoProfissional.CriarBarbeiro(Guid.NewGuid(), Guid.NewGuid()).Cargo);
    }

    private static (ServicoProfissional Servico, BarbeariaRepositorioFake Barbearias, ProfissionalRepositorioFake Profissionais)
        CriarServico(string tipo = ServicoConta.TipoBarbeiro)
    {
        var barbearia = BarbeariaEntidade.Criar(
            "Barbearia Central", "(11) 99999-9999", "contato@barbearia.com",
            "São Paulo", "Rua Central, 100", null, _codigo, "America/Sao_Paulo", Guid.NewGuid());
        var barbearias = new BarbeariaRepositorioFake(barbearia);
        var profissionais = new ProfissionalRepositorioFake();
        var servico = new ServicoProfissional(new IdentidadeFake(tipo), barbearias, profissionais);
        return (servico, barbearias, profissionais);
    }

    private sealed class BarbeariaRepositorioFake(BarbeariaEntidade? barbearia) : IBarbeariaRepositorio
    {
        public BarbeariaEntidade? Barbearia { get; set; } = barbearia;

        public Task<bool> CodigoConviteExisteAsync(string codigo, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task CriarAsync(BarbeariaEntidade barbearia, PerfilProfissional? perfilNovo, VinculoProfissional vinculo, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<BarbeariaEntidade?> ObterPorIdAsync(Guid barbeariaId, CancellationToken ct) =>
            Task.FromResult(Barbearia);

        public Task<BarbeariaEntidade?> ObterPorCodigoConviteAsync(string codigo, CancellationToken ct) =>
            Task.FromResult(Barbearia?.CodigoConvite == codigo ? Barbearia : null);

        public Task AtualizarAsync(BarbeariaEntidade barbearia, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class ProfissionalRepositorioFake : IProfissionalRepositorio
    {
        public PerfilProfissional? Perfil { get; set; }
        public VinculoProfissional? Vinculo { get; set; }
        public VinculoProfissional? VinculoAdicionado { get; private set; }
        public PerfilProfissional? PerfilAdicionado { get; private set; }
        public bool ConflitoAoAdicionar { get; set; }

        public Task<IReadOnlyList<VinculoProfissional>> ListarVinculosAtivosPorUsuarioAsync(
            string usuarioId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<VinculoProfissional>>([]);

        public Task<PerfilProfissional?> ObterPorUsuarioAsync(string usuarioId, CancellationToken ct) =>
            Task.FromResult(Perfil);

        public Task<PerfilProfissional?> ObterPorIdAsync(Guid profissionalId, CancellationToken ct) =>
            Task.FromResult(Perfil?.Id == profissionalId ? Perfil : null);

        public Task<VinculoProfissional?> ObterVinculoAsync(Guid barbeariaId, Guid profissionalId, CancellationToken ct) =>
            Task.FromResult(Vinculo);

        public Task<bool> PossuiVinculoAtivoAsync(Guid usuarioId, Guid barbeariaId, CancellationToken ct) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<ProfissionalVinculo>> ListarProfissionaisAsync(
            Guid barbeariaId,
            CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ProfissionalVinculo>>([]);

        public Task AdicionarVinculoAsync(PerfilProfissional? perfilNovo, VinculoProfissional vinculo, CancellationToken ct)
        {
            if (ConflitoAoAdicionar)
                throw new ExcecaoConflitoAplicacao(CodigosErro.TenantConviteConflito, "Vínculo duplicado.");

            PerfilAdicionado = perfilNovo;
            VinculoAdicionado = vinculo;
            return Task.CompletedTask;
        }

        public Task<VinculoProfissional?> ObterVinculoParaAtualizacaoAsync(
            Guid barbeariaId,
            Guid profissionalId,
            CancellationToken ct) =>
            Task.FromResult(Vinculo);

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class IdentidadeFake(string tipo) : IIdentidade
    {
        public Task<string> CriarUsuarioAsync(DadosRegistro dados, string tipoUsuario, CancellationToken ct) =>
            Task.FromResult(_usuarioId);

        public Task<ResultadoAutenticacao> AutenticarAsync(string email, string senha, CancellationToken ct) =>
            Task.FromResult(ResultadoAutenticacao.Sucesso);

        public Task<DadosConta?> ObterContaAsync(string usuarioId, CancellationToken ct) =>
            Task.FromResult<DadosConta?>(new DadosConta(usuarioId, "Profissional Teste", "p@exemplo.com", "11999999999", tipo));

        public Task EncerrarSessaoAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<ResultadoAtualizacaoPerfil> AtualizarPerfilAsync(
            string usuarioId, string nomeCompleto, string email, string? telefone,
            CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoAtualizacaoPerfil.Sucesso);
        public Task<string> CriarClienteAsync(DadosRegistroCliente dados, CancellationToken ct) =>
            Task.FromResult(string.Empty);

        public Task AutenticarSemSenhaAsync(string usuarioId, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<PontoBarber.Aplicacao.Auth.DadosConta?> ObterContaPorEmailAsync(string email, CancellationToken ct) =>
            Task.FromResult<PontoBarber.Aplicacao.Auth.DadosConta?>(null);
        public Task<ResultadoAlteracaoSenha> AlterarSenhaAsync(string usuarioId, string senhaAtual, string novaSenha, CancellationToken ct) =>
            Task.FromResult(ResultadoAlteracaoSenha.Sucesso);
    }
}

#pragma warning restore CA1707
