using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>
/// Janela recorrente de disponibilidade do profissional em um dia da semana
/// (TDD §8.1). Horários locais da barbearia, sem offset.
/// </summary>
public sealed class DisponibilidadeSemanal : EntidadeBase<DisponibilidadeSemanal>, IPertencenteTenant
{
    private DisponibilidadeSemanal()
    {
    }

    public Guid BarbeariaId { get; private set; }
    public Guid ProfissionalId { get; private set; }
    public DiaSemana DiaSemana { get; private set; }
    public TimeOnly IniciaEm { get; private set; }
    public TimeOnly TerminaEm { get; private set; }

    public static DisponibilidadeSemanal Criar(
        Guid barbeariaId,
        Guid profissionalId,
        DiaSemana diaSemana,
        TimeOnly iniciaEm,
        TimeOnly terminaEm)
    {
        var janela = new DisponibilidadeSemanal
        {
            BarbeariaId = barbeariaId,
            ProfissionalId = profissionalId,
            DiaSemana = diaSemana
        };
        janela.AtualizarHorario(iniciaEm, terminaEm);
        return janela;
    }

    public override IReadOnlyList<ErroValidacao> Validar() =>
        ValidarDados(BarbeariaId, ProfissionalId, DiaSemana, IniciaEm, TerminaEm);

    public override void Atualizar(DisponibilidadeSemanal entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (BarbeariaId != entidadeAtualizada.BarbeariaId ||
            ProfissionalId != entidadeAtualizada.ProfissionalId ||
            DiaSemana != entidadeAtualizada.DiaSemana)
            throw new InvalidOperationException("Barbearia, profissional e dia de uma janela não podem ser alterados.");

        AtualizarHorario(entidadeAtualizada.IniciaEm, entidadeAtualizada.TerminaEm);
    }

    public void AtualizarHorario(TimeOnly iniciaEm, TimeOnly terminaEm)
    {
        var erros = ValidarDados(BarbeariaId, ProfissionalId, DiaSemana, iniciaEm, terminaEm);
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Não foi possível salvar a disponibilidade: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        IniciaEm = iniciaEm;
        TerminaEm = terminaEm;
    }

    public static IReadOnlyList<ErroValidacao> ValidarDados(
        Guid barbeariaId,
        Guid profissionalId,
        DiaSemana diaSemana,
        TimeOnly iniciaEm,
        TimeOnly terminaEm)
    {
        var erros = new List<ErroValidacao>();

        if (barbeariaId == Guid.Empty)
            erros.Add(new ErroValidacao("barbeariaId", "Campo obrigatório."));

        if (profissionalId == Guid.Empty)
            erros.Add(new ErroValidacao("profissionalId", "Campo obrigatório."));

        if (!Enum.IsDefined(diaSemana))
            erros.Add(new ErroValidacao("diaSemana", "Dia da semana inválido."));

        if (terminaEm <= iniciaEm)
            erros.Add(new ErroValidacao("terminaEm", "O término deve ser posterior ao início."));

        return erros;
    }
}
