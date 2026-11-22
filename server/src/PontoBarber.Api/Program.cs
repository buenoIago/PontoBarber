using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using PontoBarber.Api.Compartilhado.Health;
using PontoBarber.Api.Compartilhado.Http;
using PontoBarber.Api.Compartilhado.Logging;
using PontoBarber.Api.Compartilhado.Tenant;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Aplicacao.Modulos.Disponibilidade;
using PontoBarber.Aplicacao.Modulos.Profissionais;
using PontoBarber.Aplicacao.Modulos.Servicos;
using PontoBarber.Infra;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
// AddIdentity (via AddInfrastructureServices) registra a autenticação e o cookie.
builder.Services.AdicionarAuthApi();
builder.Services.AddScoped<IContextoTenant, ContextoTenantHttp>();
builder.Services.AddScoped<IResolutorVinculoTenant, ResolutorVinculoTenantEmOrm>();
builder.Services.AddScoped<ServicoConta>();
builder.Services.AddScoped<ServicoBarbearia>();
builder.Services.AddScoped<ServicoProfissional>();
builder.Services.AddScoped<ServicoCatalogoModelo>();
builder.Services.AddScoped<GestaoServicos>();
builder.Services.AddScoped<GestaoHorariosFuncionamento>();
builder.Services.AddScoped<GestaoDisponibilidadeSemanal>();
builder.Services.AddScoped<GestaoBloqueios>();
builder.Services.AddScoped<ConsultarDisponibilidade>();

var loggerConsole = new LoggerConfiguration()
    .WriteTo.Sink(new SinkConsoleSeguro())
    .CreateLogger();

builder.Host.UseSerilog((contexto, servicos, configuracao) =>
{
    configuracao
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
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

string? arquivoIndex = null;

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // ADR-007: UI interativa sobre o documento nativo, sem gerar um segundo OpenAPI.
    app.UseSwaggerUI(opcoes =>
    {
        opcoes.RoutePrefix = "swagger";
        opcoes.SwaggerEndpoint("/openapi/v1.json", "PontoBarber API v1");
    });

    // Em Development a raiz é apenas a API; a SPA é servida pelo dev server do Angular.
    app.MapGet("/", () => Results.Json(new
    {
        aplicacao = "PontoBarber API",
        ambiente = "Development"
    }));
}
else
{
    // ADR-033: assets da SPA no mesmo host e porta da API, sem CORS e sem Nginx.
    var webRoot = new[] { app.Environment.WebRootPath, Path.Combine(AppContext.BaseDirectory, "wwwroot") }
        .FirstOrDefault(caminho => !string.IsNullOrEmpty(caminho) && File.Exists(Path.Combine(caminho, "index.html")))
        ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
    arquivoIndex = Path.Combine(webRoot, "index.html");

    if (Directory.Exists(webRoot))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(webRoot),
        });
    }
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.UseMiddleware<AntiforgeryMiddleware>();
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

// Somente rotas da SPA recebem index.html; rotas de API, health, OpenAPI e Swagger não são capturadas.
app.MapFallback(async contexto =>
{
    var caminho = contexto.Request.Path;
    var rotaReservada =
        caminho.StartsWithSegments("/api") ||
        caminho.StartsWithSegments("/health") ||
        caminho.StartsWithSegments("/openapi") ||
        caminho.StartsWithSegments("/swagger");

    if (rotaReservada || arquivoIndex is null || !File.Exists(arquivoIndex))
    {
        contexto.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    contexto.Response.ContentType = "text/html; charset=utf-8";
    await contexto.Response.SendFileAsync(arquivoIndex);
});

try
{
    app.Run();
}
finally
{
    loggerConsole.Dispose();
}

public partial class Program;
