namespace PontoBarber.Dominio.Compartilhado;

/// <summary>
/// Base das entidades de domínio com identificador gerado pela aplicação,
/// validação de invariantes e atualização controlada de estado.
/// </summary>
public abstract class EntidadeBase<T>
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public abstract IReadOnlyList<ErroValidacao> Validar();

    public abstract void Atualizar(T entidadeAtualizada);
}
