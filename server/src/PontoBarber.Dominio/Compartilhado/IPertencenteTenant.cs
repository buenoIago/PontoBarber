namespace PontoBarber.Dominio.Compartilhado;

/// <summary>
/// Identifica uma entidade que pertence a uma barbearia específica (tenant),
/// conforme ADR-006.
/// </summary>
public interface IPertencenteTenant : IEntidade
{
    Guid TenantId { get; }
}
