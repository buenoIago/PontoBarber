using PontoBarber.Dominio.Modulos.Agendamentos;

namespace PontoBarber.Dominio.Modulos.Dashboard;

/// <summary>Compromisso futuro exibido no dashboard (RF-DASH-004).</summary>
public sealed record ProximoAgendamento(
    Guid AgendamentoId,
    DateTime IniciaEmUtc,
    string NomeCliente,
    string NomeProfissional,
    SituacaoAgendamento Situacao);
