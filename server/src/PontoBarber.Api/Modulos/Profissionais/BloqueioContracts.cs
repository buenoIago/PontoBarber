using PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;

namespace PontoBarber.Api.Modulos.Profissionais;

public sealed record CriarBloqueioContract(
    DateTime IniciaEmUtc,
    DateTime TerminaEmUtc,
    string? Motivo);

public sealed record BloqueioContract(
    Guid Id,
    Guid ProfissionalId,
    DateTime IniciaEmUtc,
    DateTime TerminaEmUtc,
    string? Motivo);

internal static class BloqueioContracts
{
    public static CriarBloqueioRequest ParaRequest(CriarBloqueioContract contrato) =>
        new(contrato.IniciaEmUtc, contrato.TerminaEmUtc, contrato.Motivo);

    public static BloqueioContract DaResponse(BloqueioResponse resposta) =>
        new(
            resposta.Id,
            resposta.ProfissionalId,
            resposta.IniciaEmUtc,
            resposta.TerminaEmUtc,
            resposta.Motivo);
}
