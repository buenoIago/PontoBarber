using PontoBarber.Aplicacao.Abstracoes;

namespace PontoBarber.Infra;

/// <summary>
/// Implementação provisória que nega qualquer vínculo. Não existe ainda
/// cadastro de barbearias nem entidade de vínculo nesta etapa; a substituição
/// pela leitura dos vínculos ativos pertence às tarefas de profissionais.
/// </summary>
public sealed class ResolutorVinculoTenantNegaTodos : IResolutorVinculoTenant
{
    public Task<bool> PossuiVinculoAtivoAsync(Guid usuarioId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        _ = usuarioId;
        _ = tenantId;
        _ = cancellationToken;

        return Task.FromResult(false);
    }
}
