using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Modulos.Auth;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Modulos.Auth;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Task 3.3: geração, expiração, uso único, limites e consumo concorrente do
/// código OTP. Nenhum teste lê ou envia e-mail fora do enviador em memória.
/// </summary>
[TestClass]
public sealed class TestesOtp
{
    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task SolicitarOtp_CodigoGerado_TemSeisDigitos()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);

        var codigo = await AuthTestHelper.SolicitarCodigoAsync(cliente, email);

        StringAssert.Matches(codigo, new System.Text.RegularExpressions.Regex(@"^\d{6}$"));
    }

    [TestMethod]
    public async Task SolicitarOtp_EmailInexistente_Retorna202GenericoSemEnvio()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = $"inexistente-{Guid.NewGuid():N}@exemplo.com";

        EnviadorEmailMemoria.Limpar();
        var resposta = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/solicitar", new { email });

        Assert.AreEqual(HttpStatusCode.Accepted, resposta.StatusCode);
        Assert.IsNull(EnviadorEmailMemoria.UltimoCorpo(email));
    }

    [TestMethod]
    public async Task SolicitarOtp_Profissional_NaoEnviaCodigo()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = $"prof-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        Assert.AreEqual(HttpStatusCode.Created, (await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/cadastro/profissional", new
            {
                nomeCompleto = "Profissional",
                email,
                telefone = "11999999999",
                senha,
                confirmarSenha = senha
            })).StatusCode);

        EnviadorEmailMemoria.Limpar();
        var resposta = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/solicitar", new { email });

        Assert.AreEqual(HttpStatusCode.Accepted, resposta.StatusCode);
        Assert.IsNull(EnviadorEmailMemoria.UltimoCorpo(email));
    }

    [TestMethod]
    public async Task VerificarOtp_CodigoCorreto_AutenticaComCookie()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        var codigo = await AuthTestHelper.SolicitarCodigoAsync(cliente, email);

        var resposta = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email, codigo });

        Assert.AreEqual(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.IsTrue(resposta.Headers.GetValues("Set-Cookie")
            .Any(valor => valor.Contains("__Host-pb.auth", StringComparison.Ordinal)));
        Assert.AreEqual(HttpStatusCode.OK, (await cliente.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [TestMethod]
    public async Task VerificarOtp_CodigoInvalido_Retorna401Generico()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        await AuthTestHelper.SolicitarCodigoAsync(cliente, email);

        var resposta = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email, codigo = "000000" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(CodigosErro.AutenticacaoCredenciaisInvalidas, corpo.GetProperty("code").GetString());
        Assert.AreEqual("E-mail ou código inválidos.", corpo.GetProperty("detail").GetString());
    }

    [TestMethod]
    public async Task VerificarOtp_CodigoExpirado_Retorna401Generico()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        var codigo = await AuthTestHelper.SolicitarCodigoAsync(cliente, email);
        await AlterarOtpAsync(fabrica, email, conjunto => conjunto
            .SetProperty(item => item.ExpiraEmUtc, DateTime.UtcNow.AddMinutes(-1)));

        var resposta = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email, codigo });

        Assert.AreEqual(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.AreEqual(CodigosErro.AutenticacaoCredenciaisInvalidas,
            (await LerCorpoAsync(resposta)).GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task VerificarOtp_CodigoJaUtilizado_Retorna401Generico()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        var codigo = await AuthTestHelper.SolicitarCodigoAsync(cliente, email);

        Assert.AreEqual(HttpStatusCode.NoContent, (await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email, codigo })).StatusCode);
        var resposta = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email, codigo });

        Assert.AreEqual(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Reenvio_InvalidaCodigoAnterior()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        var primeiro = await AuthTestHelper.SolicitarCodigoAsync(cliente, email);

        // O intervalo mínimo entre reenvios é de 60 segundos.
        await AlterarOtpAsync(fabrica, email, conjunto => conjunto
            .SetProperty(item => item.CriadoEmUtc, DateTime.UtcNow.AddSeconds(-61)));
        EnviadorEmailMemoria.Limpar();
        Assert.AreEqual(HttpStatusCode.Accepted, (await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/solicitar", new { email })).StatusCode);
        var segundo = AuthTestHelper.ObterCodigo(email);

        var antigo = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email, codigo = primeiro });
        Assert.AreEqual(HttpStatusCode.Unauthorized, antigo.StatusCode);

        Assert.AreEqual(HttpStatusCode.NoContent, (await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email, codigo = segundo })).StatusCode);
    }

    [TestMethod]
    public async Task SolicitarOtp_AposTresReenvios_NaoEnviaNovoCodigo()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        await AuthTestHelper.SolicitarCodigoAsync(cliente, email);

        await AlterarOtpAsync(fabrica, email, conjunto => conjunto
            .SetProperty(item => item.Reenvios, CodigoOtp.MaximoReenvios)
            .SetProperty(item => item.CriadoEmUtc, DateTime.UtcNow.AddSeconds(-61)));
        EnviadorEmailMemoria.Limpar();

        var resposta = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/solicitar", new { email });

        Assert.AreEqual(HttpStatusCode.Accepted, resposta.StatusCode);
        Assert.IsNull(EnviadorEmailMemoria.UltimoCorpo(email));
    }

    [TestMethod]
    public async Task SolicitarOtp_AposExpiracao_RenovaContadorDeReenvios()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        await AuthTestHelper.SolicitarCodigoAsync(cliente, email);

        await AlterarOtpAsync(fabrica, email, conjunto => conjunto
            .SetProperty(item => item.Reenvios, CodigoOtp.MaximoReenvios)
            .SetProperty(item => item.ExpiraEmUtc, DateTime.UtcNow.AddSeconds(-1)));
        EnviadorEmailMemoria.Limpar();

        Assert.AreEqual(HttpStatusCode.Accepted, (await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/solicitar", new { email })).StatusCode);
        var codigo = AuthTestHelper.ObterCodigo(email);

        Assert.AreEqual(HttpStatusCode.NoContent, (await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email, codigo })).StatusCode);
    }

    [TestMethod]
    public async Task VerificarOtp_ComCincoTentativasInvalidas_BloqueiaCodigo()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        var codigo = await AuthTestHelper.SolicitarCodigoAsync(cliente, email);
        var invalido = codigo == "000000" ? "111111" : "000000";

        for (var tentativa = 0; tentativa < CodigoOtp.MaximoTentativas; tentativa++)
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await AuthTestHelper.PostAsync(
                cliente, "/api/v1/auth/otp/verificar", new { email, codigo = invalido })).StatusCode);
        }

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email, codigo })).StatusCode);
    }

    [TestMethod]
    public async Task VerificarOtp_RespostasGenericasNaoRevelamExistenciaDaConta()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var emailInexistente = $"inexistente-{Guid.NewGuid():N}@exemplo.com";
        var emailExistente = await AuthTestHelper.RegistrarClienteAsync(cliente);

        var respostaInexistente = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email = emailInexistente, codigo = "123456" });
        var respostaExistente = await AuthTestHelper.PostAsync(
            cliente, "/api/v1/auth/otp/verificar", new { email = emailExistente, codigo = "123456" });

        Assert.AreEqual(respostaInexistente.StatusCode, respostaExistente.StatusCode);
        Assert.AreEqual(
            (await LerCorpoAsync(respostaInexistente)).GetProperty("detail").GetString(),
            (await LerCorpoAsync(respostaExistente)).GetProperty("detail").GetString());
    }

    [TestMethod]
    public async Task VerificarOtp_UsoConcorrente_SomenteUmSucesso()
    {
        using var fabrica = CriarFabricaComBanco();
        var cliente = fabrica.CreateClient(_opcoesCliente);
        var email = await AuthTestHelper.RegistrarClienteAsync(cliente);
        var codigo = await AuthTestHelper.SolicitarCodigoAsync(cliente, email);

        var respostas = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            AuthTestHelper.PostAsync(cliente, "/api/v1/auth/otp/verificar", new { email, codigo })));

        Assert.AreEqual(1, respostas.Count(resposta => resposta.StatusCode == HttpStatusCode.NoContent));
        Assert.AreEqual(3, respostas.Count(resposta => resposta.StatusCode == HttpStatusCode.Unauthorized));
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberOtp_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Modo"] = "Memoria",
                    ["ConnectionStrings:DefaultConnection"] = conexao
                })));

        using var escopo = fabrica.Services.CreateScope();
        escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>().Database.EnsureCreated();
        return fabrica;
    }

    private static async Task AlterarOtpAsync(
        WebApplicationFactory<Program> fabrica,
        string email,
        Action<UpdateSettersBuilder<CodigoOtp>> alterar)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var linhas = await contexto.Set<CodigoOtp>()
            .Where(item => item.Email == CodigoOtp.Normalizar(email))
            .ExecuteUpdateAsync(alterar);
        Assert.AreEqual(1, linhas, "Registro OTP não encontrado.");
    }

    private static async Task<JsonElement> LerCorpoAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }
}
