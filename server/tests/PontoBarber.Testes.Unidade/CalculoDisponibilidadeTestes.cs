using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Disponibilidade;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>Task 6.4: limites de consulta de disponibilidade.</summary>
[TestClass]
public sealed class CalculoDisponibilidadeTestes
{
    private static readonly DateOnly _hoje = new(2026, 11, 22);

    [TestMethod]
    public void ValidarJanela_DentroDaJanelaPadrao_Aceita()
    {
        ConsultarDisponibilidade.ValidarJanela(_hoje, _hoje.AddDays(15), null);
    }

    [TestMethod]
    public void ValidarJanela_DentroDoLimiteMaximo_Aceita()
    {
        ConsultarDisponibilidade.ValidarJanela(_hoje, _hoje.AddDays(30), 30);
    }

    [TestMethod]
    public void ValidarJanela_AlemDoLimiteMaximo_Rejeita()
    {
        var excecao = Assert.ThrowsExactly<ExcecaoValidacaoAplicacao>(
            () => ConsultarDisponibilidade.ValidarJanela(_hoje, _hoje.AddDays(31), 31));

        Assert.IsTrue(excecao.Erros.ContainsKey("dias"));
    }

    [TestMethod]
    public void ValidarJanela_DataPassada_Rejeita()
    {
        var excecao = Assert.ThrowsExactly<ExcecaoValidacaoAplicacao>(
            () => ConsultarDisponibilidade.ValidarJanela(_hoje, _hoje.AddDays(-1), null));

        Assert.IsTrue(excecao.Erros.ContainsKey("data"));
    }

    [TestMethod]
    public void ValidarJanela_SemParametroAlteraLimite_AceitaAte15Dias()
    {
        Assert.ThrowsExactly<ExcecaoValidacaoAplicacao>(
            () => ConsultarDisponibilidade.ValidarJanela(_hoje, _hoje.AddDays(16), null));
    }
}

#pragma warning restore CA1707
