using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Tasks 4.2 e 4.3: consulta/edição da barbearia e ingresso por convite.
/// Os cenários que persistem dados usam SQL Server via Testcontainers.
/// </summary>
[TestClass]
public sealed class TestesGestaoEConvite
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Obter_ComProprietario_RetornaDadosDaBarbearia()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);

        var resposta = await cliente.GetAsync($"/api/v1/barbearias/{barbearia.Id}");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual("Barbearia Central", corpo.GetProperty("nome").GetString());
        Assert.AreEqual(barbearia.CodigoConvite, corpo.GetProperty("codigoConvite").GetString());
    }

    [TestMethod]
    public async Task Atualizar_ComProprietario_PreservaCodigoVinculoEHistorico()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);

        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Put, $"/api/v1/barbearias/{barbearia.Id}",
            new
            {
                nome = "Barbearia Atualizada",
                telefoneComercial = "(11) 99999-9999",
                emailComercial = "contato@barbearia.com",
                cidade = "São Paulo",
                endereco = "Rua Nova, 200",
                instagram = "@barbeariacentral"
            });

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var persistida = await contexto.Set<Barbearia>().SingleAsync();
        Assert.AreEqual("Barbearia Atualizada", persistida.Nome);
        Assert.AreEqual(barbearia.CodigoConvite, persistida.CodigoConvite);
        Assert.AreEqual(1, await contexto.Set<VinculoProfissional>().IgnoreQueryFilters().CountAsync());
    }

    [TestMethod]
    public async Task Atualizar_ComDadosInvalidos_Retorna400()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);

        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Put, $"/api/v1/barbearias/{barbearia.Id}",
            new { nome = "", emailComercial = "invalido" });

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou, corpo.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Atualizar_ComBarbeiroVinculado_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        var entrada = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, entrada.StatusCode);

        var resposta = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put, $"/api/v1/barbearias/{barbearia.Id}",
            new { nome = "Tentativa indevida" });

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Obter_DeOutraBarbearia_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var primeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(primeiro);
        var barbearia = await CriarBarbeariaAsync(primeiro);

        using var segundo = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(segundo);
        await CriarBarbeariaAsync(segundo);

        var resposta = await segundo.GetAsync($"/api/v1/barbearias/{barbearia.Id}");

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Entrar_ComCodigoValido_CriaVinculoDeBarbeiro()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        var resposta = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual("Barbeiro", corpo.GetProperty("cargo").GetString());
        Assert.AreEqual("Ativo", corpo.GetProperty("situacao").GetString());
        Assert.AreEqual(barbearia.Id.ToString(), corpo.GetProperty("barbeariaId").GetString());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var vinculo = await contexto.Set<VinculoProfissional>().IgnoreQueryFilters()
            .SingleAsync(v => v.Cargo == CargoProfissional.Barbeiro);
        Assert.AreEqual(corpo.GetProperty("profissionalId").GetString(), vinculo.ProfissionalId.ToString());
    }

    [TestMethod]
    public async Task Entrar_ComCodigoInexistente_Retorna404()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);

        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = "XXXXXXXX" });

        Assert.AreEqual(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Entrar_ComVinculoDuplicado_Retorna409()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        var corpo = new { codigoConvite = barbearia.CodigoConvite };
        Assert.AreEqual(HttpStatusCode.OK, (await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar", corpo)).StatusCode);

        var segunda = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar", corpo);

        Assert.AreEqual(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [TestMethod]
    public async Task Entrar_ComVinculoInativo_Retorna409SemReativar()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        var entrada = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, entrada.StatusCode);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            var vinculo = await contexto.Set<VinculoProfissional>().IgnoreQueryFilters()
                .SingleAsync(v => v.Cargo == CargoProfissional.Barbeiro);
            typeof(VinculoProfissional).GetProperty(nameof(VinculoProfissional.Situacao))!
                .SetValue(vinculo, StatusVinculo.Inativo);
            await contexto.SaveChangesAsync();
        }

        var segunda = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });

        Assert.AreEqual(HttpStatusCode.Conflict, segunda.StatusCode);
        using var escopoFinal = fabrica.Services.CreateScope();
        var contextoFinal = escopoFinal.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(1, await contextoFinal.Set<VinculoProfissional>().IgnoreQueryFilters()
            .CountAsync(v => v.Cargo == CargoProfissional.Barbeiro));
    }

    [TestMethod]
    public async Task Vinculo_DuplicadoNoBanco_RetornaConflito()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var barbeariaId = Guid.CreateVersion7();
        var profissionalId = Guid.CreateVersion7();
        contexto.Set<VinculoProfissional>().Add(VinculoProfissional.CriarBarbeiro(barbeariaId, profissionalId));
        await contexto.SaveChangesAsync();

        contexto.Set<VinculoProfissional>().Add(VinculoProfissional.CriarBarbeiro(barbeariaId, profissionalId));
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberGestao_{Guid.NewGuid():N}";
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
        requisicao.Headers.Add("X-CSRF-TOKEN", token);
        return await cliente.SendAsync(requisicao);
    }

    private static async Task<JsonElement> LerCorpoAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }
}
