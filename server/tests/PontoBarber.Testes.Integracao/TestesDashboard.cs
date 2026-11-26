using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Tasks 12.1 a 12.4: indicadores e próximos com persistência real.</summary>
[TestClass]
public sealed class TestesDashboard
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
    public async Task Indicadores_ComDados_RespeitamPeriodosEFuso()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica, semearDados: true);

        var resposta = await cenario.Dono.GetAsync(
            $"/api/v1/barbearias/{cenario.BarbeariaId}/dashboard");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerAsync(resposta);
        // Somente o agendamento de hoje conta em "Hoje"; os limites exatos de
        // semana/mês são verificados nos testes unitários de período.
        Assert.AreEqual(1, corpo.GetProperty("hoje").GetInt32());
        Assert.IsTrue(corpo.GetProperty("estaSemana").GetInt32() >= 1);
        Assert.AreEqual(1, corpo.GetProperty("cancelamentos").GetInt32());
        // 80 + 50 do mês atual; 999 do mês passado e 777 de outra barbearia não entram.
        Assert.AreEqual<decimal>(130m, corpo.GetProperty("recebidoNoMes").GetDecimal());

        var proximos = corpo.GetProperty("proximos").EnumerateArray().ToList();
        Assert.IsTrue(proximos.Count >= 1);
        Assert.AreEqual("Cliente Dashboard", proximos[0].GetProperty("nomeCliente").GetString());
        Assert.AreEqual("barbeiro-dash", proximos[0].GetProperty("nomeProfissional").GetString());
        Assert.AreEqual("Confirmado", proximos[0].GetProperty("situacao").GetString());
    }

    [TestMethod]
    public async Task Indicadores_SemMovimentacao_RetornamZeroEListaVazia()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica, semearDados: false);

        var resposta = await cenario.Dono.GetAsync(
            $"/api/v1/barbearias/{cenario.BarbeariaId}/dashboard");

        var corpo = await LerAsync(resposta);
        Assert.AreEqual(0, corpo.GetProperty("hoje").GetInt32());
        Assert.AreEqual(0, corpo.GetProperty("estaSemana").GetInt32());
        Assert.AreEqual(0, corpo.GetProperty("cancelamentos").GetInt32());
        Assert.AreEqual<decimal>(0m, corpo.GetProperty("recebidoNoMes").GetDecimal());
        Assert.AreEqual(0, corpo.GetProperty("proximos").GetArrayLength());
    }

    [TestMethod]
    public async Task Dashboard_AutorizacaoEIsolamentoEntreBarbearias()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica, semearDados: true);

        Assert.AreEqual(HttpStatusCode.Forbidden,
            (await cenario.Barbeiro.GetAsync(
                $"/api/v1/barbearias/{cenario.BarbeariaId}/dashboard")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,
            (await cenario.Cliente.GetAsync(
                $"/api/v1/barbearias/{cenario.BarbeariaId}/dashboard")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,
            (await cenario.OutroDono.GetAsync(
                $"/api/v1/barbearias/{cenario.BarbeariaId}/dashboard")).StatusCode);

        var daOutra = await cenario.Dono.GetAsync(
            $"/api/v1/barbearias/{cenario.OutraBarbeariaId}/dashboard");
        Assert.AreEqual(HttpStatusCode.Forbidden, daOutra.StatusCode);
    }

    private static async Task<Cenario> CriarCenarioAsync(
        WebApplicationFactory<Program> fabrica, bool semearDados)
    {
        var dono = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(dono, "dono-dash");
        var barbearia = await CriarBarbeariaAsync(dono, "Barbearia Dashboard");

        var outroDono = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(outroDono, "outro-dono-dash");
        var outraBarbearia = await CriarBarbeariaAsync(outroDono, "Outra Barbearia");

        var barbeiro = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(barbeiro, "barbeiro-dash");
        await EnviarAsync(barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.Codigo });

        var cliente = fabrica.CreateClient(_opcoes);
        await RegistrarClienteAsync(cliente);

        var donoId = await ObterUsuarioIdAsync(dono);
        var barbeiroId = await ObterUsuarioIdAsync(barbeiro);
        var clienteId = await ObterUsuarioIdAsync(cliente);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var perfilBarbeiro = await contexto.Set<PerfilProfissional>()
            .SingleAsync(item => item.UsuarioId == barbeiroId);

        if (semearDados)
        {
            var hojeLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, _fuso));
            var hojeAs10 = EmUtc(hojeLocal, 10);
            var amanhaAs10 = EmUtc(hojeLocal.AddDays(1), 10);
            var ontemAs10 = EmUtc(hojeLocal.AddDays(-1), 10);
            var mesPassado = EmUtc(new DateOnly(hojeLocal.Year, hojeLocal.Month, 1).AddMonths(-1), 10);

            var agendamentos = new[]
            {
                Agendamento.Criar(barbearia.Id, clienteId, perfilBarbeiro.Id, Guid.NewGuid(),
                    hojeAs10, "Corte", 40, 80m, null, OrigemAgendamento.Publico),
                Agendamento.Criar(barbearia.Id, clienteId, perfilBarbeiro.Id, Guid.NewGuid(),
                    amanhaAs10, "Corte", 40, 80m, null, OrigemAgendamento.Publico),
                Agendamento.Criar(barbearia.Id, clienteId, perfilBarbeiro.Id, Guid.NewGuid(),
                    ontemAs10, "Corte", 40, 80m, null, OrigemAgendamento.Publico)
            };
            var cancelado = Agendamento.Criar(barbearia.Id, clienteId, perfilBarbeiro.Id, Guid.NewGuid(),
                amanhaAs10.AddHours(2), "Corte", 40, 80m, null, OrigemAgendamento.Publico);
            cancelado.Cancelar(donoId, "Conflito", DateTime.UtcNow);
            var canceladoMesPassado = Agendamento.Criar(barbearia.Id, clienteId, perfilBarbeiro.Id, Guid.NewGuid(),
                mesPassado, "Corte", 40, 80m, null, OrigemAgendamento.Publico);
            canceladoMesPassado.Cancelar(donoId, "Conflito", mesPassado.AddMinutes(-1));

            contexto.Set<Agendamento>().AddRange(
                agendamentos.Append(cancelado).Append(canceladoMesPassado));

            contexto.Set<Pagamento>().AddRange(
                Pagamento.Criar(barbearia.Id, agendamentos[0].Id, 80m, FormaPagamento.Pix, DateTime.UtcNow, barbeiroId),
                Pagamento.Criar(barbearia.Id, agendamentos[1].Id, 50m, FormaPagamento.Cash, DateTime.UtcNow, barbeiroId),
                Pagamento.Criar(barbearia.Id, canceladoMesPassado.Id, 999m, FormaPagamento.Pix,
                    mesPassado, barbeiroId),
                Pagamento.Criar(outraBarbearia.Id, Guid.NewGuid(), 777m, FormaPagamento.Cash,
                    DateTime.UtcNow, donoId));

            SemeaduraIntegridade.GarantirTudo(contexto);
            await contexto.SaveChangesAsync();
        }

        return new Cenario(dono, barbeiro, cliente, outroDono,
            barbearia.Id, outraBarbearia.Id);
    }

    private static DateTime EmUtc(DateOnly data, int hora) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(data.ToDateTime(new TimeOnly(hora, 0)), DateTimeKind.Unspecified),
            _fuso);

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
        await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/auth/cadastro/profissional",
            new { nomeCompleto = prefixo, email, telefone = "11999999999", senha, confirmarSenha = senha });
        await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha });
    }

    private static async Task RegistrarClienteAsync(HttpClient cliente)
    {
        await AuthTestHelper.RegistrarEEntrarClienteAsync(cliente, "Cliente Dashboard");
    }

    private static async Task<(Guid Id, string Codigo)> CriarBarbeariaAsync(HttpClient cliente, string nome)
    {
        var resposta = await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/barbearias", new
        {
            nome,
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
        HttpClient Cliente,
        HttpClient OutroDono,
        Guid BarbeariaId,
        Guid OutraBarbeariaId);

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberDashboard_{Guid.NewGuid():N}";
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
