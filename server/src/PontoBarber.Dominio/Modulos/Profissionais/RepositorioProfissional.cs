namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>
/// Porta de persistência de perfis profissionais e vínculos com barbearias.
/// Utilizada também pelo resolutor de tenant e pelo ingresso por convite.
/// </summary>
public interface IProfissionalRepositorio
{
    Task<PerfilProfissional?> ObterPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Retorna o vínculo entre profissional e barbearia em qualquer situação,
    /// inclusive inativo ou removido, para evitar duplicidade.
    /// </summary>
    Task<VinculoProfissional?> ObterVinculoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        CancellationToken cancellationToken);

    Task<bool> PossuiVinculoAtivoAsync(
        Guid usuarioId,
        Guid barbeariaId,
        CancellationToken cancellationToken);

    Task AdicionarVinculoAsync(
        PerfilProfissional? perfilNovo,
        VinculoProfissional vinculo,
        CancellationToken cancellationToken);
}
