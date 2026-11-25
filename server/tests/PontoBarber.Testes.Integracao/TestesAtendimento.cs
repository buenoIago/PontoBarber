using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Tasks 10.1 a 10.3: atendimento e pagamento com persistência real.</summary>
[TestClass]
public sealed class TestesAtendimento
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private static readonly TimeZoneInfo _fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private static readonly WebApplicationFactoryClientOptions _opcoes = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Ciclo_Atendimento_IniciaEConcluiComHorariosReais()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var id = await CriarAgendamentoAsync(cenario);

        var inicio = await Acao(cenario.Barbeiro, cenario.BarbeariaId, id, "iniciar");
        Assert.AreEqual(HttpStatusCode.OK, inicio.StatusCode);
        var corpoInicio = await LerAsync(inicio);
        Assert.AreEqual("EmAndamento", corpoInicio.GetProperty("situacao").GetString());
        Assert.IsFalse(corpoInicio.GetProperty("inicioRealUtc").ValueKind == JsonValueKind.Null);

        var fim = await Acao(cenario.Barbeiro, cenario.BarbeariaId, id, "concluir");
        Assert.AreEqual(HttpStatusCode.OK, fim.StatusCode);
        var corpoFim = await LerAsync(fim);
        Assert.AreEqual("Concluido", corpoFim.GetProperty("situacao").GetString());

        var detalhe = await cenario.Barbeiro.GetAsync(
            $"/api/v1/barbearias/{cenario.BarbeariaId}/agendamentos/{id}");
        Assert.AreEqual("Pendente", (await LerAsync(detalhe)).GetProperty("situacaoPagamento").GetString());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var persistido = await contexto.Set<Agendamento>().IgnoreQueryFilters().SingleAsync(item => item.Id == id);
        Assert.AreEqual(SituacaoAgendamento.Concluido, persistido.Situacao);
        Assert.IsNotNull(persistido.InicioRealUtc);
        Assert.IsNotNull(persistido.FimRealUtc);
        Assert.IsTrue(persistido.FimRealUtc >= persistido.InicioRealUtc);
        Assert.AreEqual(0, await contexto.Set<Pagamento>().IgnoreQueryFilters().CountAsync());
    }

    [TestMethod]
    public async Task Iniciar_EmCanceladoNaoCompareceuOuDuplicado_Recusa()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        var cancelado = await CriarAgendamentoAsync(cenario, AmanhaAs10());
        Assert.AreEqual(HttpStatusCode.OK, (await Acao(cenario.Barbeiro, cenario.BarbeariaId, cancelado, "cancelar", new { motivo = "Conflito" })).StatusCode);
        var tentativaCancelado = await Acao(cenario.Barbeiro, cenario.BarbeariaId, cancelado, "iniciar");
        Assert.AreEqual(HttpStatusCode.Conflict, tentativaCancelado.StatusCode);
        Assert.AreEqual(CodigosErro.AgendamentoEstadoInvalido,
            (await LerAsync(tentativaCancelado)).GetProperty("code").GetString());

        var ausente = await CriarAgendamentoAsync(cenario);
        Assert.AreEqual(HttpStatusCode.OK, (await Acao(cenario.Barbeiro, cenario.BarbeariaId, ausente, "ausencia")).StatusCode);
        var tentativaAusente = await Acao(cenario.Barbeiro, cenario.BarbeariaId, ausente, "iniciar");
        Assert.AreEqual(HttpStatusCode.Conflict, tentativaAusente.StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict,
            (await Acao(cenario.Barbeiro, cenario.BarbeariaId, ausente, "concluir")).StatusCode);

        var duplicado = await CriarAgendamentoAsync(cenario);
        Assert.AreEqual(HttpStatusCode.OK, (await Acao(cenario.Barbeiro, cenario.BarbeariaId, duplicado, "iniciar")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict,
            (await Acao(cenario.Barbeiro, cenario.BarbeariaId, duplicado, "iniciar")).StatusCode);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var eventos = await contexto.Set<HistoricoAgendamento>().IgnoreQueryFilters()
            .CountAsync(item => item.TipoEvento == TipoEventoHistorico.NaoComparecimento);
        Assert.AreEqual(1, eventos);
        Assert.IsFalse(await contexto.Set<Agendamento>().IgnoreQueryFilters()
            .AnyAsync(item => item.Id == cancelado && item.Situacao == SituacaoAgendamento.Confirmado));
    }

    [TestMethod]
    public async Task Concluir_SemInicio_Recusa()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var id = await CriarAgendamentoAsync(cenario);

        var resposta = await Acao(cenario.Barbeiro, cenario.BarbeariaId, id, "concluir");

        Assert.AreEqual(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.AreEqual(CodigosErro.AgendamentoEstadoInvalido,
            (await LerAsync(resposta)).GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Atendimento_AutorizacaoPorPerfilEIsolamento()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var id = await CriarAgendamentoAsync(cenario);

        Assert.AreEqual(HttpStatusCode.Forbidden,
            (await Acao(cenario.Cliente, cenario.BarbeariaId, id, "iniciar")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,
            (await Acao(cenario.OutroBarbeiro, cenario.BarbeariaId, id, "iniciar")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,
            (await Acao(cenario.Dono, cenario.BarbeariaId, id, "iniciar")).StatusCode);

        var outraBarbearia = Guid.NewGuid();
        var isolado = (await Acao(cenario.Barbeiro, outraBarbearia, id, "iniciar")).StatusCode;
        Assert.IsTrue(isolado is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Pagamento_ValidoComIdempotencia()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var id = await CriarAgendamentoAsync(cenario);
        Assert.AreEqual(HttpStatusCode.OK, (await Acao(cenario.Barbeiro, cenario.BarbeariaId, id, "iniciar")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Acao(cenario.Barbeiro, cenario.BarbeariaId, id, "concluir")).StatusCode);

        var primeiro = await Pagar(cenario.Barbeiro, cenario.BarbeariaId, id, "chave-1", 80m, "Pix");
        Assert.AreEqual(HttpStatusCode.Created, primeiro.StatusCode);
        var corpo = await LerAsync(primeiro);
        Assert.AreEqual("Pago", corpo.GetProperty("situacaoPagamento").GetString());

        var repetido = await Pagar(cenario.Barbeiro, cenario.BarbeariaId, id, "chave-1", 80m, "Pix");
        Assert.AreEqual(HttpStatusCode.Created, repetido.StatusCode);
        Assert.AreEqual(corpo.GetProperty("id").GetString(),
            (await LerAsync(repetido)).GetProperty("id").GetString());

        var duplicado = await Pagar(cenario.Barbeiro, cenario.BarbeariaId, id, "chave-2", 80m, "Pix");
        Assert.AreEqual(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.AreEqual(CodigosErro.PagamentoJaRegistrado,
            (await LerAsync(duplicado)).GetProperty("code").GetString());

        var detalhe = await cenario.Barbeiro.GetAsync(
            $"/api/v1/barbearias/{cenario.BarbeariaId}/agendamentos/{id}");
        Assert.AreEqual("Pago", (await LerAsync(detalhe)).GetProperty("situacaoPagamento").GetString());
    }

    [TestMethod]
    public async Task Pagamento_ConcorrenteMesmaChaveEPayload_DevolveMesmaResposta()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var id = await CriarAgendamentoAsync(cenario);
        Assert.AreEqual(HttpStatusCode.OK, (await Acao(cenario.Barbeiro, cenario.BarbeariaId, id, "iniciar")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Acao(cenario.Barbeiro, cenario.BarbeariaId, id, "concluir")).StatusCode);

        const string chave = "chave-concorrente";
        var respostas = await Task.WhenAll(
            Pagar(cenario.Barbeiro, cenario.BarbeariaId, id, chave, 80m, "Pix"),
            Pagar(cenario.Barbeiro, cenario.BarbeariaId, id, chave, 80m, "Pix"));

        Assert.IsTrue(respostas.All(resposta => resposta.StatusCode == HttpStatusCode.Created));
        var idPagamento = (await LerAsync(respostas[0])).GetProperty("id").GetString();
        Assert.AreEqual(idPagamento, (await LerAsync(respostas[1])).GetProperty("id").GetString());

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            Assert.AreEqual(1, await contexto.Set<Pagamento>().IgnoreQueryFilters().CountAsync());
        }

        // Mesma chave, payload diferente: continua sendo conflito estável.
        var divergente = await Pagar(cenario.Barbeiro, cenario.BarbeariaId, id, chave, 80m, "Cash");
        Assert.AreEqual(HttpStatusCode.Conflict, divergente.StatusCode);
        Assert.AreEqual(CodigosErro.ConcorrenciaConflito,
            (await LerAsync(divergente)).GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Pagamento_InvalidoESemAlterarEstadoDoAtendimento()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var id = await CriarAgendamentoAsync(cenario);
        Assert.AreEqual(HttpStatusCode.OK, (await Acao(cenario.Barbeiro, cenario.BarbeariaId, id, "iniciar")).StatusCode);

        var negativo = await Pagar(cenario.Barbeiro, cenario.BarbeariaId, id, "chave-neg", -1m, "Cash");
        Assert.AreEqual(HttpStatusCode.BadRequest, negativo.StatusCode);
        var invalida = await Pagar(cenario.Barbeiro, cenario.BarbeariaId, id, "chave-met", 10m, "Bitcoin");
        Assert.AreEqual(HttpStatusCode.BadRequest, invalida.StatusCode);
        var semChave = await Pagar(cenario.Barbeiro, cenario.BarbeariaId, id, null, 10m, "Cash");
        Assert.AreEqual(HttpStatusCode.BadRequest, semChave.StatusCode);
        Assert.AreEqual(CodigosErro.IdempotenciaChaveAusente,
            (await LerAsync(semChave)).GetProperty("code").GetString());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var agendamento = await contexto.Set<Agendamento>().IgnoreQueryFilters().SingleAsync(item => item.Id == id);
        Assert.AreEqual(SituacaoAgendamento.EmAndamento, agendamento.Situacao);
        Assert.IsNull(agendamento.FimRealUtc);
        Assert.AreEqual(0, await contexto.Set<Pagamento>().IgnoreQueryFilters().CountAsync());
    }

    [TestMethod]
    public async Task Pagamento_EmNaoComparecimento_Recusa()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var id = await CriarAgendamentoAsync(cenario);
        Assert.AreEqual(HttpStatusCode.OK, (await Acao(cenario.Barbeiro, cenario.BarbeariaId, id, "ausencia")).StatusCode);

        var resposta = await Pagar(cenario.Barbeiro, cenario.BarbeariaId, id, "chave-aus", 80m, "Pix");
        Assert.AreEqual(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.AreEqual(CodigosErro.AgendamentoEstadoInvalido,
            (await LerAsync(resposta)).GetProperty("code").GetString());
    }

    // Data fixa do dia corrente: o atendimento só pode começar no dia do
    // agendamento, independentemente da hora em que a suíte é executada.
    private static DateTime HojeAs10() => As10(0);

    // Instante sempre futuro e dentro da janela da fixture para a criação via
    // API; o horário persistido é ajustado depois para HojeAs10().
    private static DateTime ProximoInstanteValido()
    {
        var local = TimeZoneInfo.ConvertTime(DateTime.UtcNow, _fuso);
        var candidato = local.TimeOfDay > new TimeSpan(23, 0, 0)
            ? local.Date.AddDays(1).AddMinutes(15)
            : local.AddMinutes(15);
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(candidato, DateTimeKind.Unspecified), _fuso);
    }

    private static DateTime AmanhaAs10() => As10(1);

    private static DateTime As10(int dias) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(
                DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, _fuso))
                    .AddDays(dias).ToDateTime(new TimeOnly(10, 0)),
                DateTimeKind.Unspecified),
            _fuso);

    private static async Task<Guid> CriarAgendamentoAsync(Cenario cenario, DateTime? inicio = null)
    {
        var resposta = await EnviarAsync(
            cenario.Dono, HttpMethod.Post, $"/api/v1/barbearias/{cenario.BarbeariaId}/agendamentos",
            new
            {
                profissionalId = cenario.BarbeiroId,
                servicoId = cenario.ServicoId,
                iniciaEmUtc = (inicio ?? ProximoInstanteValido()).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                observacoes = (string?)null,
                clienteId = cenario.ClienteId
            });
        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode, await resposta.Content.ReadAsStringAsync());
        var id = Guid.Parse((await LerAsync(resposta)).GetProperty("id").GetString()!);

        var inicioPersistido = inicio ?? HojeAs10();
        using (var escopo = cenario.Fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            await contexto.Database.ExecuteSqlRawAsync(
                "UPDATE [Agendamentos] SET [IniciaEmUtc] = {0}, [TerminaEmUtc] = {1} WHERE [Id] = {2}",
                inicioPersistido, inicioPersistido.AddMinutes(40), id);
        }
        return id;
    }

    private static Task<HttpResponseMessage> Acao(HttpClient cliente, Guid barbeariaId, Guid id, string acao) =>
        Acao(cliente, barbeariaId, id, acao, new { });

    private static Task<HttpResponseMessage> Acao(
        HttpClient cliente, Guid barbeariaId, Guid id, string acao, object corpo) =>
        EnviarAsync(cliente, HttpMethod.Post,
            $"/api/v1/barbearias/{barbeariaId}/agendamentos/{id}/{acao}", corpo);

    private static async Task<HttpResponseMessage> Pagar(
        HttpClient cliente, Guid barbeariaId, Guid id, string? chave, decimal valor, string metodo)
    {
        var respostaGet = await cliente.GetAsync("/health/live");
        var cookie = respostaGet.Headers.GetValues("Set-Cookie")
            .First(item => item.Contains(ConfiguracaoAuth.CookieRequestToken, StringComparison.Ordinal));
        var trecho = cookie.Split(';')[0];
        var token = Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);

        using var requisicao = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/barbearias/{barbeariaId}/agendamentos/{id}/pagamento")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { valor, metodo }, _json), Encoding.UTF8, "application/json")
        };
        requisicao.Headers.Add(ConfiguracaoAuth.HeaderCsrf, token);
        if (chave is not null) requisicao.Headers.Add("Idempotency-Chave", chave);
        return await cliente.SendAsync(requisicao);
    }

    private static async Task<Cenario> CriarCenarioAsync(WebApplicationFactory<Program> fabrica)
    {
        var dono = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);

        var barbeiro = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(barbeiro);
        await EnviarAsync(barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });

        var outroBarbeiro = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(outroBarbeiro);
        await EnviarAsync(outroBarbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });

        var cliente = fabrica.CreateClient(_opcoes);
        await RegistrarClienteAsync(cliente);

        Guid Perfil(string usuarioId, ContextoPontoBarber contexto) =>
            contexto.Set<PontoBarber.Dominio.Modulos.Profissionais.PerfilProfissional>()
                .IgnoreQueryFilters().Single(item => item.UsuarioId == usuarioId).Id;

        Guid barbeiroId;
        Guid clienteId;
        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            barbeiroId = Perfil(ObterUsuarioId(barbeiro).GetAwaiter().GetResult(), contexto);
            clienteId = Guid.Parse(ObterUsuarioId(cliente).GetAwaiter().GetResult());
        }

        var dias = Enumerable.Range(1, 7)
            .Select(dia => new { diaSemana = dia, abreAs = "00:00:00", fechaAs = "23:59:00", fechado = false })
            .ToArray();
        Assert.AreEqual(HttpStatusCode.OK, (await EnviarAsync(
            dono, HttpMethod.Put, $"/api/v1/barbearias/{barbearia.Id}/horarios-funcionamento", dias)).StatusCode);

        var janelas = Enumerable.Range(1, 7)
            .Select(dia => new { diaSemana = dia, iniciaEm = "00:00:00", terminaEm = "23:59:00" })
            .ToArray();
        Assert.AreEqual(HttpStatusCode.OK, (await EnviarAsync(
            dono, HttpMethod.Put,
            $"/api/v1/barbearias/{barbearia.Id}/profissionais/{barbeiroId}/disponibilidade", janelas)).StatusCode);

        var servico = await EnviarAsync(dono, HttpMethod.Post,
            $"/api/v1/barbearias/{barbearia.Id}/servicos",
            new { nome = "Corte", duracaoMinutos = 40, preco = 80m });
        var servicoId = Guid.Parse((await LerAsync(servico)).GetProperty("id").GetString()!);

        return new Cenario(fabrica, dono, barbeiro, outroBarbeiro, cliente, barbearia.Id,
            barbeiroId, clienteId.ToString(), servicoId);
    }

    private static async Task<string> ObterUsuarioId(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync("/api/v1/auth/me");
        return (await LerAsync(resposta)).GetProperty("id").GetString()!;
    }

    private static async Task RegistrarProfissionalAsync(HttpClient cliente)
    {
        var email = $"prof-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        Assert.AreEqual(HttpStatusCode.Created, (await EnviarAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/register/professional",
            new { nomeCompleto = "Profissional", email, telefone = "11999999999", senha, confirmarSenha = senha })).StatusCode);
        Assert.AreEqual(HttpStatusCode.NoContent, (await EnviarAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha })).StatusCode);
    }

    private static async Task RegistrarClienteAsync(HttpClient cliente)
    {
        await AuthTestHelper.RegistrarEEntrarClienteAsync(cliente, "Cliente");
    }

    private static async Task<(Guid Id, string CodigoConvite)> CriarBarbeariaAsync(HttpClient cliente)
    {
        var resposta = await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/barbearias", new
        {
            nome = "Barbearia Teste",
            telefoneComercial = "(11) 99999-9999",
            emailComercial = "contato@teste.com",
            cidade = "São Paulo",
            endereco = "Rua Central, 100",
            instagram = (string?)null,
            fusoHorarioId = "America/Sao_Paulo"
        });
        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
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
        WebApplicationFactory<Program> Fabrica,
        HttpClient Dono,
        HttpClient Barbeiro,
        HttpClient OutroBarbeiro,
        HttpClient Cliente,
        Guid BarbeariaId,
        Guid BarbeiroId,
        string ClienteId,
        Guid ServicoId);

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberAtendimento_{Guid.NewGuid():N}";
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
