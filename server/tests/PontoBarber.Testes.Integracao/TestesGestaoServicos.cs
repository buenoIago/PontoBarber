using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>Task 5.2: criação e edição do catálogo com permissões por cargo.</summary>
[TestClass]
public sealed class TestesGestaoServicos
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly WebApplicationFactoryClientOptions _opcoesCliente = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    public async Task Criar_ComProprietario_Retorna201EPersisteServico()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);

        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, $"/api/v1/barbearias/{barbearia.Id}/servicos",
            new { nome = "Corte premium", duracaoMinutos = 45, preco = 80.50m });

        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual("Corte premium", corpo.GetProperty("nome").GetString());
        Assert.AreEqual(45, corpo.GetProperty("duracaoMinutos").GetInt32());
        Assert.AreEqual(80.50m, corpo.GetProperty("preco").GetDecimal());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var persistido = await contexto.Set<Servico>()
            .IgnoreQueryFilters().SingleAsync(item => item.Id == Guid.Parse(corpo.GetProperty("id").GetString()!));
        Assert.AreEqual(barbearia.Id, persistido.BarbeariaId);
        Assert.IsFalse(persistido.EhModelo);
    }

    [TestMethod]
    public async Task Criar_ComPrecoNegativoOuDuracaoInvalida_Retorna400()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);
        var rota = $"/api/v1/barbearias/{barbearia.Id}/servicos";

        var preco = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, rota, new { nome = "Corte", duracaoMinutos = 30, preco = -1m });
        Assert.AreEqual(HttpStatusCode.BadRequest, preco.StatusCode);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou, (await LerCorpoAsync(preco)).GetProperty("code").GetString());

        var duracao = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, rota, new { nome = "Corte", duracaoMinutos = 0, preco = 50m });
        Assert.AreEqual(HttpStatusCode.BadRequest, duracao.StatusCode);
    }

    [TestMethod]
    public async Task Criar_ComBarbeiro_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        var entrada = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, entrada.StatusCode);

        var resposta = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, $"/api/v1/barbearias/{barbearia.Id}/servicos",
            new { nome = "Corte", duracaoMinutos = 30, preco = 50m });

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.AreEqual(CodigosErro.TenantNegado, (await LerCorpoAsync(resposta)).GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Atualizar_ComProprietario_EditaTodosOsCamposPermitidos()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);
        var servico = await CriarServicoAsync(cliente, barbearia.Id, "Corte", 30, 50m);

        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Put, $"/api/v1/barbearias/{barbearia.Id}/servicos/{servico}",
            new { nome = "Corte premium", duracaoMinutos = 50, preco = 95m, ativo = false });

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual("Corte premium", corpo.GetProperty("nome").GetString());
        Assert.IsFalse(corpo.GetProperty("ativo").GetBoolean());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var persistido = await contexto.Set<Servico>().IgnoreQueryFilters().SingleAsync(item => item.Id == servico);
        Assert.AreEqual(50, persistido.DuracaoMinutos);
        Assert.AreEqual(95m, persistido.Preco);
        Assert.IsFalse(persistido.Ativo);
    }

    [TestMethod]
    public async Task Atualizar_ComBarbeiro_EditaPrecoEDuracao()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);
        var servico = await CriarServicoAsync(proprietario, barbearia.Id, "Corte", 30, 50m);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        var entrada = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, entrada.StatusCode);

        var resposta = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put, $"/api/v1/barbearias/{barbearia.Id}/servicos/{servico}",
            new { duracaoMinutos = 35, preco = 65m });

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual("Corte", corpo.GetProperty("nome").GetString());
        Assert.AreEqual(35, corpo.GetProperty("duracaoMinutos").GetInt32());
        Assert.AreEqual(65m, corpo.GetProperty("preco").GetDecimal());
    }

    [TestMethod]
    public async Task Atualizar_ComBarbeiro_TentaAlterarNomeOuDisponibilidade_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);
        var servico = await CriarServicoAsync(proprietario, barbearia.Id, "Corte", 30, 50m);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        var entrada = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, entrada.StatusCode);
        var rota = $"/api/v1/barbearias/{barbearia.Id}/servicos/{servico}";

        var nome = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put, rota, new { nome = "Corte alterado", preco = 50m });
        Assert.AreEqual(HttpStatusCode.Forbidden, nome.StatusCode);

        var ativo = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Put, rota, new { ativo = false, preco = 50m });
        Assert.AreEqual(HttpStatusCode.Forbidden, ativo.StatusCode);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var persistido = await contexto.Set<Servico>().IgnoreQueryFilters().SingleAsync(item => item.Id == servico);
        Assert.AreEqual("Corte", persistido.Nome);
        Assert.IsTrue(persistido.Ativo);
    }

    [TestMethod]
    public async Task Atualizar_ComServicoDeOutraBarbearia_NaoPermiteAlteracao()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoB);
        var barbeariaB = await CriarBarbeariaAsync(donoB);
        var servicoB = await CriarServicoAsync(donoB, barbeariaB.Id, "Servico B", 30, 50m);

        var resposta = await EnviarComAntiforgeryAsync(
            donoA, HttpMethod.Put, $"/api/v1/barbearias/{barbeariaA.Id}/servicos/{servicoB}",
            new { preco = 1m });

        Assert.AreEqual(HttpStatusCode.NotFound, resposta.StatusCode);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var persistido = await contexto.Set<Servico>().IgnoreQueryFilters().SingleAsync(item => item.Id == servicoB);
        Assert.AreEqual(50m, persistido.Preco);
        Assert.AreEqual(barbeariaB.Id, persistido.BarbeariaId);
    }

    [TestMethod]
    public async Task Remover_ComProprietario_DesativaSemExcluirENaoAlteraDadosOriginais()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);
        var servico = await CriarServicoAsync(cliente, barbearia.Id, "Corte", 30, 50m);

        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Delete, $"/api/v1/barbearias/{barbearia.Id}/servicos/{servico}", new { });

        Assert.AreEqual(HttpStatusCode.NoContent, resposta.StatusCode);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var persistido = await contexto.Set<Servico>().IgnoreQueryFilters().SingleAsync(item => item.Id == servico);
        Assert.IsFalse(persistido.Ativo);
        Assert.AreEqual("Corte", persistido.Nome);
        Assert.AreEqual(30, persistido.DuracaoMinutos);
        Assert.AreEqual(50m, persistido.Preco);
    }

    [TestMethod]
    public async Task Remover_ComBarbeiro_Retorna403()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);
        var servico = await CriarServicoAsync(proprietario, barbearia.Id, "Corte", 30, 50m);

        using var barbeiro = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(barbeiro);
        var entrada = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Post, "/api/v1/barbearias/entrar",
            new { codigoConvite = barbearia.CodigoConvite });
        Assert.AreEqual(HttpStatusCode.OK, entrada.StatusCode);

        var resposta = await EnviarComAntiforgeryAsync(
            barbeiro, HttpMethod.Delete, $"/api/v1/barbearias/{barbearia.Id}/servicos/{servico}", new { });

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        Assert.IsTrue((await contexto.Set<Servico>().IgnoreQueryFilters().SingleAsync(item => item.Id == servico)).Ativo);
    }

    [TestMethod]
    public async Task Listar_ComCliente_NaoRetornaServicoRemovido()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var proprietario = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(proprietario);
        var barbearia = await CriarBarbeariaAsync(proprietario);
        var removido = await CriarServicoAsync(proprietario, barbearia.Id, "Corte removido", 30, 50m);
        var ativo = await CriarServicoAsync(proprietario, barbearia.Id, "Corte ativo", 40, 70m);
        await EnviarComAntiforgeryAsync(
            proprietario, HttpMethod.Delete, $"/api/v1/barbearias/{barbearia.Id}/servicos/{removido}", new { });

        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarClienteEEntrarAsync(cliente);
        var resposta = await cliente.GetAsync($"/api/v1/barbearias/{barbearia.Id}/servicos");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        var ids = corpo.EnumerateArray().Select(item => Guid.Parse(item.GetProperty("id").GetString()!)).ToList();
        Assert.IsFalse(ids.Contains(removido));
        Assert.IsTrue(ids.Contains(ativo));
        Assert.IsTrue(corpo.EnumerateArray().All(item => item.GetProperty("ativo").GetBoolean()));
    }

    [TestMethod]
    public async Task Listar_ComProprietario_IncluiServicoInativo()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        using var cliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(cliente);
        var barbearia = await CriarBarbeariaAsync(cliente);
        var removido = await CriarServicoAsync(cliente, barbearia.Id, "Corte removido", 30, 50m);
        await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Delete, $"/api/v1/barbearias/{barbearia.Id}/servicos/{removido}", new { });

        var resposta = await cliente.GetAsync($"/api/v1/barbearias/{barbearia.Id}/servicos");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        var item = corpo.EnumerateArray().Single(entry => Guid.Parse(entry.GetProperty("id").GetString()!) == removido);
        Assert.IsFalse(item.GetProperty("ativo").GetBoolean());
        Assert.AreEqual("Corte removido", item.GetProperty("nome").GetString());
    }

    [TestMethod]
    public async Task Listar_NaoIncluiServicosDeOutraBarbearia()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoB);
        var barbeariaB = await CriarBarbeariaAsync(donoB);
        var servicoB = await CriarServicoAsync(donoB, barbeariaB.Id, "Servico B", 30, 50m);

        var resposta = await donoA.GetAsync($"/api/v1/barbearias/{barbeariaA.Id}/servicos");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.IsFalse(corpo.EnumerateArray().Any(item =>
            Guid.Parse(item.GetProperty("id").GetString()!) == servicoB));
    }

    [TestMethod]
    public async Task Remover_ComServicoDeOutraBarbearia_Retorna404()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        using var donoA = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoA);
        var barbeariaA = await CriarBarbeariaAsync(donoA);

        using var donoB = fabrica.CreateClient(_opcoesCliente);
        await RegistrarEEntrarAsync(donoB);
        var barbeariaB = await CriarBarbeariaAsync(donoB);
        var servicoB = await CriarServicoAsync(donoB, barbeariaB.Id, "Servico B", 30, 50m);

        var resposta = await EnviarComAntiforgeryAsync(
            donoA, HttpMethod.Delete, $"/api/v1/barbearias/{barbeariaA.Id}/servicos/{servicoB}", new { });

        Assert.AreEqual(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    private static async Task RegistrarClienteEEntrarAsync(HttpClient cliente)
    {
        var email = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        var cadastro = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/register/customer",
            new { nomeCompleto = "Cliente Teste", email, telefone = "11999999999", senha, confirmarSenha = senha });
        Assert.AreEqual(HttpStatusCode.Created, cadastro.StatusCode);

        var login = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha });
        Assert.AreEqual(HttpStatusCode.NoContent, login.StatusCode);
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberGestaoServicos_{Guid.NewGuid():N}";
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

    private static async Task<Guid> CriarServicoAsync(
        HttpClient cliente, Guid barbeariaId, string nome, int duracao, decimal preco)
    {
        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, $"/api/v1/barbearias/{barbeariaId}/servicos",
            new { nome, duracaoMinutos = duracao, preco });
        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        return Guid.Parse((await LerCorpoAsync(resposta)).GetProperty("id").GetString()!);
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
