using PontoBarber.WebApp.Middleware;
using PontoBarber.WebApp.Tenant;
using PontoBarber.Aplicacao.Abstracoes;
using PontoBarber.Infra;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IContextoTenant, ContextoTenantHttp>();
builder.Services.AddScoped<IResolutorVinculoTenant, ResolutorVinculoTenantNegaTodos>();

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

app.MapControllers();

app.Run();
