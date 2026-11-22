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
    string Origem);

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
            resposta.Origem);
}
