namespace PontoBarber.Aplicacao.Abstracoes;

/// <summary>
/// Tenant ativo da requisição, preenchido somente após a validação do vínculo.
/// </summary>
public interface IContextoTenant
{
    Guid? TenantId { get; }
}
