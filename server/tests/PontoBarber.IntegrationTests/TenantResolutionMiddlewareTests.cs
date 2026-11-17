using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using PontoBarber.Api.Middleware;
using PontoBarber.Api.Tenant;
using PontoBarber.Application.Abstractions;

namespace PontoBarber.IntegrationTests;

public class TenantResolutionMiddlewareTests
{
    private static readonly Guid _userId = Guid.NewGuid();
    private static readonly Guid _tenantVinculado = Guid.NewGuid();
    private static readonly Guid _outroTenant = Guid.NewGuid();

    [Fact]
    public async Task TenantResolutionMiddleware_ComVinculoValido_UsaTenantDaRota()
    {
        var resolver = new StubTenantMembershipResolver((_userId, [_tenantVinculado]));
        var (context, tenantContext) = CriarContexto(_tenantVinculado, _userId, autenticado: true);
        var corpo = $"{{\"tenantId\":\"{_outroTenant}\"}}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(corpo));

        var proximoExecutado = false;
        var middleware = new TenantResolutionMiddleware(async ctx =>
        {
            proximoExecutado = true;
            using var leitor = new StreamReader(ctx.Request.Body);
            Assert.Contains(_outroTenant.ToString(), await leitor.ReadToEndAsync(), StringComparison.Ordinal);
            Assert.Equal(_tenantVinculado, tenantContext.TenantId);
        });

        await middleware.InvokeAsync(context, resolver);

        Assert.True(proximoExecutado);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task TenantResolutionMiddleware_SemVinculo_RetornaAcessoNegado()
    {
        var resolver = new StubTenantMembershipResolver();
        var (context, tenantContext) = CriarContexto(_tenantVinculado, _userId, autenticado: true);
        var proximoExecutado = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            proximoExecutado = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, resolver);

        Assert.False(proximoExecutado);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Null(tenantContext.TenantId);
    }

    [Fact]
    public async Task TenantResolutionMiddleware_ComIdentificadorDaRotaAlterado_RetornaAcessoNegado()
    {
        var resolver = new StubTenantMembershipResolver((_userId, [_tenantVinculado]));
        var (context, tenantContext) = CriarContexto(_outroTenant, _userId, autenticado: true);
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, resolver);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Null(tenantContext.TenantId);
    }

    [Fact]
    public async Task TenantResolutionMiddleware_ComTenantInexistente_RetornaAcessoNegado()
    {
        var resolver = new StubTenantMembershipResolver((_userId, [_tenantVinculado]));
        var inexistente = Guid.NewGuid();
        var (context, tenantContext) = CriarContexto(inexistente, _userId, autenticado: true);
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, resolver);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Null(tenantContext.TenantId);
    }

    [Fact]
    public async Task TenantResolutionMiddleware_SemAutenticacao_Retorna401()
    {
        var resolver = new StubTenantMembershipResolver((_userId, [_tenantVinculado]));
        var (context, tenantContext) = CriarContexto(_tenantVinculado, userId: null, autenticado: false);
        var proximoExecutado = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            proximoExecutado = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, resolver);

        Assert.False(proximoExecutado);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Null(tenantContext.TenantId);
    }

    [Fact]
    public async Task TenantResolutionMiddleware_ComIdentificadorInvalido_RetornaAcessoNegado()
    {
        var resolver = new StubTenantMembershipResolver((_userId, [_tenantVinculado]));
        var (context, tenantContext) = CriarContexto(_tenantVinculado, _userId, autenticado: true);
        context.Request.RouteValues[TenantResolutionMiddleware.RouteParameterName] = "nao-e-um-guid";
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, resolver);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Null(tenantContext.TenantId);
    }

    [Fact]
    public async Task TenantResolutionMiddleware_SemAutenticacao_Retorna401ComProblemDetails()
    {
        var resolver = new StubTenantMembershipResolver((_userId, [_tenantVinculado]));
        var (context, _) = CriarContexto(_tenantVinculado, userId: null, autenticado: false);
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, resolver);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        var corpo = await LerCorpoAsync(context);
        Assert.Equal(ErrorCodes.AuthInvalidCredentials, corpo.GetProperty("code").GetString());
        Assert.Equal(StatusCodes.Status401Unauthorized, corpo.GetProperty("status").GetInt32());
        Assert.Equal("trace-tenant-teste", corpo.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task TenantResolutionMiddleware_SemVinculo_Retorna403ComProblemDetails()
    {
        var resolver = new StubTenantMembershipResolver();
        var (context, _) = CriarContexto(_tenantVinculado, _userId, autenticado: true);
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, resolver);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        var corpo = await LerCorpoAsync(context);
        Assert.Equal(ErrorCodes.TenantForbidden, corpo.GetProperty("code").GetString());
        Assert.Equal(StatusCodes.Status403Forbidden, corpo.GetProperty("status").GetInt32());
        Assert.Equal("trace-tenant-teste", corpo.GetProperty("traceId").GetString());
    }

    private static async Task<JsonElement> LerCorpoAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var leitor = new StreamReader(context.Response.Body, leaveOpen: true);
        using var documento = JsonDocument.Parse(await leitor.ReadToEndAsync());
        return documento.RootElement.Clone();
    }

    private static (DefaultHttpContext Context, HttpTenantContext TenantContext) CriarContexto(
        Guid tenantId,
        Guid? userId,
        bool autenticado)
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-tenant-teste";
        context.Request.RouteValues[TenantResolutionMiddleware.RouteParameterName] = tenantId.ToString();
        context.Response.Body = new MemoryStream();

        if (autenticado)
        {
            var identidade = new ClaimsIdentity(
                userId is null ? [] : [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
                "Teste");
            context.User = new ClaimsPrincipal(identidade);
        }

        var accessor = new HttpContextAccessor { HttpContext = context };
        return (context, new HttpTenantContext(accessor));
    }

    private sealed class StubTenantMembershipResolver : ITenantMembershipResolver
    {
        private readonly Dictionary<Guid, HashSet<Guid>> _vinculos;

        public StubTenantMembershipResolver(params (Guid UserId, Guid[] Tenants)[] vinculos)
        {
            _vinculos = vinculos.ToDictionary(
                vinculo => vinculo.UserId,
                vinculo => new HashSet<Guid>(vinculo.Tenants));
        }

        public Task<bool> IsActiveMemberAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(_vinculos.TryGetValue(userId, out var tenants) && tenants.Contains(tenantId));
    }
}
