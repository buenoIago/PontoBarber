using System.Security.Claims;
using PontoBarber.Api.Erros;
using PontoBarber.Api.Tenant;
using PontoBarber.Aplicacao.Abstracoes;

namespace PontoBarber.Api.Middleware;

/// <summary>
/// Resolve o tenant a partir da rota e só o disponibiliza ao contexto após
/// confirmar o vínculo ativo do usuário autenticado.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string NomeParametroRota = "barbershopId";

    public async Task InvokeAsync(HttpContext contexto, IResolutorVinculoTenant resolutorVinculo)
    {
        var routeValue = contexto.Request.RouteValues[NomeParametroRota]?.ToString();

        if (string.IsNullOrWhiteSpace(routeValue))
        {
            await next(contexto);
            return;
        }

        if (!Guid.TryParse(routeValue, out var tenantId))
        {
            await NegarAcessoAsync(contexto);
            return;
        }

        var userIdValue = contexto.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (contexto.User.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(userIdValue, out var usuarioId))
        {
            await EscritorRespostaProblema.EscreverAsync(
                contexto,
                StatusCodes.Status401Unauthorized,
                CodigosErro.AutenticacaoCredenciaisInvalidas,
                "Não autenticado",
                "É necessário autenticar para acessar este recurso.");
            return;
        }

        if (!await resolutorVinculo.PossuiVinculoAtivoAsync(usuarioId, tenantId, contexto.RequestAborted))
        {
            await NegarAcessoAsync(contexto);
            return;
        }

        contexto.Items[ChavesContextoTenant.TenantId] = tenantId;
        await next(contexto);
    }

    private static Task NegarAcessoAsync(HttpContext contexto) =>
        EscritorRespostaProblema.EscreverAsync(
            contexto,
            StatusCodes.Status403Forbidden,
            CodigosErro.TenantNegado,
            "Acesso negado",
            "O usuário não possui vínculo com a barbearia solicitada.");
}
