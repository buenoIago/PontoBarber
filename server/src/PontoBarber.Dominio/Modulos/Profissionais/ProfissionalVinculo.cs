namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>
/// Par perfil e vínculo usado na listagem de profissionais de uma barbearia
/// (RF-PROF-002/003). Não é uma entidade de domínio.
/// </summary>
public sealed record ProfissionalVinculo(PerfilProfissional Perfil, VinculoProfissional Vinculo);
