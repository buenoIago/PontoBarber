using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using PontoBarber.WebApp.Observabilidade;
using PontoBarber.WebApp.Tenant;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace PontoBarber.Testes.Integracao;

public class TestesObservabilidade : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid _tenantValidado = Guid.NewGuid();

    private readonly WebApplicationFactory<Program> _fabrica;

    public TestesObservabilidade(WebApplicationFactory<Program> fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
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

        Assert.Equal("trace-observabilidade-123", raiz.GetProperty("TraceId").GetString());
        Assert.Equal("trace-observabilidade-123", raiz.GetProperty("RequestId").GetString());
        Assert.True(raiz.TryGetProperty("UserId", out var usuario));
        Assert.NotEqual(string.Empty, usuario.GetString());
        Assert.Equal(_tenantValidado.ToString(), raiz.GetProperty("TenantId").GetString());
    }

    [Fact]
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

        Assert.Equal("trace-publica-456", raiz.GetProperty("TraceId").GetString());
        Assert.False(raiz.TryGetProperty("UserId", out _));
        Assert.False(raiz.TryGetProperty("TenantId", out _));
    }

    [Fact]
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
        Assert.Contains("João", texto);
        Assert.DoesNotContain("senhaSuperSecreta", texto);
        Assert.DoesNotContain("joao.silva@exemplo.com", texto);
        Assert.DoesNotContain("11987654321", texto);
    }

    [Fact]
    public void Redacao_PropriedadesEscalaresSensiveis_SaoMascaradas()
    {
        var (logger, saida) = CriarLogger(null);

        logger.Information(
            "tentativa {Password} {Token} {Cookie}",
            "senhaNaoLogar",
            "tokenNaoLogar",
            "cookieNaoLogar");

        var texto = saida.ToString();
        Assert.DoesNotContain("senhaNaoLogar", texto);
        Assert.DoesNotContain("tokenNaoLogar", texto);
        Assert.DoesNotContain("cookieNaoLogar", texto);
    }

    [Fact]
    public void Redacao_MensagemComEmailETelefone_SaoMascarados()
    {
        var (logger, saida) = CriarLogger(null);

        logger.Information("contato maria@empresa.com 21999887766");

        var texto = saida.ToString();
        Assert.DoesNotContain("maria@empresa.com", texto);
        Assert.DoesNotContain("21999887766", texto);
    }

    [Fact]
    public void RedacaoAntesDosSinks_EventosDestinoOTLP_NaoContemValoresCompletos()
    {
        // O sink de captura representa o destino OTLP: recebe o LogEvent
        // exatamente como o exporter receberia, após a redação global.
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
        Assert.NotEmpty(eventos);

        var texto = string.Join("\n", eventos.Select(RenderizarEvento));

        Assert.DoesNotContain("senhaMuitoSecreta", texto);
        Assert.DoesNotContain("cookieMuitoSecreto", texto);
        Assert.DoesNotContain("tokenMuitoSecreto", texto);
        Assert.DoesNotContain("maria@empresa.com", texto);
        Assert.DoesNotContain("joao@exemplo.com", texto);
        Assert.DoesNotContain("21999887766", texto);
        Assert.DoesNotContain("outraSenha", texto);
        Assert.DoesNotContain("segredoNaExcecao", texto);
        Assert.DoesNotContain("senhaNaExcecao", texto);
        Assert.Contains("observacao util", texto);
        Assert.Contains("Maria", texto);
    }

    [Fact]
    public async Task Configuracao_SemEndpointOtlp_AplicacaoInicializa()
    {
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
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
