namespace PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;

/// <summary>
/// Dados iniciais da barbearia. O proprietário vem da sessão autenticada,
/// nunca do corpo da requisição (ADR-029).
/// </summary>
public sealed record CriarBarbeariaRequest(
    string? Nome,
    string? TelefoneComercial,
    string? EmailComercial,
    string? Cidade,
    string? Endereco,
    string? Instagram,
    string? FusoHorarioId);

/// <summary>
/// Edição permitida: somente os dados cadastrais. Fuso horário, código de
/// convite, proprietário e identificadores não são editáveis (Task 4.2).
/// </summary>
public sealed record AtualizarBarbeariaRequest(
    string? Nome,
    string? TelefoneComercial,
    string? EmailComercial,
    string? Cidade,
    string? Endereco,
    string? Instagram);

public sealed record BarbeariaResponse(
    Guid Id,
    string Nome,
    string TelefoneComercial,
    string EmailComercial,
    string Cidade,
    string Endereco,
    string? Instagram,
    string? CodigoConvite,
    string FusoHorarioId);
