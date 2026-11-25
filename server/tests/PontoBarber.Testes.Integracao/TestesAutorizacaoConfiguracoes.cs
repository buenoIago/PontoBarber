using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Task 6.5: autorização das configurações de horários e disponibilidade.</summary>
[TestClass]
public sealed class TestesAutorizacaoConfiguracoes
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Barbeiro_TentaAlterarHorariosGerais_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);

        using var barbeiro = await CriarBarbeiroVinculadoAsync(fabrica, barbearia.CodigoConvite);
        var resposta = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put, HorariosUrl(barbearia.Id), Semana(false));

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Barbeiro_TentaAlterarDisponibilidadeDeOutro_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        using var barbeiro = await CriarBarbeiroVinculadoAsync(fabrica, barbearia.CodigoConvite);
        var perfilDono = await ObterPerfilIdAsync(fabrica, dono);

        var alteracao = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put, DisponibilidadeUrl(barbearia.Id, perfilDono), Janelas());
        var consulta = await barbeiro.GetAsync(DisponibilidadeUrl(barbearia.Id, perfilDono));

        Assert.AreEqual(HttpStatusCode.Forbidden, alteracao.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, consulta.StatusCode);
    }

    [TestMethod]
    public async Task Barbeiro_GerenciaPropriaDisponibilidade_RetornaSucesso()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        await EnviarComAntiforgeryAsync(dono, HttpMethod.Put, HorariosUrl(barbearia.Id), Semana(false));
        using var barbeiro = await CriarBarbeiroVinculadoAsync(fabrica, barbearia.CodigoConvite);
        var perfil = await ObterPerfilIdAsync(fabrica, barbeiro);

        var alteracao = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put, DisponibilidadeUrl(barbearia.Id, perfil), Janelas());
        var consulta = await barbeiro.GetAsync(DisponibilidadeUrl(barbearia.Id, perfil));

        Assert.AreEqual(HttpStatusCode.OK, alteracao.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, consulta.StatusCode);
        Assert.AreEqual(7, (await LerCorpoAsync(consulta)).GetArrayLength());
    }

    [TestMethod]
    public async Task Barbeiro_TentaGerenciarBloqueiosDeOutro_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        using var barbeiro = await CriarBarbeiroVinculadoAsync(fabrica, barbearia.CodigoConvite);
        var perfilDono = await ObterPerfilIdAsync(fabrica, dono);

        var consulta = await barbeiro.GetAsync(BloqueiosUrl(barbearia.Id, perfilDono));
        var criacao = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, BloqueiosUrl(barbearia.Id, perfilDono), Bloqueio());

        Assert.AreEqual(HttpStatusCode.Forbidden, consulta.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, criacao.StatusCode);
    }

    [TestMethod]
    public async Task Barbeiro_GerenciaPropriosBloqueios_RetornaSucesso()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        using var barbeiro = await CriarBarbeiroVinculadoAsync(fabrica, barbearia.CodigoConvite);
        var perfil = await ObterPerfilIdAsync(fabrica, barbeiro);

        var criacao = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, BloqueiosUrl(barbearia.Id, perfil), Bloqueio());
        Assert.AreEqual(HttpStatusCode.Created, criacao.StatusCode);
        var bloqueioId = Guid.Parse((await LerCorpoAsync(criacao)).GetProperty("id").GetString()!);

        var consulta = await barbeiro.GetAsync(BloqueiosUrl(barbearia.Id, perfil));
        Assert.AreEqual(HttpStatusCode.OK, consulta.StatusCode);
        Assert.AreEqual(1, (await LerCorpoAsync(consulta)).GetArrayLength());

        var remocao = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Delete, $"{BloqueiosUrl(barbearia.Id, perfil)}/{bloqueioId}", new { });
        Assert.AreEqual(HttpStatusCode.NoContent, remocao.StatusCode);
    }

    [TestMethod]
    public async Task Proprietario_GerenciaDisponibilidadeEBloqueiosDeOutro_RetornaSucesso()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        await EnviarComAntiforgeryAsync(dono, HttpMethod.Put, HorariosUrl(barbearia.Id), Semana(false));
        using var barbeiro = await CriarBarbeiroVinculadoAsync(fabrica, barbearia.CodigoConvite);
        var perfil = await ObterPerfilIdAsync(fabrica, barbeiro);

        var disponibilidade = await EnviarComAntiforgeryAsync(
            dono, HttpMethod.Put, DisponibilidadeUrl(barbearia.Id, perfil), Janelas());
        var bloqueio = await EnviarComAntiforgeryAsync(
            dono, HttpMethod.Post, BloqueiosUrl(barbearia.Id, perfil), Bloqueio());
        var consulta = await dono.GetAsync(BloqueiosUrl(barbearia.Id, perfil));

        Assert.AreEqual(HttpStatusCode.OK, disponibilidade.StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, bloqueio.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, consulta.StatusCode);
    }

    [TestMethod]
    public async Task Cliente_TentaOperacoesAdministrativas_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarClienteEEntrarAsync(cliente);

        var horarios = await cliente.GetAsync(HorariosUrl(barbearia.Id));
        var disponibilidade = await cliente.GetAsync(DisponibilidadeUrl(barbearia.Id, Guid.NewGuid()));
        var bloqueios = await cliente.GetAsync(BloqueiosUrl(barbearia.Id, Guid.NewGuid()));

        Assert.AreEqual(HttpStatusCode.Forbidden, horarios.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, disponibilidade.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, bloqueios.StatusCode);
    }

    [TestMethod]
    public async Task Cliente_ConsultaDisponibilidadePublica_Retorna200()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        await EnviarComAntiforgeryAsync(dono, HttpMethod.Put, HorariosUrl(barbearia.Id), Semana(false));
        var perfil = await ObterPerfilIdAsync(fabrica, dono);
        await EnviarComAntiforgeryAsync(dono, HttpMethod.Put, DisponibilidadeUrl(barbearia.Id, perfil), Janelas());
        var servicoId = await ObterServicoIdAsync(fabrica, barbearia.Id);
        // A consulta não oferece horários passados; usar o próximo dia.
        var data = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"))).AddDays(1);

        using var cliente = fabrica.CreateClient(_opcoesCliente);
        var resposta = await cliente.GetAsync(
            $"/api/v1/barbearias/{barbearia.Id}/disponibilidade?servicoId={servicoId}&date={data:yyyy-MM-dd}");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        Assert.IsTrue((await LerCorpoAsync(resposta)).GetProperty("hasAvailability").GetBoolean());
    }

    [TestMethod]
    public async Task AcessoEntreBarbearias_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(donoB);
        var barbeariaB = await CriarBarbeariaAsync(donoB);
        var perfilB = await ObterPerfilIdAsync(fabrica, donoB);

        var disponibilidade = await EnviarComAntiforgeryAsync(
            donoA, HttpMethod.Put, DisponibilidadeUrl(barbeariaA.Id, perfilB), Janelas());
        var bloqueios = await donoA.GetAsync(BloqueiosUrl(barbeariaA.Id, perfilB));
        var horarios = await EnviarComAntiforgeryAsync(
            donoA, HttpMethod.Put, HorariosUrl(barbeariaB.Id), Semana(false));

        Assert.AreEqual(HttpStatusCode.Forbidden, disponibilidade.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, bloqueios.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, horarios.StatusCode);
    }

    private static string HorariosUrl(Guid barbeariaId) =>
        $"/api/v1/barbearias/{barbeariaId}/horarios-funcionamento";

    private static string DisponibilidadeUrl(Guid barbeariaId, Guid profissionalId) =>
        $"/api/v1/barbearias/{barbeariaId}/profissionais/{profissionalId}/disponibilidade";

    private static string BloqueiosUrl(Guid barbeariaId, Guid profissionalId) =>
        $"/api/v1/barbearias/{barbeariaId}/profissionais/{profissionalId}/blocks";

    private static object[] Semana(bool fechado) =>
        Enumerable.Range(1, 7)
            .Select(dia => new
            {
                diaSemana = dia,
                abreAs = fechado ? null : "09:00:00",
                fechaAs = fechado ? null : "18:00:00",
                fechado
            })
            .ToArray();

    private static object[] Janelas() =>
        Enumerable.Range(1, 7)
            .Select(dia => new
            {
                diaSemana = dia,
                iniciaEm = "10:00:00",
                terminaEm = "12:00:00"
            })
            .ToArray();

    private static object Bloqueio() => new
    {
        iniciaEmUtc = "2026-11-30T13:00:00Z",
        terminaEmUtc = "2026-11-30T14:00:00Z",
        motivo = "Bloqueio de teste"
    };

    private static async Task<Guid> ObterServicoIdAsync(WebApplicationFactory<Program> fabrica, Guid barbeariaId)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var servico = await contexto.Set<Servico>().IgnoreQueryFilters()
            .FirstAsync(item => item.BarbeariaId == barbeariaId);
        return servico.Id;
    }

    private static async Task<Guid> ObterPerfilIdAsync(
        WebApplicationFactory<Program> fabrica,
        HttpClient cliente)
    {
        var eu = await cliente.GetAsync("/api/v1/auth/me");
        var usuarioId = (await LerCorpoAsync(eu)).GetProperty("id").GetString()!;
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        return (await contexto.Set<PerfilProfissional>().SingleAsync(item => item.UsuarioId == usuarioId)).Id;
    }

    private static async Task<HttpClient> CriarBarbeiroVinculadoAsync(
        WebApplicationFactory<Program> fabrica,
        string codigoConvite)
    {
        var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalEEntrarAsync(cliente);
        var entrada = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/barbearias/entrar", new { codigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, entrada.StatusCode);
        return cliente;
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberAutorizacaoConfig_{Guid.NewGuid():N}";
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

    private static async Task RegistrarProfissionalEEntrarAsync(HttpClient cliente)
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
    }

    private static async Task RegistrarClienteEEntrarAsync(HttpClient cliente)
    {
        await AuthTestHelper.RegistrarEEntrarClienteAsync(cliente);
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
