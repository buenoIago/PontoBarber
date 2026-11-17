using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PontoBarber.Aplicacao.Abstracoes;
using PontoBarber.Infra;
using PontoBarber.Infra.Persistencia;
using PontoBarber.WebApp.Health;
using PontoBarber.WebApp.Middleware;
using PontoBarber.WebApp.Tenant;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContextoPontoBarber>((servicos, options) =>
{
    var configuracao = servicos.GetRequiredService<IConfiguration>();
    var stringConexao = configuracao.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(stringConexao))
        options.UseSqlServer(stringConexao);
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IContextoTenant, ContextoTenantHttp>();
builder.Services.AddScoped<IResolutorVinculoTenant, ResolutorVinculoTenantNegaTodos>();

builder.Services.AddHealthChecks()
    .AddCheck("vivacidade", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<PersistenciaHealthCheck>("persistencia", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
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

app.Run();

public partial class Program;
