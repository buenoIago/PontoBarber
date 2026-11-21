using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Identity;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;
using PontoBarber.Infra.Modulos.Barbearia;
using PontoBarber.Infra.Modulos.Profissionais;
using PontoBarber.Infra.Modulos.Servicos;

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

        // Único registro de Identity. A configuração de senha, bloqueio,
        // e-mail único e cookie vive em AddIdentityPontoBarber (ADR-005/TDD §11.1),
        // com os stores no ContextoPontoBarber — o mesmo contexto do domínio.
        services.AddIdentityPontoBarber();
        services.AddScoped<IIdentidade, AdaptadorIdentidade>();
        services.AddScoped<IBarbeariaRepositorio, RepositorioBarbeariaEmOrm>();
        services.AddScoped<IProfissionalRepositorio, RepositorioProfissionalEmOrm>();
        services.AddScoped<IServicoRepositorio, RepositorioServicoEmOrm>();
        services.AddDataProtection();

        return services;
    }
}
