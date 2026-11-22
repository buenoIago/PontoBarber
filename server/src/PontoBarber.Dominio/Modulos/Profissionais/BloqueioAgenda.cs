using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>
/// Bloqueio pontual na agenda do profissional (TDD §8.1). Períodos são
/// armazenados em UTC e convertidos para o fuso da barbearia na borda.
/// </summary>
public sealed class BloqueioAgenda : EntidadeBase<BloqueioAgenda>, IPertencenteTenant
{
    private BloqueioAgenda()
    {
    }

    public Guid BarbeariaId { get; private set; }
    public Guid ProfissionalId { get; private set; }
    public DateTime IniciaEmUtc { get; private set; }
    public DateTime TerminaEmUtc { get; private set; }
    public string? Motivo { get; private set; }

    public static BloqueioAgenda Criar(
        Guid barbeariaId,
        Guid profissionalId,
        DateTime iniciaEmUtc,
        DateTime terminaEmUtc,
        string? motivo)
    {
        var bloqueio = new BloqueioAgenda
        {
            BarbeariaId = barbeariaId,
            ProfissionalId = profissionalId
        };
        bloqueio.AtualizarPeriodo(iniciaEmUtc, terminaEmUtc, motivo);
        return bloqueio;
    }

    public override IReadOnlyList<ErroValidacao> Validar() =>
        ValidarDados(BarbeariaId, ProfissionalId, IniciaEmUtc, TerminaEmUtc);

    public override void Atualizar(BloqueioAgenda entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (BarbeariaId != entidadeAtualizada.BarbeariaId ||
            ProfissionalId != entidadeAtualizada.ProfissionalId)
            throw new InvalidOperationException("Barbearia e profissional de um bloqueio não podem ser alterados.");

        AtualizarPeriodo(
            entidadeAtualizada.IniciaEmUtc, entidadeAtualizada.TerminaEmUtc, entidadeAtualizada.Motivo);
    }

    public void AtualizarPeriodo(DateTime iniciaEmUtc, DateTime terminaEmUtc, string? motivo)
    {
        var erros = ValidarDados(BarbeariaId, ProfissionalId, iniciaEmUtc, terminaEmUtc);
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Não foi possível salvar o bloqueio: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        IniciaEmUtc = iniciaEmUtc;
        TerminaEmUtc = terminaEmUtc;
        Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
    }

    public static IReadOnlyList<ErroValidacao> ValidarDados(
        Guid barbeariaId,
        Guid profissionalId,
        DateTime iniciaEmUtc,
        DateTime terminaEmUtc)
    {
        var erros = new List<ErroValidacao>();

        if (barbeariaId == Guid.Empty)
            erros.Add(new ErroValidacao("barbeariaId", "Campo obrigatório."));

        if (profissionalId == Guid.Empty)
            erros.Add(new ErroValidacao("profissionalId", "Campo obrigatório."));

        if (iniciaEmUtc.Kind != DateTimeKind.Utc || terminaEmUtc.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("periodo", "Informe o período em UTC."));

        if (terminaEmUtc <= iniciaEmUtc)
            erros.Add(new ErroValidacao("terminaEmUtc", "O término deve ser posterior ao início."));

        return erros;
    }
}
