using PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;
using PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;

namespace PontoBarber.Api.Modulos.Barbearia;

/// <summary>
/// Contratos HTTP do módulo de barbearia. Mantêm o payload da API separado
/// dos DTOs de caso de uso, preservando o formato JSON existente.
/// </summary>
public sealed record CriarBarbeariaContract(
    string? Nome,
    string? TelefoneComercial,
    string? EmailComercial,
    string? Cidade,
    string? Endereco,
    string? Instagram,
    string? FusoHorarioId);

/// <summary>
/// Edição permitida. Não inclui fuso horário, código de convite nem
/// identificadores: esses campos são imutáveis pelo cliente.
/// </summary>
public sealed record AtualizarBarbeariaContract(
    string? Nome,
    string? TelefoneComercial,
    string? EmailComercial,
    string? Cidade,
    string? Endereco,
    string? Instagram);

public sealed record EntrarBarbeariaContract(string? CodigoConvite);

public sealed record VinculoContract(
    Guid Id,
    Guid BarbeariaId,
    Guid ProfissionalId,
    string Cargo,
    string Situacao);

public sealed record BarbeariaContract(
    Guid Id,
    string Nome,
    string TelefoneComercial,
    string EmailComercial,
    string Cidade,
    string Endereco,
    string? Instagram,
    string? CodigoConvite,
    string FusoHorarioId);

internal static class BarbeariaContracts
{
    public static CriarBarbeariaRequest ParaRequest(CriarBarbeariaContract contrato) =>
        new(
            contrato.Nome,
            contrato.TelefoneComercial,
            contrato.EmailComercial,
            contrato.Cidade,
            contrato.Endereco,
            contrato.Instagram,
            contrato.FusoHorarioId);

    public static BarbeariaContract DaResponse(BarbeariaResponse resposta) =>
        new(
            resposta.Id,
            resposta.Nome,
            resposta.TelefoneComercial,
            resposta.EmailComercial,
            resposta.Cidade,
            resposta.Endereco,
            resposta.Instagram,
            resposta.CodigoConvite,
            resposta.FusoHorarioId);

    public static AtualizarBarbeariaRequest ParaRequest(AtualizarBarbeariaContract contrato) =>
        new(
            contrato.Nome,
            contrato.TelefoneComercial,
            contrato.EmailComercial,
            contrato.Cidade,
            contrato.Endereco,
            contrato.Instagram);

    public static EntrarBarbeariaRequest ParaRequest(EntrarBarbeariaContract contrato) =>
        new(contrato.CodigoConvite);

    public static VinculoContract DaResponse(VinculoResponse resposta) =>
        new(
            resposta.Id,
            resposta.BarbeariaId,
            resposta.ProfissionalId,
            resposta.Cargo,
            resposta.Situacao);
}
