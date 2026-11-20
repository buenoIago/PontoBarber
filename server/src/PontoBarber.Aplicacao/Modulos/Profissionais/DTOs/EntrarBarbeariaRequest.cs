namespace PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;

/// <summary>
/// Entrada do profissional em uma barbearia por código de convite.
/// Cargo e situação são definidos pelo domínio, nunca pelo cliente.
/// </summary>
public sealed record EntrarBarbeariaRequest(string? CodigoConvite);

public sealed record VinculoResponse(
    Guid Id,
    Guid BarbeariaId,
    Guid ProfissionalId,
    string Cargo,
    string Situacao);
