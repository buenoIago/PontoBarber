namespace PontoBarber.Aplicacao.Compartilhado;

/// <summary>
/// Verifica se o usuário autenticado possui vínculo ativo com a barbearia.
/// </summary>
public interface IResolutorVinculoTenant
{
    Task<bool> PossuiVinculoAtivoAsync(Guid usuarioId, Guid tenantId, CancellationToken cancellationToken = default);
}
