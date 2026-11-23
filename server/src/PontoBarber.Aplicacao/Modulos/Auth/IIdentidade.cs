namespace PontoBarber.Aplicacao.Auth;

/// <summary>
/// Porta de identidade. A direção da dependência permite que a Aplicação
/// não conheça ASP.NET Core Identity (ADR-003/ADR-005).
/// </summary>
public interface IIdentidade
{
    Task<string> CriarUsuarioAsync(DadosRegistro dados, string tipoUsuario, CancellationToken cancellationToken);

    Task<ResultadoAutenticacao> AutenticarAsync(string email, string senha, CancellationToken cancellationToken);

    Task<DadosConta?> ObterContaAsync(string usuarioId, CancellationToken cancellationToken);

    Task EncerrarSessaoAsync(CancellationToken cancellationToken);

    Task<ResultadoAtualizacaoPerfil> AtualizarPerfilAsync(
        string usuarioId,
        string nomeCompleto,
        string email,
        string? telefone,
        CancellationToken cancellationToken);

    Task<ResultadoAlteracaoSenha> AlterarSenhaAsync(
        string usuarioId,
        string senhaAtual,
        string novaSenha,
        CancellationToken cancellationToken);
}

public sealed record DadosRegistro(
    string NomeCompleto,
    string Email,
    string Telefone,
    string Senha);

public sealed record DadosConta(
    string Id,
    string NomeCompleto,
    string Email,
    string? Telefone,
    string Tipo);

public sealed record AlterarSenhaRequest(string? SenhaAtual, string? NovaSenha, string? ConfirmarNovaSenha);

public enum ResultadoAutenticacao
{
    Sucesso,
    CredenciaisInvalidas,
    Bloqueado
}

public enum ResultadoAlteracaoSenha
{
    Sucesso,
    SenhaAtualIncorreta,
    SenhaNovaInvalida,
    UsuarioNaoEncontrado
}

public enum ResultadoAtualizacaoPerfil
{
    Sucesso,
    UsuarioNaoEncontrado,
    EmailDuplicado,
    Falha
}
