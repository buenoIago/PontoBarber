namespace PontoBarber.Dominio.Modulos.Dashboard;

/// <summary>Intervalo semiaberto [início, fim) já convertido para UTC.</summary>
public sealed record IntervaloUtc(DateTime InicioUtc, DateTime FimUtc);
