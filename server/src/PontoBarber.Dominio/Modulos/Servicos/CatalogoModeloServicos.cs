namespace PontoBarber.Dominio.Modulos.Servicos;

/// <summary>Item do catálogo modelo usado como base para barbearias novas.</summary>
public sealed record ModeloServico(string Nome, int DuracaoMinutos);

/// <summary>
/// Catálogo modelo da Task 5.1. Durações iniciais aproximadas, sem preços
/// fixos; cada barbearia recebe uma cópia que poderá ser personalizada.
/// </summary>
public static class CatalogoModeloServicos
{
    public static IReadOnlyList<ModeloServico> Todos { get; } =
    [
        // Cabelo
        new("Corte social / tradicional", 30),
        new("Corte degradê (fade / navalhado)", 40),
        new("Corte infantil", 30),
        new("Raspagem na máquina ou navalha (cabeça inteira)", 20),
        new("Acabamento / Pezinho", 15),
        new("Lavagem e finalização (modelagem com pomada ou cera)", 20),

        // Barba e bigode
        new("Barba simples (máquina e tesoura)", 20),
        new("Barba completa (alinhamento com navalha e pós-barba)", 30)
    ];
}
