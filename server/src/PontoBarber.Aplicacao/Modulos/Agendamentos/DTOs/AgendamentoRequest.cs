namespace PontoBarber.Aplicacao.Modulos.Agendamentos.DTOs;

/// <summary>
/// Entrada de criação. Preço, duração e término nunca são aceitos do cliente:
/// vêm do serviço no momento da confirmação.
/// </summary>
public sealed record CriarAgendamentoRequest(
    Guid ProfissionalId,
    Guid ServicoId,
    DateTime IniciaEmUtc,
    string? Observacoes,
    string? ClienteId = null);

public sealed record AgendamentoResponse(
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
