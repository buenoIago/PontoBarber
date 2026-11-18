using PontoBarber.Aplicacao.Abstracoes;

namespace PontoBarber.Api.Tenant;

/// <summary>
/// Expõe o tenant validado pelo middleware sem permitir que o corpo da
/// requisição altere o valor.
/// </summary>
public sealed class ContextoTenantHttp(IHttpContextAccessor accessor) : IContextoTenant
{
    public Guid? TenantId =>
        accessor.HttpContext?.Items.TryGetValue(ChavesContextoTenant.TenantId, out var valor) == true
            ? valor as Guid?
            : null;
}
