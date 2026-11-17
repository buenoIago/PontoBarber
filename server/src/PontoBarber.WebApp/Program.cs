using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PontoBarber.Aplicacao.Abstracoes;
using PontoBarber.Infra;
using PontoBarber.Infra.Persistencia;
using PontoBarber.WebApp.Health;
using PontoBarber.WebApp.Middleware;
using PontoBarber.WebApp.Observabilidade;
using PontoBarber.WebApp.Tenant;
using Serilog;
using Serilog.Core;

var builder = WebApplication.CreateBuilder(args);

var endpointOtlp = builder.Configuration["Observabilidade:OTLP:Endpoint"];

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IContextoTenant, ContextoTenantHttp>();
builder.Services.AddScoped<IResolutorVinculoTenant, ResolutorVinculoTenantNegaTodos>();

var loggerConsole = new LoggerConfiguration()
    .WriteTo.Sink(new SinkConsoleSeguro())
    .CreateLogger();

var loggers = new List<Logger> { loggerConsole };
var destinos = new List<ILogEventSink> { loggerConsole };

if (!string.IsNullOrWhiteSpace(endpointOtlp))
{
    var loggerOtlp = new LoggerConfiguration()
        .WriteTo.OpenTelemetry(opcoes => opcoes.Endpoint = endpointOtlp)
        .CreateLogger();
    loggers.Add(loggerOtlp);
    destinos.Add(loggerOtlp);
}

builder.Host.UseSerilog((contexto, servicos, configuracao) =>
{
    configuracao
        .ReadFrom.Configuration(contexto.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Aplicacao", "PontoBarber")
        .Enrich.WithProperty("Ambiente", contexto.HostingEnvironment.EnvironmentName)
        .Enrich.WithProperty("Versao", typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0")
        .Enrich.With(new CorrelacaoLogEnricher(servicos.GetRequiredService<IHttpContextAccessor>()))
        .Destructure.With<PoliticaRedacaoSensiveis>()
        .WriteTo.Sink(new RedacaoLogEventSink(destinos.ToArray()));
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddSqlClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(endpointOtlp))
            tracing.AddOtlpExporter(opcoes => opcoes.Endpoint = new Uri(endpointOtlp));
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();

        if (!string.IsNullOrWhiteSpace(endpointOtlp))
            metrics.AddOtlpExporter(opcoes => opcoes.Endpoint = new Uri(endpointOtlp));
    });

builder.Services.AddDbContext<ContextoPontoBarber>((servicos, options) =>
{
    var configuracao = servicos.GetRequiredService<IConfiguration>();
    var stringConexao = configuracao.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(stringConexao))
        options.UseSqlServer(stringConexao);
});

builder.Services.AddHealthChecks()
    .AddCheck("vivacidade", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<PersistenciaHealthCheck>("persistencia", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

try
{
    app.Run();
}
finally
{
    foreach (var logger in loggers)
        logger.Dispose();
}

public partial class Program;
