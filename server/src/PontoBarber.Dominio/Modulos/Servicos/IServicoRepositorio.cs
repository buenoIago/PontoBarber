namespace PontoBarber.Dominio.Modulos.Servicos;

/// <summary>Porta de persistência do catálogo de serviços por barbearia.</summary>
public interface IServicoRepositorio
{
    Task<IReadOnlyList<Servico>> ListarPorBarbeariaAsync(Guid barbeariaId, CancellationToken cancellationToken);

    /// <summary>Lista somente os serviços ativos da oferta da barbearia.</summary>
    Task<IReadOnlyList<Servico>> ListarAtivosPorBarbeariaAsync(Guid barbeariaId, CancellationToken cancellationToken);

    /// <summary>Obtém o serviço somente se pertencer à barbearia informada.</summary>
    Task<Servico?> ObterPorIdAsync(Guid barbeariaId, Guid servicoId, CancellationToken cancellationToken);

    Task AdicionarAsync(IReadOnlyList<Servico> servicos, CancellationToken cancellationToken);

    Task AtualizarAsync(Servico servico, CancellationToken cancellationToken);
}
