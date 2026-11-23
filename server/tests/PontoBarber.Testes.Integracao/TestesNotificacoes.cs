using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Modulos.Jobs;
using PontoBarber.Aplicacao.Modulos.Jobs.Processos;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Notificacoes;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Tasks 13.1 a 13.4: geração, idempotência, leitura e reconciliação.</summary>
[TestClass]
public sealed class TestesNotificacoes
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoes = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Reconciliacao_EventosSemNotificacao_GeraUmaVezEhIdempotente()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            var agora = DateTime.UtcNow;
            contexto.Set<HistoricoAgendamento>().AddRange(
                Evento(cenario.BarbeariaId, cenario.AgendamentoId, TipoEventoHistorico.Criacao, agora),
                Evento(cenario.BarbeariaId, cenario.AgendamentoId, TipoEventoHistorico.ServicoConcluido, agora),
                Evento(cenario.BarbeariaId, cenario.AgendamentoId, TipoEventoHistorico.PagamentoRegistrado, agora));
            await contexto.SaveChangesAsync();
        }

        var processo = fabrica.Services.CreateScope().ServiceProvider
            .GetRequiredService<RegistroProcessosPeriodicos>()
            .Obter(ReconciliarNotificacoesProcesso.NomeJob)!;

        var primeira = await ExecutarProcessoAsync(fabrica, processo);
        var segunda = await ExecutarProcessoAsync(fabrica, processo);

        Assert.IsTrue(primeira.Quantidade > 0);
        Assert.AreEqual(0, segunda.Quantidade);

        using var verificacao = fabrica.Services.CreateScope();
        var ctx = verificacao.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var notificacoes = await ctx.Set<Notificacao>().IgnoreQueryFilters().ToListAsync();
        // Criação (3), conclusão (2) e pagamento (2) = 7 notificações.
        Assert.AreEqual(7, notificacoes.Count(item => item.EventId != Guid.Empty));
        Assert.AreEqual(notificacoes.Count, notificacoes
            .Select(item => $"{item.EventId}|{item.DestinatarioUsuarioId}").Distinct().Count());
        Assert.IsTrue(notificacoes.Any(item => item.Tipo == TipoNotificacao.ServicoConcluido));
        Assert.IsTrue(notificacoes.Any(item => item.Tipo == TipoNotificacao.PagamentoRegistrado));
    }

    [TestMethod]
    public async Task Consulta_Notificacoes_RestritaAoProprioUsuario()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            var agora = DateTime.UtcNow;
            contexto.Set<Notificacao>().AddRange(
                Notificacao.Criar(cenario.BarbeariaId, cenario.ClienteAId, TipoNotificacao.AgendamentoCriado,
                    cenario.AgendamentoId, "Para A", Guid.NewGuid(), agora),
                Notificacao.Criar(cenario.BarbeariaId, cenario.ClienteAId, TipoNotificacao.ServicoConcluido,
                    cenario.AgendamentoId, "Para A 2", Guid.NewGuid(), agora),
                Notificacao.Criar(cenario.BarbeariaId, cenario.ClienteBId, TipoNotificacao.AgendamentoCriado,
                    cenario.AgendamentoId, "Para B", Guid.NewGuid(), agora));
            await contexto.SaveChangesAsync();
        }

        var lista = await cenario.ClienteA.GetAsync("/api/v1/notificacoes");
        Assert.AreEqual(HttpStatusCode.OK, lista.StatusCode);
        var corpo = await LerAsync(lista);
        Assert.AreEqual(2, corpo.GetProperty("itens").GetArrayLength());
        Assert.AreEqual(2, corpo.GetProperty("naoLidas").GetInt32());

        var idB = await ObterIdNotificacaoAsync(fabrica, cenario.ClienteBId);
        var acessoIndevido = await EnviarAsync(cenario.ClienteA, HttpMethod.Post,
            $"/api/v1/notificacoes/{idB}/ler", new { });
        Assert.AreEqual(HttpStatusCode.NotFound, acessoIndevido.StatusCode);

        var idA = await ObterIdNotificacaoAsync(fabrica, cenario.ClienteAId);
        Assert.AreEqual(HttpStatusCode.NoContent, (await EnviarAsync(cenario.ClienteA, HttpMethod.Post,
            $"/api/v1/notificacoes/{idA}/ler", new { })).StatusCode);

        var todas = await EnviarAsync(cenario.ClienteA, HttpMethod.Post,
            "/api/v1/notificacoes/ler-todas", new { });
        Assert.AreEqual(HttpStatusCode.OK, todas.StatusCode);
        Assert.AreEqual(1, (await LerAsync(todas)).GetProperty("marcadas").GetInt32());

        var contador = await cenario.ClienteA.GetAsync("/api/v1/notificacoes/nao-lidas");
        Assert.AreEqual(0, (await LerAsync(contador)).GetProperty("naoLidas").GetInt32());
    }

    [TestMethod]
    public async Task Concorrencia_Reconciliacao_SemDuplicidade()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            contexto.Set<HistoricoAgendamento>().Add(
                Evento(cenario.BarbeariaId, cenario.AgendamentoId,
                    TipoEventoHistorico.SugestaoCriada, DateTime.UtcNow));
            await contexto.SaveChangesAsync();
        }

        using var escopoA = fabrica.Services.CreateScope();
        using var escopoB = fabrica.Services.CreateScope();
        var servicoA = escopoA.ServiceProvider.GetRequiredService<ServicoExecucaoJobs>();
        var servicoB = escopoB.ServiceProvider.GetRequiredService<ServicoExecucaoJobs>();
        var processoA = escopoA.ServiceProvider.GetRequiredService<RegistroProcessosPeriodicos>()
            .Obter(ReconciliarNotificacoesProcesso.NomeJob)!;
        var processoB = escopoB.ServiceProvider.GetRequiredService<RegistroProcessosPeriodicos>()
            .Obter(ReconciliarNotificacoesProcesso.NomeJob)!;

        await Task.WhenAll(
            servicoA.ExecutarAsync(processoA, CancellationToken.None),
            servicoB.ExecutarAsync(processoB, CancellationToken.None));

        using var verificacao = fabrica.Services.CreateScope();
        var ctx = verificacao.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var notificacoes = await ctx.Set<Notificacao>().IgnoreQueryFilters().ToListAsync();
        Assert.AreEqual(1, notificacoes.Count);
        Assert.AreEqual(2, await ctx.Set<PontoBarber.Dominio.Modulos.Jobs.ExecucaoJob>()
            .CountAsync(item => item.NomeJob == ReconciliarNotificacoesProcesso.NomeJob));
    }

    private static async Task<ResultadoProcessamento> ExecutarProcessoAsync(
        WebApplicationFactory<Program> fabrica, IProcessoPeriodico processo)
    {
        using var escopo = fabrica.Services.CreateScope();
        return await escopo.ServiceProvider.GetRequiredService<ServicoExecucaoJobs>()
            .ExecutarAsync(processo, CancellationToken.None);
    }

    private static async Task<Guid> ObterIdNotificacaoAsync(
        WebApplicationFactory<Program> fabrica, string destinatarioId)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        return await contexto.Set<Notificacao>()
            .IgnoreQueryFilters()
            .Where(item => item.DestinatarioUsuarioId == destinatarioId)
            .Select(item => item.Id)
            .FirstAsync();
    }

    private static HistoricoAgendamento Evento(
        Guid barbeariaId, Guid agendamentoId, TipoEventoHistorico tipo, DateTime ocorridoEmUtc) =>
        HistoricoAgendamento.Criar(barbeariaId, agendamentoId, tipo, "sistema", ocorridoEmUtc);

    private static async Task<Cenario> CriarCenarioAsync(WebApplicationFactory<Program> fabrica)
    {
        var dono = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(dono, "dono-notif");
        var barbearia = await CriarBarbeariaAsync(dono);

        var barbeiro = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(barbeiro, "barbeiro-notif");
        await EnviarAsync(barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.Codigo });

        var clienteA = fabrica.CreateClient(_opcoes);
        await RegistrarClienteAsync(clienteA, "cliente-a-notif");
        var clienteB = fabrica.CreateClient(_opcoes);
        await RegistrarClienteAsync(clienteB, "cliente-b-notif");

        var clienteAId = await ObterUsuarioIdAsync(clienteA);
        var clienteBId = await ObterUsuarioIdAsync(clienteB);
        var barbeiroId = await ObterUsuarioIdAsync(barbeiro);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var perfil = await contexto.Set<PerfilProfissional>()
            .SingleAsync(item => item.UsuarioId == barbeiroId);
        var agendamento = Agendamento.Criar(
            barbearia.Id, clienteAId, perfil.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(1),
            "Corte", 40, 80m, null, OrigemAgendamento.Publico);
        contexto.Set<Agendamento>().Add(agendamento);
        await contexto.SaveChangesAsync();

        return new Cenario(dono, barbeiro, clienteA, clienteB, barbearia.Id, agendamento.Id,
            clienteAId, clienteBId);
    }

    private static async Task<string> ObterUsuarioIdAsync(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync("/api/v1/auth/me");
        resposta.EnsureSuccessStatusCode();
        return (await LerAsync(resposta)).GetProperty("id").GetString()!;
    }

    private static async Task RegistrarProfissionalAsync(HttpClient cliente, string prefixo)
    {
        var email = $"{prefixo}-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/auth/register/professional",
            new { nomeCompleto = prefixo, email, telefone = "11999999999", senha, confirmarSenha = senha });
        await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha });
    }

    private static async Task RegistrarClienteAsync(HttpClient cliente, string prefixo)
    {
        await AuthTestHelper.RegistrarEEntrarClienteAsync(cliente, prefixo, $"{prefixo}@exemplo.com");
    }

    private static async Task<(Guid Id, string Codigo)> CriarBarbeariaAsync(HttpClient cliente)
    {
        var resposta = await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/barbearias", new
        {
            nome = "Barbearia Notificações",
            telefoneComercial = "(11) 99999-9999",
            emailComercial = "contato@exemplo.com",
            cidade = "São Paulo",
            endereco = "Rua Central, 100",
            instagram = (string?)null,
            fusoHorarioId = "America/Sao_Paulo"
        });
        resposta.EnsureSuccessStatusCode();
        var corpo = await LerAsync(resposta);
        return (Guid.Parse(corpo.GetProperty("id").GetString()!), corpo.GetProperty("codigoConvite").GetString()!);
    }

    private static async Task<HttpResponseMessage> EnviarAsync(
        HttpClient cliente, HttpMethod metodo, string rota, object corpo)
    {
        var respostaGet = await cliente.GetAsync("/health/live");
        var valor = respostaGet.Headers.GetValues("Set-Cookie")
            .First(item => item.Contains(ConfiguracaoAuth.CookieRequestToken, StringComparison.Ordinal));
        var trecho = valor.Split(';')[0];
        var token = Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);

        using var requisicao = new HttpRequestMessage(metodo, rota)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpo, _json), Encoding.UTF8, "application/json")
        };
        requisicao.Headers.Add(ConfiguracaoAuth.HeaderCsrf, token);
        return await cliente.SendAsync(requisicao);
    }

    private static async Task<JsonElement> LerAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private sealed record Cenario(
        HttpClient Dono,
        HttpClient Barbeiro,
        HttpClient ClienteA,
        HttpClient ClienteB,
        Guid BarbeariaId,
        Guid AgendamentoId,
        string ClienteAId,
        string ClienteBId);

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberNotificacoes_{Guid.NewGuid():N}";
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
}
