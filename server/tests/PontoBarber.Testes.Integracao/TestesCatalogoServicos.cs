using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Modulos.Servicos;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Task 5.1: o catálogo modelo é aplicado automaticamente a barbearias novas,
/// sem duplicações e com isolamento entre tenants.
/// </summary>
[TestClass]
public sealed class TestesCatalogoServicos
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task CriarBarbearia_DisponibilizaOitoServicosSemPrecoFixo()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var servicos = await contexto.Set<Servico>()
            .IgnoreQueryFilters()
            .Where(item => item.BarbeariaId == barbearia.Id)
            .ToListAsync();

        Assert.AreEqual(8, servicos.Count);
        Assert.IsTrue(servicos.All(item => item.Ativo && item.EhModelo && item.Preco is null));
        Assert.AreEqual(30, servicos.Single(item => item.Nome == "Corte social / tradicional").DuracaoMinutos);
        Assert.AreEqual(40, servicos.Single(item => item.Nome == "Corte degradê (fade / navalhado)").DuracaoMinutos);
        Assert.AreEqual(30, servicos.Single(item => item.Nome == "Corte infantil").DuracaoMinutos);
        Assert.AreEqual(20, servicos.Single(item => item.Nome == "Raspagem na máquina ou navalha (cabeça inteira)").DuracaoMinutos);
        Assert.AreEqual(15, servicos.Single(item => item.Nome == "Acabamento / Pezinho").DuracaoMinutos);
        Assert.AreEqual(20, servicos.Single(item => item.Nome == "Lavagem e finalização (modelagem com pomada ou cera)").DuracaoMinutos);
        Assert.AreEqual(20, servicos.Single(item => item.Nome == "Barba simples (máquina e tesoura)").DuracaoMinutos);
        Assert.AreEqual(30, servicos.Single(item => item.Nome == "Barba completa (alinhamento com navalha e pós-barba)").DuracaoMinutos);
    }

    [TestMethod]
    public async Task AplicarCatalogoDuasVezes_NaoDuplicaServicos()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);

        using var escopo = fabrica.Services.CreateScope();
        var catalogo = escopo.ServiceProvider.GetRequiredService<ServicoCatalogoModelo>();
        await catalogo.AplicarAsync(barbearia.Id, CancellationToken.None);
        await catalogo.AplicarAsync(barbearia.Id, CancellationToken.None);

        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(8, await contexto.Set<Servico>()
            .IgnoreQueryFilters()
            .CountAsync(item => item.BarbeariaId == barbearia.Id));
    }

    [TestMethod]
    public async Task Catalogo_permaneceIsoladoEntreBarbearias()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var clienteA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(clienteA);
        var barbeariaA = await CriarBarbeariaAsync(clienteA);

        using var clienteB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(clienteB);
        var barbeariaB = await CriarBarbeariaAsync(clienteB);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var servicos = await contexto.Set<Servico>()
            .IgnoreQueryFilters()
            .ToListAsync();

        Assert.AreEqual(16, servicos.Count);
        Assert.AreEqual(8, servicos.Count(item => item.BarbeariaId == barbeariaA.Id));
        Assert.AreEqual(8, servicos.Count(item => item.BarbeariaId == barbeariaB.Id));
        Assert.AreEqual(16, servicos.Select(item => item.Id).Distinct().Count());
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberCatalogo_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
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
