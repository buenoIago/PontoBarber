using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PontoBarber.Aplicacao.Compartilhado;

namespace PontoBarber.Infra.Compartilhado.Orm;

/// <summary>
/// Cria o contexto usado apenas por ferramentas de design-time do EF Core.
/// A connection string é fictícia e serve somente para construir o modelo —
/// nenhuma conexão é aberta por comandos como
/// <c>dotnet ef migrations has-pending-model-changes</c>.
/// </summary>
public sealed class ContextoPontoBarberFactory : IDesignTimeDbContextFactory<ContextoPontoBarber>
{
    private const string _stringConexaoDesignTime =
        "Server=localhost;Database=PontoBarberDesignTime;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=1";

    public ContextoPontoBarber CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<ContextoPontoBarber>()
            .UseSqlServer(_stringConexaoDesignTime)
            .Options;

        return new ContextoPontoBarber(opcoes, new ContextoTenantDesignTime());
    }

    private sealed class ContextoTenantDesignTime : IContextoTenant
    {
        public Guid? TenantId => null;
    }
}
