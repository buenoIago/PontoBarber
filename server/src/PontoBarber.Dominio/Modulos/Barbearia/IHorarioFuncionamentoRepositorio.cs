namespace PontoBarber.Dominio.Modulos.Barbearia;

/// <summary>Porta de persistência do funcionamento semanal da barbearia.</summary>
public interface IHorarioFuncionamentoRepositorio
{
    Task<IReadOnlyList<HorarioFuncionamento>> ListarPorBarbeariaAsync(
        Guid barbeariaId,
        CancellationToken cancellationToken);

    Task AdicionarAsync(IReadOnlyList<HorarioFuncionamento> horarios, CancellationToken cancellationToken);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
