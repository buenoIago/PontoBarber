using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Infra.Compartilhado.Identity;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Infra.Identity;

/// <summary>
/// Configura Identity, política de senha e cookie de autenticação conforme
/// ADR-005 e TDD §11.1. Não registra login, cadastro, antiforgery nem autorização.
/// </summary>
public static class DependencyInjection
{
    public const string NomeCookie = "__Host-pb.auth";

    public static IServiceCollection AddIdentityPontoBarber(this IServiceCollection services)
    {
        services.AddIdentity<Usuario, IdentityRole>(opcoes =>
            {
                opcoes.Password.RequiredLength = 8;
                opcoes.Password.RequiredUniqueChars = 1;
                opcoes.Password.RequireDigit = true;
                opcoes.Password.RequireLowercase = true;
                opcoes.Password.RequireUppercase = false;
                opcoes.Password.RequireNonAlphanumeric = false;

                opcoes.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                opcoes.Lockout.MaxFailedAccessAttempts = 5;
                opcoes.Lockout.AllowedForNewUsers = true;

                opcoes.User.RequireUniqueEmail = true;
                opcoes.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ContextoPontoBarber>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<ClaimsUsuario>();

        // PBKDF2-HMAC-SHA512 com 210.000 iterações (TDD §11.1 / RNF-SEGURANCA-001).
        services.Configure<PasswordHasherOptions>(opcoes => opcoes.IterationCount = 210_000);

        services.ConfigureApplicationCookie(opcoes =>
        {
            opcoes.Cookie.Name = NomeCookie;
            opcoes.Cookie.HttpOnly = true;
            opcoes.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            opcoes.Cookie.SameSite = SameSiteMode.Lax;
            opcoes.Cookie.Path = "/";
            opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
            opcoes.SlidingExpiration = true;
        });

        return services;
    }
}
