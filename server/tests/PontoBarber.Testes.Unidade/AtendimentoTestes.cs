using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Financeiro;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

[TestClass]
public sealed class AtendimentoTestes
{
    private static readonly Guid _barbeariaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid _profissionalId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly DateTime _inicio = new(2026, 11, 22, 13, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _agora = new(2026, 11, 22, 13, 5, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Iniciar_RegistraHorarioRealEEntraEmAndamento()
    {
        var agendamento = CriarAgendamento();

        agendamento.Iniciar(_agora);

        Assert.AreEqual(SituacaoAgendamento.EmAndamento, agendamento.Situacao);
        Assert.AreEqual(_agora, agendamento.InicioRealUtc);
        Assert.IsNull(agendamento.FimRealUtc);
        Assert.AreEqual(0, agendamento.Validar().Count);
    }

    [TestMethod]
    public void Iniciar_EmCanceladoOuNaoCompareceu_Rejeita()
    {
        var cancelado = CriarAgendamento();
        cancelado.Cancelar("sistema", "Motivo", _inicio.AddMinutes(-1));
        var ausente = CriarAgendamento();
        ausente.RegistrarNaoComparecimentoManual(_agora);

        Assert.ThrowsExactly<InvalidOperationException>(() => cancelado.Iniciar(_agora));
        Assert.ThrowsExactly<InvalidOperationException>(() => ausente.Iniciar(_agora));
    }

    [TestMethod]
    public void Iniciar_Duplicado_Rejeita()
    {
        var agendamento = CriarAgendamento();
        agendamento.Iniciar(_agora);

        Assert.ThrowsExactly<InvalidOperationException>(() => agendamento.Iniciar(_agora.AddMinutes(1)));
    }

    [TestMethod]
    public void Concluir_RegistraFimReal()
    {
        var agendamento = CriarAgendamento();
        agendamento.Iniciar(_agora);

        agendamento.Concluir(_agora.AddMinutes(40));

        Assert.AreEqual(SituacaoAgendamento.Concluido, agendamento.Situacao);
        Assert.AreEqual(_agora.AddMinutes(40), agendamento.FimRealUtc);
        Assert.AreEqual(0, agendamento.Validar().Count);
    }

    [TestMethod]
    public void Concluir_SemInicioOuDuplicado_Rejeita()
    {
        var semInicio = CriarAgendamento();
        Assert.ThrowsExactly<InvalidOperationException>(() => semInicio.Concluir(_agora));

        var duplicado = CriarAgendamento();
        duplicado.Iniciar(_agora);
        duplicado.Concluir(_agora.AddMinutes(40));
        Assert.ThrowsExactly<InvalidOperationException>(
            () => duplicado.Concluir(_agora.AddMinutes(41)));
    }

    [TestMethod]
    public void NaoComparecimento_PermaneceDistintoDeCancelamento()
    {
        var ausente = CriarAgendamento();
        ausente.RegistrarNaoComparecimentoManual(_agora);

        Assert.AreEqual(SituacaoAgendamento.NaoCompareceu, ausente.Situacao);
        Assert.IsNull(ausente.CanceladoEmUtc);
        Assert.IsNull(ausente.CanceladoPorUsuarioId);
        Assert.ThrowsExactly<InvalidOperationException>(() => ausente.Concluir(_agora));
    }

    [TestMethod]
    public void Pagamento_Valido_RegistradoComUmaCasa()
    {
        var pagamento = Pagamento.Criar(
            _barbeariaId, Guid.NewGuid(), 80.5m, FormaPagamento.Pix, _agora, "barbeiro-1");

        Assert.AreEqual(80.5m, pagamento.Valor);
        Assert.AreEqual(FormaPagamento.Pix, pagamento.Metodo);
        Assert.AreEqual(_agora, pagamento.RegistradoEmUtc);
        Assert.AreEqual(0, pagamento.Validar().Count);
    }

    [TestMethod]
    public void Pagamento_ValorNegativoOuComTresCasas_Rejeita()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Pagamento.Criar(
            _barbeariaId, Guid.NewGuid(), -1m, FormaPagamento.Cash, _agora, "barbeiro-1"));
        Assert.ThrowsExactly<InvalidOperationException>(() => Pagamento.Criar(
            _barbeariaId, Guid.NewGuid(), 10.123m, FormaPagamento.Cash, _agora, "barbeiro-1"));
    }

    [TestMethod]
    public void Pagamento_AtualizarGenerica_Rejeita()
    {
        var pagamento = Pagamento.Criar(
            _barbeariaId, Guid.NewGuid(), 80m, FormaPagamento.Cash, _agora, "barbeiro-1");
        var alterado = Pagamento.Criar(
            _barbeariaId, pagamento.AgendamentoId, 90m, FormaPagamento.Pix, _agora, "barbeiro-1");

        Assert.ThrowsExactly<InvalidOperationException>(() => pagamento.Atualizar(alterado));
    }

    private static Agendamento CriarAgendamento() =>
        Agendamento.Criar(
            _barbeariaId, "cliente-1", _profissionalId, Guid.NewGuid(), _inicio,
            "Corte", 40, 80m, null, OrigemAgendamento.Publico);
}

#pragma warning restore CA1707
