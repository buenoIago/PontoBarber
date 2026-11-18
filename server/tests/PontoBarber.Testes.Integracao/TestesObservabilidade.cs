using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using PontoBarber.Api.Observabilidade;
using PontoBarber.Api.Tenant;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace PontoBarber.Testes.Integracao;

[TestClass]
public class TestesObservabilidade : IDisposable
{
    private static readonly Guid _tenantValidado = Guid.NewGuid();

    private readonly WebApplicationFactory<Program> _fabrica = new();

    public void Dispose()
    {
        _fabrica.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public void Correlacao_ComRequisicaoAutenticada_EscreveTraceIdUsuarioETenant()
    {
        var contexto = new DefaultHttpContext();
        contexto.TraceIdentifier = "trace-observabilidade-123";
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "usuario-123")], "Teste"));
        contexto.Items[ChavesContextoTenant.TenantId] = _tenantValidado;

        var (logger, saida) = CriarLogger(contexto);
        logger.Information("requisição processada");

        using var json = JsonDocument.Parse(saida.ToString());
        var raiz = json.RootElement;

        Assert.AreEqual("trace-observabilidade-123", raiz.GetProperty("TraceId").GetString());
        Assert.AreEqual("trace-observabilidade-123", raiz.GetProperty("RequestId").GetString());
        Assert.IsTrue(raiz.TryGetProperty("UserId", out var usuario));
        Assert.AreNotEqual(string.Empty, usuario.GetString());
        Assert.AreEqual(_tenantValidado.ToString(), raiz.GetProperty("TenantId").GetString());
    }

    [TestMethod]
    public void Correlacao_RotaSemTenantENaoAutenticada_NaoInventaIdentificadores()
    {
        var contexto = new DefaultHttpContext();
        contexto.TraceIdentifier = "trace-publica-456";
        contexto.Request.RouteValues["barbershopId"] = Guid.NewGuid().ToString();
        contexto.Request.Body = new MemoryStream();

        var (logger, saida) = CriarLogger(contexto);
        logger.Information("rota pública processada");

        using var json = JsonDocument.Parse(saida.ToString());
        var raiz = json.RootElement;

        Assert.AreEqual("trace-publica-456", raiz.GetProperty("TraceId").GetString());
        Assert.IsFalse(raiz.TryGetProperty("UserId", out _));
        Assert.IsFalse(raiz.TryGetProperty("TenantId", out _));
    }

    [TestMethod]
    public void Redacao_ObjetoComDadosSensiveis_MascaraPropriedades()
    {
        var (logger, saida) = CriarLogger(null);
        var registro = new RegistroSensivel
        {
            Nome = "João",
            Senha = "senhaSuperSecreta",
            Email = "joao.silva@exemplo.com",
            Telefone = "11987654321"
        };

        logger.Information("dados {@Registro}", registro);

        var texto = saida.ToString();
        Assert.IsTrue(texto.Contains("João", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("senhaSuperSecreta", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("joao.silva@exemplo.com", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("11987654321", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Redacao_PropriedadesEscalaresSensiveis_SaoMascaradas()
    {
        var (logger, saida) = CriarLogger(null);

        logger.Information(
            "tentativa {Password} {Token} {Cookie}",
            "senhaNaoLogar",
            "tokenNaoLogar",
            "cookieNaoLogar");

        var texto = saida.ToString();
        Assert.IsFalse(texto.Contains("senhaNaoLogar", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("tokenNaoLogar", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("cookieNaoLogar", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Redacao_MensagemComEmailETelefone_SaoMascarados()
    {
        var (logger, saida) = CriarLogger(null);

        logger.Information("contato maria@empresa.com 21999887766");

        var texto = saida.ToString();
        Assert.IsFalse(texto.Contains("maria@empresa.com", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("21999887766", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RedacaoAntesDosSinks_EventosParaQualquerDestino_NaoContemValoresCompletos()
    {
        // O sink de captura representa qualquer destino futuro: recebe o
        // LogEvent exatamente como o destino receberia, após a redação global.
        var captura = new SinkCaptura();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new RedacaoLogEventSink(captura))
            .CreateLogger();

        logger.Information(
            "login {Password} {Cookie} {Token}",
            "senhaMuitoSecreta",
            "cookieMuitoSecreto",
            "tokenMuitoSecreto");
        logger.Information("contato maria@empresa.com 21999887766 observacao util");
        logger.Information("objeto {@Registro}", new RegistroSensivel
        {
            Nome = "Maria",
            Senha = "outraSenha",
            Email = "maria@empresa.com",
            Telefone = "21999887766"
        });
        logger.Error(
            new InvalidOperationException("falha token=segredoNaExcecao senha=senhaNaExcecao"),
            "erro {Email}",
            "joao@exemplo.com");

        var eventos = captura.Eventos;
        Assert.IsTrue(eventos.Count > 0);

        var texto = string.Join("\n", eventos.Select(RenderizarEvento));

        Assert.IsFalse(texto.Contains("senhaMuitoSecreta", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("cookieMuitoSecreto", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("tokenMuitoSecreto", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("maria@empresa.com", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("joao@exemplo.com", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("21999887766", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("outraSenha", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("segredoNaExcecao", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("senhaNaExcecao", StringComparison.Ordinal));
        Assert.IsTrue(texto.Contains("observacao util", StringComparison.Ordinal));
        Assert.IsTrue(texto.Contains("Maria", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Configuracao_SemDestinoExterno_AplicacaoInicializa()
    {
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
    }

    private static (Logger Logger, StringWriter Saida) CriarLogger(HttpContext? contexto)
    {
        var accessor = new HttpContextAccessor { HttpContext = contexto };
        var saida = new StringWriter();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.With(new CorrelacaoLogEnricher(accessor))
            .Destructure.With<PoliticaRedacaoSensiveis>()
            .WriteTo.Sink(new RedacaoLogEventSink(new SinkConsoleSeguro(saida)))
            .CreateLogger();

        return (logger, saida);
    }

    private static string RenderizarEvento(LogEvent evento)
    {
        using var escritor = new StringWriter();
        escritor.WriteLine(evento.RenderMessage(CultureInfo.InvariantCulture));

        foreach (var propriedade in evento.Properties)
        {
            propriedade.Value.Render(escritor, format: null, CultureInfo.InvariantCulture);
            escritor.WriteLine();
        }

        escritor.WriteLine(evento.Exception?.ToString());
        return escritor.ToString();
    }

    private sealed class SinkCaptura : ILogEventSink
    {
        public List<LogEvent> Eventos { get; } = [];

        public void Emit(LogEvent logEvent) => Eventos.Add(logEvent);
    }

    private sealed class RegistroSensivel
    {
        public string Nome { get; init; } = string.Empty;
        public string Senha { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Telefone { get; init; } = string.Empty;
    }
}
