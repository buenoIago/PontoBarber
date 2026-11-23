using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Task 4.5: o proprietário possui perfil e vínculo profissionais próprios,
/// sem duplicar conta/perfil, e pode atuar como administrador e como
/// profissional com acesso autorizado à própria agenda.
/// </summary>
[TestClass]
public sealed class TestesVinculoProprietario
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task CriarBarbearia_GeraVinculoDeProprietarioAtivoNaBarbeariaCorreta()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        var usuarioId = await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var perfil = await contexto.Set<PerfilProfissional>().SingleAsync(item => item.UsuarioId == usuarioId.ToString());
        var vinculo = await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters().SingleAsync(item => item.BarbeariaId == barbearia.Id);

        Assert.AreEqual(usuarioId.ToString(), perfil.UsuarioId);
        Assert.AreEqual(perfil.Id, vinculo.ProfissionalId);
        Assert.AreEqual(perfil.Id, (await contexto.Set<Barbearia>().SingleAsync()).IdProfissionalProprietario);
        Assert.AreEqual(CargoProfissional.Proprietario, vinculo.Cargo);
        Assert.AreEqual(StatusVinculo.Ativo, vinculo.Situacao);
    }

    [TestMethod]
    public async Task CriarBarbearia_ComPerfilJaExistente_ReaproveitaPerfilEVinculo()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        var usuarioB = await RegistrarEEntrarAsync(donoB);
        var entrada = await EnviarComAntiforgeryAsync(
            donoB, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbeariaA.CodigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, entrada.StatusCode);

        var barbeariaB = await CriarBarbeariaAsync(donoB);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var perfis = await contexto.Set<PerfilProfissional>()
            .CountAsync(item => item.UsuarioId == usuarioB.ToString());
        Assert.AreEqual(1, perfis);

        var perfilB = await contexto.Set<PerfilProfissional>()
            .SingleAsync(item => item.UsuarioId == usuarioB.ToString());
        var vinculos = await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters().Where(item => item.ProfissionalId == perfilB.Id).ToListAsync();
        Assert.AreEqual(2, vinculos.Count);
        Assert.AreEqual(1, vinculos.Count(item => item.BarbeariaId == barbeariaA.Id && item.Cargo == CargoProfissional.Barbeiro));
        Assert.AreEqual(1, vinculos.Count(item => item.BarbeariaId == barbeariaB.Id && item.Cargo == CargoProfissional.Proprietario));
        Assert.IsTrue(vinculos.All(item => item.Situacao == StatusVinculo.Ativo));

        // Permissão administrativa na própria barbearia permanece disponível.
        var edicao = await EnviarComAntiforgeryAsync(
            donoB, HttpMethod.Put, $"/api/v1/barbearias/{barbeariaB.Id}",
            new
            {
                nome = "Barbearia do Dono B",
                telefoneComercial = "(11) 99999-9999",
                emailComercial = "contato@barbearia.com",
                cidade = "São Paulo",
                endereco = "Rua Nova, 200",
                instagram = "@barbeariab"
            });
        Assert.AreEqual(HttpStatusCode.OK, edicao.StatusCode);
    }

    [TestMethod]
    public async Task Proprietario_AtuaComoProfissionalComAcessoAutorizadoAPropriaAgenda()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        var usuarioA = await RegistrarEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoB);
        var barbeariaB = await CriarBarbeariaAsync(donoB);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var perfilA = await contexto.Set<PerfilProfissional>().SingleAsync(item => item.UsuarioId == usuarioA.ToString());
        var barbeariaBPersistida = await contexto.Set<Barbearia>().SingleAsync(item => item.Id == barbeariaB.Id);
        var perfilB = await contexto.Set<PerfilProfissional>()
            .SingleAsync(item => item.Id == barbeariaBPersistida.IdProfissionalProprietario);

        var servico = escopo.ServiceProvider.GetRequiredService<ServicoProfissional>();

        var propriaAgenda = await servico.ObterVinculoParaAgendaAsync(
            barbeariaA.Id, perfilA.Id, usuarioA.ToString(), CancellationToken.None);
        Assert.AreEqual(perfilA.Id, propriaAgenda.ProfissionalId);
        Assert.AreEqual("Proprietario", propriaAgenda.Cargo);
        Assert.AreEqual("Ativo", propriaAgenda.Situacao);

        var agendaDeOutroTenant = await Assert.ThrowsExactlyAsync<ExcecaoAutorizacaoAplicacao>(
            () => servico.ObterVinculoParaAgendaAsync(
                barbeariaB.Id, perfilB.Id, usuarioA.ToString(), CancellationToken.None));
        Assert.AreEqual(CodigosErro.TenantNegado, agendaDeOutroTenant.Codigo);

        // O isolamento persiste: cada barbearia mantém apenas o vínculo do seu
        // próprio proprietário.
        Assert.AreEqual(1, await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters().CountAsync(item => item.BarbeariaId == barbeariaA.Id));
        Assert.AreEqual(1, await contexto.Set<VinculoProfissional>()
            .IgnoreQueryFilters().CountAsync(item => item.BarbeariaId == barbeariaB.Id));
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberProprietario_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Modo"] = "Memoria",
                    ["ConnectionStrings:DefaultConnection"] = conexao
                })));
    }

    private static void CriarSchema(WebApplicationFactory<Program> fabrica)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        contexto.Database.EnsureCreated();
    }

    private static async Task<Guid> RegistrarEEntrarAsync(HttpClient cliente)
    {
        var email = $"prof-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        var cadastro = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/register/professional",
            new { nomeCompleto = "Profissional Teste", email, telefone = "11999999999", senha, confirmarSenha = senha });
        Assert.AreEqual(HttpStatusCode.Created, cadastro.StatusCode);

        var login = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha });
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);

        var eu = await cliente.GetAsync("/api/v1/auth/me");
        Assert.AreEqual(HttpStatusCode.OK, eu.StatusCode);
        return Guid.Parse((await LerCorpoAsync(eu)).GetProperty("id").GetString()!);
    }

    private static async Task<(Guid Id, string CodigoConvite)> CriarBarbeariaAsync(HttpClient cliente)
    {
        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/barbearias",
            new
            {
                nome = "Barbearia Central",
                telefoneComercial = "(11) 99999-9999",
                emailComercial = "contato@barbearia.com",
                cidade = "São Paulo",
                endereco = "Rua Central, 100",
                instagram = "@barbeariacentral",
                fusoHorarioId = "America/Sao_Paulo"
            });
        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        return (Guid.Parse(corpo.GetProperty("id").GetString()!), corpo.GetProperty("codigoConvite").GetString()!);
    }

    private static async Task<HttpResponseMessage> EnviarComAntiforgeryAsync(
        HttpClient cliente,
        HttpMethod metodo,
        string rota,
        object corpo)
    {
        var respostaGet = await cliente.GetAsync("/health/live");
        var valor = respostaGet.Headers.GetValues("Set-Cookie")
            .First(cookie => cookie.Contains(ConfiguracaoAuth.CookieRequestToken, StringComparison.Ordinal));
        var trecho = valor.Split(';')[0];
        var token = Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);

        using var requisicao = new HttpRequestMessage(metodo, rota)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpo, _json), Encoding.UTF8, "application/json")
        };
        requisicao.Headers.Add(ConfiguracaoAuth.HeaderCsrf, token);
        return await cliente.SendAsync(requisicao);
    }

    private static async Task<JsonElement> LerCorpoAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }
}
