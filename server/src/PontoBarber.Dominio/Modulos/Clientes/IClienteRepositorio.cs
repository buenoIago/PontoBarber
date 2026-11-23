namespace PontoBarber.Dominio.Modulos.Clientes;

/// <summary>
/// Consulta de clientes relacionados à barbearia por agendamentos, com
/// paginação e busca por prefixo executadas no banco.
/// </summary>
public interface IClienteRepositorio
{
    Task<IReadOnlyList<ClienteResumo>> ListarAsync(
        Guid barbeariaId,
        string? busca,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);

    Task<int> ContarAsync(
        Guid barbeariaId,
        string? busca,
        CancellationToken cancellationToken);

    Task<ClienteResumo?> ObterAsync(
        Guid barbeariaId,
        string usuarioId,
        CancellationToken cancellationToken);
}
