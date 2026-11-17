using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PontoBarber.Testes.Integracao;

public class TestesHealthChecks : IClassFixture<WebApplicationFactory<Program>>
{
    private const string _bancoIndisponivel =
        "Server=127.0.0.1,14330;Database=PontoBarber;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=1";

    private const string _bancoDisponivel =
        @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

    private readonly WebApplicationFactory<Program> _fabrica;

    public TestesHealthChecks(WebApplicationFactory<Program> fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task HealthChecks_ServidorAtivo_LivenessRetorna200()
    {
        using var cliente = CriarCliente(_bancoDisponivel);

        var resposta = await cliente.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task HealthChecks_BancoDisponivel_ReadinessRetorna200()
    {
        using var cliente = CriarCliente(_bancoDisponivel);

        var resposta = await cliente.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task HealthChecks_BancoIndisponivel_ReadinessRetorna503()
    {
        using var cliente = CriarCliente(_bancoIndisponivel);

        var resposta = await cliente.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
    }

    [Fact]
    public async Task HealthChecks_BancoIndisponivel_LivenessContinua200()
    {
        using var cliente = CriarCliente(_bancoIndisponivel);

        var resposta = await cliente.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task HealthChecks_SemConnectionString_ReadinessRetorna503()
    {
        using var cliente = CriarCliente(null);

        var resposta = await cliente.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
    }

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
