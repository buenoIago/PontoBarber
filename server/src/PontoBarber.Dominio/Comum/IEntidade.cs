namespace PontoBarber.Dominio.Comum;

/// <summary>
/// Identifica uma entidade persistida com chave primária do tipo <see cref="Guid"/>.
/// </summary>
public interface IEntidade
{
    Guid Id { get; }
}
