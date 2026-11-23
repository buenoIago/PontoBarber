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
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Task 6.2: disponibilidade semanal contida no funcionamento.</summary>
[TestClass]
public sealed class TestesDisponibilidadeSemanal
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Salvar_ComBarbeiroNaPropriaJanela_Retorna200EPersiste()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        await ConfigurarFuncionamentoAsync(dono, barbearia.Id);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        await EntrarAsync(barbeiro, barbearia.CodigoConvite);
        var perfilId = await ObterPerfilIdAsync(fabrica, barbeiro, barbearia.Id);

        var resposta = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put, Url(barbearia.Id, perfilId), Janelas());

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        Assert.AreEqual(1, (await LerCorpoAsync(resposta)).GetArrayLength());
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(1, await contexto.Set<DisponibilidadeSemanal>()
            .IgnoreQueryFilters().CountAsync(item => item.BarbeariaId == barbearia.Id && item.ProfissionalId == perfilId));
    }

    [TestMethod]
    public async Task Salvar_JanelaForaDoFuncionamento_Retorna422SemPersistir()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        await ConfigurarFuncionamentoAsync(dono, barbearia.Id);
        var perfilId = await ObterPerfilIdAsync(fabrica, dono, barbearia.Id);

        var fora = new[]
        {
            new { diaSemana = 1, iniciaEm = "07:00:00", terminaEm = "10:00:00" }
        };
        var resposta = await EnviarComAntiforgeryAsync(
            dono, HttpMethod.Put, Url(barbearia.Id, perfilId), fora);

        Assert.AreEqual(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.AreEqual(CodigosErro.AgendaForaFuncionamento, (await LerCorpoAsync(resposta)).GetProperty("code").GetString());
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(0, await contexto.Set<DisponibilidadeSemanal>()
            .IgnoreQueryFilters().CountAsync(item => item.BarbeariaId == barbearia.Id && item.ProfissionalId == perfilId));
    }

    [TestMethod]
    public async Task Salvar_ComBarbeiroSobreOutroProfissional_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        await ConfigurarFuncionamentoAsync(dono, barbearia.Id);
        var donoPerfilId = await ObterPerfilIdAsync(fabrica, dono, barbearia.Id);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        await EntrarAsync(barbeiro, barbearia.CodigoConvite);

        var resposta = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put, Url(barbearia.Id, donoPerfilId), Janelas());

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Salvar_ComProprietarioSobreBarbeiro_Permite()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        await ConfigurarFuncionamentoAsync(dono, barbearia.Id);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        await EntrarAsync(barbeiro, barbearia.CodigoConvite);
        var perfilId = await ObterPerfilIdAsync(fabrica, barbeiro, barbearia.Id);

        var resposta = await EnviarComAntiforgeryAsync(
            dono, HttpMethod.Put, Url(barbearia.Id, perfilId), Janelas());

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Salvar_EmOutraBarbearia_NaoAlteraDisponibilidade()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);
        await ConfigurarFuncionamentoAsync(donoA, barbeariaA.Id);
        var perfilA = await ObterPerfilIdAsync(fabrica, donoA, barbeariaA.Id);

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoB);
        var barbeariaB = await CriarBarbeariaAsync(donoB);

        var resposta = await EnviarComAntiforgeryAsync(
            donoB, HttpMethod.Put, Url(barbeariaA.Id, perfilA), Janelas());

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(0, await contexto.Set<DisponibilidadeSemanal>()
            .IgnoreQueryFilters().CountAsync(item => item.BarbeariaId == barbeariaA.Id));
    }

    private static string Url(Guid barbeariaId, Guid profissionalId) =>
        $"/api/v1/barbearias/{barbeariaId}/profissionais/{profissionalId}/disponibilidade";

    private static object[] Janelas() =>
        [new { diaSemana = 1, iniciaEm = "10:00:00", terminaEm = "12:00:00" }];

    private static async Task ConfigurarFuncionamentoAsync(HttpClient cliente, Guid barbeariaId)
    {
        var dias = Enumerable.Range(1, 7)
            .Select(dia => dia == 7
                ? new { diaSemana = dia, abreAs = (string?)null, fechaAs = (string?)null, fechado = true }
                : new { diaSemana = dia, abreAs = (string?)"09:00:00", fechaAs = (string?)"18:00:00", fechado = false })
            .ToArray();
        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Put, $"/api/v1/barbearias/{barbeariaId}/horarios-funcionamento", dias);
        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
    }

    private static async Task<Guid> ObterPerfilIdAsync(
        WebApplicationFactory<Program> fabrica,
        HttpClient cliente,
        Guid barbeariaId)
    {
        var eu = await cliente.GetAsync("/api/v1/auth/me");
        var usuarioId = (await LerCorpoAsync(eu)).GetProperty("id").GetString()!;

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var perfil = await contexto.Set<PerfilProfissional>()
            .SingleAsync(item => item.UsuarioId == usuarioId);
        return perfil.Id;
    }

    private static async Task EntrarAsync(HttpClient cliente, string codigoConvite)
    {
        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/barbearias/entrar", new { codigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberDisponibilidade_{Guid.NewGuid():N}";
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
