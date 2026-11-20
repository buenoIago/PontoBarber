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
/// Task 4.4 — gestão de vínculos: edição de dados, ativação, desativação e
/// remoção lógica, com autorização restrita ao proprietário da barbearia.
/// </summary>
[TestClass]
public sealed class GestaoProfissionaisTestes
{
    private const string _usuarioId = "11111111-1111-1111-1111-111111111111";
    private static readonly Guid _barbeariaId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [TestMethod]
    public async Task Atualizar_ComDadosValidos_AlteraSomenteNomeETelefone()
    {
        var (servico, repositorio, _, alvo) = CriarServico();
        var usuario = repositorio.PerfilAlvo!;
        var id = usuario.Id;
        var usuarioId = usuario.UsuarioId;
        var ativo = usuario.Ativo;

        var resposta = await servico.AtualizarDadosAsync(
            _barbeariaId,
            id,
            new AtualizarProfissionalRequest("Nome Novo", "11988887777"),
            _usuarioId,
            CancellationToken.None);

        Assert.AreEqual("Nome Novo", resposta.NomeCompleto);
        Assert.AreEqual("11988887777", resposta.Telefone);
        Assert.AreEqual("Ativo", resposta.Situacao);
        Assert.AreEqual("Nome Novo", usuario.NomeCompleto);
        Assert.AreEqual("11988887777", usuario.Telefone);
        Assert.AreEqual(id, usuario.Id);
        Assert.AreEqual(usuarioId, usuario.UsuarioId);
        Assert.AreEqual(ativo, usuario.Ativo);
        Assert.AreEqual(_barbeariaId, alvo.BarbeariaId);
        Assert.IsTrue(repositorio.Salvou);
    }

    [TestMethod]
    public async Task Atualizar_ComDadosInvalidos_RejeitaSemAlterar()
    {
        var (servico, repositorio, _, _) = CriarServico();
        var nomeAntes = repositorio.PerfilAlvo!.NomeCompleto;

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.AtualizarDadosAsync(
                _barbeariaId,
                repositorio.PerfilAlvo.Id,
                new AtualizarProfissionalRequest(" ", "123"),
                _usuarioId,
                CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("nomeCompleto"));
        Assert.IsTrue(excecao.Erros.ContainsKey("telefone"));
        Assert.AreEqual(nomeAntes, repositorio.PerfilAlvo.NomeCompleto);
        Assert.IsFalse(repositorio.Salvou);
    }

    [TestMethod]
    public async Task Atualizar_ComBarbeiro_NegaAcesso()
    {
        var (servico, repositorio, _, _) = CriarServico(CargoProfissional.Barbeiro);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.AtualizarDadosAsync(
                _barbeariaId,
                repositorio.PerfilAlvo!.Id,
                new AtualizarProfissionalRequest("Nome Novo", null),
                _usuarioId,
                CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.IsFalse(repositorio.Salvou);
    }

    [TestMethod]
    public async Task Atualizar_ComBarbeariaDiferente_NegaAcesso()
    {
        var (servico, repositorio, _, _) = CriarServico(cargo: CargoProfissional.Proprietario, barbeariaDoUsuario: Guid.NewGuid());

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.AtualizarDadosAsync(
                _barbeariaId,
                repositorio.PerfilAlvo!.Id,
                new AtualizarProfissionalRequest("Nome Novo", null),
                _usuarioId,
                CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.IsFalse(repositorio.Salvou);
    }

    [TestMethod]
    public async Task AlterarStatus_DesativaEReativaAtravesDeTransicoesDeDominio()
    {
        var (servico, repositorio, _, vinculo) = CriarServico();
        var profissionalId = repositorio.PerfilAlvo!.Id;

        var desativado = await servico.AlterarStatusAsync(
            _barbeariaId, profissionalId, new AlterarStatusProfissionalRequest(false), _usuarioId, CancellationToken.None);
        Assert.AreEqual("Inativo", desativado.Situacao);
        Assert.AreEqual(StatusVinculo.Inativo, vinculo.Situacao);

        var reativado = await servico.AlterarStatusAsync(
            _barbeariaId, profissionalId, new AlterarStatusProfissionalRequest(true), _usuarioId, CancellationToken.None);
        Assert.AreEqual("Ativo", reativado.Situacao);
        Assert.AreEqual(StatusVinculo.Ativo, vinculo.Situacao);
        Assert.IsTrue(repositorio.Salvou);
    }

    [TestMethod]
    public async Task AlterarStatus_ComSituacaoAusente_Rejeita()
    {
        var (servico, repositorio, _, _) = CriarServico();

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.AlterarStatusAsync(
                _barbeariaId, repositorio.PerfilAlvo!.Id,
                new AlterarStatusProfissionalRequest(null), _usuarioId, CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("ativo"));
        Assert.IsFalse(repositorio.Salvou);
    }

    [TestMethod]
    public async Task Remover_MarcaVinculoRemovidoEPreservaPerfil()
    {
        var (servico, repositorio, _, vinculo) = CriarServico();
        var perfil = repositorio.PerfilAlvo!;

        await servico.RemoverVinculoAsync(_barbeariaId, perfil.Id, _usuarioId, CancellationToken.None);

        Assert.AreEqual(StatusVinculo.Removido, vinculo.Situacao);
        Assert.IsNotNull(vinculo.RemovidoEmUtc);
        Assert.IsTrue(repositorio.Perfis.Contains(perfil));
        Assert.IsTrue(repositorio.Salvou);
    }

    [TestMethod]
    public async Task Remover_ComVinculoJaRemovido_Rejeita()
    {
        var (servico, repositorio, _, _) = CriarServico();
        var perfilId = repositorio.PerfilAlvo!.Id;

        await servico.RemoverVinculoAsync(_barbeariaId, perfilId, _usuarioId, CancellationToken.None);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => servico.RemoverVinculoAsync(_barbeariaId, perfilId, _usuarioId, CancellationToken.None));
    }

    [TestMethod]
    public async Task ObterVinculoParaAgenda_ComProprietario_PermitePropriaAgendaEOutrosProfissionais()
    {
        var (servico, repositorio, proprietario, _) = CriarServico();

        var proprio = await servico.ObterVinculoParaAgendaAsync(
            _barbeariaId, proprietario.Id, _usuarioId, CancellationToken.None);
        Assert.AreEqual(proprietario.Id, proprio.ProfissionalId);
        Assert.AreEqual("Proprietario", proprio.Cargo);

        var outro = await servico.ObterVinculoParaAgendaAsync(
            _barbeariaId, repositorio.PerfilAlvo!.Id, _usuarioId, CancellationToken.None);
        Assert.AreEqual(repositorio.PerfilAlvo.Id, outro.ProfissionalId);
        Assert.AreEqual("Barbeiro", outro.Cargo);
    }

    [TestMethod]
    public async Task ObterVinculoParaAgenda_ComBarbeiro_PermiteSomentePropriaAgenda()
    {
        var (servico, repositorio, proprietario, _) = CriarServico(CargoProfissional.Barbeiro);

        var proprio = await servico.ObterVinculoParaAgendaAsync(
            _barbeariaId, proprietario.Id, _usuarioId, CancellationToken.None);
        Assert.AreEqual(proprietario.Id, proprio.ProfissionalId);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.ObterVinculoParaAgendaAsync(
                _barbeariaId, repositorio.PerfilAlvo!.Id, _usuarioId, CancellationToken.None));
        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
    }

    [TestMethod]
    public async Task ObterVinculoParaAgenda_DeOutraBarbearia_NegaAcesso()
    {
        var (servico, repositorio, _, _) = CriarServico(cargo: CargoProfissional.Proprietario, barbeariaDoUsuario: Guid.NewGuid());

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.ObterVinculoParaAgendaAsync(
                _barbeariaId, repositorio.PerfilAlvo!.Id, _usuarioId, CancellationToken.None));
        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
    }

    [TestMethod]
    public async Task ListarProfissionais_ComVinculoAtivo_RetornaNomeTelefoneESituacao()
    {
        var (servico, _, _, _) = CriarServico();

        var lista = await servico.ListarProfissionaisAsync(_barbeariaId, _usuarioId, CancellationToken.None);

        Assert.AreEqual(2, lista.Count);
        Assert.IsTrue(lista.Any(item => item.NomeCompleto == "Proprietario Teste" && item.Situacao == "Ativo"));
        Assert.IsTrue(lista.Any(item => item.NomeCompleto == "Profissional Alvo" && item.Situacao == "Ativo"));
    }

    [TestMethod]
    public void Vinculo_TransicoesInvalidas_SaoRejeitadas()
    {
        var vinculo = VinculoProfissional.CriarBarbeiro(_barbeariaId, Guid.NewGuid());

        Assert.ThrowsExactly<InvalidOperationException>(() => vinculo.Ativar());

        vinculo.Desativar();
        Assert.ThrowsExactly<InvalidOperationException>(() => vinculo.Desativar());

        vinculo.Ativar();
        Assert.AreEqual(StatusVinculo.Ativo, vinculo.Situacao);

        vinculo.Remover();
        Assert.AreEqual(StatusVinculo.Removido, vinculo.Situacao);
        Assert.ThrowsExactly<InvalidOperationException>(() => vinculo.Remover());
        Assert.ThrowsExactly<InvalidOperationException>(() => vinculo.Ativar());
    }

    private static (ServicoProfissional Servico, ProfissionalRepositorioFake Repositorio, PerfilProfissional Proprietario, VinculoProfissional VinculoAlvo)
        CriarServico(
            CargoProfissional cargo = CargoProfissional.Proprietario,
            Guid? barbeariaDoUsuario = null)
    {
        var proprietario = PerfilProfissional.Criar(_usuarioId, "Proprietario Teste", "11999999999");
        var vinculoProprietario = cargo == CargoProfissional.Proprietario
            ? VinculoProfissional.Criar(barbeariaDoUsuario ?? _barbeariaId, proprietario.Id)
            : VinculoProfissional.CriarBarbeiro(barbeariaDoUsuario ?? _barbeariaId, proprietario.Id);

        var alvo = PerfilProfissional.Criar("99999999-9999-9999-9999-999999999999", "Profissional Alvo", "11999998888");
        var vinculoAlvo = VinculoProfissional.CriarBarbeiro(_barbeariaId, alvo.Id);

        var repositorio = new ProfissionalRepositorioFake(proprietario, vinculoProprietario, alvo, vinculoAlvo);
        var servico = new ServicoProfissional(new IdentidadeFake(), new BarbeariaRepositorioFake(), repositorio);
        return (servico, repositorio, proprietario, vinculoAlvo);
    }

    private sealed class ProfissionalRepositorioFake(
        PerfilProfissional proprietario,
        VinculoProfissional vinculoProprietario,
        PerfilProfissional alvo,
        VinculoProfissional vinculoAlvo) : IProfissionalRepositorio
    {
        public List<PerfilProfissional> Perfis { get; } = [proprietario, alvo];
        public PerfilProfissional? PerfilAlvo => Perfis.Count > 1 ? Perfis[1] : null;
        public bool Salvou { get; private set; }

        public Task<PerfilProfissional?> ObterPorUsuarioAsync(string usuarioId, CancellationToken ct) =>
            Task.FromResult(usuarioId == _usuarioId ? proprietario : null);

        public Task<PerfilProfissional?> ObterPorIdAsync(Guid profissionalId, CancellationToken ct) =>
            Task.FromResult(Perfis.FirstOrDefault(perfil => perfil.Id == profissionalId));

        public Task<VinculoProfissional?> ObterVinculoAsync(Guid barbeariaId, Guid profissionalId, CancellationToken ct) =>
            Task.FromResult(vinculoProprietario.BarbeariaId == barbeariaId && vinculoProprietario.ProfissionalId == profissionalId
                ? vinculoProprietario
                : null);

        public Task<bool> PossuiVinculoAtivoAsync(Guid usuarioId, Guid barbeariaId, CancellationToken ct) =>
            Task.FromResult(vinculoProprietario.BarbeariaId == barbeariaId && vinculoProprietario.Situacao == StatusVinculo.Ativo);

        public Task<IReadOnlyList<ProfissionalVinculo>> ListarProfissionaisAsync(
            Guid barbeariaId,
            CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ProfissionalVinculo>>(
            [
                new ProfissionalVinculo(proprietario, vinculoProprietario),
                new ProfissionalVinculo(alvo, vinculoAlvo)
            ]);

        public Task AdicionarVinculoAsync(PerfilProfissional? perfilNovo, VinculoProfissional vinculo, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<VinculoProfissional?> ObterVinculoParaAtualizacaoAsync(
            Guid barbeariaId,
            Guid profissionalId,
            CancellationToken ct)
        {
            var candidatos = new[] { vinculoProprietario, vinculoAlvo };
            var encontrado = candidatos.FirstOrDefault(vinculo =>
                vinculo.BarbeariaId == barbeariaId && vinculo.ProfissionalId == profissionalId);
            return Task.FromResult(encontrado);
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct)
        {
            Salvou = true;
            return Task.CompletedTask;
        }
    }

    private sealed class BarbeariaRepositorioFake : IBarbeariaRepositorio
    {
        public Task<bool> CodigoConviteExisteAsync(string codigo, CancellationToken ct) => Task.FromResult(false);

        public Task CriarAsync(
            BarbeariaEntidade barbearia,
            PerfilProfissional? perfilNovo,
            VinculoProfissional vinculo,
            CancellationToken ct) => Task.CompletedTask;

        public Task<BarbeariaEntidade?> ObterPorIdAsync(Guid barbeariaId, CancellationToken ct) =>
            Task.FromResult<BarbeariaEntidade?>(null);

        public Task<BarbeariaEntidade?> ObterPorCodigoConviteAsync(string codigo, CancellationToken ct) =>
            Task.FromResult<BarbeariaEntidade?>(null);

        public Task AtualizarAsync(BarbeariaEntidade barbearia, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class IdentidadeFake : IIdentidade
    {
        public Task<string> CriarUsuarioAsync(DadosRegistro dados, string tipoUsuario, CancellationToken ct) =>
            Task.FromResult(_usuarioId);

        public Task<ResultadoAutenticacao> AutenticarAsync(string email, string senha, CancellationToken ct) =>
            Task.FromResult(ResultadoAutenticacao.Sucesso);

        public Task<DadosConta?> ObterContaAsync(string usuarioId, CancellationToken ct) =>
            Task.FromResult<DadosConta?>(new DadosConta(
                usuarioId, "Proprietario Teste", "p@exemplo.com", "11999999999", ServicoConta.TipoBarbeiro));

        public Task EncerrarSessaoAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<ResultadoAlteracaoSenha> AlterarSenhaAsync(
            string usuarioId, string senhaAtual, string novaSenha, CancellationToken ct) =>
            Task.FromResult(ResultadoAlteracaoSenha.Sucesso);
    }
}

#pragma warning restore CA1707
