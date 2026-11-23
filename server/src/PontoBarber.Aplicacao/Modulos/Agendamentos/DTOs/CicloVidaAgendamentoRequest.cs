namespace PontoBarber.Aplicacao.Modulos.Agendamentos.DTOs;

public sealed record CancelarAgendamentoRequest(string? Motivo);

public sealed record RemarcarAgendamentoRequest(DateTime IniciaEmUtc);

public sealed record SugerirHorarioRequest(DateTime IniciaEmUtc, string Motivo);

public sealed record RegistrarPagamentoRequest(decimal Valor, string Metodo);

public sealed record SugestaoResponse(
    Guid Id,
    Guid AgendamentoId,
    Guid ProfissionalId,
    DateTime InicioSugeridoUtc,
    DateTime FimSugeridoUtc,
    string Motivo,
    string Situacao,
    DateTime ExpiraEmUtc,
    DateTime? RespondidoEmUtc,
    string AutorUsuarioId);

public sealed record HistoricoResponse(
    Guid Id,
    Guid AgendamentoId,
    string TipoEvento,
    string? AtorUsuarioId,
    DateTime OcorridoEmUtc,
    DateTime? InicioAnteriorUtc,
    DateTime? NovoInicioUtc,
    string? Detalhes);

public sealed record CancelamentoResponse(
    Guid AgendamentoId,
    string Situacao,
    DateTime? CanceladoEmUtc,
    string? CanceladoPorUsuarioId,
    string? MotivoCancelamento,
    Guid HistoricoId);
