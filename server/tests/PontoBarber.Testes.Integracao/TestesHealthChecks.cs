using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PontoBarber.Testes.Integracao;

[TestClass]
public class TestesHealthChecks : IDisposable
{
    private const string _bancoIndisponivel =
        "Server=127.0.0.1,14330;Database=PontoBarber;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=1";

    private readonly WebApplicationFactory<Program> _fabrica = new();

    public void Dispose()
    {
        _fabrica.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task HealthChecks_ServidorAtivo_LivenessRetorna200()
    {
        using var cliente = CriarCliente(ObterStringConexaoDisponivel());

        var resposta = await cliente.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
    }

    [TestMethod]
    public async Task HealthChecks_BancoDisponivel_ReadinessRetorna200()
    {
        using var cliente = CriarCliente(ObterStringConexaoDisponivel());

        var resposta = await cliente.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
    }

    [TestMethod]
    public async Task HealthChecks_BancoIndisponivel_ReadinessRetorna503()
    {
        using var cliente = CriarCliente(_bancoIndisponivel);

        var resposta = await cliente.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
    }

    [TestMethod]
    public async Task HealthChecks_BancoIndisponivel_LivenessContinua200()
    {
        using var cliente = CriarCliente(_bancoIndisponivel);

        var resposta = await cliente.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
    }

    [TestMethod]
    public async Task HealthChecks_SemConnectionString_ReadinessRetorna503()
    {
        using var cliente = CriarCliente(null);

        var resposta = await cliente.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
    }

    private static string ObterStringConexaoDisponivel() =>
        ConfiguracaoTestesIntegracao.Container.CriarBancoIsolado("PontoBarberHealth");

    private HttpClient CriarCliente(string? stringConexao) =>
        _fabrica.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuracao) =>
            {
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = stringConexao
                });
            });
        }).CreateClient();
}
