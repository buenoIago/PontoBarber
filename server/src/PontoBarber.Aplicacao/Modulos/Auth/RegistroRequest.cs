namespace PontoBarber.Aplicacao.Auth;

/// <summary>
/// Cadastro de cliente: sem senha e sem confirmação (Tasks 3.2/3.3).
/// A identidade vem da sessão; o tipo é definido pelo endpoint.
/// </summary>
public sealed record RegistroClienteRequest(
    string? NomeCompleto,
    string? Email,
    string? Telefone);

/// <summary>Cadastro de barbeiro/proprietário: mantém e-mail e senha.</summary>
public sealed record RegistroProfissionalRequest(
    string? NomeCompleto,
    string? Email,
    string? Telefone,
    string? Senha,
    string? ConfirmarSenha);

public sealed record LoginRequest(string? Email, string? Senha);

public sealed record SolicitarOtpRequest(string? Email);

public sealed record VerificarOtpRequest(string? Email, string? Codigo);
