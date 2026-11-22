namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>Porta de persistência da disponibilidade semanal por profissional.</summary>
public interface IDisponibilidadeSemanalRepositorio
{
    Task<IReadOnlyList<DisponibilidadeSemanal>> ListarAsync(
        Guid barbeariaId,
        Guid profissionalId,
        CancellationToken cancellationToken);

    Task AdicionarAsync(IReadOnlyList<DisponibilidadeSemanal> janelas, CancellationToken cancellationToken);

    Task RemoverAsync(IReadOnlyList<DisponibilidadeSemanal> janelas, CancellationToken cancellationToken);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
