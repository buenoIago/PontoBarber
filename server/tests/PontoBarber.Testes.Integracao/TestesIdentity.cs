using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// ADR-005 / TDD §11.1: Identity com política de senha, cookie protegido e
/// duração de sessão por perfil. Não cobre cadastro, login, logout nem
/// autorização (Tasks 3.2 a 3.5).
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
        // Reserva para tickets sem ExpiresUtc; o prazo real é definido por perfil no OnSigningIn.
        Assert.AreEqual(TimeSpan.FromDays(30), opcoes.ExpireTimeSpan);
        Assert.IsTrue(opcoes.SlidingExpiration);
        Assert.IsNotNull(opcoes.TimeProvider);
        Assert.IsNotNull(opcoes.Events?.OnSigningIn);
    }

    [TestMethod]
    [DataRow(ServicoConta.TipoCliente, 60)]
    [DataRow(ServicoConta.TipoBarbeiro, 30)]
    public async Task EventoSigningIn_DeveDefinirPrazoPorPerfil(string tipo, int dias)
    {
        var relogio = new RelogioFalso(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        using var fabrica = CriarFabricaComRelogio(relogio);
        var opcoes = ObterOpcoesCookie(fabrica);

        var contexto = CriarContexto(opcoes, new Claim("UserType", tipo));
        await opcoes.Events!.OnSigningIn!(contexto);

        Assert.AreEqual(relogio.Agora, contexto.Properties.IssuedUtc);
        Assert.AreEqual(relogio.Agora.AddDays(dias), contexto.Properties.ExpiresUtc);
        Assert.IsTrue(contexto.Properties.IsPersistent);
        Assert.IsTrue(contexto.Properties.AllowRefresh);
    }

    [TestMethod]
    public async Task EventoSigningIn_Proprietario_CompartilhaPrazoDeProfissional()
    {
        var relogio = new RelogioFalso(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        using var fabrica = CriarFabricaComRelogio(relogio);
        var opcoes = ObterOpcoesCookie(fabrica);

        var contexto = CriarContexto(opcoes,
            new Claim("UserType", ServicoConta.TipoBarbeiro),
            new Claim("TenantOwner", "true"));
        await opcoes.Events!.OnSigningIn!(contexto);

        Assert.AreEqual(relogio.Agora.AddDays(30), contexto.Properties.ExpiresUtc);
    }

    [TestMethod]
    public async Task EventoSigningIn_SemPerfilConhecido_UsaPrazoMenor()
    {
        var relogio = new RelogioFalso(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        using var fabrica = CriarFabricaComRelogio(relogio);
        var opcoes = ObterOpcoesCookie(fabrica);

        var contexto = CriarContexto(opcoes);
        await opcoes.Events!.OnSigningIn!(contexto);

        Assert.AreEqual(relogio.Agora.AddDays(30), contexto.Properties.ExpiresUtc);
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
        // Banco exclusivo deste teste: o schema do Identity é criado apenas
        // neste catálogo isolado, nunca no master do container.
        var database = $"PontoBarberIdentity_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        using var fabrica = _fabrica.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = conexao
                })));

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        contexto.Database.EnsureCreated();

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

    private static WebApplicationFactory<Program> CriarFabricaComRelogio(RelogioFalso relogio) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(servicos => servicos.AddSingleton<TimeProvider>(relogio)));

    private static CookieAuthenticationOptions ObterOpcoesCookie(WebApplicationFactory<Program> fabrica) =>
        fabrica.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

    private static CookieSigningInContext CriarContexto(
        CookieAuthenticationOptions opcoes,
        params Claim[] claims)
    {
        var esquema = new AuthenticationScheme(
            IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Teste"));
        return new CookieSigningInContext(
            new DefaultHttpContext(), esquema, opcoes, principal, new AuthenticationProperties(), new CookieOptions());
    }

    private sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
    {
        public DateTimeOffset Agora { get; private set; } = agora;

        public override DateTimeOffset GetUtcNow() => Agora;

        public void Avancar(TimeSpan intervalo) => Agora = Agora.Add(intervalo);
    }
}
