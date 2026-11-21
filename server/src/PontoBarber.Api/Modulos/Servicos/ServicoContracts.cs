using PontoBarber.Aplicacao.Modulos.Servicos.DTOs;

namespace PontoBarber.Api.Modulos.Servicos;

/// <summary>Contratos HTTP da criação e edição do catálogo (TDD §10.2).</summary>
public sealed record CriarServicoContract(string? Nome, int? DuracaoMinutos, decimal? Preco);

public sealed record AtualizarServicoContract(
    string? Nome,
    int? DuracaoMinutos,
    decimal? Preco,
    bool? Ativo);

public sealed record ServicoContract(
    Guid Id,
    Guid BarbeariaId,
    string Nome,
    int DuracaoMinutos,
    decimal? Preco,
    bool Ativo,
    bool EhModelo);

internal static class ServicoContracts
{
    public static CriarServicoRequest ParaRequest(CriarServicoContract contrato) =>
        new(contrato.Nome, contrato.DuracaoMinutos, contrato.Preco);

    public static AtualizarServicoRequest ParaRequest(AtualizarServicoContract contrato) =>
        new(contrato.Nome, contrato.DuracaoMinutos, contrato.Preco, contrato.Ativo);

    public static ServicoContract DaResponse(ServicoResponse resposta) =>
        new(
            resposta.Id,
            resposta.BarbeariaId,
            resposta.Nome,
            resposta.DuracaoMinutos,
            resposta.Preco,
            resposta.Ativo,
            resposta.EhModelo);
}
