using PontoBarber.Aplicacao.Modulos.Clientes.DTOs;

namespace PontoBarber.Api.Modulos.Clientes;

public sealed record ClienteContract(
    string Id,
    string NomeCompleto,
    string Email,
    string? Telefone);

public sealed record ListaClientesContract(
    IReadOnlyList<ClienteContract> Itens,
    int Total,
    int Pagina,
    int Tamanho);

public sealed record AtualizarPerfilClienteContract(
    string NomeCompleto,
    string Email,
    string? Telefone);

internal static class ClienteContracts
{
    public static ClienteContract DaResponse(ClienteResponse resposta) =>
        new(resposta.Id, resposta.NomeCompleto, resposta.Email, resposta.Telefone);

    public static ClienteContract DaResponse(ClientePerfilResponse resposta) =>
        new(resposta.Id, resposta.NomeCompleto, resposta.Email, resposta.Telefone);

    public static AtualizarPerfilClienteRequest ParaRequest(AtualizarPerfilClienteContract contrato) =>
        new(contrato.NomeCompleto, contrato.Email, contrato.Telefone);
}
