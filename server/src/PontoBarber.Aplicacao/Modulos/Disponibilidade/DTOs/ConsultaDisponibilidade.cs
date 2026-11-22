namespace PontoBarber.Aplicacao.Modulos.Disponibilidade.DTOs;

/// <summary>Horário livre calculado no fuso da barbearia.</summary>
public sealed record SlotDisponibilidade(
    Guid ProfissionalId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string StartsAtLocal);

public sealed record DisponibilidadeResponse(
    DateOnly Data,
    string FusoHorarioId,
    IReadOnlyList<SlotDisponibilidade> Slots,
    bool HasAvailability);
