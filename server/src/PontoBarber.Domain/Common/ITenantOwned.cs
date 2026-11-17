namespace PontoBarber.Domain.Common;

/// <summary>
/// Identifica uma entidade que pertence a uma barbearia específica (tenant),
/// conforme ADR-006.
/// </summary>
public interface ITenantOwned : IEntity
{
    Guid TenantId { get; }
}
