using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// ADR-005 / TDD §11.1: Identity com política de senha e cookie protegido.
/// Não cobre cadastro, login, logout nem autorização (Tasks 3.2 a 3.5).
/// </summary>
[TestClass]
public sealed class TestesIdentity : IDisposable
{
    private readonly WebApplicationFactory<Program> _fabrica = new();

    public void Dispose()
    {
        _fabrica.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public void CookieAutenticacao_DeveTerAtributosDeSeguranca()
    {
        var opcoes = _fabrica.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.AreEqual(DependencyInjection.NomeCookie, opcoes.Cookie.Name);
        Assert.AreEqual("__Host-pb.auth", opcoes.Cookie.Name);
        Assert.IsTrue(opcoes.Cookie.HttpOnly);
        Assert.AreEqual(CookieSecurePolicy.Always, opcoes.Cookie.SecurePolicy);
        Assert.AreEqual(SameSiteMode.Lax, opcoes.Cookie.SameSite);
        Assert.AreEqual("/", opcoes.Cookie.Path);
        Assert.AreEqual(TimeSpan.FromHours(8), opcoes.ExpireTimeSpan);
        Assert.IsTrue(opcoes.SlidingExpiration);
    }

    [TestMethod]
    public void HashSenha_DeveUsarPbkdf2Com210MilIteracoes()
    {
        using var escopo = _fabrica.Services.CreateScope();
        var hasher = escopo.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

        var hash = hasher.HashPassword(new Usuario(), "Senha123");

        // Formato binário do PasswordHasher v3: versão(1), PRF(1), iterações(4, big-endian).
        var bytes = Convert.FromBase64String(hash);
        Assert.AreEqual((byte)1, bytes[0]);
        Assert.AreEqual((byte)2, bytes[4]); // 2 = HMAC-SHA512
        Assert.AreEqual(210_000u, System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5)));
    }

    [TestMethod]
    public async Task UserManager_ComSenhaForaDaPolitica_RejeitaSemPersistir()
    {
        using var escopo = _fabrica.Services.CreateScope();
        var gerenciador = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var resultado = await gerenciador.CreateAsync(
            new Usuario { UserName = "fraco", Email = "fraco@exemplo.com" },
            "abc");

        Assert.IsFalse(resultado.Succeeded);
        Assert.IsTrue(resultado.Errors.Any(erro => erro.Code == "PasswordTooShort"));
    }

    [TestMethod]
    public async Task UserManager_ComSenhaValida_CriaUsuario()
    {
        using var fabrica = _fabrica.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        ConfiguracaoTestesIntegracao.Container.ObterStringConexao("master")
                })));

        using var escopo = fabrica.Services.CreateScope();
        var gerenciador = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var resultado = await gerenciador.CreateAsync(
            new Usuario
            {
                UserName = "usuario-valido-" + Guid.NewGuid().ToString("N")[..8],
                Email = "usuario-valido@exemplo.com",
                FullName = "Usuário Válido"
            },
            "Senha123");

        Assert.IsTrue(resultado.Succeeded, string.Join("; ", resultado.Errors.Select(erro => erro.Code)));
    }
}
