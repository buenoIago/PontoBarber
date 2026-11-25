using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>Task 6.1: funcionamento semanal da barbearia.</summary>
[TestClass]
public sealed class HorariosFuncionamentoTestes
{
    private static readonly Guid _barbeariaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string _usuarioId = "11111111-1111-1111-1111-111111111111";

    [TestMethod]
    public void Entidade_ConfiguracaoValida_AceitaDiasAbertosEFechados()
    {
        var aberto = HorarioFuncionamento.Criar(_barbeariaId, DiaSemana.Segunda, new TimeOnly(9, 0), new TimeOnly(18, 0), false);
        var fechado = HorarioFuncionamento.Criar(_barbeariaId, DiaSemana.Domingo, null, null, true);

        Assert.AreEqual(0, aberto.Validar().Count);
        Assert.AreEqual(0, fechado.Validar().Count);
        Assert.IsFalse(fechado.Fechado == false);
    }

    [TestMethod]
    public void Entidade_PeriodoInvertido_Rejeita()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(() =>
            HorarioFuncionamento.Criar(_barbeariaId, DiaSemana.Terca, new TimeOnly(18, 0), new TimeOnly(9, 0), false));

        StringAssert.Contains(excecao.Message, "posterior");
    }

    [TestMethod]
    public void Entidade_DiaAbertoSemHorarios_Rejeita()
    {
        var erros = HorarioFuncionamento.ValidarDados(_barbeariaId, DiaSemana.Quarta, null, null, false);

        Assert.IsTrue(erros.Any(erro => erro.Campo == "abreAs"));
        Assert.IsTrue(erros.Any(erro => erro.Campo == "fechaAs"));
    }

    [TestMethod]
    public async Task Salvar_ComProprietario_PersisteSeteDias()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario);

        var resposta = await servico.SalvarAsync(_barbeariaId, SemanaValida(), _usuarioId, CancellationToken.None);

        Assert.AreEqual(7, resposta.Count);
        Assert.AreEqual(7, repositorio.Quantidade(_barbeariaId));
        Assert.AreEqual(1, resposta.Count(dia => dia.DiaSemana == (int)DiaSemana.Domingo && dia.Fechado));
    }

    [TestMethod]
    public async Task Salvar_ComBarbeiro_NegaAcesso()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Barbeiro);

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.SalvarAsync(_barbeariaId, SemanaValida(), _usuarioId, CancellationToken.None));

        Assert.AreEqual(CodigosErro.TenantNegado, excecao.Codigo);
        Assert.AreEqual(0, repositorio.Quantidade(_barbeariaId));
    }

    [TestMethod]
    public async Task Salvar_ComPeriodoInvertido_RejeitaSemAlterar()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Proprietario);
        await servico.SalvarAsync(_barbeariaId, SemanaValida(), _usuarioId, CancellationToken.None);
        var invalida = SemanaValida().ToList();
        invalida[0] = invalida[0] with { AbreAs = new TimeOnly(18, 0), FechaAs = new TimeOnly(9, 0) };

        var excecao = await Assert.ThrowsExactlyAsync<ExcecaoValidacaoAplicacao>(
            () => servico.SalvarAsync(_barbeariaId, invalida, _usuarioId, CancellationToken.None));

        Assert.IsTrue(excecao.Erros.ContainsKey("fechaAs"));
        Assert.AreEqual(new TimeOnly(9, 0), repositorio.Obter(_barbeariaId, DiaSemana.Segunda)!.AbreAs);
    }

    [TestMethod]
    public async Task Listar_ComBarbeiroVinculado_RetornaSemanaDaBarbearia()
    {
        var (servico, repositorio, _) = CriarServico(CargoProfissional.Barbeiro);
        foreach (var dia in SemanaValida())
            repositorio.Adicionar(HorarioFuncionamento.Criar(
                _barbeariaId, (DiaSemana)dia.DiaSemana, dia.AbreAs, dia.FechaAs, dia.Fechado));

        var dias = await servico.ListarAsync(_barbeariaId, _usuarioId, CancellationToken.None);

        Assert.AreEqual(7, dias.Count);
    }

    private static List<DiaFuncionamentoRequest> SemanaValida() =>
        Enumerable.Range(1, 7)
            .Select(dia => dia == 7
                ? new DiaFuncionamentoRequest(dia, null, null, true)
                : new DiaFuncionamentoRequest(dia, new TimeOnly(9, 0), new TimeOnly(18, 0), false))
            .ToList();

    private static (GestaoHorariosFuncionamento Servico, HorarioRepositorioFake Repositorio, PerfilProfissional Perfil)
        CriarServico(CargoProfissional cargo)
    {
        var perfil = PerfilProfissional.Criar(_usuarioId, "Profissional Teste", "11999999999");
        var vinculo = cargo == CargoProfissional.Proprietario
            ? VinculoProfissional.Criar(_barbeariaId, perfil.Id)
            : VinculoProfissional.CriarBarbeiro(_barbeariaId, perfil.Id);
        var repositorio = new HorarioRepositorioFake();
        return (new GestaoHorariosFuncionamento(repositorio, new ProfissionalRepositorioFake(perfil, vinculo)), repositorio, perfil);
    }

    private sealed class HorarioRepositorioFake : IHorarioFuncionamentoRepositorio
    {
        private readonly Dictionary<Guid, List<HorarioFuncionamento>> _dias = [];

        public Task<IReadOnlyList<HorarioFuncionamento>> ListarPorBarbeariaAsync(
            Guid barbeariaId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HorarioFuncionamento>>(
                _dias.TryGetValue(barbeariaId, out var lista) ? [.. lista] : []);

        public Task AdicionarAsync(IReadOnlyList<HorarioFuncionamento> horarios, CancellationToken cancellationToken)
        {
            foreach (var grupo in horarios.GroupBy(item => item.BarbeariaId))
            {
                if (!_dias.TryGetValue(grupo.Key, out var lista))
                {
                    lista = [];
                    _dias[grupo.Key] = lista;
                }
                lista.AddRange(grupo);
            }
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Adicionar(HorarioFuncionamento horario) =>
            AdicionarAsync([horario], CancellationToken.None).GetAwaiter().GetResult();

        public int Quantidade(Guid barbeariaId) =>
            _dias.TryGetValue(barbeariaId, out var lista) ? lista.Count : 0;

        public HorarioFuncionamento? Obter(Guid barbeariaId, DiaSemana dia) =>
            _dias.TryGetValue(barbeariaId, out var lista)
                ? lista.FirstOrDefault(item => item.DiaSemana == dia)
                : null;
    }

    private sealed class ProfissionalRepositorioFake(
        PerfilProfissional perfil,
        VinculoProfissional vinculo) : IProfissionalRepositorio
    {
        public Task<IReadOnlyList<VinculoProfissional>> ListarVinculosAtivosPorUsuarioAsync(
            string usuarioId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<VinculoProfissional>>([]);

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
}

#pragma warning restore CA1707
