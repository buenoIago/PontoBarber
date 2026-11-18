using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra;

/// <summary>
/// Registra os serviços de infraestrutura da aplicação.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddDbContext<ContextoPontoBarber>((servicos, options) =>
        {
            var configuracao = servicos.GetRequiredService<IConfiguration>();
            var stringConexao = configuracao.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrWhiteSpace(stringConexao))
                options.UseSqlServer(stringConexao);
        });

        return services;
    }
}
