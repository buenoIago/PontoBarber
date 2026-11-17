using System.Security.Claims;
using PontoBarber.Api.Tenant;
using PontoBarber.Application.Abstractions;

namespace PontoBarber.Api.Middleware;

/// <summary>
/// Resolve o tenant a partir da rota e só o disponibiliza ao contexto após
/// confirmar o vínculo ativo do usuário autenticado.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string RouteParameterName = "barbershopId";

    public async Task InvokeAsync(HttpContext context, ITenantMembershipResolver membershipResolver)
    {
        var routeValue = context.Request.RouteValues[RouteParameterName]?.ToString();

        if (string.IsNullOrWhiteSpace(routeValue))
        {
            await next(context);
            return;
        }

        if (!Guid.TryParse(routeValue, out var tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (context.User.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(userIdValue, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!await membershipResolver.IsActiveMemberAsync(userId, tenantId, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        context.Items[TenantContextKeys.TenantId] = tenantId;
        await next(context);
    }
}
