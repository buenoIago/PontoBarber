using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Task 4.1: criação de barbearia, validação e código de convite único.
/// Os cenários que persistem dados usam SQL Server via Testcontainers.
/// </summary>
[TestClass]
public sealed class TestesBarbearia
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Criar_ComDadosValidos_Retorna201EPersisteBarbearia()
    {
        using var fabrica = CriarFabricaComBanco(out _);
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);

        var resposta = await PostComAntiforgeryAsync(cliente, "/api/v1/barbearias", RequisicaoValida());

        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(8, corpo.GetProperty("codigoConvite").GetString()?.Length);
        Assert.AreEqual("America/Sao_Paulo", corpo.GetProperty("fusoHorarioId").GetString());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var barbearia = await contexto.Set<Barbearia>().SingleAsync();
        Assert.AreEqual(corpo.GetProperty("id").GetString(), barbearia.Id.ToString());
        // Fora do contexto HTTP não há tenant ativo: o filtro global esconderia o vínculo.
        Assert.IsTrue(await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters()
            .AnyAsync(vinculo => vinculo.Cargo == CargoProfissional.Proprietario &&
                                 vinculo.Situacao == StatusVinculo.Ativo));
    }

    [TestMethod]
    public async Task Criar_ComCampoInvalido_Retorna400SemPersistir()
    {
        using var fabrica = CriarFabricaComBanco(out _);
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);

        var request = RequisicaoValida() with { Nome = "", EmailComercial = "invalido" };
        var resposta = await PostComAntiforgeryAsync(cliente, "/api/v1/barbearias", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou, corpo.GetProperty("code").GetString());
        Assert.IsTrue(corpo.GetProperty("errors").TryGetProperty("nome", out _));
        Assert.IsTrue(corpo.GetProperty("errors").TryGetProperty("emailComercial", out _));

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(0, await contexto.Set<Barbearia>().CountAsync());
    }

    [TestMethod]
    public async Task CodigoConvite_ComValorDuplicado_RetornaConflitoDoBanco()
    {
        using var fabrica = CriarFabricaComBanco(out _);
        CriarSchema(fabrica);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();

        const string codigo = "ABC23456";
        contexto.Set<Barbearia>().Add(Barbearia.Criar(
            "Primeira", "11999999999", "um@exemplo.com", "São Paulo", "Rua Um, 1",
            null, codigo, "America/Sao_Paulo", Guid.CreateVersion7()));
        SemeaduraIntegridade.GarantirTudo(contexto);
        await contexto.SaveChangesAsync();

        contexto.Set<Barbearia>().Add(Barbearia.Criar(
            "Segunda", "11999999998", "dois@exemplo.com", "São Paulo", "Rua Dois, 2",
            null, codigo, "America/Sao_Paulo", Guid.CreateVersion7()));

        SemeaduraIntegridade.GarantirTudo(contexto);
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco(out string stringConexao)
    {
        var database = $"PontoBarberBarbearia_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);
        stringConexao = conexao;

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
        var email = "barbeiro-" + Guid.NewGuid().ToString("N")[..8] + "@exemplo.com";
        var senha = "Senha123";

        var cadastro = await PostComAntiforgeryAsync(cliente, "/api/v1/auth/register/professional", new
        {
            nomeCompleto = "Barbeiro Teste",
            email,
            telefone = "11999999999",
            senha,
            confirmarSenha = senha
        });
        Assert.AreEqual(HttpStatusCode.Created, cadastro.StatusCode);

        var login = await PostComAntiforgeryAsync(cliente, "/api/v1/auth/login", new { email, senha });
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);
    }

    private static CriarBarbeariaRequest RequisicaoValida() => new(
        "Barbearia Central",
        "(11) 99999-9999",
        "contato@barbearia.com",
        "São Paulo",
        "Rua Central, 100",
        "@barbeariacentral",
        "America/Sao_Paulo");

    private static async Task<HttpResponseMessage> PostComAntiforgeryAsync(
        HttpClient cliente,
        string rota,
        object corpo)
    {
        var respostaGet = await cliente.GetAsync("/health/live");
        var valor = respostaGet.Headers.GetValues("Set-Cookie")
            .First(cookie => cookie.Contains(ConfiguracaoAuth.CookieRequestToken, StringComparison.Ordinal));
        var trecho = valor.Split(';')[0];
        var token = Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, rota)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpo, _json), Encoding.UTF8, "application/json")
        };
        requisicao.Headers.Add("X-CSRF-TOKEN", token);
        return await cliente.SendAsync(requisicao);
    }

    private static async Task<JsonElement> LerCorpoAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }
}
