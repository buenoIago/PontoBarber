using PontoBarber.Dominio.Modulos.Profissionais;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

namespace PontoBarber.Dominio.Modulos.Barbearia;

/// <summary>
/// Porta de persistência da criação de barbearia. A verificação do código de
/// convite e a gravação do tenant, do perfil do proprietário e do vínculo
/// ocorrem na mesma transação (ADR-019).
/// </summary>
public interface IBarbeariaRepositorio
{
    Task<bool> CodigoConviteExisteAsync(string codigo, CancellationToken cancellationToken);

    Task CriarAsync(
        BarbeariaEntidade barbearia,
        PerfilProfissional proprietario,
        VinculoProfissional vinculo,
        CancellationToken cancellationToken);
}
