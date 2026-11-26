using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Tasks 3.4 e 3.5: sessão por perfil, OTP, antiforgery e autorização.</summary>
[TestClass]
public sealed class TestesSessaoConta
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Me_SemAutenticacao_Retorna401()
    {
        using var fabrica = new WebApplicationFactory<Program>();
        using var cliente = fabrica.CreateClient(_opcoesCliente);

        Assert.AreEqual(HttpStatusCode.Unauthorized,
            (await cliente.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [TestMethod]
    public async Task PostSemAntiforgery_Retorna400()
    {
        using var fabrica = new WebApplicationFactory<Program>();
        using var cliente = fabrica.CreateClient(_opcoesCliente);

        var resposta = await cliente.PostAsync(
            "/api/v1/auth/login", new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou,
            (await LerCorpoAsync(resposta)).GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Antiforgery_EmiteCookieComNomeEsperado()
    {
        using var fabrica = new WebApplicationFactory<Program>();
        using var cliente = fabrica.CreateClient(_opcoesCliente);

        var resposta = await cliente.GetAsync("/health/live");
        var cookies = resposta.Headers.GetValues("Set-Cookie");
        Assert.IsTrue(cookies.Any(c => c.Contains(ConfiguracaoAuth.CookieCsrf, StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Policies_DePerfil_SaoAplicadas()
    {
        using var fabrica = new WebApplicationFactory<Program>();
        using var escopo = fabrica.Services.CreateScope();
        var autorizacao = escopo.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var cliente = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("UserType", ServicoConta.TipoCliente)], "Teste"));
        var profissional = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("UserType", ServicoConta.TipoBarbeiro)], "Teste"));

        Assert.IsTrue((await autorizacao.AuthorizeAsync(cliente, ConfiguracaoAuth.PolíticaCliente)).Succeeded);
        Assert.IsFalse((await autorizacao.AuthorizeAsync(cliente, ConfiguracaoAuth.PolíticaProfissional)).Succeeded);
        Assert.IsTrue((await autorizacao.AuthorizeAsync(profissional, ConfiguracaoAuth.PolíticaProfissional)).Succeeded);
        Assert.IsFalse((await autorizacao.AuthorizeAsync(profissional, ConfiguracaoAuth.PolíticaCliente)).Succeeded);
    }

    [TestMethod]
    public async Task Me_ComAutenticacao_RetornaDadosDaConta()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        await AuthTestHelper.LoginClienteAsync(cliente, email);

        var resposta = await cliente.GetAsync("/api/v1/auth/me");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(email, corpo.GetProperty("email").GetString());
        Assert.AreEqual(ServicoConta.TipoCliente, corpo.GetProperty("tipo").GetString());
    }

    [TestMethod]
    public async Task Logout_EncerraSessao()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        await AuthTestHelper.LoginClienteAsync(cliente, email);

        Assert.AreEqual(HttpStatusCode.NoContent,
            (await PostComAntiforgeryAsync(cliente, "/api/v1/auth/logout", new { })).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized,
            (await cliente.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [TestMethod]
    public async Task LoginCliente_CookiePersistenteComSessentaDias()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);

        var codigo = await AuthTestHelper.SolicitarCodigoAsync(cliente, email);
        var resposta = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email, codigo });
        Assert.AreEqual(HttpStatusCode.NoContent, resposta.StatusCode);
        var cookie = resposta.Headers.GetValues("Set-Cookie")
            .First(valor => valor.Contains("__Host-pb.auth", StringComparison.Ordinal));
        Assert.IsTrue(cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        var esperado = DateTimeOffset.UtcNow.AddDays(60);
        Assert.IsTrue(Math.Abs((ObterExpiracaoCookie(cookie) - esperado).TotalMinutes) <= 2);
    }

    [TestMethod]
    public async Task LoginProfissional_CookiePersistenteComTrintaDias()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var (email, senha) = await CriarProfissionalAsync(cliente);

        var resposta = await PostComAntiforgeryAsync(cliente, "/api/v1/auth/login", new { email, senha });

        Assert.AreEqual(HttpStatusCode.NoContent, resposta.StatusCode);
        var cookie = resposta.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(valor => valor.Contains("__Host-pb.auth", StringComparison.Ordinal));
        Assert.IsNotNull(cookie);
        Assert.IsTrue(Math.Abs((ObterExpiracaoCookie(cookie) - DateTimeOffset.UtcNow.AddDays(30)).TotalMinutes) <= 2);
    }

    [TestMethod]
    public async Task Sessao_ComAcessoValidoAposMetadeDoPrazo_RenovaCookie()
    {
        var relogio = new RelogioFalso(DateTimeOffset.UtcNow);
        using var fabrica = CriarFabricaComBanco(relogio);
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var (email, senha) = await CriarProfissionalAsync(cliente);
        await PostComAntiforgeryAsync(cliente, "/api/v1/auth/login", new { email, senha });

        relogio.Avancar(TimeSpan.FromDays(16));
        var resposta = await cliente.GetAsync("/api/v1/auth/me");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var cookie = resposta.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(valor => valor.Contains("__Host-pb.auth", StringComparison.Ordinal));
        Assert.IsNotNull(cookie);
        Assert.IsTrue(Math.Abs((ObterExpiracaoCookie(cookie) - relogio.Agora.AddDays(30)).TotalMinutes) <= 2);
    }

    [TestMethod]
    public async Task Sessao_AposInatividadeDeTrintaDias_ExigeNovaAutenticacao()
    {
        var relogio = new RelogioFalso(DateTimeOffset.UtcNow);
        using var fabrica = CriarFabricaComBanco(relogio);
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var (email, senha) = await CriarProfissionalAsync(cliente);
        await PostComAntiforgeryAsync(cliente, "/api/v1/auth/login", new { email, senha });

        relogio.Avancar(TimeSpan.FromDays(31));

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [TestMethod]
    public async Task Cliente_NaoAlteraSenha_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        await AuthTestHelper.LoginClienteAsync(cliente, email);

        var resposta = await PostComAntiforgeryAsync(cliente, "/api/v1/auth/change-password", new
        {
            senhaAtual = "Senha123",
            novaSenha = "NovaSenha123",
            confirmarNovaSenha = "NovaSenha123"
        });

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [TestMethod]
    public async Task AlterarSenha_ComSenhaAtualIncorreta_Retorna400()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var (email, senha) = await CriarProfissionalAsync(cliente);
        await PostComAntiforgeryAsync(cliente, "/api/v1/auth/login", new { email, senha });

        var resposta = await PostComAntiforgeryAsync(cliente, "/api/v1/auth/change-password", new
        {
            senhaAtual = "SenhaErrada1",
            novaSenha = "NovaSenha123",
            confirmarNovaSenha = "NovaSenha123"
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.IsTrue((await LerCorpoAsync(resposta)).GetProperty("errors").TryGetProperty("senhaAtual", out _));
    }

    [TestMethod]
    public async Task AlterarSenha_ComDadosValidos_AtualizaSenha()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var (email, senha) = await CriarProfissionalAsync(cliente);
        await PostComAntiforgeryAsync(cliente, "/api/v1/auth/login", new { email, senha });

        var resposta = await PostComAntiforgeryAsync(cliente, "/api/v1/auth/change-password", new
        {
            senhaAtual = senha,
            novaSenha = "NovaSenha123",
            confirmarNovaSenha = "NovaSenha123"
        });

        Assert.AreEqual(HttpStatusCode.NoContent, resposta.StatusCode);
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco() =>
        CriarFabricaComBanco(TimeProvider.System);

    private static WebApplicationFactory<Program> CriarFabricaComBanco(TimeProvider relogio)
    {
        var database = $"PontoBarberSessao_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Modo"] = "Memoria",
                    ["ConnectionStrings:DefaultConnection"] = conexao
                }));
            builder.ConfigureServices(servicos => servicos.AddSingleton(relogio));
        });

        using var escopo = fabrica.Services.CreateScope();
        escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>().Database.EnsureCreated();
        return fabrica;
    }

    private static async Task<(string Email, string Senha)> CriarProfissionalAsync(HttpClient cliente)
    {
        var email = "sessao-" + Guid.NewGuid().ToString("N")[..8] + "@exemplo.com";
        const string senha = "Senha123";
        var resposta = await PostComAntiforgeryAsync(cliente, "/api/v1/auth/cadastro/profissional", new
        {
            nomeCompleto = "Profissional Sessão",
            email,
            telefone = "11999999999",
            senha,
            confirmarSenha = senha
        });
        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        return (email, senha);
    }

    private static async Task<HttpResponseMessage> PostComAntiforgeryAsync(
        HttpClient cliente, string rota, object corpo)
    {
        var pagina = await cliente.GetAsync("/health/live");
        var valor = pagina.Headers.GetValues("Set-Cookie")
            .First(item => item.Contains(ConfiguracaoAuth.CookieRequestToken, StringComparison.Ordinal));
        var trecho = valor.Split(';')[0];
        var token = Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, rota)
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

    private static DateTimeOffset ObterExpiracaoCookie(string cookie)
    {
        var atributo = cookie.Split(';')
            .Select(parte => parte.Trim())
            .FirstOrDefault(parte => parte.StartsWith("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(atributo);
        return DateTimeOffset.Parse(atributo!["expires=".Length..], CultureInfo.InvariantCulture);
    }

    private sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
    {
        public DateTimeOffset Agora { get; private set; } = agora;
        public override DateTimeOffset GetUtcNow() => Agora;
        public void Avancar(TimeSpan intervalo) => Agora = Agora.Add(intervalo);
    }
}
