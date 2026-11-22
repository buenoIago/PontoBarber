namespace PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;

/// <summary>Janela de disponibilidade recorrente do profissional.</summary>
public sealed record JanelaDisponibilidadeRequest(
    int DiaSemana,
    TimeOnly IniciaEm,
    TimeOnly TerminaEm);

public sealed record DisponibilidadeSemanalResponse(
    Guid Id,
    Guid ProfissionalId,
    int DiaSemana,
    TimeOnly IniciaEm,
    TimeOnly TerminaEm);
