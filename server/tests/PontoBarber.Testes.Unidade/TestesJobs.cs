using PontoBarber.Aplicacao.Modulos.Jobs;
using PontoBarber.Dominio.Modulos.Jobs;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

[TestClass]
public sealed class TestesJobs
{
    private static readonly DateTime _inicio = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void ExecucaoJob_Iniciar_RegistraInicioSemConclusao()
    {
        var execucao = ExecucaoJob.Iniciar("JobTeste", _inicio);

        Assert.AreEqual(ResultadoExecucaoJob.EmExecucao, execucao.Resultado);
        Assert.AreEqual(_inicio, execucao.IniciadoEmUtc);
        Assert.IsNull(execucao.FinalizadoEmUtc);
        Assert.AreEqual(0, execucao.Validar().Count);
    }

    [TestMethod]
    public void ExecucaoJob_Concluir_RegistraFimEQuantidade()
    {
        var execucao = ExecucaoJob.Iniciar("JobTeste", _inicio);

        execucao.Concluir(7, _inicio.AddMinutes(2));

        Assert.AreEqual(ResultadoExecucaoJob.Sucesso, execucao.Resultado);
        Assert.AreEqual(7, execucao.QuantidadeProcessados);
        Assert.AreEqual(_inicio.AddMinutes(2), execucao.FinalizadoEmUtc);
        Assert.IsNull(execucao.Erro);
        Assert.AreEqual(0, execucao.Validar().Count);
    }

    [TestMethod]
    public void ExecucaoJob_Falhar_RegistraErroResumido()
    {
        var execucao = ExecucaoJob.Iniciar("JobTeste", _inicio);

        execucao.Falhar("InvalidOperationException", _inicio.AddSeconds(5));

        Assert.AreEqual(ResultadoExecucaoJob.Falha, execucao.Resultado);
        Assert.AreEqual("InvalidOperationException", execucao.Erro);
        Assert.AreEqual(_inicio.AddSeconds(5), execucao.FinalizadoEmUtc);
    }

    [TestMethod]
    public void ExecucaoJob_ConcluirDuplicado_Rejeita()
    {
        var execucao = ExecucaoJob.Iniciar("JobTeste", _inicio);
        execucao.Concluir(1, _inicio.AddSeconds(1));

        Assert.ThrowsExactly<InvalidOperationException>(
            () => execucao.Falhar("Erro", _inicio.AddSeconds(2)));
    }

    [TestMethod]
    public void ExecucaoJob_IniciarSemNome_Rejeita()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => ExecucaoJob.Iniciar("  ", _inicio));
    }

    [TestMethod]
    public async Task ServicoExecucaoJobs_Sucesso_RegistraConclusao()
    {
        var repositorio = new RepositorioFake();
        var servico = new ServicoExecucaoJobs(repositorio);

        var resultado = await servico.ExecutarAsync(new ProcessoFake("JobOk", 4), CancellationToken.None);

        Assert.AreEqual(4, resultado.Quantidade);
        Assert.AreEqual(1, repositorio.Iniciados);
        Assert.AreEqual(4, repositorio.QuantidadeConcluida);
        Assert.AreEqual(0, repositorio.Falhas);
    }

    [TestMethod]
    public async Task ServicoExecucaoJobs_Falha_RegistraResultadoEPropaga()
    {
        var repositorio = new RepositorioFake();
        var servico = new ServicoExecucaoJobs(repositorio);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => servico.ExecutarAsync(new ProcessoFake("JobFalha", 0, true), CancellationToken.None));

        Assert.AreEqual(1, repositorio.Iniciados);
        Assert.AreEqual(0, repositorio.Conclusoes);
        Assert.AreEqual(1, repositorio.Falhas);
        Assert.AreEqual("InvalidOperationException", repositorio.UltimoErro);
    }

    [TestMethod]
    public async Task ServicoExecucaoJobs_FalhaNoRegistroDaConclusao_NaoReportaSucesso()
    {
        var repositorio = new RepositorioFake { FalharConclusao = true };
        var servico = new ServicoExecucaoJobs(repositorio);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => servico.ExecutarAsync(new ProcessoFake("JobOk", 1), CancellationToken.None));
    }

    private sealed class ProcessoFake(string nome, int quantidade, bool falhar = false) : IProcessoPeriodico
    {
        public string Nome { get; } = nome;

        public Task<ResultadoProcessamento> ExecutarAsync(
            DateTime agoraUtc, CancellationToken cancellationToken)
        {
            if (falhar) throw new InvalidOperationException("Falha do processo");
            return Task.FromResult(new ResultadoProcessamento(quantidade));
        }
    }

    private sealed class RepositorioFake : IExecucaoJobRepositorio
    {
        public int Iniciados { get; private set; }
        public int Conclusoes { get; private set; }
        public int QuantidadeConcluida { get; private set; }
        public int Falhas { get; private set; }
        public string? UltimoErro { get; private set; }
        public bool FalharConclusao { get; set; }

        public Task IniciarAsync(ExecucaoJob execucao, CancellationToken cancellationToken)
        {
            Iniciados++;
            return Task.CompletedTask;
        }

        public Task ConcluirAsync(
            Guid execucaoId, int quantidade, DateTime finalizadoEmUtc, CancellationToken cancellationToken)
        {
            if (FalharConclusao) throw new InvalidOperationException("Falha no registro");
            Conclusoes++;
            QuantidadeConcluida = quantidade;
            return Task.CompletedTask;
        }

        public Task FalharAsync(
            Guid execucaoId, string erroResumo, DateTime finalizadoEmUtc, CancellationToken cancellationToken)
        {
            Falhas++;
            UltimoErro = erroResumo;
            return Task.CompletedTask;
        }
    }
}

#pragma warning restore CA1707
