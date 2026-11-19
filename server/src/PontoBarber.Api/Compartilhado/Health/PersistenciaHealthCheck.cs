using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Api.Compartilhado.Health;

/// <summary>
/// Verifica se a persistência relacional responde, resolvendo o contexto em
/// um escopo próprio para não depender do escopo da requisição.
/// </summary>
public sealed class PersistenciaHealthCheck(IServiceScopeFactory fabricaEscopos) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var escopo = fabricaEscopos.CreateScope();
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            var conectado = await contexto.Database.CanConnectAsync(cancellationToken);

            return conectado
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("A persistência não responde.");
        }
        catch (Exception excecao)
        {
            return HealthCheckResult.Unhealthy("A persistência não responde.", excecao);
        }
    }
}
