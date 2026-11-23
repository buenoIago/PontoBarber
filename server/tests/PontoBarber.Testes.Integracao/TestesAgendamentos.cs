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

    [TestMethod]
    public async Task Cancelamento_ClienteCancelaProprioELiberaHorario()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var inicio = ProximoInicio();
        var agendamentoId = await CriarAgendamentoAsync(cenario.Cliente, cenario, inicio);

        var resposta = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, agendamentoId, "cancelar"),
            new { motivo = "Desistência" });

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual("Cancelado", corpo.GetProperty("situacao").GetString());
        Assert.AreEqual(cenario.ClienteId.ToString(), corpo.GetProperty("canceladoPorUsuarioId").GetString());

        using var outroCliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarClienteAsync(outroCliente);
        var reocupacao = await EnviarComAntiforgeryAsync(
            outroCliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, inicio));
        Assert.AreEqual(HttpStatusCode.Created, reocupacao.StatusCode);
    }

    [TestMethod]
    public async Task Cancelamento_ClienteNaoCancelaAgendamentoDeOutro()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var agendamentoId = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());

        using var outroCliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarClienteAsync(outroCliente);
        var resposta = await EnviarComAntiforgeryAsync(
            outroCliente, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, agendamentoId, "cancelar"),
            new { motivo = "Indevido" });

        Assert.AreEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Cancelamento_BarbeiroEProprietario_RespeitamPermissao()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var idBarbeiro = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());
        var idDono = await CriarAgendamentoAsync(cenario.Cliente, cenario, HorarioLocal(1, 11));

        var peloBarbeiro = await EnviarComAntiforgeryAsync(
            cenario.Profissional, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, idBarbeiro, "cancelar"),
            new { motivo = "Cancelado pelo barbeiro" });
        var peloDono = await EnviarComAntiforgeryAsync(
            cenario.Dono, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, idDono, "cancelar"),
            new { motivo = "Cancelado pelo proprietário" });

        Assert.AreEqual(HttpStatusCode.OK, peloBarbeiro.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, peloDono.StatusCode);
    }

    [TestMethod]
    public async Task Cancelamento_SemMotivoDoProfissional_Rejeita()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var agendamentoId = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());

        var resposta = await EnviarComAntiforgeryAsync(
            cenario.Profissional, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, agendamentoId, "cancelar"),
            new { motivo = (string?)null });

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [TestMethod]
    public async Task Cancelamento_DuplicadoEAposInicio_Rejeita()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var primeiro = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());
        var segundo = await CriarAgendamentoAsync(cenario.Cliente, cenario, HorarioLocal(1, 11));

        var duplicado = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, primeiro, "cancelar"), new { });
        var segundaTentativa = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, primeiro, "cancelar"), new { });
        Assert.AreEqual(HttpStatusCode.OK, duplicado.StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, segundaTentativa.StatusCode);
        Assert.AreEqual(CodigosErro.AgendamentoEstadoInvalido,
            (await LerCorpoAsync(segundaTentativa)).GetProperty("code").GetString());

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            var passado = DateTime.UtcNow.AddMinutes(-10);
            await contexto.Database.ExecuteSqlRawAsync(
                "UPDATE [Agendamentos] SET [IniciaEmUtc] = {0}, [TerminaEmUtc] = {1} WHERE [Id] = {2}",
                passado, passado.AddMinutes(40), segundo);
        }

        var iniciado = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, segundo, "cancelar"), new { });
        Assert.AreEqual(HttpStatusCode.Conflict, iniciado.StatusCode);
        Assert.AreEqual(CodigosErro.AgendamentoJaIniciado,
            (await LerCorpoAsync(iniciado)).GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Remarcacao_Valida_MudaHorarioEPreservaSnapshot()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var agendamentoId = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());
        var novoInicio = HorarioLocal(1, 11);

        var resposta = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, agendamentoId, "remarcar"),
            new { iniciaEmUtc = novoInicio.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) });

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerCorpoAsync(resposta);
        Assert.AreEqual(novoInicio, corpo.GetProperty("iniciaEmUtc").GetDateTime());
        Assert.AreEqual("Corte degradê", corpo.GetProperty("nomeServico").GetString());
        Assert.AreEqual(40, corpo.GetProperty("duracaoMinutos").GetInt32());
        Assert.AreEqual<decimal>(80m, corpo.GetProperty("preco").GetDecimal());

        var historico = await LerCorpoAsync(await cenario.Cliente.GetAsync(
            $"/api/v1/barbearias/{cenario.BarbeariaId}/agendamentos/{agendamentoId}/historico"));
        Assert.IsTrue(historico.EnumerateArray().Any(item => item.GetProperty("tipoEvento").GetString() == "Remarcacao"));
    }

    [TestMethod]
    public async Task Remarcacao_HorarioOcupado_MantemHorarioOriginal()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var original = ProximoInicio();
        var agendamentoId = await CriarAgendamentoAsync(cenario.Cliente, cenario, original);
        await CriarAgendamentoAsync(cenario.Cliente, cenario, HorarioLocal(1, 11));

        var resposta = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, agendamentoId, "remarcar"),
            new { iniciaEmUtc = HorarioLocal(1, 11).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) });

        Assert.AreEqual(HttpStatusCode.Conflict, resposta.StatusCode);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var persistido = await contexto.Set<Agendamento>().IgnoreQueryFilters().SingleAsync(item => item.Id == agendamentoId);
        Assert.AreEqual(original, persistido.IniciaEmUtc);
    }

    [TestMethod]
    public async Task Remarcacao_Concorrente_AceitaSomenteUma()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var idA = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());
        var idB = await CriarAgendamentoAsync(cenario.Cliente, cenario, HorarioLocal(1, 11));
        var alvo = HorarioLocal(2, 10);
        var corpo = new { iniciaEmUtc = alvo.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) };

        var respostas = await Task.WhenAll(
            EnviarComAntiforgeryAsync(cenario.Cliente, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, idA, "remarcar"), corpo),
            EnviarComAntiforgeryAsync(cenario.Cliente, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, idB, "remarcar"), corpo));

        Assert.AreEqual(1, respostas.Count(resposta => resposta.StatusCode == HttpStatusCode.OK));
        Assert.AreEqual(1, respostas.Count(resposta => resposta.StatusCode == HttpStatusCode.Conflict));
    }

    [TestMethod]
    public async Task Sugestao_FluxoCompleto_ComAceiteTransacional()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var agendamentoId = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());
        var sugerido = HorarioLocal(1, 11);

        var criacao = await EnviarComAntiforgeryAsync(
            cenario.Profissional, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, agendamentoId, "sugestoes"),
            new { iniciaEmUtc = sugerido.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), motivo = "Horário melhor" });
        Assert.AreEqual(HttpStatusCode.Created, criacao.StatusCode);
        var sugestaoId = Guid.Parse((await LerCorpoAsync(criacao)).GetProperty("id").GetString()!);

        var aceite = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post,
            UrlAcao(cenario.BarbeariaId, agendamentoId, $"sugestoes/{sugestaoId}/aceitar"), new { });
        Assert.AreEqual(HttpStatusCode.OK, aceite.StatusCode);
        Assert.AreEqual("Aceita", (await LerCorpoAsync(aceite)).GetProperty("situacao").GetString());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var persistido = await contexto.Set<Agendamento>().IgnoreQueryFilters().SingleAsync(item => item.Id == agendamentoId);
        Assert.AreEqual(SituacaoAgendamento.Confirmado, persistido.Situacao);
        Assert.AreEqual(sugerido, persistido.IniciaEmUtc);
        Assert.AreEqual("Corte degradê", persistido.NomeServicoSnapshot);

        var eventos = await contexto.Set<HistoricoAgendamento>().IgnoreQueryFilters()
            .Where(item => item.AgendamentoId == agendamentoId).ToListAsync();
        Assert.IsTrue(eventos.Any(item => item.TipoEvento == TipoEventoHistorico.SugestaoCriada));
        Assert.IsTrue(eventos.Any(item => item.TipoEvento == TipoEventoHistorico.SugestaoAceita));
    }

    [TestMethod]
    public async Task Sugestao_SemMotivoEComConflito_Rejeita()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var agendamentoId = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());
        var sugerido = HorarioLocal(1, 11);
        var body = new { iniciaEmUtc = sugerido.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), motivo = "" };

        var semMotivo = await EnviarComAntiforgeryAsync(
            cenario.Profissional, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, agendamentoId, "sugestoes"), body);
        Assert.AreEqual(HttpStatusCode.BadRequest, semMotivo.StatusCode);

        await CriarAgendamentoAsync(cenario.Cliente, cenario, sugerido);
        var conflito = await EnviarComAntiforgeryAsync(
            cenario.Profissional, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, agendamentoId, "sugestoes"),
            new { body.iniciaEmUtc, motivo = "Horário melhor" });
        Assert.AreEqual(HttpStatusCode.Conflict, conflito.StatusCode);
    }

    [TestMethod]
    public async Task Sugestao_Rejeicao_LiberaReserva()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var agendamentoId = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());
        var sugerido = HorarioLocal(1, 11);
        var criacao = await EnviarComAntiforgeryAsync(
            cenario.Profissional, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, agendamentoId, "sugestoes"),
            new { iniciaEmUtc = sugerido.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), motivo = "Troca" });
        var sugestaoId = Guid.Parse((await LerCorpoAsync(criacao)).GetProperty("id").GetString()!);

        var rejeicao = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post,
            UrlAcao(cenario.BarbeariaId, agendamentoId, $"sugestoes/{sugestaoId}/rejeitar"), new { });
        Assert.AreEqual(HttpStatusCode.OK, rejeicao.StatusCode);

        using var outroCliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarClienteAsync(outroCliente);
        var reocupacao = await EnviarComAntiforgeryAsync(
            outroCliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, sugerido));
        Assert.AreEqual(HttpStatusCode.Created, reocupacao.StatusCode);
    }

    [TestMethod]
    public async Task Sugestao_Expirada_NaoPodeAceitarEJobLiberaReserva()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var agendamentoId = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());
        var sugerido = HorarioLocal(1, 11);
        var criacao = await EnviarComAntiforgeryAsync(
            cenario.Profissional, HttpMethod.Post, UrlAcao(cenario.BarbeariaId, agendamentoId, "sugestoes"),
            new { iniciaEmUtc = sugerido.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), motivo = "Troca" });
        var sugestaoId = Guid.Parse((await LerCorpoAsync(criacao)).GetProperty("id").GetString()!);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            await contexto.Database.ExecuteSqlRawAsync(
                "UPDATE [SugestoesHorario] SET [ExpiraEmUtc] = {0} WHERE [Id] = {1}",
                DateTime.UtcNow.AddHours(-1), sugestaoId);
        }

        var aceiteExpirado = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post,
            UrlAcao(cenario.BarbeariaId, agendamentoId, $"sugestoes/{sugestaoId}/aceitar"), new { });
        Assert.AreEqual(HttpStatusCode.Conflict, aceiteExpirado.StatusCode);
        Assert.AreEqual(CodigosErro.SugestaoExpirada,
            (await LerCorpoAsync(aceiteExpirado)).GetProperty("code").GetString());

        var job = await EnviarComAntiforgeryAsync(
            cenario.Cliente, HttpMethod.Post, "/api/v1/jobs/expirar-sugestoes", new { });
        Assert.AreEqual(HttpStatusCode.OK, job.StatusCode);
        Assert.IsTrue((await LerCorpoAsync(job)).GetProperty("expiradas").GetInt32() >= 1);

        using var outroCliente = fabrica.CreateClient(_opcoesCliente);
        await RegistrarClienteAsync(outroCliente);
        var reocupacao = await EnviarComAntiforgeryAsync(
            outroCliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, sugerido));
        Assert.AreEqual(HttpStatusCode.Created, reocupacao.StatusCode);
    }

    [TestMethod]
    public async Task Historico_AppendOnly_RejeitaEdicaoERemocao()
    {
        using var fabrica = CriarFabricaComBanco();
        CriarSchema(fabrica);
        var cenario = await CriarCenarioAsync(fabrica);
        var agendamentoId = await CriarAgendamentoAsync(cenario.Cliente, cenario, ProximoInicio());

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var evento = await contexto.Set<HistoricoAgendamento>().IgnoreQueryFilters().FirstAsync();

        Assert.ThrowsExactly<InvalidOperationException>(() => evento.Atualizar(evento));
        contexto.Set<HistoricoAgendamento>().Remove(evento);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => contexto.SaveChangesAsync());
    }

    private static string UrlAcao(Guid barbeariaId, Guid agendamentoId, string acao) =>
        $"/api/v1/barbearias/{barbeariaId}/agendamentos/{agendamentoId}/{acao}";

    private static DateTime HorarioLocal(int dias, int hora)
    {
        var data = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, _fuso)).AddDays(dias);
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(data.ToDateTime(new TimeOnly(hora, 0)), DateTimeKind.Unspecified), _fuso);
    }

    private static async Task<Guid> CriarAgendamentoAsync(
        HttpClient cliente, Cenario cenario, DateTime inicio)
    {
        var resposta = await EnviarComAntiforgeryAsync(
            cliente, HttpMethod.Post, PublicUrl(cenario.BarbeariaId),
            NovoAgendamento(cenario.ProfissionalId, cenario.ServicoId, inicio));
        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        return Guid.Parse((await LerCorpoAsync(resposta)).GetProperty("id").GetString()!);
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
