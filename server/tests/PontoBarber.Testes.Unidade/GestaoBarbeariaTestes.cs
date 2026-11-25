using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;
using PontoBarber.Aplicacao.Modulos.Servicos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>
/// Task 4.2 — consulta e edição da barbearia com autorização por vínculo.
/// </summary>
[TestClass]
public sealed class GestaoBarbeariaTestes
{
    private const string _usuarioId = "11111111-1111-1111-1111-111111111111";
    private static readonly Guid _barbeariaId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [TestMethod]
    public async Task Obter_ComProprietario_RetornaDadosDaBarbearia()
    {
        var (servico, _, _, barbearia) = CriarServico(CargoProfissional.Proprietario);

        var resposta = await servico.ObterAsync(barbearia.Id, _usuarioId, CancellationToken.None);

        Assert.AreEqual(barbearia.Id, resposta.Id);
        Assert.AreEqual("Barbearia Central", resposta.Nome);
        Assert.AreEqual("America/Sao_Paulo", resposta.FusoHorarioId);
    }

    [TestMethod]
    public async Task Obter_ComBarbeiroVinculado_NaoExpoeCodigoDeConvite()
    {
        var (servico, _, _, barbearia) = CriarServico(CargoProfissional.Barbeiro);

        var resposta = await servico.ObterAsync(barbearia.Id, _usuarioId, CancellationToken.None);

        Assert.AreEqual("Barbearia Central", resposta.Nome);
        Assert.IsNull(resposta.CodigoConvite);
    }

    [TestMethod]
    public async Task Atualizar_ComDadosValidos_AlteraSomenteCamposPermitidos()
    {
        var (servico, repositorio, _, _) = CriarServico(CargoProfissional.Proprietario);
        var original = repositorio.Barbearia!;
        var id = original.Id;
        var codigo = original.CodigoConvite;
        var proprietario = original.IdProfissionalProprietario;
        var fuso = original.FusoHorarioId;
        var criado = original.CriadoEmUtc;
        var ativo = original.Ativo;

        var resposta = await servico.AtualizarAsync(
            _barbeariaId,
            NovaRequisicao() with { Nome = "Barbearia Atualizada", Instagram = null },
            _usuarioId,
            CancellationToken.None);

        Assert.AreEqual("Barbearia Atualizada", resposta.Nome);
        Assert.IsNull(resposta.Instagram);
        Assert.AreEqual("Rua Nova, 200", resposta.Endereco);
        Assert.AreEqual(id, original.Id);
        Assert.AreEqual(codigo, original.CodigoConvite);
        Assert.AreEqual(proprietario, original.IdProfissionalProprietario);
        Assert.AreEqual(fuso, original.FusoHorarioId);
        Assert.AreEqual(criado, original.CriadoEmUtc);
        Assert.AreEqual(ativo, original.Ativo);
        Assert.IsTrue(repositorio.Atualizado);
    }

    [TestMethod]
    public async Task Atualizar_ComDadosInvalidos_RejeitaSemPersistir()
    {
        var (servico, repositorio, _, _) = CriarServico(CargoProfissional.Proprietario);
        var nomeAntes = repositorio.Barbearia!.Nome;

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.AtualizarAsync(
                _barbeariaId,
                NovaRequisicao() with { Nome = " ", EmailComercial = "invalido", TelefoneComercial = "123" },
                _usuarioId,
                CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("nome"));
        Assert.IsTrue(excecao.Erros.ContainsKey("emailComercial"));
        Assert.IsTrue(excecao.Erros.ContainsKey("telefoneComercial"));
        Assert.AreEqual(nomeAntes, repositorio.Barbearia.Nome);
        Assert.IsFalse(repositorio.Atualizado);
    }

    [TestMethod]
    public async Task Atualizar_ComBarbeiroSemCargoDeProprietario_NegaAcesso()
    {
        var (servico, repositorio, _, _) = CriarServico(CargoProfissional.Barbeiro);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.AtualizarAsync(_barbeariaId, NovaRequisicao(), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.IsFalse(repositorio.Atualizado);
    }

    [TestMethod]
    public async Task Obter_ComBarbeariaDiferente_NegaAcesso()
    {
        var (servico, _, profissional, _) = CriarServico(CargoProfissional.Proprietario);
        profissional.Vinculo = null;

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.ObterAsync(Guid.NewGuid(), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
    }

    [TestMethod]
    public async Task Atualizar_ComVinculoInativo_NegaAcesso()
    {
        var (servico, repositorio, profissional, _) = CriarServico(CargoProfissional.Proprietario);
        profissional.Vinculo = VinculoProfissional.Criar(_barbeariaId, Guid.NewGuid());
        typeof(VinculoProfissional).GetProperty(nameof(VinculoProfissional.Situacao))!
            .SetValue(profissional.Vinculo, StatusVinculo.Inativo);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.AtualizarAsync(_barbeariaId, NovaRequisicao(), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.IsFalse(repositorio.Atualizado);
    }

    private static ServicoCatalogoModelo NovoServicoCatalogo() =>
        new(new ServicoRepositorioFake());

    private static AtualizarBarbeariaRequest NovaRequisicao() => new(
        "Barbearia Central",
        "(11) 99999-9999",
        "contato@barbearia.com",
        "São Paulo",
        "Rua Nova, 200",
        "@barbeariacentral");

    private static (ServicoBarbearia Servico, BarbeariaRepositorioFake Repositorio, ProfissionalRepositorioFake Profissional, BarbeariaEntidade Barbearia)
        CriarServico(CargoProfissional cargo)
    {
        var perfil = PerfilProfissional.Criar(_usuarioId, "Profissional Teste", "11999999999");
        var barbearia = BarbeariaEntidade.Criar(
            "Barbearia Central", "(11) 99999-9999", "contato@barbearia.com",
            "São Paulo", "Rua Central, 100", "@barbeariacentral",
            "ABC23456", "America/Sao_Paulo", perfil.Id);
        var vinculo = VinculoProfissional.Criar(barbearia.Id, perfil.Id);
        if (cargo == CargoProfissional.Barbeiro)
            vinculo = VinculoProfissional.CriarBarbeiro(barbearia.Id, perfil.Id);

        var repositorio = new BarbeariaRepositorioFake(barbearia);
        var profissional = new ProfissionalRepositorioFake(perfil, vinculo);
        var servico = new ServicoBarbearia(new IdentidadeFake(), repositorio, profissional, NovoServicoCatalogo());
        return (servico, repositorio, profissional, barbearia);
    }

    private sealed class BarbeariaRepositorioFake(BarbeariaEntidade? barbearia) : IBarbeariaRepositorio
    {
        public BarbeariaEntidade? Barbearia { get; private set; } = barbearia;
        public bool Atualizado { get; private set; }

        public Task<bool> CodigoConviteExisteAsync(string codigo, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task CriarAsync(BarbeariaEntidade barbearia, PerfilProfissional? perfilNovo, VinculoProfissional vinculo, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<BarbeariaEntidade?> ObterPorIdAsync(Guid barbeariaId, CancellationToken cancellationToken) =>
            Task.FromResult(Barbearia);

        public Task<BarbeariaEntidade?> ObterPorCodigoConviteAsync(string codigo, CancellationToken cancellationToken) =>
            Task.FromResult(Barbearia);

        public Task AtualizarAsync(BarbeariaEntidade barbearia, CancellationToken cancellationToken)
        {
            Barbearia = barbearia;
            Atualizado = true;
            return Task.CompletedTask;
        }
    }

    private sealed class ProfissionalRepositorioFake : IProfissionalRepositorio
    {
        private readonly PerfilProfissional? _perfil;

        public ProfissionalRepositorioFake(PerfilProfissional? perfil, VinculoProfissional? vinculo)
        {
            _perfil = perfil;
            Vinculo = vinculo;
        }

        public VinculoProfissional? Vinculo { get; set; }

        public Task<IReadOnlyList<VinculoProfissional>> ListarVinculosAtivosPorUsuarioAsync(
            string usuarioId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<VinculoProfissional>>([]);

        public Task<PerfilProfissional?> ObterPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken) =>
            Task.FromResult(_perfil);

        public Task<PerfilProfissional?> ObterPorIdAsync(Guid profissionalId, CancellationToken cancellationToken) =>
            Task.FromResult(_perfil?.Id == profissionalId ? _perfil : null);

        public Task<VinculoProfissional?> ObterVinculoAsync(Guid barbeariaId, Guid profissionalId, CancellationToken ct) =>
            Task.FromResult(Vinculo);

        public Task<bool> PossuiVinculoAtivoAsync(Guid usuarioId, Guid barbeariaId, CancellationToken ct) =>
            Task.FromResult(Vinculo is not null);

        public Task<IReadOnlyList<ProfissionalVinculo>> ListarProfissionaisAsync(
            Guid barbeariaId,
            CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ProfissionalVinculo>>([]);

        public Task AdicionarVinculoAsync(PerfilProfissional? perfilNovo, VinculoProfissional vinculo, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<VinculoProfissional?> ObterVinculoParaAtualizacaoAsync(
            Guid barbeariaId,
            Guid profissionalId,
            CancellationToken ct) =>
            Task.FromResult(Vinculo);

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class ServicoRepositorioFake : IServicoRepositorio
    {
        public Task<IReadOnlyList<Servico>> ListarPorBarbeariaAsync(
            Guid barbeariaId,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Servico>>([]);

        public Task<IReadOnlyList<Servico>> ListarAtivosPorBarbeariaAsync(
            Guid barbeariaId,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Servico>>([]);

        public Task<Servico?> ObterPorIdAsync(
            Guid barbeariaId,
            Guid servicoId,
            CancellationToken cancellationToken) => Task.FromResult<Servico?>(null);

        public Task AtualizarAsync(Servico servico, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task AdicionarAsync(IReadOnlyList<Servico> servicos, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class IdentidadeFake : IIdentidade
    {
        public Task<string> CriarUsuarioAsync(DadosRegistro dados, string tipoUsuario, CancellationToken ct) =>
            Task.FromResult(_usuarioId);

        public Task<ResultadoAutenticacao> AutenticarAsync(string email, string senha, CancellationToken ct) =>
            Task.FromResult(ResultadoAutenticacao.Sucesso);

        public Task<DadosConta?> ObterContaAsync(string usuarioId, CancellationToken ct) =>
            Task.FromResult<DadosConta?>(new DadosConta(usuarioId, "Profissional Teste", "p@exemplo.com", "11999999999", ServicoConta.TipoBarbeiro));

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
