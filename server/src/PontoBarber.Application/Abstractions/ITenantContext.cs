namespace PontoBarber.Application.Abstractions;

/// <summary>
/// Tenant ativo da requisição, preenchido somente após a validação do vínculo.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
}
