using PontoBarber.Aplicacao.Modulos.Agendamentos.DTOs;

namespace PontoBarber.Api.Modulos.Agendamentos;

public sealed record CriarAgendamentoContract(
    Guid ProfissionalId,
    Guid ServicoId,
    DateTime IniciaEmUtc,
    string? Observacoes,
    string? ClienteId);

public sealed record AgendamentoContract(
    Guid Id,
    Guid BarbeariaId,
    Guid ProfissionalId,
    Guid ServicoId,
    DateTime IniciaEmUtc,
    DateTime TerminaEmUtc,
    string NomeServico,
    int DuracaoMinutos,
    decimal Preco,
    string Situacao,
    string Origem,
    DateTime? CanceladoEmUtc,
    string? CanceladoPorUsuarioId,
    string? MotivoCancelamento);

public sealed record CancelarAgendamentoContract(string? Motivo);

public sealed record RemarcarAgendamentoContract(DateTime IniciaEmUtc);

public sealed record SugerirHorarioContract(DateTime IniciaEmUtc, string Motivo);

public sealed record SugestaoContract(
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

public sealed record HistoricoContract(
    Guid Id,
    Guid AgendamentoId,
    string TipoEvento,
    string? AtorUsuarioId,
    DateTime OcorridoEmUtc,
    DateTime? InicioAnteriorUtc,
    DateTime? NovoInicioUtc,
    string? Detalhes);

public sealed record CancelamentoContract(
    Guid AgendamentoId,
    string Situacao,
    DateTime? CanceladoEmUtc,
    string? CanceladoPorUsuarioId,
    string? MotivoCancelamento,
    Guid HistoricoId);

internal static class AgendamentoContracts
{
    public static CriarAgendamentoRequest ParaRequest(CriarAgendamentoContract contrato) =>
        new(
            contrato.ProfissionalId,
            contrato.ServicoId,
            contrato.IniciaEmUtc,
            contrato.Observacoes,
            contrato.ClienteId);

    public static AgendamentoContract DaResponse(AgendamentoResponse resposta) =>
        new(
            resposta.Id,
            resposta.BarbeariaId,
            resposta.ProfissionalId,
            resposta.ServicoId,
            resposta.IniciaEmUtc,
            resposta.TerminaEmUtc,
            resposta.NomeServico,
            resposta.DuracaoMinutos,
            resposta.Preco,
            resposta.Situacao,
            resposta.Origem,
            resposta.CanceladoEmUtc,
            resposta.CanceladoPorUsuarioId,
            resposta.MotivoCancelamento);

    public static SugestaoContract DaResponse(SugestaoResponse resposta) =>
        new(
            resposta.Id,
            resposta.AgendamentoId,
            resposta.ProfissionalId,
            resposta.InicioSugeridoUtc,
            resposta.FimSugeridoUtc,
            resposta.Motivo,
            resposta.Situacao,
            resposta.ExpiraEmUtc,
            resposta.RespondidoEmUtc,
            resposta.AutorUsuarioId);

    public static HistoricoContract DaResponse(HistoricoResponse resposta) =>
        new(
            resposta.Id,
            resposta.AgendamentoId,
            resposta.TipoEvento,
            resposta.AtorUsuarioId,
            resposta.OcorridoEmUtc,
            resposta.InicioAnteriorUtc,
            resposta.NovoInicioUtc,
            resposta.Detalhes);

    public static CancelamentoContract DaResponse(CancelamentoResponse resposta) =>
        new(
            resposta.AgendamentoId,
            resposta.Situacao,
            resposta.CanceladoEmUtc,
            resposta.CanceladoPorUsuarioId,
            resposta.MotivoCancelamento,
            resposta.HistoricoId);
}
