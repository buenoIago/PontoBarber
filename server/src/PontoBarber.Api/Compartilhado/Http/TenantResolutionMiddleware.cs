using System.Security.Claims;
using PontoBarber.Api.Compartilhado.Erros;
using PontoBarber.Api.Compartilhado.Tenant;
using PontoBarber.Aplicacao.Compartilhado;

namespace PontoBarber.Api.Compartilhado.Http;

/// <summary>
/// Resolve o tenant a partir da rota e só o disponibiliza ao contexto após
/// confirmar o vínculo ativo do usuário autenticado.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string NomeParametroRota = "barbeariaId";

    public async Task InvokeAsync(HttpContext contexto, IResolutorVinculoTenant resolutorVinculo)
    {
        var routeValue = contexto.Request.RouteValues[NomeParametroRota]?.ToString();

        if (string.IsNullOrWhiteSpace(routeValue))
        {
            await next(contexto);
            return;
        }

        // Catálogo e consulta de horários são leituras públicas previstas na
        // SPEC/TDD; a resolução de tenant fica a cargo do caso de uso, que
        // filtra por BarbeariaId. Nenhuma outra rota é eximida.
        if (EhLeituraPublicaPermitida(contexto) ||
            EhAgendamentoPublico(contexto) ||
            EhAcaoPorAgendamento(contexto))
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
            await EscritorProblemDetails.EscreverAsync(
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

    private static bool EhLeituraPublicaPermitida(HttpContext contexto)
    {
        if (!HttpMethods.IsGet(contexto.Request.Method))
            return false;

        var partes = contexto.Request.Path.Value?.Trim('/').Split('/');
        if (partes is { Length: 5 } &&
            Igual(partes[0], "api") &&
            Igual(partes[1], "v1") &&
            Igual(partes[2], "barbearias") &&
            Guid.TryParse(partes[3], out _))
            return Igual(partes[4], "servicos") ||
                   Igual(partes[4], "disponibilidade") ||
                   // Leitura de apoio ao agendamento do cliente (Task 7.7). O
                   // caso de uso filtra pelo barbeariaId da rota e não expõe
                   // dados de gestão.
                   Igual(partes[4], "agendamento-publico");

        return false;
    }

    private static bool EhAgendamentoPublico(HttpContext contexto)
    {
        if (!HttpMethods.IsPost(contexto.Request.Method))
            return false;

        var partes = contexto.Request.Path.Value?.Trim('/').Split('/');
        return partes is { Length: 6 } &&
               Igual(partes[0], "api") &&
               Igual(partes[1], "v1") &&
               Igual(partes[2], "barbearias") &&
               Guid.TryParse(partes[3], out _) &&
               Igual(partes[4], "agendamentos") &&
               Igual(partes[5], "public");
    }

    /// <summary>
    /// Ações sobre um agendamento específico são autorizadas na aplicação
    /// pelo perfil e pela titularidade do agendamento ou da agenda. Clientes não têm vínculo
    /// com a barbearia, mas podem cancelar, remarcar e responder sugestões.
    /// </summary>
    private static bool EhAcaoPorAgendamento(HttpContext contexto)
    {
        var partes = contexto.Request.Path.Value?.Trim('/').Split('/');
        // A leitura de um agendamento específico tem 6 segmentos; as demais
        // ações (cancelar, sugestões, histórico...) têm 7 ou mais. A titularidade
        // é conferida no caso de uso, não aqui.
        return partes is { Length: >= 6 } &&
               Igual(partes[0], "api") &&
               Igual(partes[1], "v1") &&
               Igual(partes[2], "barbearias") &&
               Guid.TryParse(partes[3], out _) &&
               Igual(partes[4], "agendamentos") &&
               Guid.TryParse(partes[5], out _);
    }

    private static bool Igual(string? valor, string esperado) =>
        string.Equals(valor, esperado, StringComparison.OrdinalIgnoreCase);

    private static Task NegarAcessoAsync(HttpContext contexto) =>
        EscritorProblemDetails.EscreverAsync(
            contexto,
            StatusCodes.Status403Forbidden,
            CodigosErro.TenantNegado,
            "Acesso negado",
            "O usuário não possui vínculo com a barbearia solicitada.");
}
