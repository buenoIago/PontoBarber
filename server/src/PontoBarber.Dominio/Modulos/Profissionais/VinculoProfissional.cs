using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>
/// Vínculo entre um profissional e uma barbearia (ADR-006). O criador da
/// barbearia recebe o vínculo <see cref="CargoProfissional.Proprietario"/>.
/// </summary>
public sealed class VinculoProfissional : IPertencenteTenant
{
    private VinculoProfissional()
    {
    }

    public Guid Id { get; private set; }
    public Guid BarbeariaId { get; private set; }
    public Guid ProfissionalId { get; private set; }
    public CargoProfissional Cargo { get; private set; }
    public StatusVinculo Situacao { get; private set; }
    public DateTime VinculadoEmUtc { get; private set; }
    public DateTime? RemovidoEmUtc { get; private set; }

    public static VinculoProfissional Criar(Guid barbeariaId, Guid profissionalId) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            BarbeariaId = barbeariaId,
            ProfissionalId = profissionalId,
            Cargo = CargoProfissional.Proprietario,
            Situacao = StatusVinculo.Ativo,
            VinculadoEmUtc = DateTime.UtcNow
        };
}
