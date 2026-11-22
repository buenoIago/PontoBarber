using PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;

namespace PontoBarber.Api.Modulos.Barbearia;

public sealed record DiaFuncionamentoContract(
    int DiaSemana,
    TimeOnly? AbreAs,
    TimeOnly? FechaAs,
    bool Fechado);

internal static class HorarioFuncionamentoContracts
{
    public static IReadOnlyList<DiaFuncionamentoRequest> ParaRequest(
        IReadOnlyList<DiaFuncionamentoContract> contratos) =>
        contratos
            .Select(contrato => new DiaFuncionamentoRequest(
                contrato.DiaSemana, contrato.AbreAs, contrato.FechaAs, contrato.Fechado))
            .ToList();
}
