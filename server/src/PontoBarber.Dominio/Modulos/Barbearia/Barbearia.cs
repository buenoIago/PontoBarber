using System.Security.Cryptography;
using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Barbearia;

/// <summary>
/// Barbearia — raiz do tenant (ADR-006). Não implementa
/// <see cref="IPertencenteTenant"/>: seu próprio Id identifica o tenant.
/// </summary>
public sealed class Barbearia : IEntidade
{
    private const int _tamanhoCodigoConvite = 8;
    private const string _alfabetoCodigoConvite = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private Barbearia()
    {
    }

    public Guid Id { get; private set; }
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
            Id = Guid.CreateVersion7(),
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
