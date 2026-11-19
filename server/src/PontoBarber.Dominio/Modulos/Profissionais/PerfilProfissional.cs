using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>
/// Perfil profissional vinculado a uma conta de acesso (TDD §8.1).
/// Entidade global: o perfil não pertence a uma barbearia específica.
/// </summary>
public sealed class PerfilProfissional : IEntidade
{
    private PerfilProfissional()
    {
    }

    public Guid Id { get; private set; }
    public string UsuarioId { get; private set; } = string.Empty;
    public string NomeCompleto { get; private set; } = string.Empty;
    public string? Telefone { get; private set; }
    public bool Ativo { get; private set; }

    public static PerfilProfissional Criar(string usuarioId, string nomeCompleto, string? telefone) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = usuarioId,
            NomeCompleto = nomeCompleto,
            Telefone = telefone,
            Ativo = true
        };
}
