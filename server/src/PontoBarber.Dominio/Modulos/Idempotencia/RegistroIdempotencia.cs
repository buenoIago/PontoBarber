using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Idempotencia;

/// <summary>
/// Estrutura oficial de idempotência definida no TDD §8.1/ADR-021. Não existe
/// outra estrutura equivalente no projeto; o processo de expurgo atua somente
/// sobre os registros vencidos desta entidade.
/// </summary>
public sealed class RegistroIdempotencia : EntidadeBase<RegistroIdempotencia>
{
    private RegistroIdempotencia()
    {
    }

    public string UsuarioId { get; private set; } = string.Empty;
    public string Chave { get; private set; } = string.Empty;
    public string HashRequisicao { get; private set; } = string.Empty;
    public string? JsonResposta { get; private set; }
    public int CodigoStatus { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime ExpiraEmUtc { get; private set; }

    public static RegistroIdempotencia Criar(
        string usuarioId,
        string chave,
        string hashRequisicao,
        string? jsonResposta,
        int codigoStatus,
        DateTime criadoEmUtc,
        DateTime expiraEmUtc)
    {
        var registro = new RegistroIdempotencia
        {
            UsuarioId = usuarioId,
            Chave = chave,
            HashRequisicao = hashRequisicao,
            JsonResposta = jsonResposta,
            CodigoStatus = codigoStatus,
            CriadoEmUtc = criadoEmUtc,
            ExpiraEmUtc = expiraEmUtc
        };

        var erros = registro.Validar();
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Não foi possível registrar a idempotência: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        return registro;
    }

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (string.IsNullOrWhiteSpace(UsuarioId))
            erros.Add(new ErroValidacao("usuarioId", "Campo obrigatório."));
        if (string.IsNullOrWhiteSpace(Chave))
            erros.Add(new ErroValidacao("chave", "Campo obrigatório."));
        if (string.IsNullOrWhiteSpace(HashRequisicao))
            erros.Add(new ErroValidacao("hashRequisicao", "Campo obrigatório."));
        if (CriadoEmUtc.Kind != DateTimeKind.Utc || ExpiraEmUtc.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("periodo", "Informe as datas em UTC."));
        if (ExpiraEmUtc <= CriadoEmUtc)
            erros.Add(new ErroValidacao("expiraEmUtc", "A expiração deve ser posterior à criação."));

        return erros;
    }

    public override void Atualizar(RegistroIdempotencia entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (Id != entidadeAtualizada.Id ||
            UsuarioId != entidadeAtualizada.UsuarioId ||
            Chave != entidadeAtualizada.Chave ||
            HashRequisicao != entidadeAtualizada.HashRequisicao ||
            JsonResposta != entidadeAtualizada.JsonResposta ||
            CodigoStatus != entidadeAtualizada.CodigoStatus ||
            CriadoEmUtc != entidadeAtualizada.CriadoEmUtc ||
            ExpiraEmUtc != entidadeAtualizada.ExpiraEmUtc)
            throw new InvalidOperationException("Um registro de idempotência não pode ser alterado.");
    }
}
