using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Notificacoes;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

[TestClass]
public sealed class NotificacoesTestes
{
    private static readonly Guid _barbeariaId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly Guid _agendamentoId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid _eventoId = Guid.NewGuid();
    private static readonly DateTime _agora = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Mapear_TodosOsEventosDoCiclo_RetornaTipo()
    {
        var eventos = new[]
        {
            TipoEventoHistorico.Criacao, TipoEventoHistorico.Cancelamento,
            TipoEventoHistorico.SugestaoCriada, TipoEventoHistorico.SugestaoAceita,
            TipoEventoHistorico.SugestaoRejeitada, TipoEventoHistorico.SugestaoExpirada,
            TipoEventoHistorico.ServicoIniciado, TipoEventoHistorico.ServicoConcluido,
            TipoEventoHistorico.PagamentoRegistrado
        };

        Assert.AreEqual(9, eventos.Count(evento => NotificacoesPorEvento.Mapear(evento) is not null));
        Assert.IsNull(NotificacoesPorEvento.Mapear(TipoEventoHistorico.Remarcacao));
    }

    [TestMethod]
    public void SugestaoCriada_DestinatarioEhApenasCliente()
    {
        var notificacoes = NotificacoesPorEvento.Criar(
            _barbeariaId, _agendamentoId, _eventoId, TipoNotificacao.SugestaoCriada,
            "cliente-1", "barbeiro-1", "dono-1", "Corte", _agora);

        Assert.AreEqual(1, notificacoes.Count);
        Assert.AreEqual("cliente-1", notificacoes[0].DestinatarioUsuarioId);
        Assert.AreEqual(_eventoId, notificacoes[0].EventId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(notificacoes[0].Mensagem));
    }

    [TestMethod]
    public void SugestaoRejeitada_DestinatariosSaoProfissionalEDono()
    {
        var notificacoes = NotificacoesPorEvento.Criar(
            _barbeariaId, _agendamentoId, _eventoId, TipoNotificacao.SugestaoRejeitada,
            "cliente-1", "barbeiro-1", "dono-1", "Corte", _agora);

        Assert.AreEqual(2, notificacoes.Count);
        Assert.IsFalse(notificacoes.Any(item => item.DestinatarioUsuarioId == "cliente-1"));
        Assert.IsTrue(notificacoes.Any(item => item.DestinatarioUsuarioId == "barbeiro-1"));
        Assert.IsTrue(notificacoes.Any(item => item.DestinatarioUsuarioId == "dono-1"));
    }

    [TestMethod]
    public void AgendamentoCriado_DestinatariosSaoEnvolvidos_SemDuplicarMesmoUsuario()
    {
        var notificacoes = NotificacoesPorEvento.Criar(
            _barbeariaId, _agendamentoId, _eventoId, TipoNotificacao.AgendamentoCriado,
            "cliente-1", "barbeiro-1", "barbeiro-1", "Corte", _agora);

        Assert.AreEqual(2, notificacoes.Count);
        Assert.AreEqual(2, notificacoes.Select(item => item.DestinatarioUsuarioId).Distinct().Count());
    }

    [TestMethod]
    public void MarcarComoLida_OutroUsuario_Rejeita()
    {
        var notificacao = Notificacao.Criar(
            _barbeariaId, "cliente-1", TipoNotificacao.AgendamentoCriado,
            _agendamentoId, "Mensagem", _eventoId, _agora);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => notificacao.MarcarComoLida("outro", _agora));
    }

    [TestMethod]
    public void MarcarComoLida_Idempotente()
    {
        var notificacao = Notificacao.Criar(
            _barbeariaId, "cliente-1", TipoNotificacao.AgendamentoCriado,
            _agendamentoId, "Mensagem", _eventoId, _agora);

        notificacao.MarcarComoLida("cliente-1", _agora);
        notificacao.MarcarComoLida("cliente-1", _agora.AddHours(1));

        Assert.AreEqual(_agora, notificacao.LidoEmUtc);
    }
}

#pragma warning restore CA1707
