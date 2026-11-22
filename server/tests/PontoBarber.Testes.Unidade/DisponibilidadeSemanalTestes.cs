using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Aplicacao.Modulos.Profissionais;
using PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>Task 6.2: disponibilidade semanal do profissional.</summary>
[TestClass]
public sealed class DisponibilidadeSemanalTestes
{
    private static readonly Guid _barbeariaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string _usuarioId = "11111111-1111-1111-1111-111111111111";

    [TestMethod]
    public void Entidade_JanelaValida_AceitaEInvalidaRejeita()
    {
        var janela = DisponibilidadeSemanal.Criar(
            _barbeariaId, Guid.NewGuid(), DiaSemana.Segunda, new TimeOnly(9, 0), new TimeOnly(12, 0));
        Assert.AreEqual(0, janela.Validar().Count);

        var erros = DisponibilidadeSemanal.ValidarDados(
            _barbeariaId, Guid.NewGuid(), DiaSemana.Terca, new TimeOnly(12, 0), new TimeOnly(9, 0));
        Assert.IsTrue(erros.Any(erro => erro.Campo == "terminaEm"));
    }

    [TestMethod]
    public async Task Salvar_ComJanelaDentroDoFuncionamento_Persiste()
    {
        var (servico, repositorio, alvoId, _) = CriarServico(CargoProfissional.Proprietario);

        var janelas = await servico.SalvarAsync(
            _barbeariaId, alvoId, [JanelaValida()], _usuarioId, CancellationToken.None);

        Assert.AreEqual(1, janelas.Count);
        Assert.AreEqual(1, repositorio.Quantidade(_barbeariaId, alvoId));
    }

    [TestMethod]
    public async Task Salvar_ComJanelaForaDoFuncionamento_Rejeita()
    {
        var (servico, repositorio, alvoId, _) = CriarServico(CargoProfissional.Proprietario);
        var fora = new JanelaDisponibilidadeRequest(
            (int)DiaSemana.Segunda, new TimeOnly(7, 0), new TimeOnly(10, 0));

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoHorarioAplicacao>(
            () => servico.SalvarAsync(_barbeariaId, alvoId, [fora], _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.AgendaForaFuncionamento, excecao.Codigo);
        Assert.AreEqual(0, repositorio.Quantidade(_barbeariaId, alvoId));
    }

    [TestMethod]
    public async Task Salvar_ComBarbeiroSobrePropriaJanela_Permite()
    {
        var (servico, repositorio, alvoId, _) = CriarServico(CargoProfissional.Barbeiro);

        await servico.SalvarAsync(_barbeariaId, alvoId, [JanelaValida()], _usuarioId, CancellationToken.None);

        Assert.AreEqual(1, repositorio.Quantidade(_barbeariaId, alvoId));
    }

    [TestMethod]
    public async Task Salvar_ComBarbeiroSobreOutroProfissional_NegaAcesso()
    {
        var (servico, repositorio, _, outroId) = CriarServico(CargoProfissional.Barbeiro);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.SalvarAsync(_barbeariaId, outroId, [JanelaValida()], _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.AreEqual(0, repositorio.Quantidade(_barbeariaId, outroId));
    }

    [TestMethod]
    public async Task Salvar_EmOutraBarbearia_Rejeita()
    {
        var (servico, repositorio, alvoId, _) = CriarServico(CargoProfissional.Proprietario);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.SalvarAsync(Guid.NewGuid(), alvoId, [JanelaValida()], _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.AreEqual(0, repositorio.Quantidade(_barbeariaId, alvoId));
    }

    [TestMethod]
    public async Task Listar_ComBarbeiroSobreOutroPerfil_NegaAcesso()
    {
        var (servico, _, _, outroId) = CriarServico(CargoProfissional.Barbeiro);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.ListarAsync(_barbeariaId, outroId, _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
    }

    [TestMethod]
    public async Task Listar_ComProprietario_RetornaJanelasDoProfissional()
    {
        var (servico, _, alvoId, _) = CriarServico(CargoProfissional.Proprietario);
        await servico.SalvarAsync(
            _barbeariaId, alvoId, [JanelaValida()], _usuarioId, CancellationToken.None);

        var janelas = await servico.ListarAsync(
            _barbeariaId, alvoId, _usuarioId, CancellationToken.None);

        Assert.AreEqual(1, janelas.Count);
        Assert.AreEqual((int)DiaSemana.Segunda, janelas[0].DiaSemana);
    }

    private static JanelaDisponibilidadeRequest JanelaValida() =>
        new((int)DiaSemana.Segunda, new TimeOnly(10, 0), new TimeOnly(12, 0));

    private static (GestaoDisponibilidadeSemanal Servico, DisponibilidadeRepositorioFake Repositorio, Guid AlvoId, Guid OutroId)
        CriarServico(CargoProfissional cargo)
    {
        var perfil = PerfilProfissional.Criar(_usuarioId, "Profissional Teste", "11999999999");
        var alvo = cargo == CargoProfissional.Proprietario
            ? PerfilProfissional.Criar("99999999-9999-9999-9999-999999999999", "Alvo", "11999998888")
            : perfil;
        var outro = PerfilProfissional.Criar("88888888-8888-8888-8888-888888888888", "Outro", "11999998888");

        var vinculoUsuario = VinculoProfissional.Criar(_barbeariaId, perfil.Id);
        if (cargo == CargoProfissional.Barbeiro)
            vinculoUsuario = VinculoProfissional.CriarBarbeiro(_barbeariaId, perfil.Id);
        var vinculoAlvo = VinculoProfissional.CriarBarbeiro(_barbeariaId, alvo.Id);

        var horarios = new HorarioRepositorioFake();
        horarios.Adicionar(HorarioFuncionamento.Criar(
            _barbeariaId, DiaSemana.Segunda, new TimeOnly(9, 0), new TimeOnly(18, 0), false));

        var repositorio = new DisponibilidadeRepositorioFake();
        var profissionais = new ProfissionalRepositorioFake(perfil, vinculoUsuario, vinculoAlvo);
        return (
            new GestaoDisponibilidadeSemanal(
                repositorio, horarios, profissionais,
                new AgendamentoRepositorioFake(), new BarbeariaRepositorioFake()),
            repositorio,
            alvo.Id,
            outro.Id);
    }

    private sealed class AgendamentoRepositorioFake : IAgendamentoRepositorio
    {
        public Task<ResultadoConfirmacao> ConfirmarAsync(
            Agendamento agendamento, string chaveLock, CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoConfirmacao.Sucesso);

        public Task<bool> ExisteConflitoAsync(
            Guid barbeariaId, Guid profissionalId, DateTime inicioUtc, DateTime fimUtc,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<IReadOnlyList<Agendamento>> ListarAtivosPorPeriodoAsync(
            Guid barbeariaId, IReadOnlyCollection<Guid> profissionalIds, DateTime inicioUtc,
            DateTime fimUtc, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Agendamento>>([]);

        public Task<IReadOnlyList<Agendamento>> ListarPorBarbeariaAsync(
            Guid barbeariaId, DateTime inicioUtc, DateTime fimUtc, Guid? profissionalId,
            string? clienteId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Agendamento>>([]);

        public Task<IReadOnlyList<Agendamento>> ListarDoClienteAsync(
            string clienteId, DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Agendamento>>([]);

        public Task<Agendamento?> ObterPorIdAsync(
            Guid barbeariaId, Guid agendamentoId, CancellationToken cancellationToken) =>
            Task.FromResult<Agendamento?>(null);
    }

    private sealed class BarbeariaRepositorioFake : IBarbeariaRepositorio
    {
        public Task<bool> CodigoConviteExisteAsync(string codigo, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task CriarAsync(
            BarbeariaEntidade barbearia, PerfilProfissional? perfilNovo,
            VinculoProfissional vinculo, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<BarbeariaEntidade?> ObterPorIdAsync(Guid barbeariaId, CancellationToken cancellationToken) =>
            Task.FromResult<BarbeariaEntidade?>(null);

        public Task<BarbeariaEntidade?> ObterPorCodigoConviteAsync(
            string codigo, CancellationToken cancellationToken) => Task.FromResult<BarbeariaEntidade?>(null);

        public Task AtualizarAsync(BarbeariaEntidade barbearia, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class HorarioRepositorioFake : IHorarioFuncionamentoRepositorio
    {
        private readonly List<HorarioFuncionamento> _dias = [];

        public Task<IReadOnlyList<HorarioFuncionamento>> ListarPorBarbeariaAsync(
            Guid barbeariaId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HorarioFuncionamento>>(
                _dias.Where(dia => dia.BarbeariaId == barbeariaId).ToList());

        public Task AdicionarAsync(IReadOnlyList<HorarioFuncionamento> horarios, CancellationToken cancellationToken)
        {
            _dias.AddRange(horarios);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Adicionar(HorarioFuncionamento horario) =>
            AdicionarAsync([horario], CancellationToken.None).GetAwaiter().GetResult();
    }

    private sealed class DisponibilidadeRepositorioFake : IDisponibilidadeSemanalRepositorio
    {
        private readonly List<DisponibilidadeSemanal> _janelas = [];

        public Task<IReadOnlyList<DisponibilidadeSemanal>> ListarAsync(
            Guid barbeariaId,
            Guid profissionalId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DisponibilidadeSemanal>>(
                _janelas.Where(janela => janela.BarbeariaId == barbeariaId && janela.ProfissionalId == profissionalId)
                    .ToList());

        public Task AdicionarAsync(IReadOnlyList<DisponibilidadeSemanal> janelas, CancellationToken cancellationToken)
        {
            _janelas.AddRange(janelas);
            return Task.CompletedTask;
        }

        public Task RemoverAsync(IReadOnlyList<DisponibilidadeSemanal> janelas, CancellationToken cancellationToken)
        {
            foreach (var janela in janelas)
                _janelas.Remove(janela);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public int Quantidade(Guid barbeariaId, Guid profissionalId) =>
            _janelas.Count(janela => janela.BarbeariaId == barbeariaId && janela.ProfissionalId == profissionalId);
    }

    private sealed class ProfissionalRepositorioFake(
        PerfilProfissional perfil,
        VinculoProfissional vinculoUsuario,
        VinculoProfissional vinculoAlvo) : IProfissionalRepositorio
    {
        public Task<PerfilProfissional?> ObterPorUsuarioAsync(string usuarioId, CancellationToken ct) =>
            Task.FromResult<PerfilProfissional?>(perfil);

        public Task<PerfilProfissional?> ObterPorIdAsync(Guid profissionalId, CancellationToken ct) =>
            Task.FromResult<PerfilProfissional?>(null);

        public Task<VinculoProfissional?> ObterVinculoAsync(
            Guid barbeariaId, Guid profissionalId, CancellationToken ct)
        {
            if (vinculoUsuario.BarbeariaId == barbeariaId && vinculoUsuario.ProfissionalId == profissionalId)
                return Task.FromResult<VinculoProfissional?>(vinculoUsuario);
            if (vinculoAlvo.BarbeariaId == barbeariaId && vinculoAlvo.ProfissionalId == profissionalId)
                return Task.FromResult<VinculoProfissional?>(vinculoAlvo);
            return Task.FromResult<VinculoProfissional?>(null);
        }

        public Task<bool> PossuiVinculoAtivoAsync(Guid usuarioId, Guid barbeariaId, CancellationToken ct) =>
            Task.FromResult(vinculoUsuario.BarbeariaId == barbeariaId);

        public Task<IReadOnlyList<ProfissionalVinculo>> ListarProfissionaisAsync(
            Guid barbeariaId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ProfissionalVinculo>>([new(perfil, vinculoUsuario)]);

        public Task AdicionarVinculoAsync(
            PerfilProfissional? perfilNovo, VinculoProfissional vinculoNovo, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<VinculoProfissional?> ObterVinculoParaAtualizacaoAsync(
            Guid barbeariaId, Guid profissionalId, CancellationToken ct) =>
            Task.FromResult<VinculoProfissional?>(null);

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}

#pragma warning restore CA1707
