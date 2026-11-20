using System.Security.Cryptography;
using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Barbearia;

/// <summary>
/// Barbearia — raiz do tenant (ADR-006). Não implementa
/// <see cref="IPertencenteTenant"/>: seu próprio Id identifica o tenant.
/// </summary>
public sealed class Barbearia : EntidadeBase<Barbearia>
{
    private const int _tamanhoCodigoConvite = 8;
    private const string _alfabetoCodigoConvite = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private Barbearia()
    {
    }

    public string Nome { get; private set; } = string.Empty;
    public string TelefoneComercial { get; private set; } = string.Empty;
    public string EmailComercial { get; private set; } = string.Empty;
    public string Cidade { get; private set; } = string.Empty;
    public string Endereco { get; private set; } = string.Empty;
    public string? Instagram { get; private set; }
    public string CodigoConvite { get; private set; } = string.Empty;
    public string FusoHorarioId { get; private set; } = string.Empty;
    public Guid IdProfissionalProprietario { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime AtualizadoEmUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Barbearia Criar(
        string nome,
        string telefoneComercial,
        string emailComercial,
        string cidade,
        string endereco,
        string? instagram,
        string codigoConvite,
        string fusoHorarioId,
        Guid idProfissionalProprietario)
    {
        var agora = DateTime.UtcNow;

        return new Barbearia
        {
            Nome = nome,
            TelefoneComercial = telefoneComercial,
            EmailComercial = emailComercial,
            Cidade = cidade,
            Endereco = endereco,
            Instagram = instagram,
            CodigoConvite = codigoConvite,
            FusoHorarioId = fusoHorarioId,
            IdProfissionalProprietario = idProfissionalProprietario,
            Ativo = true,
            CriadoEmUtc = agora,
            AtualizadoEmUtc = agora
        };
    }

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (string.IsNullOrWhiteSpace(Nome))
            erros.Add(new ErroValidacao("nome", "Campo obrigatório."));

        if (string.IsNullOrWhiteSpace(TelefoneComercial))
            erros.Add(new ErroValidacao("telefoneComercial", "Campo obrigatório."));
        else if (TelefoneComercial.Count(char.IsDigit) < 10)
            erros.Add(new ErroValidacao("telefoneComercial", "Informe um telefone válido."));

        if (string.IsNullOrWhiteSpace(EmailComercial))
            erros.Add(new ErroValidacao("emailComercial", "Campo obrigatório."));
        else if (!EmailValido(EmailComercial))
            erros.Add(new ErroValidacao("emailComercial", "Informe um e-mail válido."));

        if (string.IsNullOrWhiteSpace(Cidade))
            erros.Add(new ErroValidacao("cidade", "Campo obrigatório."));

        if (string.IsNullOrWhiteSpace(Endereco))
            erros.Add(new ErroValidacao("endereco", "Campo obrigatório."));

        if (string.IsNullOrWhiteSpace(FusoHorarioId))
            erros.Add(new ErroValidacao("fusoHorarioId", "Campo obrigatório."));
        else if (!FusoIanaValido(FusoHorarioId))
            erros.Add(new ErroValidacao("fusoHorarioId", "Informe um identificador de fuso IANA válido."));

        return erros;
    }

    /// <summary>
    /// Copia somente os dados cadastrais permitidos. Identificador,
    /// proprietário, código de convite, fuso, situação, auditoria de criação
    /// e RowVersion não são alterados (SPEC §2, Task 4.2).
    /// </summary>
    public override void Atualizar(Barbearia entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        Nome = entidadeAtualizada.Nome;
        TelefoneComercial = entidadeAtualizada.TelefoneComercial;
        EmailComercial = entidadeAtualizada.EmailComercial;
        Cidade = entidadeAtualizada.Cidade;
        Endereco = entidadeAtualizada.Endereco;
        Instagram = entidadeAtualizada.Instagram;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    private static bool EmailValido(string email)
    {
        var arroba = email.IndexOf('@');
        return arroba > 0 &&
            arroba < email.Length - 1 &&
            email.IndexOf('.', arroba + 1) > arroba + 1;
    }

    private static bool FusoIanaValido(string fuso)
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

    /// <summary>
    /// Código de 8 caracteres, aleatório e sem caracteres ambíguos (0/O/1/I).
    /// A unicidade global é garantida pelo índice único e pela verificação
    /// na aplicação antes da persistência.
    /// </summary>
    public static string GerarCodigoConvite()
    {
        Span<char> codigo = stackalloc char[_tamanhoCodigoConvite];
        for (var indice = 0; indice < codigo.Length; indice++)
            codigo[indice] = _alfabetoCodigoConvite[
                RandomNumberGenerator.GetInt32(_alfabetoCodigoConvite.Length)];

        return new string(codigo);
    }
}
