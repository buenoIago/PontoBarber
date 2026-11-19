using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Tasks 3.4 e 3.5: sessão, conta, antiforgery e autorização.
/// </summary>
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

        var resposta = await cliente.GetAsync("/api/v1/auth/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [TestMethod]
    public async Task PostSemAntiforgery_Retorna400()
    {
        using var fabrica = new WebApplicationFactory<Program>();
        using var cliente = fabrica.CreateClient(_opcoesCliente);

        var resposta = await cliente.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode, "STATUS=" + resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou, corpo.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Antiforgery_EmiteCookieComNomeEsperado()
    {
        using var fabrica = new WebApplicationFactory<Program>();
        using var cliente = fabrica.CreateClient(_opcoesCliente);

        var resposta = await cliente.GetAsync("/health/live");

        var temCookie = resposta.Headers.TryGetValues("Set-Cookie", out var cookies) &&
            cookies.Any(c => c.Contains(ConfiguracaoAuth.CookieCsrf, StringComparison.Ordinal));
        Assert.IsTrue(temCookie, "COOKIES=" + string.Join(" | ", cookies ?? []));
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
        var (email, senha) = await CriarUsuarioAsync(cliente);

        var clienteAutenticado = await AutenticarAsync(fabrica, email, senha);
        var resposta = await clienteAutenticado.GetAsync("/api/v1/auth/me");

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
        var (email, senha) = await CriarUsuarioAsync(cliente);
        var autenticado = await AutenticarAsync(fabrica, email, senha);

        var resposta = await PostComAntiforgeryAsync(autenticado, "/api/v1/auth/logout", new { });

        Assert.AreEqual(HttpStatusCode.NoContent, resposta.StatusCode);
        var aposLogout = await autenticado.GetAsync("/api/v1/auth/me");
        Assert.AreEqual(HttpStatusCode.Unauthorized, aposLogout.StatusCode);
    }

    [TestMethod]
    public async Task AlterarSenha_ComSenhaAtualIncorreta_Retorna400()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var (email, senha) = await CriarUsuarioAsync(cliente);
        var autenticado = await AutenticarAsync(fabrica, email, senha);

        var resposta = await PostComAntiforgeryAsync(autenticado, "/api/v1/auth/change-password", new
        {
            senhaAtual = "SenhaErrada1",
            novaSenha = "NovaSenha123",
            confirmarNovaSenha = "NovaSenha123"
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.IsTrue(corpo.GetProperty("errors").TryGetProperty("senhaAtual", out _));
    }

    [TestMethod]
    public async Task AlterarSenha_ComDadosValidos_AtualizaSenha()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var (email, senha) = await CriarUsuarioAsync(cliente);
        var autenticado = await AutenticarAsync(fabrica, email, senha);

        var resposta = await PostComAntiforgeryAsync(autenticado, "/api/v1/auth/change-password", new
        {
            senhaAtual = senha,
            novaSenha = "NovaSenha123",
            confirmarNovaSenha = "NovaSenha123"
        });

        Assert.AreEqual(HttpStatusCode.NoContent, resposta.StatusCode);
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        ConfiguracaoTestesIntegracao.Container.ObterStringConexao("master")
                })));

    private static async Task<(string Email, string Senha)> CriarUsuarioAsync(HttpClient cliente)
    {
        var email = "sessao-" + Guid.NewGuid().ToString("N")[..8] + "@exemplo.com";
        var senha = "Senha123";

        var resposta = await PostComAntiforgeryAsync(cliente, "/api/v1/auth/register/customer", new
        {
            nomeCompleto = "Usuário Sessão",
            email,
            telefone = "11999999999",
            senha,
            confirmarSenha = senha
        });

        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        return (email, senha);
    }

    private static async Task<HttpClient> AutenticarAsync(
        WebApplicationFactory<Program> fabrica,
        string email,
        string senha)
    {
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var resposta = await PostComAntiforgeryAsync(cliente, "/api/v1/auth/login", new { email, senha });
        Assert.AreEqual(HttpStatusCode.NoContent, resposta.StatusCode);
        return cliente;
    }

    private static async Task<HttpResponseMessage> PostComAntiforgeryAsync(
        HttpClient cliente,
        string rota,
        object corpo)
    {
        var pagina = await cliente.GetAsync("/health/live");
        var token = ObterTokenAntiforgery(pagina);
        var cookieCsrf = ObterCookie(pagina, ConfiguracaoAuth.CookieCsrf);

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, rota)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpo, _json), Encoding.UTF8, "application/json")
        };
        requisicao.Headers.Add(ConfiguracaoAuth.HeaderCsrf, token);
        requisicao.Headers.Add("Cookie", cookieCsrf);

        return await cliente.SendAsync(requisicao);
    }

    private static string ObterTokenAntiforgery(HttpResponseMessage resposta)
    {
        foreach (var cookie in resposta.Headers.GetValues("Set-Cookie"))
        {
            if (!cookie.Contains(ConfiguracaoAuth.CookieCsrf, StringComparison.Ordinal))
                continue;

            var trecho = cookie.Split(';')[0];
            var valor = Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);
            var inicio = valor.IndexOf(':');
            return inicio >= 0 ? valor[..inicio] : valor;
        }

        throw new InvalidOperationException("Cookie antiforgery não emitido.");
    }

    private static string ObterCookie(HttpResponseMessage resposta, string nome)
    {
        foreach (var cookie in resposta.Headers.GetValues("Set-Cookie"))
        {
            if (cookie.Contains(nome, StringComparison.Ordinal))
                return cookie.Split(';')[0];
        }

        throw new InvalidOperationException($"Cookie {nome} não emitido.");
    }

    private static async Task<JsonElement> LerCorpoAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }
}
