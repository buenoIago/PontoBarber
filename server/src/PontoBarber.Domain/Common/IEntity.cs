namespace PontoBarber.Domain.Common;

/// <summary>
/// Identifica uma entidade persistida com chave primária do tipo <see cref="Guid"/>.
/// </summary>
public interface IEntity
{
    Guid Id { get; }
}
