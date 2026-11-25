using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Agendamentos;
using PontoBarber.Aplicacao.Modulos.Jobs;
using PontoBarber.Aplicacao.Modulos.Jobs.Processos;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Idempotencia;
using PontoBarber.Dominio.Modulos.Jobs;
using PontoBarber.Dominio.Modulos.Notificacoes;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Tasks 9.1 a 9.5: processos periódicos com persistência real.</summary>
[TestClass]
public sealed class TestesProcessosPeriodicos
{
    [TestMethod]
    public async Task Execucao_SucessoEFalha_SaoRegistradasComQuantidadeEMomentos()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var escopo = fabrica.Services.CreateScope();
        var servico = escopo.ServiceProvider.GetRequiredService<ServicoExecucaoJobs>();

        var sucesso = await servico.ExecutarAsync(new ProcessoFake("JobSucesso", 3), CancellationToken.None);
        Assert.AreEqual(3, sucesso.Quantidade);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => servico.ExecutarAsync(new ProcessoFake("JobFalha", 0, falhar: true), CancellationToken.None));

        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var concluida = await contexto.Set<ExecucaoJob>()
            .SingleAsync(item => item.NomeJob == "JobSucesso");
        Assert.AreEqual(ResultadoExecucaoJob.Sucesso, concluida.Resultado);
        Assert.AreEqual(3, concluida.QuantidadeProcessados);
        Assert.IsNotNull(concluida.FinalizadoEmUtc);
        Assert.IsTrue(concluida.FinalizadoEmUtc >= concluida.IniciadoEmUtc);

        var falha = await contexto.Set<ExecucaoJob>()
            .SingleAsync(item => item.NomeJob == "JobFalha");
        Assert.AreEqual(ResultadoExecucaoJob.Falha, falha.Resultado);
        Assert.AreEqual("InvalidOperationException", falha.Erro);
        Assert.IsNotNull(falha.FinalizadoEmUtc);
    }

    [TestMethod]
    public async Task Expiracao_ExpiraVencidaEPreservaVigente_ComNotificacaoUnica()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = SemearCenario(fabrica);
        var processo = Processo(fabrica, ExpirarSugestoesProcesso.NomeJob);

        var primeira = await processo.ExecutarAsync(DateTime.UtcNow, CancellationToken.None);
        var segunda = await processo.ExecutarAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.AreEqual(1, primeira.Quantidade);
        Assert.AreEqual(0, segunda.Quantidade);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var sugestoes = await contexto.Set<SugestaoHorario>().IgnoreQueryFilters().ToListAsync();
        Assert.AreEqual(SituacaoSugestao.Expirada, sugestoes.Single(item => item.ExpiraEmUtc < DateTime.UtcNow).Situacao);
        Assert.AreEqual(SituacaoSugestao.Pendente, sugestoes.Single(item => item.ExpiraEmUtc > DateTime.UtcNow).Situacao);

        var notificacoes = await contexto.Set<Notificacao>().IgnoreQueryFilters().ToListAsync();
        Assert.AreEqual(3, notificacoes.Count);
        Assert.IsTrue(notificacoes.All(item => item.Tipo == TipoNotificacao.SugestaoExpirada));
        Assert.AreEqual(3, notificacoes.Select(item => item.DestinatarioUsuarioId).Distinct().Count());

        var eventos = await contexto.Set<HistoricoAgendamento>().IgnoreQueryFilters()
            .CountAsync(item => item.TipoEvento == TipoEventoHistorico.SugestaoExpirada);
        Assert.AreEqual(1, eventos);
    }

    [TestMethod]
    public async Task Fechamento_VencidoMudaParaNaoComparecimentoEFuturoECanceladoSaoPreservados()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = SemearCenario(fabrica);
        var agora = DateTime.UtcNow;
        var vencido = CriarAgendamento(cenario.BarbeariaId, cenario.ClienteId, cenario.BarbeiroId,
            agora.AddHours(-3), SituacaoAgendamento.Confirmado);
        var futuro = CriarAgendamento(cenario.BarbeariaId, cenario.ClienteId, cenario.BarbeiroId,
            agora.AddDays(1), SituacaoAgendamento.Confirmado);
        var cancelado = CriarAgendamento(cenario.BarbeariaId, cenario.ClienteId, cenario.BarbeiroId,
            agora.AddHours(-3), SituacaoAgendamento.Cancelado, "sistema", "Conflito");

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            contexto.Set<Agendamento>().AddRange(vencido, futuro, cancelado);
            SemeaduraIntegridade.GarantirTudo(contexto);
            await contexto.SaveChangesAsync();
        }

        var processo = Processo(fabrica, FecharVencidosProcesso.NomeJob);
        Assert.AreEqual(1, (await processo.ExecutarAsync(agora, CancellationToken.None)).Quantidade);
        Assert.AreEqual(0, (await processo.ExecutarAsync(agora, CancellationToken.None)).Quantidade);

        using var verificacao = fabrica.Services.CreateScope();
        var ctx = verificacao.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(SituacaoAgendamento.NaoCompareceu,
            (await ctx.Set<Agendamento>().IgnoreQueryFilters().SingleAsync(item => item.Id == vencido.Id)).Situacao);
        Assert.AreEqual(SituacaoAgendamento.Confirmado,
            (await ctx.Set<Agendamento>().IgnoreQueryFilters().SingleAsync(item => item.Id == futuro.Id)).Situacao);
        Assert.AreEqual(SituacaoAgendamento.Cancelado,
            (await ctx.Set<Agendamento>().IgnoreQueryFilters().SingleAsync(item => item.Id == cancelado.Id)).Situacao);

        Assert.AreEqual(1, await ctx.Set<HistoricoAgendamento>().IgnoreQueryFilters()
            .CountAsync(item => item.AgendamentoId == vencido.Id &&
                                item.TipoEvento == TipoEventoHistorico.NaoComparecimento));
    }

    [TestMethod]
    public async Task Expurgo_RemoveSomenteExpiradosEÉSeguroEmRepeticao()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var agora = DateTime.UtcNow;
        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            contexto.Set<RegistroIdempotencia>().AddRange(
                Registro("exp1", agora.AddHours(-2), agora.AddHours(-1)),
                Registro("exp2", agora.AddHours(-3), agora.AddHours(-2)),
                Registro("vigente", agora.AddHours(-1), agora.AddHours(1)));
            SemeaduraIntegridade.GarantirTudo(contexto);
            await contexto.SaveChangesAsync();
        }

        var processo = Processo(fabrica, ExpurgarIdempotenciaProcesso.NomeJob);
        Assert.AreEqual(2, (await processo.ExecutarAsync(agora, CancellationToken.None)).Quantidade);
        Assert.AreEqual(0, (await processo.ExecutarAsync(agora, CancellationToken.None)).Quantidade);

        using var verificacao = fabrica.Services.CreateScope();
        var ctx = verificacao.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var restantes = await ctx.Set<RegistroIdempotencia>().ToListAsync();
        Assert.AreEqual(1, restantes.Count);
        Assert.AreEqual("vigente", restantes[0].Chave);
    }

    [TestMethod]
    public async Task ExecucoesSimultaneasMesmoProcesso_NaoDuplicamNotificacoesNemEventos()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        SemearCenario(fabrica);
        var processo = Processo(fabrica, ExpirarSugestoesProcesso.NomeJob);
        using var escopoA = fabrica.Services.CreateScope();
        using var escopoB = fabrica.Services.CreateScope();
        var servicoA = escopoA.ServiceProvider.GetRequiredService<ServicoExecucaoJobs>();
        var servicoB = escopoB.ServiceProvider.GetRequiredService<ServicoExecucaoJobs>();

        await Task.WhenAll(
            servicoA.ExecutarAsync(processo, CancellationToken.None),
            servicoB.ExecutarAsync(processo, CancellationToken.None));

        using var verificacao = fabrica.Services.CreateScope();
        var ctx = verificacao.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(3, await ctx.Set<Notificacao>().IgnoreQueryFilters().CountAsync());
        Assert.AreEqual(1, await ctx.Set<HistoricoAgendamento>().IgnoreQueryFilters()
            .CountAsync(item => item.TipoEvento == TipoEventoHistorico.SugestaoExpirada));
        Assert.AreEqual(2, await ctx.Set<ExecucaoJob>().CountAsync());
    }

    [TestMethod]
    public async Task CancelamentosSimultaneos_ComMesmoLock_RegistramUmUnicoEvento()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = SemearCenario(fabrica);
        var agora = DateTime.UtcNow.AddDays(1);
        var agendamento = CriarAgendamento(cenario.BarbeariaId, cenario.ClienteId, cenario.BarbeiroId,
            agora.AddDays(2), SituacaoAgendamento.Confirmado);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            contexto.Set<Agendamento>().Add(agendamento);
            SemeaduraIntegridade.GarantirTudo(contexto);
            await contexto.SaveChangesAsync();
        }

        using var escopoChave = fabrica.Services.CreateScope();
        var validador = escopoChave.ServiceProvider.GetRequiredService<ValidadorHorarioAgendamento>();
        var chave = await validador.ObterChaveLockAsync(
            cenario.BarbeariaId, cenario.BarbeiroId, agendamento.IniciaEmUtc, CancellationToken.None);

        using var escopoA = fabrica.Services.CreateScope();
        using var escopoB = fabrica.Services.CreateScope();
        var repositorioA = escopoA.ServiceProvider.GetRequiredService<IAgendamentoRepositorio>();
        var repositorioB = escopoB.ServiceProvider.GetRequiredService<IAgendamentoRepositorio>();

        var resultados = await Task.WhenAll(
            repositorioA.CancelarAsync(cenario.BarbeariaId, agendamento.Id, chave, "sistema", "Um", agora, CancellationToken.None),
            repositorioB.CancelarAsync(cenario.BarbeariaId, agendamento.Id, chave, "sistema", "Dois", agora, CancellationToken.None));

        Assert.AreEqual(1, resultados.Count(resultado => resultado == ResultadoCicloVida.Sucesso));
        Assert.AreEqual(1, resultados.Count(resultado =>
            resultado is ResultadoCicloVida.EstadoInvalido or ResultadoCicloVida.Conflito or ResultadoCicloVida.Indisponivel));

        using var verificacao = fabrica.Services.CreateScope();
        var ctx = verificacao.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(1, await ctx.Set<HistoricoAgendamento>().IgnoreQueryFilters()
            .CountAsync(item => item.AgendamentoId == agendamento.Id &&
                                item.TipoEvento == TipoEventoHistorico.Cancelamento));
    }

    [TestMethod]
    public async Task Fechamento_NaoCancelaAgendamentoVencidoForaDoPrazo()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = SemearCenario(fabrica);
        var agora = DateTime.UtcNow;
        var agendamento = CriarAgendamento(cenario.BarbeariaId, cenario.ClienteId, cenario.BarbeiroId,
            agora.AddHours(-3), SituacaoAgendamento.Confirmado);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            contexto.Set<Agendamento>().Add(agendamento);
            SemeaduraIntegridade.GarantirTudo(contexto);
            await contexto.SaveChangesAsync();
        }

        using var escopoCancelamento = fabrica.Services.CreateScope();
        var repositorio = escopoCancelamento.ServiceProvider.GetRequiredService<IAgendamentoRepositorio>();
        var validador = escopoCancelamento.ServiceProvider.GetRequiredService<ValidadorHorarioAgendamento>();
        var chave = await validador.ObterChaveLockAsync(
            cenario.BarbeariaId, cenario.BarbeiroId, agendamento.IniciaEmUtc, CancellationToken.None);

        var cancelamento = await repositorio.CancelarAsync(
            cenario.BarbeariaId, agendamento.Id, chave, "sistema", "Tarde", agora, CancellationToken.None);
        Assert.AreEqual(ResultadoCicloVida.JaIniciado, cancelamento);

        var fechamento = Processo(fabrica, FecharVencidosProcesso.NomeJob);
        Assert.AreEqual(1, (await fechamento.ExecutarAsync(agora, CancellationToken.None)).Quantidade);

        using var verificacao = fabrica.Services.CreateScope();
        var ctx = verificacao.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var final = await ctx.Set<Agendamento>().IgnoreQueryFilters().SingleAsync(item => item.Id == agendamento.Id);
        Assert.AreEqual(SituacaoAgendamento.NaoCompareceu, final.Situacao);
        Assert.AreEqual(1, await ctx.Set<HistoricoAgendamento>().IgnoreQueryFilters()
            .CountAsync(item => item.AgendamentoId == agendamento.Id &&
                                item.TipoEvento == TipoEventoHistorico.NaoComparecimento));
    }

    private static ProcessoProxy Processo(WebApplicationFactory<Program> fabrica, string nome) =>
        new(fabrica, nome);

    private static RegistroIdempotencia Registro(string chave, DateTime criado, DateTime expira) =>
        RegistroIdempotencia.Criar("usuario-1", chave, "hash-" + chave, null, 200, criado, expira);

    private static Agendamento CriarAgendamento(
        Guid barbeariaId, string clienteId, Guid profissionalId, DateTime inicio,
        SituacaoAgendamento situacao, string? autor = null, string? motivo = null)
    {
        var agendamento = Agendamento.Criar(
            barbeariaId, clienteId, profissionalId, Guid.NewGuid(), inicio,
            "Corte", 40, 80m, null, OrigemAgendamento.Publico);
        if (situacao == SituacaoAgendamento.Cancelado)
            agendamento.Cancelar(autor ?? "sistema", motivo, inicio.AddMinutes(-1));
        if (situacao == SituacaoAgendamento.NaoCompareceu)
            agendamento.RegistrarNaoComparecimento(inicio.AddHours(3));
        return agendamento;
    }

    private static Cenario SemearCenario(WebApplicationFactory<Program> fabrica)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();

        var dono = PerfilProfissional.Criar("dono-1", "Dono", null);
        var barbeiro = PerfilProfissional.Criar("barbeiro-1", "Barbeiro", null);
        contexto.Set<PerfilProfissional>().AddRange(dono, barbeiro);

        var barbearia = Barbearia.Criar(
            "Barbearia Teste", "(11) 99999-9999", "contato@teste.com", "São Paulo",
            "Rua Central, 100", null, "ABC23456", "America/Sao_Paulo", dono.Id);
        contexto.Set<Barbearia>().Add(barbearia);

        contexto.Set<VinculoProfissional>().AddRange(
            VinculoProfissional.Criar(barbearia.Id, dono.Id),
            VinculoProfissional.CriarBarbeiro(barbearia.Id, barbeiro.Id));

        var agendamento = Agendamento.Criar(
            barbearia.Id, "cliente-1", barbeiro.Id, Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1), "Corte", 40, 80m, null, OrigemAgendamento.Publico);
        contexto.Set<Agendamento>().Add(agendamento);

        var outroAgendamento = Agendamento.Criar(
            barbearia.Id, "cliente-1", barbeiro.Id, Guid.NewGuid(),
            DateTime.UtcNow.AddDays(4), "Corte", 40, 80m, null, OrigemAgendamento.Publico);
        contexto.Set<Agendamento>().Add(outroAgendamento);

        var vencida = SugestaoHorario.Criar(
            barbearia.Id, agendamento.Id, barbeiro.Id, DateTime.UtcNow.AddDays(2), 40,
            "Troca", "barbeiro-1", DateTime.UtcNow.AddHours(-25));
        var vigente = SugestaoHorario.Criar(
            barbearia.Id, outroAgendamento.Id, barbeiro.Id, DateTime.UtcNow.AddDays(5), 40,
            "Outra troca", "barbeiro-1", DateTime.UtcNow);
        contexto.Set<SugestaoHorario>().AddRange(vencida, vigente);

        SemeaduraIntegridade.GarantirTudo(contexto);
        contexto.SaveChanges();
        return new Cenario(barbearia.Id, "cliente-1", barbeiro.Id);
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberJobs_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Modo"] = "Memoria",
                    ["ConnectionStrings:DefaultConnection"] = conexao,
                    ["Jobs:IntervaloMinutos"] = "60"
                })));
    }

    private static void CriarSchema(WebApplicationFactory<Program> fabrica)
    {
        using var escopo = fabrica.Services.CreateScope();
        escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>().Database.EnsureCreated();
    }

    private sealed record Cenario(Guid BarbeariaId, string ClienteId, Guid BarbeiroId);

    /// <summary>Resolve o processo em um escopo novo a cada execução.</summary>
    private sealed class ProcessoProxy(WebApplicationFactory<Program> fabrica, string nome) : IProcessoPeriodico
    {
        public string Nome => nome;

        public async Task<ResultadoProcessamento> ExecutarAsync(
            DateTime agoraUtc, CancellationToken cancellationToken)
        {
            using var escopo = fabrica.Services.CreateScope();
            var processo = escopo.ServiceProvider
                .GetRequiredService<RegistroProcessosPeriodicos>().Obter(nome)!;
            return await processo.ExecutarAsync(agoraUtc, cancellationToken);
        }
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
}
