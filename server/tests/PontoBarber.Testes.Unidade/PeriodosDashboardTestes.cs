using PontoBarber.Dominio.Modulos.Dashboard;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

[TestClass]
public sealed class PeriodosDashboardTestes
{
    private static readonly TimeZoneInfo _fuso =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    [TestMethod]
    public void Dia_ConverteMeiaNoiteLocalParaUtc()
    {
        var periodo = PeriodosDashboard.Dia(new DateOnly(2026, 11, 22), _fuso);

        Assert.AreEqual(new DateTime(2026, 11, 22, 3, 0, 0, DateTimeKind.Utc), periodo.InicioUtc);
        Assert.AreEqual(new DateTime(2026, 11, 23, 3, 0, 0, DateTimeKind.Utc), periodo.FimUtc);
    }

    [TestMethod]
    public void SemanaIso_QuartaFeira_RetornaSegundaADomingo()
    {
        var periodo = PeriodosDashboard.SemanaIso(new DateOnly(2026, 11, 18), _fuso);

        Assert.AreEqual(new DateTime(2026, 11, 16, 3, 0, 0, DateTimeKind.Utc), periodo.InicioUtc);
        Assert.AreEqual(new DateTime(2026, 11, 23, 3, 0, 0, DateTimeKind.Utc), periodo.FimUtc);
    }

    [TestMethod]
    public void SemanaIso_Domingo_PertenceASemanaQueComecouNaSegunda()
    {
        var periodo = PeriodosDashboard.SemanaIso(new DateOnly(2026, 11, 22), _fuso);

        Assert.AreEqual(new DateTime(2026, 11, 16, 3, 0, 0, DateTimeKind.Utc), periodo.InicioUtc);
        Assert.AreEqual(new DateTime(2026, 11, 23, 3, 0, 0, DateTimeKind.Utc), periodo.FimUtc);
    }

    [TestMethod]
    public void SemanaIso_ViradaDeAno_AbrangeDiasDoAnoAnterior()
    {
        var periodo = PeriodosDashboard.SemanaIso(new DateOnly(2027, 1, 1), _fuso);

        Assert.AreEqual(new DateTime(2026, 12, 28, 3, 0, 0, DateTimeKind.Utc), periodo.InicioUtc);
        Assert.AreEqual(new DateTime(2027, 1, 4, 3, 0, 0, DateTimeKind.Utc), periodo.FimUtc);
    }

    [TestMethod]
    public void Mes_PrimeiroEDiaFinal_CobremOMesInteiro()
    {
        var periodo = PeriodosDashboard.Mes(new DateOnly(2026, 11, 22), _fuso);

        Assert.AreEqual(new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc), periodo.InicioUtc);
        Assert.AreEqual(new DateTime(2026, 12, 1, 3, 0, 0, DateTimeKind.Utc), periodo.FimUtc);

        var janeiro = PeriodosDashboard.Mes(new DateOnly(2027, 1, 31), _fuso);
        Assert.AreEqual(new DateTime(2027, 2, 1, 3, 0, 0, DateTimeKind.Utc), janeiro.FimUtc);
    }
}

#pragma warning restore CA1707
