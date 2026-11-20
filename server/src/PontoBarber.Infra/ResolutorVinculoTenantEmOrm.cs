using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Modulos.Profissionais;

namespace PontoBarber.Infra;

/// <summary>
/// Resolve o tenant da rota confrontando-o com os vínculos ativos do
/// usuário autenticado (ADR-006, TDD §11.2).
/// </summary>
public sealed class ResolutorVinculoTenantEmOrm(IProfissionalRepositorio profissionais)
    : IResolutorVinculoTenant
{
    public Task<bool> PossuiVinculoAtivoAsync(
        Guid usuarioId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        profissionais.PossuiVinculoAtivoAsync(usuarioId, tenantId, cancellationToken);
}
