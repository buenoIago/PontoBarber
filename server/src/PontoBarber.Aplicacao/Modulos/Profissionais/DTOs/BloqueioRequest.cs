namespace PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;

/// <summary>Período em UTC; o tenant e o profissional vêm da rota.</summary>
public sealed record CriarBloqueioRequest(
    DateTime IniciaEmUtc,
    DateTime TerminaEmUtc,
    string? Motivo);

public sealed record BloqueioResponse(
    Guid Id,
    Guid ProfissionalId,
    DateTime IniciaEmUtc,
    DateTime TerminaEmUtc,
    string? Motivo);
