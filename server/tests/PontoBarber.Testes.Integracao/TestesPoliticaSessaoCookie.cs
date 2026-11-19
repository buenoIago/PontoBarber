using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Valida a política de duração da sessão diretamente no handler do cookie,
/// sem banco: emissão persistente, renovação deslizante, expiração por
/// inatividade e logout imediato (SPEC §1 / TDD §11.1, Task 3.4).
/// </summary>
[TestClass]
public sealed class TestesPoliticaSessaoCookie
{
    [TestMethod]
    public async Task Cliente_CookiePersistenteExpiraEmSessentaDias()
    {
        var (host, relogio) = await CriarServidorAsync();
        using var servidor = host;
        using var cliente = host.GetTestClient();
        cliente.BaseAddress = new Uri("https://localhost"); // cookie SecurePolicy.Always

        var resposta = await cliente.GetAsync("/signin/Customer");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, resposta.StatusCode);
        var cookie = ObterCookie(resposta);
        Assert.IsTrue(cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase), "Cookie não persistente.");
        AssertExpiracaoProxima(relogio.Agora.AddDays(60), ObterExpiracao(cookie));
    }

    [TestMethod]
    public async Task Profissional_CookieExpiraEmTrintaDias()
    {
        var (host, relogio) = await CriarServidorAsync();
        using var servidor = host;
        using var cliente = host.GetTestClient();
        cliente.BaseAddress = new Uri("https://localhost"); // cookie SecurePolicy.Always

        var resposta = await cliente.GetAsync("/signin/Professional");

        AssertExpiracaoProxima(relogio.Agora.AddDays(30), ObterExpiracao(ObterCookie(resposta)));
    }

    [TestMethod]
    public async Task AcessoValidoAposMetadeDoPrazo_RenovaPeloPerfilDoCliente()
    {
        var (host, relogio) = await CriarServidorAsync();
        using var servidor = host;
        using var cliente = host.GetTestClient();
        cliente.BaseAddress = new Uri("https://localhost"); // cookie SecurePolicy.Always
        cliente.DefaultRequestHeaders.Add("Cookie", ObterParCookie(
            ObterCookie(await cliente.GetAsync("/signin/Customer"))));

        // 31 dias > metade dos 60: o handler dispara a renovação deslizante.
        relogio.Avancar(TimeSpan.FromDays(31));
        var resposta = await cliente.GetAsync("/ping");

        Assert.AreEqual("Customer", await resposta.Content.ReadAsStringAsync());
        var cookie = ObterCookie(resposta);
        AssertExpiracaoProxima(relogio.Agora.AddDays(60), ObterExpiracao(cookie));
    }

    [TestMethod]
    public async Task InatividadeAlemDoPrazo_ExigeNovaAutenticacao()
    {
        var (host, relogio) = await CriarServidorAsync();
        using var servidor = host;
        using var cliente = host.GetTestClient();
        cliente.BaseAddress = new Uri("https://localhost"); // cookie SecurePolicy.Always
        cliente.DefaultRequestHeaders.Add("Cookie", ObterParCookie(
            ObterCookie(await cliente.GetAsync("/signin/Professional"))));

        relogio.Avancar(TimeSpan.FromDays(31));
        var resposta = await cliente.GetAsync("/ping");

        Assert.AreEqual("anon", await resposta.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Logout_EncerraSessaoImediatamente()
    {
        var (host, _) = await CriarServidorAsync();
        using var servidor = host;
        using var cliente = host.GetTestClient();
        cliente.BaseAddress = new Uri("https://localhost"); // cookie SecurePolicy.Always
        cliente.DefaultRequestHeaders.Add("Cookie", ObterParCookie(
            ObterCookie(await cliente.GetAsync("/signin/Professional"))));
        Assert.AreEqual("Professional", await (await cliente.GetAsync("/ping")).Content.ReadAsStringAsync());

        await cliente.GetAsync("/signout");
        cliente.DefaultRequestHeaders.Remove("Cookie");

        Assert.AreEqual("anon", await (await cliente.GetAsync("/ping")).Content.ReadAsStringAsync());
    }

    private static async Task<(IHost Host, RelogioFalso Relogio)> CriarServidorAsync()
    {
        var relogio = new RelogioFalso(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        var relogioLocal = relogio;

        var host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.ConfigureAppConfiguration((_, configuracao) => configuracao.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            "Server=localhost;Database=PontoBarberSessao;Trusted_Connection=True;TrustServerCertificate=True"
                    }));
                web.ConfigureServices(servicos =>
                {
                    servicos.AddSingleton<TimeProvider>(relogioLocal);
                    servicos.AddSingleton<IContextoTenant, ContextoTenantNulo>();
                    servicos.AddRouting();
                    servicos.AddDbContext<ContextoPontoBarber>(opcoes =>
                        opcoes.UseSqlServer("Server=localhost;Database=PontoBarberSessao;Trusted_Connection=True;TrustServerCertificate=True"));
                    servicos.AddIdentityPontoBarber();
                    // Sem banco neste teste: a validação de SecurityStamp do Identity
                    // rejeitaria o principal ao ultrapassar o intervalo de 30 minutos.
                    servicos.Configure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
                        IdentityConstants.ApplicationScheme,
                        opcoes => opcoes.Events.OnValidatePrincipal = _ => Task.CompletedTask);
                    servicos.AddAuthorization();
                    servicos.Configure<AuthenticationOptions>(opcoes =>
                    {
                        opcoes.DefaultScheme = IdentityConstants.ApplicationScheme;
                        opcoes.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                        opcoes.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
                    });
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/signin/{tipo}", async (HttpContext contexto, string tipo) =>
                        {
                            var principal = new ClaimsPrincipal(
                                new ClaimsIdentity([new Claim("UserType", tipo)], "Teste"));
                            await contexto.SignInAsync(
                                IdentityConstants.ApplicationScheme,
                                principal,
                                new AuthenticationProperties { IsPersistent = true });
                            return Results.Ok();
                        });
                        endpoints.MapGet("/signout", async (HttpContext contexto) =>
                        {
                            await contexto.SignOutAsync(IdentityConstants.ApplicationScheme);
                            return Results.Ok();
                        });
                        endpoints.MapGet("/ping", (HttpContext contexto) =>
                            Results.Text(contexto.User.FindFirstValue("UserType") ?? "anon"));
                    });
                });
                web.UseTestServer();
            })
            .StartAsync();

        return (host, relogio);
    }

    private static string ObterCookie(HttpResponseMessage resposta) =>
        resposta.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(valor => valor.Contains("__Host-pb.auth", StringComparison.Ordinal))
        ?? throw new InvalidOperationException("Cookie __Host-pb.auth não emitido.");

    private static void AssertExpiracaoProxima(DateTimeOffset esperado, DateTimeOffset atual) =>
        Assert.IsTrue(
            Math.Abs((atual - esperado).TotalMinutes) <= 2,
            $"Expiração {atual}, esperado próximo de {esperado}.");

    private static string ObterParCookie(string cookie) => cookie.Split(';')[0].Trim();

    private static DateTimeOffset ObterExpiracao(string cookie)
    {
        var atributo = cookie.Split(';')
            .Select(parte => parte.Trim())
            .FirstOrDefault(parte => parte.StartsWith("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(atributo, $"Cookie sem expires: {cookie}");
        return DateTimeOffset.Parse(atributo!["expires=".Length..], CultureInfo.InvariantCulture);
    }

    private sealed class ContextoTenantNulo : IContextoTenant
    {
        public Guid? TenantId => null;
    }

    private sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
    {
        public DateTimeOffset Agora { get; private set; } = agora;

        public override DateTimeOffset GetUtcNow() => Agora;

        public void Avancar(TimeSpan intervalo) => Agora = Agora.Add(intervalo);
    }
}
