using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace PontoBarber.Infra.Compartilhado.Identity;

/// <summary>
/// Aplica a duração por perfil a toda emissão do cookie <c>__Host-pb.auth</c>,
/// inclusive à renovação deslizante: o handler do cookie limpa
/// <c>IssuedUtc</c>/<c>ExpiresUtc</c> antes de disparar <c>OnSigningIn</c>, e
/// este evento os reconstrói com o prazo do perfil autenticado.
/// </summary>
internal sealed class ConfiguracaoSessaoCookie(TimeProvider relogio)
    : IPostConfigureOptions<CookieAuthenticationOptions>
{
    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        if (!string.Equals(name, IdentityConstants.ApplicationScheme, StringComparison.Ordinal))
            return;

        options.TimeProvider = relogio;
        options.Events ??= new CookieAuthenticationEvents();
        options.Events.OnSigningIn = contexto =>
        {
            PoliticaSessaoPorPerfil.Aplicar(contexto, relogio);
            return Task.CompletedTask;
        };
    }
}
