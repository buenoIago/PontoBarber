using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using PontoBarber.WebApp.Middleware;
using PontoBarber.WebApp.Tenant;
using PontoBarber.Aplicacao.Abstracoes;

namespace PontoBarber.Testes.Integracao;

public class TestesMiddlewareResolucaoTenant
{
    private static readonly Guid _userId = Guid.NewGuid();
    private static readonly Guid _tenantVinculado = Guid.NewGuid();
    private static readonly Guid _outroTenant = Guid.NewGuid();

    [Fact]
    public async Task MiddlewareResolucaoTenant_ComVinculoValido_UsaTenantDaRota()
    {
        var resolver = new ResolutorVinculoTenantFalso((_userId, [_tenantVinculado]));
        var (contexto, contextoTenant) = CriarContexto(_tenantVinculado, _userId, autenticado: true);
        var corpo = $"{{\"tenantId\":\"{_outroTenant}\"}}";
        contexto.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(corpo));

        var proximoExecutado = false;
        var middleware = new TenantResolutionMiddleware(async ctx =>
        {
            proximoExecutado = true;
            using var leitor = new StreamReader(ctx.Request.Body);
            Assert.Contains(_outroTenant.ToString(), await leitor.ReadToEndAsync(), StringComparison.Ordinal);
            Assert.Equal(_tenantVinculado, contextoTenant.TenantId);
        });

        await middleware.InvokeAsync(contexto, resolver);

        Assert.True(proximoExecutado);
        Assert.Equal(StatusCodes.Status200OK, contexto.Response.StatusCode);
    }

    [Fact]
    public async Task MiddlewareResolucaoTenant_SemVinculo_RetornaAcessoNegado()
    {
        var resolver = new ResolutorVinculoTenantFalso();
        var (contexto, contextoTenant) = CriarContexto(_tenantVinculado, _userId, autenticado: true);
        var proximoExecutado = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            proximoExecutado = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(contexto, resolver);

        Assert.False(proximoExecutado);
        Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
        Assert.Null(contextoTenant.TenantId);
    }

    [Fact]
    public async Task MiddlewareResolucaoTenant_ComIdentificadorDaRotaAlterado_RetornaAcessoNegado()
    {
        var resolver = new ResolutorVinculoTenantFalso((_userId, [_tenantVinculado]));
        var (contexto, contextoTenant) = CriarContexto(_outroTenant, _userId, autenticado: true);
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(contexto, resolver);

        Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
        Assert.Null(contextoTenant.TenantId);
    }

    [Fact]
    public async Task MiddlewareResolucaoTenant_ComTenantInexistente_RetornaAcessoNegado()
    {
        var resolver = new ResolutorVinculoTenantFalso((_userId, [_tenantVinculado]));
        var inexistente = Guid.NewGuid();
        var (contexto, contextoTenant) = CriarContexto(inexistente, _userId, autenticado: true);
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(contexto, resolver);

        Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
        Assert.Null(contextoTenant.TenantId);
    }

    [Fact]
    public async Task MiddlewareResolucaoTenant_SemAutenticacao_Retorna401()
    {
        var resolver = new ResolutorVinculoTenantFalso((_userId, [_tenantVinculado]));
        var (contexto, contextoTenant) = CriarContexto(_tenantVinculado, usuarioId: null, autenticado: false);
        var proximoExecutado = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            proximoExecutado = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(contexto, resolver);

        Assert.False(proximoExecutado);
        Assert.Equal(StatusCodes.Status401Unauthorized, contexto.Response.StatusCode);
        Assert.Null(contextoTenant.TenantId);
    }

    [Fact]
    public async Task MiddlewareResolucaoTenant_ComIdentificadorInvalido_RetornaAcessoNegado()
    {
        var resolver = new ResolutorVinculoTenantFalso((_userId, [_tenantVinculado]));
        var (contexto, contextoTenant) = CriarContexto(_tenantVinculado, _userId, autenticado: true);
        contexto.Request.RouteValues[TenantResolutionMiddleware.NomeParametroRota] = "nao-e-um-guid";
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(contexto, resolver);

        Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
        Assert.Null(contextoTenant.TenantId);
    }

    [Fact]
    public async Task MiddlewareResolucaoTenant_SemAutenticacao_Retorna401ComProblemDetails()
    {
        var resolver = new ResolutorVinculoTenantFalso((_userId, [_tenantVinculado]));
        var (contexto, _) = CriarContexto(_tenantVinculado, usuarioId: null, autenticado: false);
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(contexto, resolver);

        Assert.Equal(StatusCodes.Status401Unauthorized, contexto.Response.StatusCode);
        Assert.Equal("application/problem+json", contexto.Response.ContentType);
        var corpo = await LerCorpoAsync(contexto);
        Assert.Equal(CodigosErro.AutenticacaoCredenciaisInvalidas, corpo.GetProperty("code").GetString());
        Assert.Equal(StatusCodes.Status401Unauthorized, corpo.GetProperty("status").GetInt32());
        Assert.Equal("trace-tenant-teste", corpo.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task MiddlewareResolucaoTenant_SemVinculo_Retorna403ComProblemDetails()
    {
        var resolver = new ResolutorVinculoTenantFalso();
        var (contexto, _) = CriarContexto(_tenantVinculado, _userId, autenticado: true);
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(contexto, resolver);

        Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
        Assert.Equal("application/problem+json", contexto.Response.ContentType);
        var corpo = await LerCorpoAsync(contexto);
        Assert.Equal(CodigosErro.TenantNegado, corpo.GetProperty("code").GetString());
        Assert.Equal(StatusCodes.Status403Forbidden, corpo.GetProperty("status").GetInt32());
        Assert.Equal("trace-tenant-teste", corpo.GetProperty("traceId").GetString());
    }

    private static async Task<JsonElement> LerCorpoAsync(DefaultHttpContext contexto)
    {
        contexto.Response.Body.Position = 0;
        using var leitor = new StreamReader(contexto.Response.Body, leaveOpen: true);
        using var documento = JsonDocument.Parse(await leitor.ReadToEndAsync());
        return documento.RootElement.Clone();
    }

    private static (DefaultHttpContext Context, ContextoTenantHttp TenantContext) CriarContexto(
        Guid tenantId,
        Guid? usuarioId,
        bool autenticado)
    {
        var contexto = new DefaultHttpContext();
        contexto.TraceIdentifier = "trace-tenant-teste";
        contexto.Request.RouteValues[TenantResolutionMiddleware.NomeParametroRota] = tenantId.ToString();
        contexto.Response.Body = new MemoryStream();

        if (autenticado)
        {
            var identidade = new ClaimsIdentity(
                usuarioId is null ? [] : [new Claim(ClaimTypes.NameIdentifier, usuarioId.Value.ToString())],
                "Teste");
            contexto.User = new ClaimsPrincipal(identidade);
        }

        var accessor = new HttpContextAccessor { HttpContext = contexto };
        return (contexto, new ContextoTenantHttp(accessor));
    }

    private sealed class ResolutorVinculoTenantFalso : IResolutorVinculoTenant
    {
        private readonly Dictionary<Guid, HashSet<Guid>> _vinculos;

        public ResolutorVinculoTenantFalso(params (Guid UserId, Guid[] Tenants)[] vinculos)
        {
            _vinculos = vinculos.ToDictionary(
                vinculo => vinculo.UserId,
                vinculo => new HashSet<Guid>(vinculo.Tenants));
        }

        public Task<bool> PossuiVinculoAtivoAsync(Guid usuarioId, Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(_vinculos.TryGetValue(usuarioId, out var tenants) && tenants.Contains(tenantId));
    }
}
