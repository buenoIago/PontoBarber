namespace PontoBarber.Aplicacao.Modulos.Barbearia.Util;

/// <summary>
/// Validações de entrada da criação de barbearia (ADR-029). Regras puras,
/// sem dependência de infraestrutura.
/// </summary>
internal static class ValidadorBarbearia
{
    public static string? ValidarObrigatorio(
        string? valor,
        string campo,
        Dictionary<string, string[]> erros)
    {
        if (!string.IsNullOrWhiteSpace(valor))
            return valor;

        AdicionarErro(erros, campo, "Campo obrigatório.");
        return null;
    }

    public static void AdicionarErro(
        Dictionary<string, string[]> erros,
        string campo,
        string mensagem)
    {
        if (!erros.TryGetValue(campo, out var mensagens))
        {
            mensagens = [];
            erros[campo] = mensagens;
        }

        erros[campo] = [.. mensagens, mensagem];
    }

    public static bool EmailValido(string email)
    {
        var arroba = email.IndexOf('@');
        return arroba > 0 &&
            arroba < email.Length - 1 &&
            email.IndexOf('.', arroba + 1) > arroba + 1;
    }

    public static bool FusoIanaValido(string fuso)
    {
        // Identificadores IANA possuem barra; ids locais do Windows não são aceitos.
        if (!fuso.Contains('/', StringComparison.Ordinal))
            return false;

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(fuso);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
