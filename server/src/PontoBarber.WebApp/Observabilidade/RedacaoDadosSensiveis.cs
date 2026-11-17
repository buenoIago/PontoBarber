using System.Text.RegularExpressions;

namespace PontoBarber.WebApp.Observabilidade;

/// <summary>
/// Regras de redação aplicadas a logs estruturados e texto renderizado.
/// </summary>
public static partial class RedacaoDadosSensiveis
{
    private static readonly HashSet<string> _nomesSensiveis = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "Senha", "Token", "Cookie", "Authorization", "Secret",
        "Email", "E-mail", "Telefone", "Phone", "PhoneNumber", "ApiKey", "ConnectionString"
    };

    public const string ValorMascarado = "***";

    public static bool EHNomeSensivel(string nome) =>
        !string.IsNullOrWhiteSpace(nome) && _nomesSensiveis.Contains(nome);

    public static string MascararTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return texto;

        var resultado = EmailRegex().Replace(texto, "***@***");
        resultado = TelefoneRegex().Replace(resultado, ValorMascarado);
        resultado = ParChaveValorRegex().Replace(resultado, "${chave}=***");
        return PortadorRegex().Replace(resultado, "Bearer ***");
    }

    public static object? MascararValor(string nome, object? valor)
    {
        if (EHNomeSensivel(nome))
            return ValorMascarado;

        return valor is string texto ? MascararTexto(texto) : valor;
    }

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?<![\w])\+?\d[\d\s\-()]{8,}\d(?![\w])")]
    private static partial Regex TelefoneRegex();

    [GeneratedRegex(@"(?i)(?<chave>password|senha|token|cookie|authorization|apikey|secret)\s*[:=]\s*[^\s,;]+")]
    private static partial Regex ParChaveValorRegex();

    [GeneratedRegex(@"(?i)bearer\s+[A-Za-z0-9._\-]+")]
    private static partial Regex PortadorRegex();
}
