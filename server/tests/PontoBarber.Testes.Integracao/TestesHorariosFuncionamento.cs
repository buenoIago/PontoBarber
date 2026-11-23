using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Task 6.1: funcionamento semanal por dia da semana.</summary>
[TestClass]
public sealed class TestesHorariosFuncionamento
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Salvar_ComProprietario_PersisteSeteDiasComDomingoFechado()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);

        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Put, SemanaUrl(barbearia.Id), Semana());

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(7, corpo.GetArrayLength());
        Assert.IsTrue(corpo.EnumerateArray().Any(dia =>
            dia.GetProperty("diaSemana").GetInt32() == (int)DiaSemana.Domingo &&
            dia.GetProperty("fechado").GetBoolean()));

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(7, await contexto.Set<HorarioFuncionamento>()
            .IgnoreQueryFilters().CountAsync(dia => dia.BarbeariaId == barbearia.Id));
    }

    [TestMethod]
    public async Task Salvar_ComPeriodoInvertido_Retorna400EPreservaConfiguracaoAnterior()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);
        await EnviarComAntiforgeryAsync(cliente, HttpMethod.Put, SemanaUrl(barbearia.Id), Semana());

        var invalida = Semana();
        invalida[0] = invalida[0] with { AbreAs = "18:00:00", FechaAs = "09:00:00" };
        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Put, SemanaUrl(barbearia.Id), invalida);

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou, (await LerCorpoAsync(resposta)).GetProperty("code").GetString());
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var segunda = await contexto.Set<HorarioFuncionamento>().IgnoreQueryFilters()
            .SingleAsync(dia => dia.BarbeariaId == barbearia.Id && dia.DiaSemana == DiaSemana.Segunda);
        Assert.AreEqual(new TimeOnly(9, 0), segunda.AbreAs);
    }

    [TestMethod]
    public async Task Salvar_ComBarbeiro_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });

        var resposta = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put, SemanaUrl(barbearia.Id), Semana());

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Listar_ComBarbeiroVinculado_RetornaSemanaDaBarbearia()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);
        await EnviarComAntiforgeryAsync(proprietario, HttpMethod.Put, SemanaUrl(barbearia.Id), Semana());

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });

        var resposta = await barbeiro.GetAsync(SemanaUrl(barbearia.Id));

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        Assert.AreEqual(7, (await LerCorpoAsync(resposta)).GetArrayLength());
    }

    [TestMethod]
    public async Task Configuracao_FicaIsoladaEntreBarbearias()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);
        await EnviarComAntiforgeryAsync(donoA, HttpMethod.Put, SemanaUrl(barbeariaA.Id), Semana());

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoB);
        var barbeariaB = await CriarBarbeariaAsync(donoB);
        var resposta = await donoB.GetAsync(SemanaUrl(barbeariaB.Id));

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        Assert.AreEqual(0, (await LerCorpoAsync(resposta)).GetArrayLength());
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(7, await contexto.Set<HorarioFuncionamento>()
            .IgnoreQueryFilters().CountAsync(dia => dia.BarbeariaId == barbeariaA.Id));
    }

    private static string SemanaUrl(Guid barbeariaId) =>
        $"/api/v1/barbearias/{barbeariaId}/horarios-funcionamento";

    private sealed record DiaTest(int DiaSemana, string? AbreAs, string? FechaAs, bool Fechado);

    private static List<DiaTest> Semana() =>
        Enumerable.Range(1, 7)
            .Select(dia => dia == 7
                ? new DiaTest(dia, null, null, true)
                : new DiaTest(dia, "09:00:00", "18:00:00", false))
            .ToList();

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberHorarios_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Modo"] = "Memoria",
                    ["ConnectionStrings:DefaultConnection"] = conexao
                })));
    }

    private static void CriarSchema(WebApplicationFactory<Program> fabrica)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        contexto.Database.EnsureCreated();
    }

    private static async Task RegistrarEEntrarAsync(HttpClient cliente)
    {
        var email = $"prof-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        var cadastro = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/register/professional",
            new { nomeCompleto = "Profissional Teste", email, telefone = "11999999999", senha, confirmarSenha = senha });
        Assert.AreEqual(HttpStatusCode.Created, cadastro.StatusCode);

        var login = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha });
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);
    }

    private static async Task<(Guid Id, string CodigoConvite)> CriarBarbeariaAsync(HttpClient cliente)
    {
        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/barbearias",
            new
            {
                nome = "Barbearia Central",
                telefoneComercial = "(11) 99999-9999",
                emailComercial = "contato@barbearia.com",
                cidade = "São Paulo",
                endereco = "Rua Central, 100",
                instagram = "@barbeariacentral",
                fusoHorarioId = "America/Sao_Paulo"
            });
        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        return (Guid.Parse(corpo.GetProperty("id").GetString()!), corpo.GetProperty("codigoConvite").GetString()!);
    }

    private static async Task<HttpResponseMessage> EnviarComAntiforgeryAsync(
        HttpClient cliente,
        HttpMethod metodo,
        string rota,
        object corpo)
    {
        var respostaGet = await cliente.GetAsync("/health/live");
        var valor = respostaGet.Headers.GetValues("Set-Cookie")
            .First(cookie => cookie.Contains(ConfiguracaoAuth.CookieRequestToken, StringComparison.Ordinal));
        var trecho = valor.Split(';')[0];
        var token = Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);

        using var requisicao = new HttpRequestMessage(metodo, rota)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpo, _json), Encoding.UTF8, "application/json")
        };
        requisicao.Headers.Add(ConfiguracaoAuth.HeaderCsrf, token);
        return await cliente.SendAsync(requisicao);
    }

    private static async Task<JsonElement> LerCorpoAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }
}
