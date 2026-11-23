namespace PontoBarber.Dominio.Modulos.Dashboard;

/// <summary>
/// Cálculo puro dos períodos do dashboard no fuso IANA da barbearia, com
/// conversão para UTC. Os limites são semiabertos para não perder registros
/// em viradas de dia, mês ou ano (RN-DASH-002 a RN-DASH-005).
/// </summary>
public static class PeriodosDashboard
{
    public static IntervaloUtc Dia(DateOnly data, TimeZoneInfo fuso) =>
        Intervalo(data, data.AddDays(1), fuso);

    /// <summary>Semana ISO: segunda-feira a domingo, mesmo na virada de ano.</summary>
    public static IntervaloUtc SemanaIso(DateOnly data, TimeZoneInfo fuso)
    {
        var deslocamento = ((int)data.DayOfWeek + 6) % 7;
        var inicio = data.AddDays(-deslocamento);
        return Intervalo(inicio, inicio.AddDays(7), fuso);
    }

    public static IntervaloUtc Mes(DateOnly data, TimeZoneInfo fuso) =>
        Intervalo(
            new DateOnly(data.Year, data.Month, 1),
            new DateOnly(data.Year, data.Month, 1).AddMonths(1),
            fuso);

    private static IntervaloUtc Intervalo(DateOnly inicio, DateOnly fim, TimeZoneInfo fuso) =>
        new(
            ParaUtc(inicio, fuso),
            ParaUtc(fim, fuso));

    private static DateTime ParaUtc(DateOnly data, TimeZoneInfo fuso) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(data.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified),
            fuso);
}
