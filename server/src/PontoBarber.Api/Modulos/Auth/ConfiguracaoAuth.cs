using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using PontoBarber.Aplicacao.Auth;

namespace PontoBarber.Api.Modulos.Auth;

/// <summary>
/// Antiforgery, autorização por perfil e policies de tenant (TDD §11.2/§12.3).
/// </summary>
public static class ConfiguracaoAuth
{
    public const string CookieCsrf = "__Host-pb.csrf";
    public const string HeaderCsrf = "X-CSRF-TOKEN";

    public const string PolíticaCliente = "Cliente";
    public const string PolíticaProfissional = "Profissional";
    public const string PolíticaTenantMember = "TenantMember";
    public const string PolíticaTenantOwner = "TenantOwner";

    public static IServiceCollection AdicionarAuthApi(this IServiceCollection services)
    {
        services.AddAntiforgery(opcoes =>
        {
            opcoes.Cookie.Name = CookieCsrf;
            opcoes.Cookie.HttpOnly = false; // o cliente Angular lê o cookie e envia o header
            opcoes.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            opcoes.Cookie.SameSite = SameSiteMode.Lax;
            opcoes.Cookie.Path = "/";
            opcoes.HeaderName = HeaderCsrf;
        });

        services.AddAuthorization(opcoes =>
        {
            opcoes.AddPolicy(PolíticaCliente, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("UserType", ServicoConta.TipoCliente));
            opcoes.AddPolicy(PolíticaProfissional, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("UserType", ServicoConta.TipoBarbeiro));
            opcoes.AddPolicy(PolíticaTenantMember, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("TenantMember"));
            opcoes.AddPolicy(PolíticaTenantOwner, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("TenantOwner"));
        });

        return services;
    }
}
