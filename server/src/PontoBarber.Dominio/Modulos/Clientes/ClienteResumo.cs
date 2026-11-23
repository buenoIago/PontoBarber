namespace PontoBarber.Dominio.Modulos.Clientes;

/// <summary>
/// Cliente derivado dos agendamentos da barbearia. A identidade já é
/// representada pelo usuário do Identity; não existe PerfilCliente duplicado.
/// </summary>
public sealed record ClienteResumo(
    string UsuarioId,
    string NomeCompleto,
    string Email,
    string? Telefone);
