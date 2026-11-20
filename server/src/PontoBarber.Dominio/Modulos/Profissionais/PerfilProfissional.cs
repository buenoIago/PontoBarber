using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>
/// Perfil profissional vinculado a uma conta de acesso (TDD §8.1).
/// Entidade global: o perfil não pertence a uma barbearia específica.
/// </summary>
public sealed class PerfilProfissional : EntidadeBase<PerfilProfissional>
{
    private PerfilProfissional()
    {
    }

    public string UsuarioId { get; private set; } = string.Empty;
    public string NomeCompleto { get; private set; } = string.Empty;
    public string? Telefone { get; private set; }
    public bool Ativo { get; private set; }

    public static PerfilProfissional Criar(string usuarioId, string nomeCompleto, string? telefone) =>
        new()
        {
            UsuarioId = usuarioId,
            NomeCompleto = nomeCompleto,
            Telefone = telefone,
            Ativo = true
        };

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (string.IsNullOrWhiteSpace(UsuarioId))
            erros.Add(new ErroValidacao("usuarioId", "Campo obrigatório."));

        if (string.IsNullOrWhiteSpace(NomeCompleto))
            erros.Add(new ErroValidacao("nomeCompleto", "Campo obrigatório."));

        if (!string.IsNullOrWhiteSpace(Telefone) && Telefone.Count(char.IsDigit) < 10)
            erros.Add(new ErroValidacao("telefone", "Informe um telefone válido."));

        return erros;
    }

    /// <summary>
    /// Atualiza apenas os dados cadastrais do profissional (nome e telefone).
    /// Identificador, usuário, situação e demais estado não são alterados.
    /// </summary>
    public override void Atualizar(PerfilProfissional entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        NomeCompleto = entidadeAtualizada.NomeCompleto;
        Telefone = entidadeAtualizada.Telefone;
    }
}
