using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Clientes;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Dominio.Modulos.Idempotencia;
using PontoBarber.Dominio.Modulos.Jobs;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Identity;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;
using PontoBarber.Infra.Modulos.Agendamentos;
using PontoBarber.Infra.Modulos.Barbearia;
using PontoBarber.Infra.Modulos.Clientes;
using PontoBarber.Infra.Modulos.Financeiro;
using PontoBarber.Infra.Modulos.Idempotencia;
using PontoBarber.Infra.Modulos.Jobs;
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
        services.AddScoped<IHorarioFuncionamentoRepositorio, RepositorioHorarioFuncionamentoEmOrm>();
        services.AddScoped<IDisponibilidadeSemanalRepositorio, RepositorioDisponibilidadeSemanalEmOrm>();
        services.AddScoped<IBloqueioAgendaRepositorio, RepositorioBloqueioAgendaEmOrm>();
        services.AddScoped<IAgendamentoRepositorio, RepositorioAgendamentoEmOrm>();
        services.AddScoped<IExecucaoJobRepositorio, RepositorioExecucaoJobEmOrm>();
        services.AddScoped<IRegistroIdempotenciaRepositorio, RepositorioRegistroIdempotenciaEmOrm>();
        services.AddScoped<IPagamentoRepositorio, RepositorioPagamentoEmOrm>();
        services.AddScoped<IClienteRepositorio, RepositorioClienteEmOrm>();
        services.AddDataProtection();

        return services;
    }
}
