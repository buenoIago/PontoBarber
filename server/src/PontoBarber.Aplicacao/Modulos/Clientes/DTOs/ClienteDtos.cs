namespace PontoBarber.Aplicacao.Modulos.Clientes.DTOs;

public sealed record ClienteResponse(
    string Id,
    string NomeCompleto,
    string Email,
    string? Telefone);

public sealed record ListaClientesResponse(
    IReadOnlyList<ClienteResponse> Itens,
    int Total,
    int Pagina,
    int Tamanho);

public sealed record ClientePerfilResponse(
    string Id,
    string NomeCompleto,
    string Email,
    string? Telefone);

public sealed record AtualizarPerfilClienteRequest(
    string NomeCompleto,
    string Email,
    string? Telefone);
