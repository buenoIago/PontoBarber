using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Harness de carga local (k6 não está disponível neste ambiente). Executa
/// contra um banco SQL Server isolado em container, com dados representativos,
/// e registra p50/p95/p99, vazão e taxa de erro por fluxo.
/// </summary>
[TestClass]
public sealed class TestesCarga
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private static readonly TimeZoneInfo _fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private static readonly WebApplicationFactoryClientOptions _opcoes = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    [TestMethod]
    [TestCategory("Carga")]
    public async Task Carga_AgendaDashboardEConfirmacao_RespeitamMetasDoTdd()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);

        var dono = fabrica.CreateClient(_opcoes);
        await RegistrarProfissionalAsync(dono);
        var barbearia = await CriarBarbeariaAsync(dono);
        var donoId = await ObterUsuarioIdAsync(dono);

        var cliente = fabrica.CreateClient(_opcoes);
        await RegistrarClienteAsync(cliente);
        var clienteId = await ObterUsuarioIdAsync(cliente);
        var csrf = await ObterCsrfAsync(dono);

        var (profissionalIds, servicoId) = SemearDados(fabrica, barbearia.Id, donoId, clienteId);
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, _fuso));
        var fim = hoje.AddDays(14);
        var periodo = $"de={hoje:yyyy-MM-dd}&ate={fim:yyyy-MM-dd}";

        var agenda = await MedirAsync(
            () => dono.GetAsync($"/api/v1/barbearias/{barbearia.Id}/agendamentos?{periodo}"),
            total: 200, concorrencia: 10);
        var dashboard = await MedirAsync(
            () => dono.GetAsync($"/api/v1/barbearias/{barbearia.Id}/dashboard"),
            total: 100, concorrencia: 10);
        var confirmacao = await MedirAsync(
            () => EnviarAsync(dono, HttpMethod.Post,
                $"/api/v1/barbearias/{barbearia.Id}/agendamentos",
                NovoAgendamento(profissionalIds, servicoId, clienteId, hoje), csrf),
            total: 100, concorrencia: 5);

        Console.WriteLine("[CARGA] " + agenda.Resumo("agenda"));
        Console.WriteLine("[CARGA] " + dashboard.Resumo("dashboard"));
        Console.WriteLine("[CARGA] " + confirmacao.Resumo("confirmacao"));

        Assert.AreEqual(0, agenda.ErrosInesperados, "Agenda retornou erro inesperado.");
        Assert.AreEqual(0, dashboard.ErrosInesperados, "Dashboard retornou erro inesperado.");
        Assert.IsTrue(agenda.P95.TotalMilliseconds <= 2000, $"Agenda p95={agenda.P95.TotalMilliseconds:F0}ms > 2000ms");
        Assert.IsTrue(dashboard.P95.TotalMilliseconds <= 2000, $"Dashboard p95={dashboard.P95.TotalMilliseconds:F0}ms > 2000ms");
        Assert.IsTrue(confirmacao.P95.TotalMilliseconds <= 3000,
            $"Confirmação p95={confirmacao.P95.TotalMilliseconds:F0}ms > 3000ms");
    }

    private static (Guid[] ProfissionalIds, Guid ServicoId) SemearDados(
        WebApplicationFactory<Program> fabrica, Guid barbeariaId, string donoId, string clienteId)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();

        // O cadastro via HTTP já criou o perfil do proprietário e seu vínculo.
        foreach (var dia in Enumerable.Range(1, 7))
        {
            contexto.Set<HorarioFuncionamento>().Add(HorarioFuncionamento.Criar(
                barbeariaId,
                (Dominio.Compartilhado.DiaSemana)dia,
                new TimeOnly(9, 0),
                new TimeOnly(18, 0),
                false));
        }

        var perfis = new List<PerfilProfissional>();
        for (var indice = 0; indice < 10; indice++)
        {
            var perfil = PerfilProfissional.Criar(
                $"carga-prof-{indice}", $"Profissional {indice}", null);
            perfis.Add(perfil);
            contexto.Set<PerfilProfissional>().Add(perfil);
            contexto.Set<VinculoProfissional>().Add(VinculoProfissional.CriarBarbeiro(barbeariaId, perfil.Id));
        }

        foreach (var perfil in perfis)
        {
            foreach (var dia in Enumerable.Range(1, 7))
            {
                contexto.Set<DisponibilidadeSemanal>().Add(DisponibilidadeSemanal.Criar(
                    barbeariaId,
                    perfil.Id,
                    (Dominio.Compartilhado.DiaSemana)dia,
                    new TimeOnly(9, 0),
                    new TimeOnly(18, 0)));
            }
        }

        var servico = Servico.Criar(barbeariaId, "Corte carga", 40, 80m);
        contexto.Set<Servico>().Add(servico);

        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, _fuso));
        for (var dia = 0; dia < 14; dia++)
        {
            for (var indice = 0; indice < 10; indice++)
            {
                var inicio = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(
                        hoje.AddDays(dia).ToDateTime(new TimeOnly(9, 0).AddMinutes(indice * 40)),
                        DateTimeKind.Unspecified),
                    _fuso);
                contexto.Set<Agendamento>().Add(Agendamento.Criar(
                    barbeariaId, clienteId, perfis[indice].Id, servico.Id, inicio,
                    servico.Nome, servico.DuracaoMinutos, servico.Preco!.Value, null,
                    OrigemAgendamento.Manual));
            }
        }

        contexto.SaveChanges();
        return (perfis.Select(item => item.Id).ToArray(), servico.Id);
    }

    private static object NovoAgendamento(
        Guid[] profissionalIds, Guid servicoId, string clienteId, DateOnly hoje)
    {
        var numero = Interlocked.Increment(ref _contadorCarga);
        var indice = numero % profissionalIds.Length;
        var dia = 1 + (numero / profissionalIds.Length) % 10;
        // 16:00 fica dentro do funcionamento (9-18) e fora da faixa semeada
        // (9:00 às 15:40), evitando conflitos de negócio na medição.
        var inicio = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(
                hoje.AddDays(dia).ToDateTime(new TimeOnly(16, 0)),
                DateTimeKind.Unspecified),
            _fuso);

        return new
        {
            profissionalId = profissionalIds[indice],
            servicoId,
            iniciaEmUtc = inicio.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            observacoes = (string?)null,
            clienteId
        };
    }

    private static int _contadorCarga;

    private static async Task<Metricas> MedirAsync(
        Func<Task<HttpResponseMessage>> requisicao, int total, int concorrencia)
    {
        var latencias = new List<TimeSpan>(total);
        var erros = 0;
        string? primeiroErro = null;
        using var limite = new SemaphoreSlim(concorrencia);
        var tarefas = Enumerable.Range(0, total).Select(async _ =>
        {
            await limite.WaitAsync();
            var cronometro = Stopwatch.StartNew();
            try
            {
                using var resposta = await requisicao();
                if (!resposta.IsSuccessStatusCode)
                {
                    Interlocked.Increment(ref erros);
                    primeiroErro ??= $"{(int)resposta.StatusCode} " +
                        (await resposta.Content.ReadAsStringAsync())[..Math.Min(200,
                            (await resposta.Content.ReadAsStringAsync()).Length)];
                }
            }
            finally
            {
                cronometro.Stop();
                lock (latencias) latencias.Add(cronometro.Elapsed);
                limite.Release();
            }
        });
        await Task.WhenAll(tarefas);

        var ordenado = latencias.OrderBy(item => item).ToList();
        return new Metricas(total, concorrencia, ordenado, erros, primeiroErro);
    }

    private static async Task<string> ObterCsrfAsync(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync("/health/live");
        var valor = resposta.Headers.GetValues("Set-Cookie")
            .First(item => item.Contains(ConfiguracaoAuth.CookieRequestToken, StringComparison.Ordinal));
        var trecho = valor.Split(';')[0];
        return Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);
    }

    private static async Task<HttpResponseMessage> EnviarAsync(
        HttpClient cliente, HttpMethod metodo, string rota, object corpo, string csrf)
    {
        using var requisicao = new HttpRequestMessage(metodo, rota)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpo, _json), Encoding.UTF8, "application/json")
        };
        requisicao.Headers.Add(ConfiguracaoAuth.HeaderCsrf, csrf);
        return await cliente.SendAsync(requisicao);
    }

    private static async Task<string> ObterUsuarioIdAsync(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync("/api/v1/auth/me");
        resposta.EnsureSuccessStatusCode();
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.GetProperty("id").GetString()!;
    }

    private static async Task RegistrarProfissionalAsync(HttpClient cliente)
    {
        var email = $"carga-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/auth/register/professional",
            new { nomeCompleto = "Dono Carga", email, telefone = "11999999999", senha, confirmarSenha = senha },
            await ObterCsrfAsync(cliente));
        await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha },
            await ObterCsrfAsync(cliente));
    }

    private static async Task RegistrarClienteAsync(HttpClient cliente)
    {
        var email = $"carga-cliente-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Senha123";
        await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/auth/register/customer",
            new { nomeCompleto = "Cliente Carga", email, telefone = "11999999999", senha, confirmarSenha = senha },
            await ObterCsrfAsync(cliente));
        await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/auth/login", new { email, senha },
            await ObterCsrfAsync(cliente));
    }

    private static async Task<(Guid Id, string Codigo)> CriarBarbeariaAsync(HttpClient cliente)
    {
        var resposta = await EnviarAsync(cliente, HttpMethod.Post, "/api/v1/barbearias", new
        {
            nome = "Barbearia Carga",
            telefoneComercial = "(11) 99999-9999",
            emailComercial = "contato@exemplo.com",
            cidade = "São Paulo",
            endereco = "Rua Central, 100",
            instagram = (string?)null,
            fusoHorarioId = "America/Sao_Paulo"
        }, await ObterCsrfAsync(cliente));
        resposta.EnsureSuccessStatusCode();
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return (
            Guid.Parse(documento.RootElement.GetProperty("id").GetString()!),
            documento.RootElement.GetProperty("codigoConvite").GetString()!);
    }

    private sealed record Metricas(
        int Total,
        int Concorrencia,
        IReadOnlyList<TimeSpan> Latencias,
        int ErrosInesperados,
        string? PrimeiroErro)
    {
        public TimeSpan P50 => Percentil(0.50);
        public TimeSpan P95 => Percentil(0.95);
        public TimeSpan P99 => Percentil(0.99);
        public double TaxaErro => Total == 0 ? 0 : (double)ErrosInesperados / Total;
        public double RequisicoesPorSegundo => Latencias.Count == 0
            ? 0
            : Total / Math.Max(Latencias.Sum(item => item.TotalSeconds) / Concorrencia, 0.001);

        private TimeSpan Percentil(double fracao)
        {
            if (Latencias.Count == 0) return TimeSpan.Zero;
            var indice = (int)Math.Ceiling(Latencias.Count * fracao) - 1;
            return Latencias[Math.Clamp(indice, 0, Latencias.Count - 1)];
        }

        public string Resumo(string nome) =>
            $"{nome}: req={Total}, concorrencia={Concorrencia}, p50={P50.TotalMilliseconds:F0}ms, " +
            $"p95={P95.TotalMilliseconds:F0}ms, p99={P99.TotalMilliseconds:F0}ms, " +
            $"erros={ErrosInesperados} ({TaxaErro:P2}), rps={RequisicoesPorSegundo:F1}" +
            (PrimeiroErro is null ? string.Empty : $", primeiroErro={PrimeiroErro}");
    }

    private static WebApplicationFactory<Program> CriarFabricaComBanco()
    {
        var database = $"PontoBarberCarga_{Guid.NewGuid():N}";
        var externa = Environment.GetEnvironmentVariable("PONTOBARBER_TEST_CONNECTION");
        string conexao;

        if (!string.IsNullOrWhiteSpace(externa))
        {
            // Ambiente sem daemon Docker: usa SQL Server LocalDB isolado.
            var construtorMaster = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(externa)
            {
                InitialCatalog = "master"
            };
            using (var conexaoMaster = new Microsoft.Data.SqlClient.SqlConnection(construtorMaster.ToString()))
            {
                conexaoMaster.Open();
                using var comando = conexaoMaster.CreateCommand();
                comando.CommandText = $"CREATE DATABASE [{database}]";
                comando.ExecuteNonQuery();
            }

            var construtor = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(externa)
            {
                InitialCatalog = database,
                TrustServerCertificate = true
            };
            conexao = construtor.ToString();
        }
        else
        {
            conexao = ConfiguracaoTestesIntegracao.Container.ObterStringConexao(database);
        }

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
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
