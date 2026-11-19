using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Infra.Compartilhado.Identity;

/// <summary>
/// Inclui o tipo do usuário nos claims para as policies de perfil (TDD §11.2).
/// </summary>
public sealed class ClaimsUsuario(
    UserManager<Usuario> usuarios,
    IOptions<IdentityOptions> opcoes) : UserClaimsPrincipalFactory<Usuario>(usuarios, opcoes)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario usuario)
    {
        var identidade = await base.GenerateClaimsAsync(usuario);

        if (!string.IsNullOrWhiteSpace(usuario.UserType))
            identidade.AddClaim(new Claim("UserType", usuario.UserType));

        return identidade;
    }
}
