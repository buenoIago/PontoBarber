using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Servicos;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>Task 7.1: snapshot, término e valor do agendamento.</summary>
[TestClass]
public sealed class AgendamentoTestes
{
    private static readonly Guid _barbeariaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime _inicio = new(2026, 11, 30, 13, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Criar_CalculaTerminoEValorESnapshotDoServico()
    {
        var agendamento = CriarAgendamento();

        Assert.AreEqual(_inicio.AddMinutes(40), agendamento.TerminaEmUtc);
        Assert.AreEqual(80m, agendamento.PrecoSnapshot);
        Assert.AreEqual("Corte degradê", agendamento.NomeServicoSnapshot);
        Assert.AreEqual(40, agendamento.DuracaoSnapshot);
        Assert.AreEqual(SituacaoAgendamento.Confirmado, agendamento.Situacao);
        Assert.AreEqual(OrigemAgendamento.Publico, agendamento.Origem);
        Assert.AreEqual(0, agendamento.Validar().Count);
    }

    [TestMethod]
    public void Snapshot_NaoMudaQuandooServicoEAlterado()
    {
        var servico = Servico.Criar(_barbeariaId, "Corte degradê", 40, 80m);
        var agendamento = Agendamento.Criar(
            _barbeariaId, "cliente-1", Guid.NewGuid(), servico.Id, _inicio,
            servico.Nome, servico.DuracaoMinutos, servico.Preco!.Value, null, OrigemAgendamento.Publico);

        servico.AtualizarDados("Corte premium", 60, 150m, true);

        Assert.AreEqual("Corte degradê", agendamento.NomeServicoSnapshot);
        Assert.AreEqual(40, agendamento.DuracaoSnapshot);
        Assert.AreEqual(80m, agendamento.PrecoSnapshot);
    }

    [TestMethod]
    public void Criar_PeriodoInvertido_Rejeita()
    {
        var excecao = Assert.ThrowsExactly<InvalidOperationException>(() =>
            Agendamento.Criar(
                _barbeariaId, "cliente-1", Guid.NewGuid(), Guid.NewGuid(),
                _inicio, "Corte", -10, 50m, null, OrigemAgendamento.Publico));

        StringAssert.Contains(excecao.Message, "término");
    }

    [TestMethod]
    public void Atualizar_Generica_RejeitaAlteracaoHistorica()
    {
        var original = CriarAgendamento();
        var alterado = Agendamento.Criar(
            _barbeariaId, "cliente-1", original.ProfissionalId, original.ServicoId,
            _inicio.AddHours(1), "Outro", 30, 10m, null, OrigemAgendamento.Manual);

        Assert.ThrowsExactly<InvalidOperationException>(() => original.Atualizar(alterado));
    }

    private static Agendamento CriarAgendamento() =>
        Agendamento.Criar(
            _barbeariaId, "cliente-1", Guid.NewGuid(), Guid.NewGuid(), _inicio,
            "Corte degradê", 40, 80m, "Sem especificação", OrigemAgendamento.Publico);
}

#pragma warning restore CA1707
