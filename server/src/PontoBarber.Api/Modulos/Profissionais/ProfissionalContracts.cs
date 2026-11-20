using PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;

namespace PontoBarber.Api.Modulos.Profissionais;

/// <summary>
/// Contratos HTTP da gestão de profissionais. O identificador do vínculo e o
/// tenant vêm da rota; cargo, situação e conta nunca são recebidos do cliente.
/// </summary>
public sealed record AtualizarProfissionalContract(string? NomeCompleto, string? Telefone);

public sealed record AlterarStatusProfissionalContract(bool? Ativo);

public sealed record ProfissionalContract(
    Guid Id,
    string NomeCompleto,
    string? Telefone,
    string Situacao);

internal static class ProfissionalContracts
{
    public static AtualizarProfissionalRequest ParaRequest(AtualizarProfissionalContract contrato) =>
        new(contrato.NomeCompleto, contrato.Telefone);

    public static AlterarStatusProfissionalRequest ParaRequest(AlterarStatusProfissionalContract contrato) =>
        new(contrato.Ativo);

    public static ProfissionalContract DaResponse(ProfissionalResponse resposta) =>
        new(resposta.Id, resposta.NomeCompleto, resposta.Telefone, resposta.Situacao);
}
