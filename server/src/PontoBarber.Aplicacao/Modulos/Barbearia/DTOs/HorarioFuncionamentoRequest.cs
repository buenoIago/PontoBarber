namespace PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;

/// <summary>Configuração de um dia da semana (TDD §10.2).</summary>
public sealed record DiaFuncionamentoRequest(
    int DiaSemana,
    TimeOnly? AbreAs,
    TimeOnly? FechaAs,
    bool Fechado);

public sealed record DiaFuncionamentoResponse(
    int DiaSemana,
    TimeOnly? AbreAs,
    TimeOnly? FechaAs,
    bool Fechado);
