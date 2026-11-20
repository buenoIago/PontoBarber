using PontoBarber.Dominio.Modulos.Profissionais;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

namespace PontoBarber.Dominio.Modulos.Barbearia;

/// <summary>
/// Porta de persistência da barbearia: criação, leitura e atualização de
/// dados cadastrais (ADR-019). O tenant é derivado do vínculo do usuário,
/// nunca aceito do corpo da requisição.
/// </summary>
public interface IBarbeariaRepositorio
{
    Task<bool> CodigoConviteExisteAsync(string codigo, CancellationToken cancellationToken);

    /// <summary>
    /// Persiste a barbearia, o vínculo de proprietário e, quando informado, o
    /// perfil recém-criado. Um perfil já existente é reaproveitado e não deve
    /// ser incluído novamente (Task 4.5).
    /// </summary>
    Task CriarAsync(
        BarbeariaEntidade barbearia,
        PerfilProfissional? perfilNovo,
        VinculoProfissional vinculo,
        CancellationToken cancellationToken);

    Task<BarbeariaEntidade?> ObterPorIdAsync(Guid barbeariaId, CancellationToken cancellationToken);

    Task<BarbeariaEntidade?> ObterPorCodigoConviteAsync(string codigo, CancellationToken cancellationToken);

    Task AtualizarAsync(BarbeariaEntidade barbearia, CancellationToken cancellationToken);
}
