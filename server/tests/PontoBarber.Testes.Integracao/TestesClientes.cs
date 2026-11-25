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
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Tasks 11.1 a 11.4: clientes derivados de agendamentos e autoatendimento.</summary>
[TestClass]
public sealed class TestesClientes
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoes = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Listagem_RelacaoDerivadaDeAgendamentos_ComBuscaEPaginacao()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        var lista = await Get(cenario.Dono, $"/api/v1/barbearias/{cenario.BarbeariaId}/customers");
        Assert.AreEqual(HttpStatusCode.OK, lista.StatusCode);
        var corpo = await LerAsync(lista);
        Assert.AreEqual(1, corpo.GetProperty("total").GetInt32());
        Assert.AreEqual(1, corpo.GetProperty("itens").GetArrayLength());
        Assert.AreEqual(cenario.ClienteAId,
            corpo.GetProperty("itens")[0].GetProperty("id").GetString());
        Assert.IsFalse(cenario.SemAgendamentoId == cenario.ClienteAId);

        var porNome = await Get(cenario.Dono,
            $"/api/v1/barbearias/{cenario.BarbeariaId}/customers?busca=Cliente%20Alvo");
        Assert.AreEqual(1, (await LerAsync(porNome)).GetProperty("total").GetInt32());

        var porEmail = await Get(cenario.Dono,
            $"/api/v1/barbearias/{cenario.BarbeariaId}/customers?busca=cliente-alvo@");
        Assert.AreEqual(1, (await LerAsync(porEmail)).GetProperty("total").GetInt32());

        var semResultado = await Get(cenario.Dono,
            $"/api/v1/barbearias/{cenario.BarbeariaId}/customers?busca=inexistente");
        Assert.AreEqual(0, (await LerAsync(semResultado)).GetProperty("total").GetInt32());
    }

    [TestMethod]
    public async Task Listagem_AutorizacaoEIsolamentoEntreBarbearias()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        Assert.AreEqual(HttpStatusCode.Forbidden,
            (await Get(cenario.Barbeiro, $"/api/v1/barbearias/{cenario.BarbeariaId}/customers")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,
            (await Get(cenario.ClienteA, $"/api/v1/barbearias/{cenario.BarbeariaId}/customers")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,
            (await Get(cenario.OutroDono, $"/api/v1/barbearias/{cenario.BarbeariaId}/customers")).StatusCode);

        var daOutra = await Get(cenario.Dono,
            $"/api/v1/barbearias/{cenario.OutraBarbeariaId}/customers");
        Assert.AreEqual(HttpStatusCode.Forbidden, daOutra.StatusCode);

        var detalheOutroCliente = await Get(cenario.Dono,
            $"/api/v1/barbearias/{cenario.BarbeariaId}/customers/{cenario.ClienteBId}");
        Assert.IsTrue(detalheOutroCliente.StatusCode
            is HttpStatusCode.NotFound or HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Consulta_ClienteRelacionado_RetornaDadosPermitidos()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        var resposta = await Get(cenario.Dono,
            $"/api/v1/barbearias/{cenario.BarbeariaId}/customers/{cenario.ClienteAId}");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerAsync(resposta);
        Assert.AreEqual(cenario.ClienteAId, corpo.GetProperty("id").GetString());
        Assert.AreEqual("Cliente Alvo", corpo.GetProperty("nomeCompleto").GetString());
    }

    [TestMethod]
    public async Task PerfilDoCliente_ConsultaEEdicaoDosPropriosDados()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        var atual = await Get(cenario.ClienteA, "/api/v1/me/customer-profile");
        Assert.AreEqual(HttpStatusCode.OK, atual.StatusCode);
        Assert.AreEqual(cenario.ClienteAId, (await LerAsync(atual)).GetProperty("id").GetString());

        var edicao = await EnviarAsync(cenario.ClienteA, HttpMethod.Put,
            "/api/v1/me/customer-profile",
            new { nomeCompleto = "Cliente Atualizado", email = "cliente-novo@exemplo.com", telefone = "11999999999", id = "outro", tipo = "Professional" });
        Assert.AreEqual(HttpStatusCode.OK, edicao.StatusCode);
        var corpo = await LerAsync(edicao);
        Assert.AreEqual("Cliente Atualizado", corpo.GetProperty("nomeCompleto").GetString());
        Assert.AreEqual(cenario.ClienteAId, corpo.GetProperty("id").GetString());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var agendamento = await contexto.Set<Agendamento>().IgnoreQueryFilters()
            .FirstAsync(item => item.ClienteId == cenario.ClienteAId);
        Assert.AreEqual("Corte", agendamento.NomeServicoSnapshot);
        Assert.AreEqual(80m, agendamento.PrecoSnapshot);
    }

    [TestMethod]
    public async Task PerfilDoCliente_EmailDuplicadoEValidacao_SemPerderDados()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);

        var duplicado = await EnviarAsync(cenario.ClienteA, HttpMethod.Put,
            "/api/v1/me/customer-profile",
            new { nomeCompleto = "Cliente Alvo", email = "cliente-sem-agendamento@exemplo.com", telefone = (string?)null });
        Assert.AreEqual(HttpStatusCode.Conflict, duplicado.StatusCode);

        var invalido = await EnviarAsync(cenario.ClienteA, HttpMethod.Put,
            "/api/v1/me/customer-profile",
            new { nomeCompleto = "ab", email = "invalido", telefone = (string?)null });
        Assert.AreEqual(HttpStatusCode.BadRequest, invalido.StatusCode);

        var atual = await Get(cenario.ClienteA, "/api/v1/me/customer-profile");
        var corpo = await LerAsync(atual);
        Assert.AreEqual("Cliente Alvo", corpo.GetProperty("nomeCompleto").GetString());
        Assert.AreEqual("cliente-alvo@exemplo.com", corpo.GetProperty("email").GetString());
    }

    [TestMethod]
    public async Task GestaoManualDeClientes_NaoEstaExpostaNaApi()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var rota = $"/api/v1/barbearias/{cenario.BarbeariaId}/customers";

        foreach (var metodo in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Patch })
        {
            var resposta = await EnviarAsync(cenario.Dono, metodo, rota, new { nomeCompleto = "Novo" });
            Assert.IsTrue(
                resposta.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
                $"{metodo} não deveria ser exposto: {resposta.StatusCode}");
        }
    }

    private static async Task<Cenario> CriarCenarioAsync(WebApplicationFactory<Program> fabrica)
    {
        var dono = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(dono, "dono");
        var barbearia = await CriarBarbeariaAsync(dono, "Barbearia Alvo");

        var barbeiro = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(barbeiro, "barbeiro");
        await EnviarAsync(barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.Codigo });

        var clienteA = fabrica.CreateClient(_opcoes);
        await RegistrarClienteAsync(clienteA, "cliente-alvo", "Cliente Alvo");

        var semAgendamento = fabrica.CreateClient(_opcoes);
        await RegistrarClienteAsync(semAgendamento, "cliente-sem-agendamento", "Cliente Sem Agendamento");

        var outroDono = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(outroDono, "outro-dono");
        var outraBarbearia = await CriarBarbeariaAsync(outroDono, "Outra Barbearia");

        var clienteB = fabrica.CreateClient(_opcoes);
        await RegistrarClienteAsync(clienteB, "cliente-outro", "Cliente Outro");

        var clienteAId = await ObterUsuarioIdAsync(clienteA);
        var clienteBId = await ObterUsuarioIdAsync(clienteB);
        var semAgendamentoId = await ObterUsuarioIdAsync(semAgendamento);
        var barbeiroUsuarioId = await ObterUsuarioIdAsync(barbeiro);
        var outroDonoUsuarioId = await ObterUsuarioIdAsync(outroDono);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var perfilBarbeiro = await contexto.Set<PerfilProfissional>()
            .SingleAsync(item => item.UsuarioId == barbeiroUsuarioId);
        var perfilOutroDono = await contexto.Set<PerfilProfissional>()
            .SingleAsync(item => item.UsuarioId == outroDonoUsuarioId);

        var agora = DateTime.UtcNow;
        contexto.Set<Agendamento>().AddRange(
            CriarAgendamento(barbearia.Id, clienteAId, perfilBarbeiro.Id, agora.AddDays(1)),
            CriarAgendamento(barbearia.Id, clienteAId, perfilBarbeiro.Id, agora.AddDays(2), concluido: true),
            CriarAgendamento(outraBarbearia.Id, clienteBId, perfilOutroDono.Id, agora.AddDays(1)));
        SemeaduraIntegridade.GarantirTudo(contexto);
        await contexto.SaveChangesAsync();

        return new Cenario(
            dono, barbeiro, clienteA, outroDono, barbearia.Id, outraBarbearia.Id,
            clienteAId, clienteBId, semAgendamentoId);
    }

    private static Agendamento CriarAgendamento(
        Guid barbeariaId, string clienteId, Guid profissionalId, DateTime inicio, bool concluido = false)
    {
        var agendamento = Agendamento.Criar(
            barbeariaId, clienteId, profissionalId, Guid.NewGuid(), inicio,
            "Corte", 40, 80m, null, OrigemAgendamento.Publico);
        if (concluido)
        {
            agendamento.Iniciar(inicio.AddMinutes(1));
            agendamento.Concluir(inicio.AddMinutes(40));
        }
        return agendamento;
    }

    private static async Task<string> ObterUsuarioIdAsync(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync("/api/v1/auth/me");
        resposta.EnsureSuccessStatusCode();
        return (await LerAsync(resposta)).GetProperty("id").GetString()!;
    }

    private static Task<HttpResponseMessage> Get(HttpClient cliente, string rota) =>
        cliente.GetAsync(rota);

    private static async Task RegistrarProfissionalAsync(HttpClient cliente, string prefixo)
    {
        var email = $"{prefixo}-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/auth/register/professional",
            new { nomeCompleto = prefixo, email, telefone = "11999999999", senha, confirmarSenha = senha });
        await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha });
    }

    private static async Task RegistrarClienteAsync(HttpClient cliente, string prefixo, string nome)
    {
        await AuthTestHelper.RegistrarEEntrarClienteAsync(cliente, nome, $"{prefixo}@exemplo.com");
    }

    private static async Task<(Guid Id, string Codigo)> CriarBarbeariaAsync(HttpClient cliente, string nome)
    {
        var resposta = await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/barbearias", new
        {
            nome,
            telefoneComercial = "(11) 99999-9999",
            emailComercial = "contato@exemplo.com",
            cidade = "São Paulo",
            endereco = "Rua Central, 100",
            instagram = (string?)null,
            fusoHorarioId = "America/Sao_Paulo"
        });
        resposta.EnsureSuccessStatusCode();
        var corpo = await LerAsync(resposta);
        return (Guid.Parse(corpo.GetProperty("id").GetString()!), corpo.GetProperty("codigoConvite").GetString()!);
    }

    private static async Task<HttpResponseMessage> EnviarAsync(
        HttpClient cliente, HttpMethod metodo, string rota, object corpo)
    {
        var respostaGet = await cliente.GetAsync("/health/live");
        var valor = respostaGet.Headers.GetValues("Set-Cookie")
            .First(item => item.Contains(ConfiguracaoAuth.CookieRequestToken, StringComparison.Ordinal));
        var trecho = valor.Split(';')[0];
        var token = Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);

        using var requisicao = new HttpRequestMessage(metodo, rota)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpo, _json), Encoding.UTF8, "application/json")
        };
        requisicao.Headers.Add(ConfiguracaoAuth.HeaderCsrf, token);
        return await cliente.SendAsync(requisicao);
    }

    private static async Task<JsonElement> LerAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private sealed record Cenario(
        HttpClient Dono,
        HttpClient Barbeiro,
        HttpClient ClienteA,
        HttpClient OutroDono,
        Guid BarbeariaId,
        Guid OutraBarbeariaId,
        string ClienteAId,
        string ClienteBId,
        string SemAgendamentoId);

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberClientes_{Guid.NewGuid():N}";
        var conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Modo"] = "Memoria",
                    ["ConnectionStrings:DefaultConnection"] = conexao,
                    ["Jobs:IntervaloMinutos"] = "60"
                })));
    }

    private static void CriarSchema(WebApplicationFactory<Program> fabrica)
    {
        using var escopo = fabrica.Services.CreateScope();
        escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>().Database.EnsureCreated();
    }
}
