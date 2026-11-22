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
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Tasks 7.1 a 7.5: reserva, concorrência e consulta por perfil.</summary>
[TestClass]
public sealed class TestesAgendamentos
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private static readonly TimeZoneInfo _fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Publico_ComClienteAutenticado_ConfirmaComSnapshot()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var inicio = ProximoInicio();

        var resposta = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, inicio));

        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual("Corte degradê", corpo.GetProperty("nomeServico").GetString());
        Assert.AreEqual(40, corpo.GetProperty("duracaoMinutos").GetInt32());
        Assert.AreEqual<decimal>(80m, corpo.GetProperty("preco").GetDecimal());
        Assert.AreEqual(inicio, corpo.GetProperty("iniciaEmUtc").GetDateTime());
        Assert.AreEqual(inicio.AddMinutes(40), corpo.GetProperty("terminaEmUtc").GetDateTime());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var persistido = await contexto.Set<Agendamento>().IgnoreQueryFilters().SingleAsync();
        Assert.AreEqual("Corte degradê", persistido.NomeServicoSnapshot);
        Assert.AreEqual(cenario.ClienteId.ToString(), persistido.ClienteId);
    }

    [TestMethod]
    public async Task Publico_SemAutenticacao_Retorna401()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        using var visitante = fabrica.CreateClient(_opcoesCliente);
        var resposta = await EnviarComAntiforgeryAsync(
            visitante, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, ProximoInicio()));

        Assert.AreEqual(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Publico_ServicoInativo_Retorna400()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        await EnviarComAntiforgeryAsync(
            cenario.Dono, HttpMethod.Delete,
            $"/api/v1/barbearias/{cenario.BarbeariaId}/servicos/{cenario.ServicoId}", new { });

        var resposta = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, ProximoInicio()));

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Publico_ProfissionalInativo_Retorna400()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        await EnviarComAntiforgeryAsync(
            cenario.Dono, HttpMethod.Patch,
            $"/api/v1/barbearias/{cenario.BarbeariaId}/profissionais/{cenario.ProfissionalId}/status",
            new { ativo = false });

        var resposta = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, ProximoInicio()));

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Publico_HorarioOcupado_Retorna409()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var inicio = ProximoInicio();
        var primeiro = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, inicio));
        Assert.AreEqual(HttpStatusCode.Created, primeiro.StatusCode);

        using var outroCliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarClienteAsync(outroCliente);
        var segundo = await EnviarComAntiforgeryAsync(
            outroCliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, inicio));

        Assert.AreEqual(HttpStatusCode.Conflict, segundo.StatusCode);
        Assert.AreEqual(CodigosErro.AgendaHorarioIndisponivel,
            (await LerCorpoAsync(segundo)).GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Concorrencia_DuasConfirmacoesSimultaneas_AceitaSomenteUma()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        using var clienteB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarClienteAsync(clienteB);
        var inicio = ProximoInicio();
        var corpo = NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, inicio);

        var tarefas = new[]
        {
            EnviarComAntiforgeryAsync(cenario.Cliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId), corpo),
            EnviarComAntiforgeryAsync(clienteB, HttpMethod.Post, PublicUrl(cenario.BarbeariaId), corpo)
        };
        var respostas = await Task.WhenAll(tarefas);

        Assert.AreEqual(1, respostas.Count(resposta => resposta.StatusCode == HttpStatusCode.Created));
        Assert.AreEqual(1, respostas.Count(resposta => resposta.StatusCode == HttpStatusCode.Conflict));
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(1, await contexto.Set<Agendamento>().IgnoreQueryFilters().CountAsync());
    }

    [TestMethod]
    public async Task Manual_ProprietarioAgendaParaProfissional()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        var resposta = await EnviarComAntiforgeryAsync(
            cenario.Dono, HttpMethod.Post, $"/api/v1/barbearias/{cenario.BarbeariaId}/agendamentos",
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, ProximoInicio(), cenario.ClienteId.ToString()));

        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        Assert.AreEqual("Manual", (await LerCorpoAsync(resposta)).GetProperty("origem").GetString());
    }

    [TestMethod]
    public async Task Manual_BarbeiroNaoAgendaParaOutroProfissional()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        var resposta = await EnviarComAntiforgeryAsync(
            cenario.Profissional, HttpMethod.Post, $"/api/v1/barbearias/{cenario.BarbeariaId}/agendamentos",
            NovoAgendamento(cenario.DonoPerfilId, cenario.ServicoId, ProximoInicio(), cenario.ClienteId.ToString()));

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Consulta_RespeitaPerfilEIsolamento()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var inicio = ProximoInicio();
        Assert.AreEqual(HttpStatusCode.Created, (await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, inicio))).StatusCode);

        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, _fuso));
        var filtro = $"?de={hoje:yyyy-MM-dd}&ate={hoje.AddDays(7):yyyy-MM-dd}";

        var doDono = await cenario.Dono.GetAsync($"/api/v1/barbearias/{cenario.BarbeariaId}/agendamentos{filtro}");
        var doBarbeiro = await cenario.Profissional.GetAsync($"/api/v1/barbearias/{cenario.BarbeariaId}/agendamentos{filtro}");
        var doCliente = await cenario.Cliente.GetAsync($"/api/v1/me/agendamentos{filtro}");

        Assert.AreEqual(1, (await LerCorpoAsync(doDono)).GetArrayLength());
        Assert.AreEqual(1, (await LerCorpoAsync(doBarbeiro)).GetArrayLength());
        Assert.AreEqual(1, (await LerCorpoAsync(doCliente)).GetArrayLength());

        var id = Guid.Parse((await LerCorpoAsync(doDono))[0].GetProperty("id").GetString()!);
        using var outroCliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarClienteAsync(outroCliente);
        var acessoIndevido = await outroCliente.GetAsync(
            $"/api/v1/barbearias/{cenario.BarbeariaId}/agendamentos/{id}");
        Assert.AreEqual(HttpStatusCode.Forbidden, acessoIndevido.StatusCode);
    }

    [TestMethod]
    public async Task DisponibilidadeEBlocks_ConsideramAgendamentosExistentes()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var inicio = ProximoInicio();
        Assert.AreEqual(HttpStatusCode.Created, (await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, inicio))).StatusCode);

        var data = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(inicio, _fuso));
        var consulta = await cenario.Cliente.GetAsync(
            $"/api/v1/barbearias/{cenario.BarbeariaId}/disponibilidade?servicoId={cenario.ServicoId}&date={data:yyyy-MM-dd}");
        var slots = (await LerCorpoAsync(consulta)).GetProperty("slots").EnumerateArray().ToList();
        Assert.IsFalse(slots.Any(slot => slot.GetProperty("startsAtUtc").GetDateTime() == inicio));

        var bloqueio = await EnviarComAntiforgeryAsync(
            cenario.Dono, HttpMethod.Post,
            $"/api/v1/barbearias/{cenario.BarbeariaId}/profissionais/{cenario.ProfissionalId}/blocks",
            new
            {
                iniciaEmUtc = inicio.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                terminaEmUtc = inicio.AddMinutes(40).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                motivo = "Conflito"
            });
        Assert.AreEqual(HttpStatusCode.Conflict, bloqueio.StatusCode);
    }

    [TestMethod]
    public async Task DisponibilidadeSemanal_PreservaAgendamentoExistente()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        Assert.AreEqual(HttpStatusCode.Created, (await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, ProximoInicio()))).StatusCode);

        var janelaReduzida = Enumerable.Range(1, 7)
            .Select(dia => new { diaSemana = dia, iniciaEm = "13:00:00", terminaEm = "14:00:00" })
            .ToArray();
        var recusa = await EnviarComAntiforgeryAsync(
            cenario.Dono, HttpMethod.Put,
            $"/api/v1/barbearias/{cenario.BarbeariaId}/profissionais/{cenario.ProfissionalId}/disponibilidade",
            janelaReduzida);
        Assert.AreEqual(HttpStatusCode.BadRequest, recusa.StatusCode);

        var janelaAmpla = Enumerable.Range(1, 7)
            .Select(dia => new { diaSemana = dia, iniciaEm = "09:00:00", terminaEm = "18:00:00" })
            .ToArray();
        var aceite = await EnviarComAntiforgeryAsync(
            cenario.Dono, HttpMethod.Put,
            $"/api/v1/barbearias/{cenario.BarbeariaId}/profissionais/{cenario.ProfissionalId}/disponibilidade",
            janelaAmpla);
        Assert.AreEqual(HttpStatusCode.OK, aceite.StatusCode);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.AreEqual(1, await contexto.Set<Agendamento>().IgnoreQueryFilters().CountAsync());
    }

    private static string PublicUrl(Guid barbeariaId) =>
        $"/api/v1/barbearias/{barbeariaId}/agendamentos/public";

    private static DateTime ProximoInicio()
    {
        var data = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, _fuso)).AddDays(1);
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(data.ToDateTime(new TimeOnly(10, 0)), DateTimeKind.Unspecified), _fuso);
    }

    private static object NovoAgendamento(
        Guid profissionalId, Guid servicoId, DateTime inicio, string? clienteId = null) =>
        new
        {
            profissionalId,
            servicoId,
            iniciaEmUtc = inicio.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            observacoes = (string?)null,
            clienteId
        };

    private static async Task<Cenario> CriarCenarioAsync(WebApplicationFactory<Program> fabrica)
    {
        var dono = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);

        var profissional = fabrica.CreateClient(_opcoesCliente);
        await RegistrarProfissionalAsync(profissional);
        await EnviarComAntiforgeryAsync(
            profissional, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });

        var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarClienteAsync(cliente);

        var (profissionalId, donoPerfilId, clienteId) = await ObterIdsAsync(fabrica, profissional, dono, cliente);
        await ConfigurarAsync(dono, barbearia.Id, profissionalId);
        var servicoId = await CriarServicoAsync(dono, barbearia.Id);

        return new Cenario(fabrica, dono, profissional, cliente, barbearia.Id, servicoId, profissionalId, donoPerfilId, clienteId);
    }

    private static async Task ConfigurarAsync(
        HttpClient dono, Guid barbeariaId, Guid profissionalId)
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
        Assert.AreEqual(HttpStatusCode.OK, (await EnviarComAntiforgeryAsync(
            dono, HttpMethod.Put, $"/api/v1/barbearias/{barbeariaId}/horarios-funcionamento", dias)).StatusCode);

        var janelas = Enumerable.Range(1, 7)
            .Select(dia => new { diaSemana = dia, iniciaEm = "10:00:00", terminaEm = "12:00:00" })
            .ToArray();
        Assert.AreEqual(HttpStatusCode.OK, (await EnviarComAntiforgeryAsync(
            dono, HttpMethod.Put,
            $"/api/v1/barbearias/{barbeariaId}/profissionais/{profissionalId}/disponibilidade",
            janelas)).StatusCode);
    }

    private static async Task<Guid> CriarServicoAsync(HttpClient dono, Guid barbeariaId)
    {
        var resposta = await EnviarComAntiforgeryAsync(
            dono, HttpMethod.Post, $"/api/v1/barbearias/{barbeariaId}/servicos",
            new { nome = "Corte degradê", duracaoMinutos = 40, preco = 80m });
        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        return Guid.Parse((await LerCorpoAsync(resposta)).GetProperty("id").GetString()!);
    }

    private static async Task<(Guid ProfissionalId, Guid DonoPerfilId, Guid ClienteId)> ObterIdsAsync(
        WebApplicationFactory<Program> fabrica,
        HttpClient profissional,
        HttpClient dono,
        HttpClient cliente)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var id = async (HttpClient http) =>
        {
            var resposta = await http.GetAsync("/api/v1/auth/me");
            var usuarioId = (await LerCorpoAsync(resposta)).GetProperty("id").GetString()!;
            return (await contexto.Set<PontoBarber.Dominio.Modulos.Profissionais.PerfilProfissional>()
                .SingleOrDefaultAsync(item => item.UsuarioId == usuarioId))?.Id;
        };

        return (
            await id(profissional) ?? Guid.Empty,
            await id(dono) ?? Guid.Empty,
            Guid.Parse((await LerCorpoAsync(await cliente.GetAsync("/api/v1/auth/me"))).GetProperty("id").GetString()!));
    }

    private sealed record Cenario(
        WebApplicationFactory<Program> Fabrica,
        HttpClient Dono,
        HttpClient Profissional,
        HttpClient Cliente,
        Guid BarbeariaId,
        Guid ServicoId,
        Guid ProfissionalId,
        Guid DonoPerfilId,
        Guid ClienteId);

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberAgendamentos_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = conexao
                })));
    }

    private static void CriarSchema(WebApplicationFactory<Program> fabrica)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        contexto.Database.EnsureCreated();
    }

    private static async Task RegistrarProfissionalAsync(HttpClient cliente)
    {
        var email = $"prof-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        Assert.AreEqual(HttpStatusCode.Created, (await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/register/professional",
            new { nomeCompleto = "Profissional Teste", email, telefone = "11999999999", senha, confirmarSenha = senha })).StatusCode);
        Assert.AreEqual(HttpStatusCode.NoContent, (await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha })).StatusCode);
    }

    private static async Task RegistrarClienteAsync(HttpClient cliente)
    {
        var email = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        Assert.AreEqual(HttpStatusCode.Created, (await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/register/customer",
            new { nomeCompleto = "Cliente Teste", email, telefone = "11999999999", senha, confirmarSenha = senha })).StatusCode);
        Assert.AreEqual(HttpStatusCode.NoContent, (await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha })).StatusCode);
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
