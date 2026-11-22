using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Profissionais;
using PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Profissionais;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>Task 6.3: bloqueios pontuais da agenda.</summary>
[TestClass]
public sealed class BloqueiosTestes
{
    private static readonly Guid _barbeariaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string _usuarioId = "11111111-1111-1111-1111-111111111111";
    private static readonly DateTime _inicio = new(2026, 11, 23, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _fim = new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Entidade_PeriodoValido_AceitaMotivoOpcional()
    {
        var bloqueio = BloqueioAgenda.Criar(_barbeariaId, Guid.NewGuid(), _inicio, _fim, "  Almoço  ");

        Assert.AreEqual(0, bloqueio.Validar().Count);
        Assert.AreEqual("Almoço", bloqueio.Motivo);
        Assert.AreEqual(DateTimeKind.Utc, bloqueio.IniciaEmUtc.Kind);
    }

    [TestMethod]
    public void Entidade_PeriodoInvertidoOuLocal_Rejeita()
    {
        var invertido = BloqueioAgenda.ValidarDados(_barbeariaId, Guid.NewGuid(), _fim, _inicio);
        Assert.IsTrue(invertido.Any(erro => erro.Campo == "terminaEmUtc"));

        var local = BloqueioAgenda.ValidarDados(
            _barbeariaId, Guid.NewGuid(), DateTime.SpecifyKind(_inicio, DateTimeKind.Local), _fim);
        Assert.IsTrue(local.Any(erro => erro.Campo == "periodo"));
    }

    [TestMethod]
    public async Task Criar_ComProprietario_PersisteBloqueioDoProfissional()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario, false);

        var resposta = await servico.CriarAsync(
            _barbeariaId, repositorio.ProfissionalAlvoId,
            new CriarBloqueioRequest(_inicio, _fim, null), _usuarioId, CancellationToken.None);

        Assert.AreEqual(1, repositorio.Quantidade(_barbeariaId));
        Assert.AreEqual(_inicio, resposta.IniciaEmUtc);
        Assert.IsNull(resposta.Motivo);
    }

    [TestMethod]
    public async Task Criar_ComPeriodoInvertido_RejeitaSemPersistir()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario, false);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.CriarAsync(
                _barbeariaId, repositorio.ProfissionalAlvoId,
                new CriarBloqueioRequest(_fim, _inicio, null), _usuarioId, CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("terminaEmUtc"));
        Assert.AreEqual(0, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Criar_ComBarbeiroSobreOutroProfissional_NegaAcesso()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Barbeiro, false);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.CriarAsync(
                _barbeariaId, repositorio.ProfissionalAlvoId,
                new CriarBloqueioRequest(_inicio, _fim, null), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.AreEqual(0, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Criar_ComBarbeiroSobreProprioPerfil_Permite()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Barbeiro, true);

        await servico.CriarAsync(
            _barbeariaId, repositorio.ProfissionalAlvoId,
            new CriarBloqueioRequest(_inicio, _fim, null), _usuarioId, CancellationToken.None);

        Assert.AreEqual(1, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Remover_ComProprietario_RemoveBloqueio()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario, false);
        var criado = await servico.CriarAsync(
            _barbeariaId, repositorio.ProfissionalAlvoId,
            new CriarBloqueioRequest(_inicio, _fim, null), _usuarioId, CancellationToken.None);

        await servico.RemoverAsync(
            _barbeariaId, repositorio.ProfissionalAlvoId, criado.Id, _usuarioId, CancellationToken.None);

        Assert.AreEqual(0, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Listar_EmOutraBarbearia_NegaAcesso()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario, false);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.ListarAsync(Guid.NewGuid(), repositorio.ProfissionalAlvoId, _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
    }

    private static (GestaoBloqueios Servico, BloqueioRepositorioFake Repositorio, PerfilProfissional Perfil)
        CriarServico(CargoProfissional cargo, bool alvoEhUsuario)
    {
        var perfil = PerfilProfissional.Criar(_usuarioId, "Profissional Teste", "11999999999");
        var alvo = alvoEhUsuario
            ? perfil
            : PerfilProfissional.Criar("99999999-9999-9999-9999-999999999999", "Alvo", "11999998888");
        var vinculoUsuario = cargo == CargoProfissional.Proprietario
            ? VinculoProfissional.Criar(_barbeariaId, perfil.Id)
            : VinculoProfissional.CriarBarbeiro(_barbeariaId, perfil.Id);
        var vinculoAlvo = VinculoProfissional.CriarBarbeiro(_barbeariaId, alvo.Id);

        var repositorio = new BloqueioRepositorioFake(alvo.Id);
        return (
            new GestaoBloqueios(
                repositorio,
                new ProfissionalRepositorioFake(perfil, vinculoUsuario, vinculoAlvo),
                new AgendamentoRepositorioFake()),
            repositorio,
            perfil);
    }

    private sealed class BloqueioRepositorioFake(Guid profissionalAlvoId) : IBloqueioAgendaRepositorio
    {
        private readonly List<BloqueioAgenda> _bloqueios = [];

        public Guid ProfissionalAlvoId { get; } = profissionalAlvoId;

        public Task<IReadOnlyList<BloqueioAgenda>> ListarAsync(
            Guid barbeariaId, Guid profissionalId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BloqueioAgenda>>(
                _bloqueios.Where(item => item.BarbeariaId == barbeariaId && item.ProfissionalId == profissionalId)
                    .ToList());

        public Task<IReadOnlyList<BloqueioAgenda>> ListarPeriodoAsync(
            Guid barbeariaId, DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BloqueioAgenda>>(
                _bloqueios.Where(item => item.BarbeariaId == barbeariaId &&
                                         item.IniciaEmUtc < fimUtc && item.TerminaEmUtc > inicioUtc)
                    .ToList());

        public Task<BloqueioAgenda?> ObterPorIdAsync(
            Guid barbeariaId, Guid bloqueioId, CancellationToken cancellationToken) =>
            Task.FromResult(_bloqueios.FirstOrDefault(item =>
                item.BarbeariaId == barbeariaId && item.Id == bloqueioId));

        public Task AdicionarAsync(BloqueioAgenda bloqueio, CancellationToken cancellationToken)
        {
            _bloqueios.Add(bloqueio);
            return Task.CompletedTask;
        }

        public Task RemoverAsync(BloqueioAgenda bloqueio, CancellationToken cancellationToken)
        {
            _bloqueios.Remove(bloqueio);
            return Task.CompletedTask;
        }

        public int Quantidade(Guid barbeariaId) =>
            _bloqueios.Count(item => item.BarbeariaId == barbeariaId);
    }

    private sealed class AgendamentoRepositorioFake : IAgendamentoRepositorio
    {
        public bool Conflito { get; set; }

        public Task<ResultadoConfirmacao> ConfirmarAsync(
            Agendamento agendamento, string chaveLock, CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoConfirmacao.Sucesso);

        public Task<bool> ExisteConflitoAsync(
            Guid barbeariaId, Guid profissionalId, DateTime inicioUtc, DateTime fimUtc,
            CancellationToken cancellationToken) => Task.FromResult(Conflito);

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

        public Task<ResultadoCicloVida> CancelarAsync(
            Guid barbeariaId, Guid agendamentoId, string chaveLock, string autorUsuarioId,
            string? motivo, DateTime agoraUtc, CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoCicloVida.Sucesso);

        public Task<ResultadoCicloVida> RemarcarAsync(
            Guid barbeariaId, Guid agendamentoId, DateTime novoInicioUtc,
            IReadOnlyList<string> chavesLock, DateTime agoraUtc, CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoCicloVida.Sucesso);

        public Task<ResultadoCicloVida> SugerirAsync(
            Guid barbeariaId, Guid agendamentoId, DateTime inicioSugeridoUtc, string motivo,
            string autorUsuarioId, string chaveLock, DateTime agoraUtc, CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoCicloVida.Sucesso);

        public Task<ResultadoCicloVida> ResponderSugestaoAsync(
            Guid barbeariaId, Guid agendamentoId, Guid sugestaoId, bool aceitar,
            IReadOnlyList<string> chavesLock, DateTime agoraUtc, CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoCicloVida.Sucesso);

        public Task<IReadOnlyList<SugestaoExpiradaInfo>> ExpirarSugestoesAsync(
            DateTime agoraUtc, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SugestaoExpiradaInfo>>([]);

        public Task<int> FecharVencidosAsync(DateTime agoraUtc, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<HistoricoAgendamento>> ListarHistoricoAsync(
            Guid barbeariaId, Guid agendamentoId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HistoricoAgendamento>>([]);

        public Task<IReadOnlyList<SugestaoHorario>> ListarSugestoesAsync(
            Guid barbeariaId, Guid agendamentoId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SugestaoHorario>>([]);

        public Task<SugestaoHorario?> ObterSugestaoAsync(
            Guid barbeariaId, Guid agendamentoId, Guid sugestaoId, CancellationToken cancellationToken) =>
            Task.FromResult<SugestaoHorario?>(null);
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
