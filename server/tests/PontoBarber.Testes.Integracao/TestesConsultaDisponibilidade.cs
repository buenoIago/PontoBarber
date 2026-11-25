using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Task 6.4: cálculo de horários livres.</summary>
[TestClass]
public sealed class TestesConsultaDisponibilidade
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Consultar_DentroDaJornada_RetornaSlotsNoFusoDaBarbearia()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);
        var (profissionalId, servicoId, duracao) = await PrepararConfiguracaoAsync(fabrica, cliente, barbearia.Id);
        // Horários do próprio dia já passados não são oferecidos.
        var data = HojeNaBarbearia().AddDays(1);

        var resposta = await cliente.GetAsync(Url(barbearia.Id, servicoId, profissionalId, data));

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode, await resposta.Content.ReadAsStringAsync());
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual("America/Sao_Paulo", corpo.GetProperty("fusoHorarioId").GetString());
        Assert.IsTrue(corpo.GetProperty("hasAvailability").GetBoolean());

        var slots = corpo.GetProperty("slots").EnumerateArray().ToList();
        Assert.AreEqual(4, slots.Count);
        Assert.IsTrue(slots.All(slot => slot.GetProperty("startsAtLocal").GetString()!.Contains("10:") ||
                                       slot.GetProperty("startsAtLocal").GetString()!.Contains("11:")));

        var fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var esperado = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(data.ToDateTime(new TimeOnly(10, 0)), DateTimeKind.Unspecified), fuso);
        Assert.AreEqual(esperado, slots[0].GetProperty("startsAtUtc").GetDateTime());
        Assert.AreEqual(esperado.AddMinutes(duracao), slots[0].GetProperty("endsAtUtc").GetDateTime());
    }

    [TestMethod]
    public async Task Consultar_BarbeariaInativa_Retorna400()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);
        var (profissionalId, servicoId, _) = await PrepararConfiguracaoAsync(fabrica, cliente, barbearia.Id);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            await contexto.Database.ExecuteSqlRawAsync(
                "UPDATE \"Barbearias\" SET \"Ativo\" = false WHERE \"Id\" = {0}", barbearia.Id);
        }

        var resposta = await cliente.GetAsync(
            Url(barbearia.Id, servicoId, profissionalId, HojeNaBarbearia()));

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou, corpo.GetProperty("code").GetString());
        Assert.IsTrue(corpo.GetProperty("errors").TryGetProperty("barbeariaId", out _));
    }

    [TestMethod]
    public async Task Consultar_ComBloqueio_RemoveSlotsSobrepostos()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);
        var (profissionalId, servicoId, _) = await PrepararConfiguracaoAsync(fabrica, cliente, barbearia.Id);
        var data = HojeNaBarbearia().AddDays(1);
        var fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var inicio = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(data.ToDateTime(new TimeOnly(10, 0)), DateTimeKind.Unspecified), fuso);
        var termino = inicio.AddHours(1);

        var bloqueio = await EnviarComAntiforgeryAsync(
            cliente,
            HttpMethod.Post,
            $"/api/v1/barbearias/{barbearia.Id}/profissionais/{profissionalId}/blocks",
            new
            {
                iniciaEmUtc = inicio.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                terminaEmUtc = termino.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                motivo = "Almoço"
            });
        Assert.AreEqual(HttpStatusCode.Created, bloqueio.StatusCode);

        var resposta = await cliente.GetAsync(Url(barbearia.Id, servicoId, profissionalId, data));

        var slots = (await LerCorpoAsync(resposta)).GetProperty("slots").EnumerateArray().ToList();
        Assert.AreEqual(2, slots.Count);
        Assert.IsTrue(slots.All(slot => slot.GetProperty("startsAtUtc").GetDateTime() >= termino));
    }

    [TestMethod]
    public async Task Consultar_DiaFechado_NaoRetornaSlots()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);
        var (profissionalId, servicoId, _) = await PrepararConfiguracaoAsync(fabrica, cliente, barbearia.Id);

        var dias = Enumerable.Range(1, 7)
            .Select(dia => new
            {
                diaSemana = dia,
                abreAs = (string?)null,
                fechaAs = (string?)null,
                fechado = true
            })
            .ToArray();
        var atualizacao = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Put,
            $"/api/v1/barbearias/{barbearia.Id}/horarios-funcionamento", dias);
        Assert.AreEqual(HttpStatusCode.OK, atualizacao.StatusCode);

        var resposta = await cliente.GetAsync(Url(barbearia.Id, servicoId, profissionalId, HojeNaBarbearia()));

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(0, corpo.GetProperty("slots").GetArrayLength());
        Assert.IsFalse(corpo.GetProperty("hasAvailability").GetBoolean());
    }

    [TestMethod]
    public async Task Consultar_AlemDoLimiteDe30Dias_Retorna400()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);
        var (profissionalId, servicoId, _) = await PrepararConfiguracaoAsync(fabrica, cliente, barbearia.Id);

        var resposta = await cliente.GetAsync(
            Url(barbearia.Id, servicoId, profissionalId, HojeNaBarbearia().AddDays(31), 31));

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou, (await LerCorpoAsync(resposta)).GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Consultar_ComProfissionalDeOutraBarbearia_NaoRetornaSlots()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);
        await PrepararConfiguracaoAsync(fabrica, donoA, barbeariaA.Id);

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoB);
        var barbeariaB = await CriarBarbeariaAsync(donoB);
        var (_, servicoB, _) = await PrepararConfiguracaoAsync(fabrica, donoB, barbeariaB.Id);
        var profissionalA = await ObterPerfilIdAsync(fabrica, donoA);

        var resposta = await donoB.GetAsync(
            Url(barbeariaB.Id, servicoB, profissionalA, HojeNaBarbearia()));

        Assert.AreEqual(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    private static DateOnly HojeNaBarbearia() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));

    private static string Url(
        Guid barbeariaId,
        Guid servicoId,
        Guid profissionalId,
        DateOnly data,
        int? dias = null) =>
        $"/api/v1/barbearias/{barbeariaId}/disponibilidade?servicoId={servicoId}&profissionalId={profissionalId}&date={data:yyyy-MM-dd}" +
        (dias.HasValue ? $"&dias={dias.Value}" : string.Empty);

    private static async Task<(Guid ProfissionalId, Guid ServicoId, int Duracao)> PrepararConfiguracaoAsync(
        WebApplicationFactory<Program> fabrica,
        HttpClient cliente,
        Guid barbeariaId)
    {
        var dias = Enumerable.Range(1, 7)
            .Select(dia => new
            {
                diaSemana = dia,
                abreAs = (string?)"09:00:00",
                fechaAs = (string?)"18:00:00",
                fechado = false
            })
            .ToArray();
        var funcionamento = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Put, $"/api/v1/barbearias/{barbeariaId}/horarios-funcionamento", dias);
        Assert.AreEqual(HttpStatusCode.OK, funcionamento.StatusCode);

        var profissionalId = await ObterPerfilIdAsync(fabrica, cliente);
        var janelas = Enumerable.Range(1, 7)
            .Select(dia => new
            {
                diaSemana = dia,
                iniciaEm = "10:00:00",
                terminaEm = "12:00:00"
            })
            .ToArray();
        var disponibilidade = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Put,
            $"/api/v1/barbearias/{barbeariaId}/profissionais/{profissionalId}/disponibilidade", janelas);
        Assert.AreEqual(HttpStatusCode.OK, disponibilidade.StatusCode);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var servico = await contexto.Set<Servico>()
            .IgnoreQueryFilters()
            .SingleAsync(item => item.BarbeariaId == barbeariaId &&
                                 item.Nome == "Corte social / tradicional");
        return (profissionalId, servico.Id, servico.DuracaoMinutos);
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

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberDisponibilidadeConsulta_{Guid.NewGuid():N}";
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

    private static async Task RegistrarEEntrarAsync(HttpClient cliente)
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
