using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PontoBarber.Api.Health;
using PontoBarber.Api.Middleware;
using PontoBarber.Api.Observabilidade;
using PontoBarber.Api.Tenant;
using PontoBarber.Aplicacao.Abstracoes;
using PontoBarber.Infra;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
        .WriteTo.Sink(new RedacaoLogEventSink(loggerConsole));
});

builder.Services.AddInfrastructureServices();

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
    loggerConsole.Dispose();
}

public partial class Program;
