namespace PontoBarber.Application.Abstractions;

/// <summary>
/// Verifica se o usuário autenticado possui vínculo ativo com a barbearia.
/// </summary>
public interface ITenantMembershipResolver
{
    Task<bool> IsActiveMemberAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}
