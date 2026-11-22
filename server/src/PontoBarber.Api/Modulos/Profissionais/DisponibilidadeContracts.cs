using PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;

namespace PontoBarber.Api.Modulos.Profissionais;

public sealed record JanelaDisponibilidadeContract(
    int DiaSemana,
    TimeOnly IniciaEm,
    TimeOnly TerminaEm);

internal static class DisponibilidadeContracts
{
    public static IReadOnlyList<JanelaDisponibilidadeRequest> ParaRequest(
        IReadOnlyList<JanelaDisponibilidadeContract> contratos) =>
        contratos
            .Select(contrato => new JanelaDisponibilidadeRequest(
                contrato.DiaSemana, contrato.IniciaEm, contrato.TerminaEm))
            .ToList();
}
