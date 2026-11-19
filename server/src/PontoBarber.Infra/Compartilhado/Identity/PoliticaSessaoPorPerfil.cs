using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PontoBarber.Aplicacao.Auth;

namespace PontoBarber.Infra.Compartilhado.Identity;

/// <summary>
/// Duração da sessão por perfil (SPEC §1 e TDD §11.1): clientes têm 60 dias
/// de inatividade; barbeiros e proprietários têm 30 dias. Proprietário não é
/// um <c>UserType</c> separado — é um <c>Professional</c> com vínculo de
/// propriedade — e por isso compartilha o prazo dos profissionais.
/// </summary>
internal static class PoliticaSessaoPorPerfil
{
    public static readonly TimeSpan Cliente = TimeSpan.FromDays(60);
    public static readonly TimeSpan Profissional = TimeSpan.FromDays(30);

    /// <summary>
    /// Define o prazo na emissão e em cada renovação deslizante do cookie.
    /// Como <see cref="AuthenticationProperties.ExpiresUtc"/> é sempre
    /// preenchido aqui, o valor padrão de <c>ExpireTimeSpan</c> nunca limita
    /// a sessão — não existe limite absoluto de duração.
    /// </summary>
    public static void Aplicar(CookieSigningInContext contexto, TimeProvider relogio)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(relogio);

        var tipo = contexto.Principal?.FindFirstValue("UserType");
        var duracao = tipo switch
        {
            ServicoConta.TipoCliente => Cliente,
            ServicoConta.TipoBarbeiro => Profissional,
            _ => Profissional
        };

        var agora = relogio.GetUtcNow();
        contexto.Properties.IssuedUtc = agora;
        contexto.Properties.ExpiresUtc = agora.Add(duracao);
        contexto.Properties.IsPersistent = true;
        contexto.Properties.AllowRefresh = true;
    }
}
