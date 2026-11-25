namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>
/// Porta de persistência de perfis profissionais e vínculos com barbearias.
/// Utilizada também pelo resolutor de tenant e pelo ingresso por convite.
/// </summary>
public interface IProfissionalRepositorio
{
    Task<PerfilProfissional?> ObterPorUsuarioAsync(string usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém o perfil pelo identificador, sem depender da conta de acesso.
    /// </summary>
    Task<PerfilProfissional?> ObterPorIdAsync(Guid profissionalId, CancellationToken cancellationToken);

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

    /// <summary>
    /// Vínculos ativos do profissional identificado pela conta de acesso,
    /// independentemente da barbearia. Usado para reidratar o tenant no boot
    /// da SPA; não expõe dados de outras contas.
    /// </summary>
    Task<IReadOnlyList<VinculoProfissional>> ListarVinculosAtivosPorUsuarioAsync(
        string usuarioId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lista os profissionais vinculados à barbearia, exceto os vínculos
    /// removidos. A lista é filtrada pelo tenant da rota.
    /// </summary>
    Task<IReadOnlyList<ProfissionalVinculo>> ListarProfissionaisAsync(
        Guid barbeariaId,
        CancellationToken cancellationToken);

    Task AdicionarVinculoAsync(
        PerfilProfissional? perfilNovo,
        VinculoProfissional vinculo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retorna o vínculo em qualquer situação, acompanhado pelo contexto, para
    /// que a alteração de estado passe pelos métodos de domínio e seja
    /// persistida sem recriar a entidade.
    /// </summary>
    Task<VinculoProfissional?> ObterVinculoParaAtualizacaoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        CancellationToken cancellationToken);

    /// <summary>Persiste as alterações feitas nas entidades acompanhadas.</summary>
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
