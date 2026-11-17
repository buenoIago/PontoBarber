using PontoBarber.Application.Abstractions;

namespace PontoBarber.Infrastructure;

/// <summary>
/// Implementação provisória que nega qualquer vínculo. Não existe ainda
/// cadastro de barbearias nem entidade de vínculo nesta etapa; a substituição
/// pela leitura dos vínculos ativos pertence às tarefas de profissionais.
/// </summary>
public sealed class DenyAllTenantMembershipResolver : ITenantMembershipResolver
{
    public Task<bool> IsActiveMemberAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        _ = userId;
        _ = tenantId;
        _ = cancellationToken;

        return Task.FromResult(false);
    }
}
