namespace PontoBarber.Aplicacao.Modulos.Dashboard.DTOs;

public sealed record ProximoAgendamentoResponse(
    Guid Id,
    DateTime IniciaEmUtc,
    string NomeCliente,
    string NomeProfissional,
    string Situacao);

public sealed record DashboardResponse(
    int Hoje,
    int EstaSemana,
    int Cancelamentos,
    decimal RecebidoNoMes,
    IReadOnlyList<ProximoAgendamentoResponse> Proximos);
