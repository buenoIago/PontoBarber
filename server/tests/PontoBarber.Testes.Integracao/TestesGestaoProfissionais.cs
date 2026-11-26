using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Task 4.4: ativação, desativação, edição e remoção de vínculos com
/// autorização por barbearia. Persistência real via Testcontainers.
/// </summary>
[TestClass]
public sealed class TestesGestaoProfissionais
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task AlterarStatus_ComProprietario_DesativaEAtivaVinculo()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);
        var profissionalId = await CriarProfissionalVinculadoAsync(fabrica, proprietario, barbearia.CodigoConvite);

        var desativado = await EnviarComAntiforgeryAsync(
            proprietario, HttpMethod.Patch,
            $"/api/v1/barbearias/{barbearia.Id}/profissionais/{profissionalId}/status",
            new { ativo = false });
        Assert.AreEqual(HttpStatusCode.OK, desativado.StatusCode);
        Assert.AreEqual("Inativo", (await LerCorpoAsync(desativado)).GetProperty("situacao").GetString());

        var ativado = await EnviarComAntiforgeryAsync(
            proprietario, HttpMethod.Patch,
            $"/api/v1/barbearias/{barbearia.Id}/profissionais/{profissionalId}/status",
            new { ativo = true });
        Assert.AreEqual(HttpStatusCode.OK, ativado.StatusCode);
        Assert.AreEqual("Ativo", (await LerCorpoAsync(ativado)).GetProperty("situacao").GetString());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var vinculo = await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters()
            .SingleAsync(item => item.ProfissionalId == profissionalId);
        Assert.AreEqual(StatusVinculo.Ativo, vinculo.Situacao);
    }

    [TestMethod]
    public async Task ListarProfissionais_ComProprietario_RetornaVinculosDaBarbearia()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        var proprietarioId = await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);
        var barbeiroId = await CriarProfissionalVinculadoAsync(fabrica, proprietario, barbearia.CodigoConvite);
        var perfilProprietario = await ObterPerfilIdAsync(fabrica, proprietarioId);

        var resposta = await proprietario.GetAsync($"/api/v1/barbearias/{barbearia.Id}/profissionais");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(2, corpo.GetArrayLength());
        var ids = corpo.EnumerateArray().Select(item => Guid.Parse(item.GetProperty("id").GetString()!)).ToList();
        Assert.IsTrue(ids.Contains(perfilProprietario));
        Assert.IsTrue(ids.Contains(barbeiroId));
        Assert.IsTrue(corpo.EnumerateArray().All(item => item.GetProperty("situacao").GetString() == "Ativo"));
    }

    [TestMethod]
    public async Task EditarDados_ComProprietario_AlteraNomeETelefoneSemMudarConta()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);
        var profissionalId = await CriarProfissionalVinculadoAsync(fabrica, proprietario, barbearia.CodigoConvite);

        var resposta = await EnviarComAntiforgeryAsync(
            proprietario, HttpMethod.Put,
            $"/api/v1/barbearias/{barbearia.Id}/profissionais/{profissionalId}",
            new { nomeCompleto = "Nome Editado", telefone = "11977776666" });

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual("Nome Editado", corpo.GetProperty("nomeCompleto").GetString());
        Assert.AreEqual("11977776666", corpo.GetProperty("telefone").GetString());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var perfil = await contexto.Set<PerfilProfissional>().SingleAsync(item => item.Id == profissionalId);
        Assert.AreEqual("Nome Editado", perfil.NomeCompleto);
        Assert.AreEqual(barbearia.Id, (await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters().SingleAsync(item => item.ProfissionalId == profissionalId)).BarbeariaId);
    }

    [TestMethod]
    public async Task Remover_ComProprietario_MarcaVinculoEPreservaContaEPerfil()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);
        var (profissionalId, email) = await CriarProfissionalVinculadoComEmailAsync(
            fabrica, proprietario, barbearia.CodigoConvite);

        var resposta = await EnviarComAntiforgeryAsync(
            proprietario, HttpMethod.Delete,
            $"/api/v1/barbearias/{barbearia.Id}/profissionais/{profissionalId}",
            new { });

        Assert.AreEqual(HttpStatusCode.NoContent, resposta.StatusCode);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var vinculo = await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters().SingleAsync(item => item.ProfissionalId == profissionalId);
        Assert.AreEqual(StatusVinculo.Removido, vinculo.Situacao);
        Assert.IsNotNull(vinculo.RemovidoEmUtc);
        Assert.IsTrue(await contexto.Set<PerfilProfissional>().AnyAsync(item => item.Id == profissionalId));

        var usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Assert.IsNotNull(await usuarios.FindByEmailAsync(email));
        Assert.AreEqual(2, await contexto.Set<VinculoProfissional>().IgnoreQueryFilters().CountAsync());
    }

    [TestMethod]
    public async Task Editar_ComBarbeiro_NegaAcessoAoVinculoDoProprietario()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        var proprietarioId = await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);
        var alvoId = await ObterPerfilIdAsync(fabrica, proprietarioId);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        var entrada = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, entrada.StatusCode);

        var resposta = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put,
            $"/api/v1/barbearias/{barbearia.Id}/profissionais/{alvoId}",
            new { nomeCompleto = "Tentativa indevida", telefone = "11999999999" });

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.AreEqual(CodigosErro.TenantNegado, (await LerCorpoAsync(resposta)).GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task AlterarStatus_ComProprietarioDeOutraBarbearia_NaoAlteraVinculo()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoB);
        var barbeariaB = await CriarBarbeariaAsync(donoB);
        var profissionalB = await CriarProfissionalVinculadoAsync(fabrica, donoB, barbeariaB.CodigoConvite);

        var resposta = await EnviarComAntiforgeryAsync(
            donoA, HttpMethod.Patch,
            $"/api/v1/barbearias/{barbeariaA.Id}/profissionais/{profissionalB}/status",
            new { ativo = false });

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var vinculo = await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters().SingleAsync(item => item.ProfissionalId == profissionalB);
        Assert.AreEqual(StatusVinculo.Ativo, vinculo.Situacao);
        Assert.AreEqual(barbeariaB.Id, vinculo.BarbeariaId);
    }

    [TestMethod]
    public async Task Remover_ComProprietarioDeOutraBarbearia_NaoRemoveVinculo()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoB);
        var barbeariaB = await CriarBarbeariaAsync(donoB);
        var profissionalB = await CriarProfissionalVinculadoAsync(fabrica, donoB, barbeariaB.CodigoConvite);

        var resposta = await EnviarComAntiforgeryAsync(
            donoA, HttpMethod.Delete,
            $"/api/v1/barbearias/{barbeariaA.Id}/profissionais/{profissionalB}",
            new { });

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var vinculo = await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters().SingleAsync(item => item.ProfissionalId == profissionalB);
        Assert.AreEqual(StatusVinculo.Ativo, vinculo.Situacao);
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberProfissionais_{Guid.NewGuid():N}";
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

    private static async Task<Guid> RegistrarEEntrarAsync(HttpClient cliente)
    {
        var (usuarioId, _) = await RegistrarEEntrarComEmailAsync(cliente);
        return usuarioId;
    }

    private static async Task<(Guid UsuarioId, string Email)> RegistrarEEntrarComEmailAsync(HttpClient cliente)
    {
        var email = $"prof-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        var cadastro = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/cadastro/profissional",
            new { nomeCompleto = "Profissional Teste", email, telefone = "11999999999", senha, confirmarSenha = senha });
        Assert.AreEqual(HttpStatusCode.Created, cadastro.StatusCode);

        var login = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha });
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);

        var eu = await cliente.GetAsync("/api/v1/auth/me");
        Assert.AreEqual(HttpStatusCode.OK, eu.StatusCode);
        var usuarioId = Guid.Parse((await LerCorpoAsync(eu)).GetProperty("id").GetString()!);
        return (usuarioId, email);
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

    private static async Task<Guid> CriarProfissionalVinculadoAsync(
        WebApplicationFactory<Program> fabrica,
        HttpClient proprietario,
        string codigoConvite)
    {
        var (id, _) = await CriarProfissionalVinculadoComEmailAsync(fabrica, proprietario, codigoConvite);
        return id;
    }

    private static async Task<(Guid Id, string Email)> CriarProfissionalVinculadoComEmailAsync(
        WebApplicationFactory<Program> fabrica,
        HttpClient proprietario,
        string codigoConvite)
    {
        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        var (usuarioId, email) = await RegistrarEEntrarComEmailAsync(barbeiro);
        var entrada = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar", new { codigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, entrada.StatusCode);

        var perfilId = await ObterPerfilIdAsync(fabrica, usuarioId);
        return (perfilId, email);
    }

    private static async Task<Guid> ObterPerfilIdAsync(
        WebApplicationFactory<Program> fabrica,
        Guid usuarioId)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var perfil = await contexto.Set<PerfilProfissional>()
            .SingleAsync(item => item.UsuarioId == usuarioId.ToString());
        return perfil.Id;
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
