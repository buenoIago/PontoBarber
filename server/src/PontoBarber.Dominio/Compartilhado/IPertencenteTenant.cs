namespace PontoBarber.Dominio.Compartilhado;

/// <summary>
/// Identifica uma entidade que pertence a uma barbearia específica (tenant),
/// conforme ADR-006.
/// </summary>
public interface IPertencenteTenant : IEntidade
{
    /// <summary>
    /// Identificador da barbearia (tenant) a que a entidade pertence.
    /// O nome da propriedade adotado nas entidades é <c>BarbeariaId</c>.
    /// </summary>
    Guid BarbeariaId { get; }
}
