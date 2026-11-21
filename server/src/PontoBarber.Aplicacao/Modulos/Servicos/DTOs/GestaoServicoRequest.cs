namespace PontoBarber.Aplicacao.Modulos.Servicos.DTOs;

/// <summary>Criação de serviço pelo proprietário (RF-SERV-002).</summary>
public sealed record CriarServicoRequest(string? Nome, int? DuracaoMinutos, decimal? Preco);

/// <summary>
/// Edição do catálogo. Campos ausentes preservam o valor atual. Nome e
/// disponibilidade só podem ser alterados pelo proprietário (Task 5.2).
/// </summary>
public sealed record AtualizarServicoRequest(
    string? Nome,
    int? DuracaoMinutos,
    decimal? Preco,
    bool? Ativo);

public sealed record ServicoResponse(
    Guid Id,
    Guid BarbeariaId,
    string Nome,
    int DuracaoMinutos,
    decimal? Preco,
    bool Ativo,
    bool EhModelo);
