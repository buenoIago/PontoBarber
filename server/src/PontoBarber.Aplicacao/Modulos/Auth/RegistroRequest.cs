namespace PontoBarber.Aplicacao.Auth;

/// <summary>
/// Dados de cadastro. Não contém tipo de usuário: ele é definido pelo endpoint
/// e pelo caso de uso, nunca pelo corpo da requisição.
/// </summary>
public sealed record RegistroRequest(
    string? NomeCompleto,
    string? Email,
    string? Telefone,
    string? Senha,
    string? ConfirmarSenha);

public sealed record LoginRequest(string? Email, string? Senha);
