using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Clientes;

/// <summary>Campos pessoais que o cliente pode alterar (RF-CLI-006).</summary>
public sealed record DadosPerfilCliente(string NomeCompleto, string Email, string? Telefone)
{
    public static DadosPerfilCliente Criar(string nomeCompleto, string email, string? telefone)
    {
        var dados = new DadosPerfilCliente(
            nomeCompleto?.Trim() ?? string.Empty,
            email?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim());

        var erros = dados.Validar();
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Dados do cliente inválidos: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        return dados;
    }

    public IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (string.IsNullOrWhiteSpace(NomeCompleto) || NomeCompleto.Length < 3 || NomeCompleto.Length > 120)
            erros.Add(new ErroValidacao("nomeCompleto", "Informe um nome entre 3 e 120 caracteres."));
        if (string.IsNullOrWhiteSpace(Email) || Email.Length > 256 || !Email.Contains('@'))
            erros.Add(new ErroValidacao("email", "Informe um e-mail válido."));
        if (Telefone?.Length > 20)
            erros.Add(new ErroValidacao("telefone", "Máximo de 20 caracteres."));

        return erros;
    }
}
