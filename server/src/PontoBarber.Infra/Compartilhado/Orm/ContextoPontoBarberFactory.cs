using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PontoBarber.Aplicacao.Compartilhado;

namespace PontoBarber.Infra.Compartilhado.Orm;

/// <summary>
/// Cria o contexto oficial usado apenas por ferramentas de design-time do EF Core.
/// A connection string vem da mesma configuração da aplicação
/// (<c>ConnectionStrings:DefaultConnection</c>, ou
/// <c>ConnectionStrings__DefaultConnection</c> no ambiente), sem credenciais no
/// código. Comandos que apenas montam o modelo não abrem conexão.
/// </summary>
public sealed class ContextoPontoBarberFactory : IDesignTimeDbContextFactory<ContextoPontoBarber>
{
    public ContextoPontoBarber CreateDbContext(string[] args)
    {
        var stringConexao = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(stringConexao))
            throw new InvalidOperationException(
                "Defina ConnectionStrings__DefaultConnection para executar ferramentas de design-time do EF Core.");

        var opcoes = new DbContextOptionsBuilder<ContextoPontoBarber>()
            .UseNpgsql(stringConexao, npgsql => npgsql.EnableRetryOnFailure(3))
            .Options;

        return new ContextoPontoBarber(opcoes, new ContextoTenantDesignTime());
    }

    private sealed class ContextoTenantDesignTime : IContextoTenant
    {
        public Guid? TenantId => null;
    }
}
