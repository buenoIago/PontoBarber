namespace PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;

/// <summary>
/// Dados mínimos necessários para o cliente montar um agendamento público:
/// identificação/fuso da barbearia e profissionais com vínculo ativo.
/// Não expõe telefone, cargo, código de convite nem dados de outros tenants.
/// </summary>
public sealed record ProfissionalDisponivelResponse(Guid Id, string NomeCompleto);

public sealed record DadosAgendamentoPublicoResponse(
    Guid BarbeariaId,
    string NomeBarbearia,
    string FusoHorarioId,
    IReadOnlyList<ProfissionalDisponivelResponse> Profissionais);
