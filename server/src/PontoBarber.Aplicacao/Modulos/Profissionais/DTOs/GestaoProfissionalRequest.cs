namespace PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;

/// <summary>
/// Edição dos dados cadastrais do profissional (RF-PROF-005). Identificador,
/// usuário, cargo e situação nunca vêm do corpo da requisição.
/// </summary>
public sealed record AtualizarProfissionalRequest(string? NomeCompleto, string? Telefone);

/// <summary>
/// Ativação (<c>true</c>) ou desativação (<c>false</c>) do vínculo (RF-PROF-006).
/// </summary>
public sealed record AlterarStatusProfissionalRequest(bool? Ativo);

public sealed record ProfissionalResponse(
    Guid Id,
    string NomeCompleto,
    string? Telefone,
    string Situacao);
