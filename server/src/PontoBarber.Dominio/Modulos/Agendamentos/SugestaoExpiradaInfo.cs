namespace PontoBarber.Dominio.Modulos.Agendamentos;

/// <summary>Contexto mínimo de uma sugestão expirada para notificar os envolvidos.</summary>
public sealed record SugestaoExpiradaInfo(
    Guid SugestaoId,
    Guid BarbeariaId,
    Guid AgendamentoId,
    string ClienteId,
    Guid ProfissionalId,
    DateTime InicioSugeridoUtc);
