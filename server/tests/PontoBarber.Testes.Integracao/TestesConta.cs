using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Tasks 3.2 e 3.3: cadastro distinto e login compartilhado com bloqueio.
/// Os cenários que persistem dados usam SQL Server via Testcontainers.
/// </summary>
[TestClass]
public sealed class TestesConta
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task CadastroCliente_ComConfirmacaoDivergente_Retorna400ComErroPorCampo()
    {
        using var fabrica = new WebApplicationFactory<Program>();
        using var cliente = fabrica.CreateClient(_opcoesCliente);

        var resposta = await PostAsync(cliente, "/api/v1/auth/register/customer", new
        {
            nomeCompleto = "Cliente Teste",
            email = "cliente-divergente@exemplo.com",
            telefone = "11999999999",
            senha = "Senha123",
            confirmarSenha = "Senha456"
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou, corpo.GetProperty("code").GetString());
        Assert.IsTrue(corpo.GetProperty("errors").TryGetProperty("confirmarSenha", out _));
    }

    [TestMethod]
    public async Task ServicoConta_DefineTipoNoServidorENaoConfianteNoRequest()
    {
        var identidade = new IdentidadeMemoria();
        var servico = new ServicoConta(identidade);
        var request = new RegistroRequest(
            "Nome", "cliente@exemplo.com", "11999999999", "Senha123", "Senha123");

        await servico.CadastrarClienteAsync(request, CancellationToken.None);
        await servico.CadastrarBarbeiroAsync(request with { Email = "barbeiro@exemplo.com" }, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { ServicoConta.TipoCliente, ServicoConta.TipoBarbeiro },
            identidade.Tipos);

        Assert.AreEqual(
            ResultadoAutenticacao.CredenciaisInvalidas,
            await servico.AutenticarAsync(new LoginRequest("ninguem@exemplo.com", "Senha123"), CancellationToken.None));
    }

    [TestMethod]
    public async Task CadastroCliente_ComCamposVazios_Retorna400()
    {
        using var fabrica = new WebApplicationFactory<Program>();
        using var cliente = fabrica.CreateClient(_opcoesCliente);

        var resposta = await PostAsync(cliente, "/api/v1/auth/register/customer", new
        {
            nomeCompleto = "",
            email = "",
            telefone = "",
            senha = "",
            confirmarSenha = ""
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.IsTrue(corpo.GetProperty("errors").TryGetProperty("email", out _));
    }

    [TestMethod]
    public async Task CadastroCliente_Valido_CriaUsuarioDoTipoCliente()
    {
        using var fabrica = CriarFabricaComBanco();
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = "cliente-" + Guid.NewGuid().ToString("N")[..8] + "@exemplo.com";

        var resposta = await PostAsync(cliente, "/api/v1/auth/register/customer", Registro(email));

        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        var usuario = await ObterUsuarioAsync(fabrica, email);
        Assert.AreEqual(ServicoConta.TipoCliente, usuario.UserType);
    }

    [TestMethod]
    public async Task CadastroBarbeiro_Valido_CriaUsuarioDoTipoProfissional()
    {
        using var fabrica = CriarFabricaComBanco();
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = "barbeiro-" + Guid.NewGuid().ToString("N")[..8] + "@exemplo.com";

        var resposta = await PostAsync(cliente, "/api/v1/auth/register/professional", Registro(email));

        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        var usuario = await ObterUsuarioAsync(fabrica, email);
        Assert.AreEqual(ServicoConta.TipoBarbeiro, usuario.UserType);
    }

    [TestMethod]
    public async Task CadastroCliente_ComEmailDuplicado_Retorna409ENaoCriaSegundoUsuario()
    {
        using var fabrica = CriarFabricaComBanco();
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = "duplicado-" + Guid.NewGuid().ToString("N")[..8] + "@exemplo.com";

        var primeiro = await PostAsync(cliente, "/api/v1/auth/register/customer", Registro(email));
        var segundo = await PostAsync(cliente, "/api/v1/auth/register/customer", Registro(email));

        Assert.AreEqual(HttpStatusCode.Created, primeiro.StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, segundo.StatusCode);
        var corpo = await LerCorpoAsync(segundo);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou, corpo.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Login_ComCredenciaisInvalidas_Retorna401Generico()
    {
        using var fabrica = CriarFabricaComBanco();
        using var cliente = fabrica.CreateClient(_opcoesCliente);

        var resposta = await PostAsync(cliente, "/api/v1/auth/login", new
        {
            email = "inexistente@exemplo.com",
            senha = "Senha123"
        });

        Assert.AreEqual(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(CodigosErro.AutenticacaoCredenciaisInvalidas, corpo.GetProperty("code").GetString());
        Assert.AreEqual("E-mail ou senha incorretos.", corpo.GetProperty("detail").GetString());
    }

    [TestMethod]
    public async Task Login_ComCincoFalhas_Retorna423EContaFicaBloqueada()
    {
        using var fabrica = CriarFabricaComBanco();
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = "bloqueio-" + Guid.NewGuid().ToString("N")[..8] + "@exemplo.com";

        Assert.AreEqual(
            HttpStatusCode.Created,
            (await PostAsync(cliente, "/api/v1/auth/register/customer", Registro(email))).StatusCode);

        for (var tentativa = 1; tentativa <= 5; tentativa++)
        {
            var resposta = await PostAsync(cliente, "/api/v1/auth/login", new { email, senha = "SenhaErrada1" });
            var esperado = tentativa == 5 ? HttpStatusCode.Locked : HttpStatusCode.Unauthorized;
            Assert.AreEqual(esperado, resposta.StatusCode, $"tentativa {tentativa}");
        }

        var corpo = await LerCorpoAsync(
            await PostAsync(cliente, "/api/v1/auth/login", new { email, senha = "Senha123" }));
        Assert.AreEqual(CodigosErro.AutenticacaoBloqueada, corpo.GetProperty("code").GetString());
    }

    private sealed class IdentidadeMemoria : IIdentidade
    {
        public List<string> Tipos { get; } = [];

        public Task<string> CriarUsuarioAsync(
            DadosRegistro dados,
            string tipoUsuario,
            CancellationToken cancellationToken)
        {
            Tipos.Add(tipoUsuario);
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        public Task<ResultadoAutenticacao> AutenticarAsync(
            string email,
            string senha,
            CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoAutenticacao.CredenciaisInvalidas);

        public Task<DadosConta?> ObterContaAsync(string usuarioId, CancellationToken cancellationToken) =>
            Task.FromResult<DadosConta?>(null);

        public Task EncerrarSessaoAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<ResultadoAtualizacaoPerfil> AtualizarPerfilAsync(
            string usuarioId, string nomeCompleto, string email, string? telefone,
            CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoAtualizacaoPerfil.Sucesso);
        public Task<ResultadoAlteracaoSenha> AlterarSenhaAsync(
            string usuarioId,
            string senhaAtual,
            string novaSenha,
            CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoAlteracaoSenha.Sucesso);
    }

    private static object Registro(string email) => new
    {
        nomeCompleto = "Nome Teste",
        email,
        telefone = "11999999999",
        senha = "Senha123",
        confirmarSenha = "Senha123"
    };

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        // Banco exclusivo deste teste: evita colisões entre classes e impede
        // que o schema da aplicação seja criado no banco master do container.
        var database = $"PontoBarberConta_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = conexao
                })));

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        contexto.Database.EnsureCreated();

        return fabrica;
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient cliente, string rota, object corpo)
    {
        var respostaGet = await cliente.GetAsync("/health/live");
        var token = ObterTokenAntiforgery(respostaGet);

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, rota)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpo, _json), Encoding.UTF8, "application/json")
        };
        requisicao.Headers.Add("X-CSRF-TOKEN", token);

        return await cliente.SendAsync(requisicao);
    }

    private static string ObterTokenAntiforgery(HttpResponseMessage resposta)
    {
        foreach (var cookie in resposta.Headers.GetValues("Set-Cookie"))
        {
            if (!cookie.Contains(ConfiguracaoAuth.CookieRequestToken, StringComparison.Ordinal))
                continue;

            var trecho = cookie.Split(';')[0];
            return Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);
        }

        throw new InvalidOperationException("Cookie de request token antiforgery não emitido.");
    }

    private static async Task<JsonElement> LerCorpoAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private static async Task<Usuario> ObterUsuarioAsync(WebApplicationFactory<Program> fabrica, string email)
    {
        using var escopo = fabrica.Services.CreateScope();
        var usuarios = escopo.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Usuario>>();
        return await usuarios.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"Usuário {email} não encontrado.");
    }
}
