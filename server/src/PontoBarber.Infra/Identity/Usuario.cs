using Microsoft.AspNetCore.Identity;

namespace PontoBarber.Infra.Identity;

/// <summary>
/// Usuário do Identity. Fica na Infra porque ADR-003 proíbe referência a
/// ASP.NET Core Identity no Domínio. Campos adicionais conforme TDD §8.
/// </summary>
public sealed class Usuario : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string? UserType { get; set; }
}
