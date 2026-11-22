using PontoBarber.Dominio.Modulos.Agendamentos;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>Tasks 8.1 a 8.4: invariantes de domínio do ciclo de vida.</summary>
[TestClass]
public sealed class CicloVidaAgendamentoTestes
{
    private static readonly Guid _barbeariaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid _profissionalId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly DateTime _agora = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _inicio = new(2026, 11, 23, 13, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Cancelar_RegistraAutorMomentoEPeriodoEficaLivre()
    {
        var agendamento = CriarAgendamento();

        agendamento.Cancelar("usuario-1", "Conflito pessoal", _agora);

        Assert.AreEqual(SituacaoAgendamento.Cancelado, agendamento.Situacao);
        Assert.AreEqual("usuario-1", agendamento.CanceladoPorUsuarioId);
        Assert.AreEqual(_agora, agendamento.CanceladoEmUtc);
        Assert.AreEqual("Conflito pessoal", agendamento.MotivoCancelamento);
        Assert.AreEqual(0, agendamento.Validar().Count);
    }

    [TestMethod]
    public void Cancelar_AposInicio_Rejeita()
    {
        var agendamento = CriarAgendamento();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => agendamento.Cancelar("usuario-1", "Motivo", _inicio.AddMinutes(1)));
    }

    [TestMethod]
    public void Cancelar_Duplicado_Rejeita()
    {
        var agendamento = CriarAgendamento();
        agendamento.Cancelar("usuario-1", "Motivo", _agora);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => agendamento.Cancelar("usuario-1", "Motivo", _agora));
    }

    [TestMethod]
    public void Remarcar_MudaPeriodoEPreservaSnapshot()
    {
        var agendamento = CriarAgendamento();
        var novoInicio = _inicio.AddDays(1);

        agendamento.Remarcar(novoInicio, _agora);

        Assert.AreEqual(novoInicio, agendamento.IniciaEmUtc);
        Assert.AreEqual(novoInicio.AddMinutes(40), agendamento.TerminaEmUtc);
        Assert.AreEqual("Corte degradê", agendamento.NomeServicoSnapshot);
        Assert.AreEqual(40, agendamento.DuracaoSnapshot);
        Assert.AreEqual(80m, agendamento.PrecoSnapshot);
    }

    [TestMethod]
    public void Remarcar_AposInicio_Rejeita()
    {
        var agendamento = CriarAgendamento();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => agendamento.Remarcar(_inicio.AddDays(1), _inicio));
    }

    [TestMethod]
    public void Sugestao_SemMotivo_Rejeita()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            SugestaoHorario.Criar(
                _barbeariaId, Guid.NewGuid(), _profissionalId, _inicio.AddDays(1), 40,
                "   ", "barbeiro-1", _agora));
    }

    [TestMethod]
    public void Sugestao_ExpiraEm24Horas()
    {
        var sugestao = CriarSugestao();

        Assert.AreEqual(_agora.AddHours(24), sugestao.ExpiraEmUtc);
        Assert.IsFalse(sugestao.EstaExpirada(_agora.AddHours(23)));
        Assert.IsTrue(sugestao.EstaExpirada(_agora.AddHours(25)));
    }

    [TestMethod]
    public void Sugestao_AceitaExpirada_Rejeita()
    {
        var sugestao = CriarSugestao();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => sugestao.Aceitar(_agora.AddHours(25)));
        Assert.AreEqual(SituacaoSugestao.Pendente, sugestao.Situacao);
    }

    [TestMethod]
    public void Sugestao_AceitarERejeitar_RegistramResposta()
    {
        var aceita = CriarSugestao();
        aceita.Aceitar(_agora.AddHours(1));
        Assert.AreEqual(SituacaoSugestao.Aceita, aceita.Situacao);
        Assert.AreEqual(_agora.AddHours(1), aceita.RespondidoEmUtc);

        var rejeitada = CriarSugestao();
        rejeitada.Rejeitar(_agora.AddHours(1));
        Assert.AreEqual(SituacaoSugestao.Rejeitada, rejeitada.Situacao);
    }

    [TestMethod]
    public void Sugestao_Expirar_RegistraSituacao()
    {
        var sugestao = CriarSugestao();

        sugestao.Expirar(_agora.AddHours(25));

        Assert.AreEqual(SituacaoSugestao.Expirada, sugestao.Situacao);
        Assert.AreEqual(_agora.AddHours(25), sugestao.RespondidoEmUtc);
    }

    [TestMethod]
    public void AceitarSugestao_ReativaAgendamentoNoNovoHorario()
    {
        var agendamento = CriarAgendamento();
        agendamento.Cancelar("barbeiro-1", "Sugestão", _agora);
        var sugestao = SugestaoHorario.Criar(
            _barbeariaId, agendamento.Id, _profissionalId, _inicio.AddDays(1), 40,
            "Horário melhor", "barbeiro-1", _agora);

        agendamento.Reativar(sugestao.InicioSugeridoUtc, _agora.AddHours(1));

        Assert.AreEqual(SituacaoAgendamento.Confirmado, agendamento.Situacao);
        Assert.AreEqual(_inicio.AddDays(1), agendamento.IniciaEmUtc);
        Assert.IsNull(agendamento.CanceladoEmUtc);
        Assert.IsNull(agendamento.CanceladoPorUsuarioId);
        Assert.AreEqual("Corte degradê", agendamento.NomeServicoSnapshot);
    }

    [TestMethod]
    public void Historico_AtualizarOuRemover_Rejeita()
    {
        var evento = HistoricoAgendamento.Criar(
            _barbeariaId, Guid.NewGuid(), TipoEventoHistorico.Cancelamento,
            "usuario-1", _agora, detalhes: "Motivo");

        Assert.ThrowsExactly<InvalidOperationException>(() => evento.Atualizar(evento));
    }

    private static Agendamento CriarAgendamento() =>
        Agendamento.Criar(
            _barbeariaId, "cliente-1", _profissionalId, Guid.NewGuid(), _inicio,
            "Corte degradê", 40, 80m, null, OrigemAgendamento.Publico);

    private static SugestaoHorario CriarSugestao() =>
        SugestaoHorario.Criar(
            _barbeariaId, Guid.NewGuid(), _profissionalId, _inicio.AddDays(1), 40,
            "Horário melhor", "barbeiro-1", _agora);
}

#pragma warning restore CA1707
