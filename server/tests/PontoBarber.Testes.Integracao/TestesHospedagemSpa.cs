using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// ADR-033: a SPA é servida pelo ASP.NET Core na mesma origem da API, com
/// fallback para index.html que não captura /api nem /health.
/// </summary>
[TestClass]
public sealed class TestesHospedagemSpa : IDisposable
{
    private const string _index = "<!doctype html><html><body><app-root></app-root></body></html>";

    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), "pb-spa-" + Guid.NewGuid().ToString("N"));

    public TestesHospedagemSpa()
    {
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), _index);
    }

    public void Dispose()
    {
        Directory.Delete(_webRoot, recursive: true);
        GC.SuppressFinalize(this);
    }

    private HttpClient CriarCliente() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseWebRoot(_webRoot))
            .CreateClient();

    [TestMethod]
    public async Task RotaNaoApi_RetornaIndexDaSpa()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.GetAsync("/qualquer-rota-da-spa");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        StringAssert.Contains(await resposta.Content.ReadAsStringAsync(), "<app-root>");
    }

    [TestMethod]
    public async Task RotaInexistenteSobApi_NaoRetornaIndexDaSpa()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.GetAsync("/api/v1/rota-inexistente");

        Assert.AreEqual(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.IsFalse((await resposta.Content.ReadAsStringAsync()).Contains("<app-root>", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task HealthLive_NaoCapturadoPeloFallback()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
    }
}
