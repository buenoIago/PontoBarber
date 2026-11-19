using System.Globalization;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using PontoBarber.Api.Compartilhado.Http;
using PontoBarber.Api.Compartilhado.Logging;
using PontoBarber.Api.Compartilhado.Tenant;
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

        var (logger, eventos) = CriarLogger(contexto);
        logger.Information("requisição processada");

        var evento = eventos.Single();
        Assert.AreEqual("trace-observabilidade-123", ObterTexto(evento, "TraceId"));
        Assert.AreEqual("trace-observabilidade-123", ObterTexto(evento, "RequestId"));
        Assert.AreEqual(32, ObterTexto(evento, "UserId")?.Length);
        Assert.AreEqual(_tenantValidado.ToString(), ObterTexto(evento, "TenantId"));
    }

    [TestMethod]
    public void Correlacao_RotaSemTenantENaoAutenticada_NaoInventaIdentificadores()
    {
        var contexto = new DefaultHttpContext();
        contexto.TraceIdentifier = "trace-publica-456";
        contexto.Request.RouteValues[TenantResolutionMiddleware.NomeParametroRota] = Guid.NewGuid().ToString();
        contexto.Request.Body = new MemoryStream();

        var (logger, eventos) = CriarLogger(contexto);
        logger.Information("rota pública processada");

        var evento = eventos.Single();
        Assert.AreEqual("trace-publica-456", ObterTexto(evento, "TraceId"));
        Assert.IsNull(ObterTexto(evento, "UserId"));
        Assert.IsNull(ObterTexto(evento, "TenantId"));
    }

    [TestMethod]
    public void Redacao_ObjetoComDadosSensiveis_MascaraPropriedades()
    {
        var (logger, eventos) = CriarLogger(null);
        var registro = new RegistroSensivel
        {
            Nome = "João",
            Senha = "senhaSuperSecreta",
            Email = "joao.silva@exemplo.com",
            Telefone = "11987654321"
        };

        logger.Information("dados {@Registro}", registro);

        var texto = RenderizarEvento(eventos.Single());
        Assert.IsTrue(texto.Contains("João", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("senhaSuperSecreta", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("joao.silva@exemplo.com", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("11987654321", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Redacao_PropriedadesEscalaresSensiveis_SaoMascaradas()
    {
        var (logger, eventos) = CriarLogger(null);

        logger.Information(
            "tentativa {Password} {Token} {Cookie}",
            "senhaNaoLogar",
            "tokenNaoLogar",
            "cookieNaoLogar");

        var texto = RenderizarEvento(eventos.Single());
        Assert.IsFalse(texto.Contains("senhaNaoLogar", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("tokenNaoLogar", StringComparison.Ordinal));
        Assert.IsFalse(texto.Contains("cookieNaoLogar", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Redacao_MensagemComEmailETelefone_SaoMascarados()
    {
        var (logger, eventos) = CriarLogger(null);

        logger.Information("contato maria@empresa.com 21999887766");

        var texto = RenderizarEvento(eventos.Single());
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

    private static (Logger Logger, List<LogEvent> Eventos) CriarLogger(HttpContext? contexto)
    {
        var accessor = new HttpContextAccessor { HttpContext = contexto };
        var captura = new SinkCaptura();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.With(new CorrelacaoLogEnricher(accessor))
            .Destructure.With<PoliticaRedacaoSensiveis>()
            .WriteTo.Sink(new RedacaoLogEventSink(captura))
            .CreateLogger();

        return (logger, captura.Eventos);
    }

    private static string? ObterTexto(LogEvent evento, string nome) =>
        evento.Properties.TryGetValue(nome, out var valor)
            ? valor is ScalarValue { Value: not null } escalar
                ? escalar.Value.ToString()
                : valor.ToString()
            : null;

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
