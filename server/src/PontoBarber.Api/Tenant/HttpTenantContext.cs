using PontoBarber.Application.Abstractions;

namespace PontoBarber.Api.Tenant;

/// <summary>
/// Expõe o tenant validado pelo middleware sem permitir que o corpo da
/// requisição altere o valor.
/// </summary>
public sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public Guid? TenantId =>
        accessor.HttpContext?.Items.TryGetValue(TenantContextKeys.TenantId, out var value) == true
            ? value as Guid?
            : null;
}
