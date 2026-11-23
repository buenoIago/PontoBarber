namespace PontoBarber.Dominio.Modulos.Financeiro;

/// <summary>Formas presenciais permitidas na V1.0 (TDD §8.1/PRD RF-ATD-007).</summary>
public enum FormaPagamento
{
    Cash = 0,
    Pix = 1,
    DebitCard = 2,
    CreditCard = 3
}
