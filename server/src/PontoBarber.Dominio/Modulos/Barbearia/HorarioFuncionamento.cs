using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Barbearia;

/// <summary>
/// Funcionamento da barbearia em um dia da semana (TDD §8.1). Os horários são
/// locais, no fuso da barbearia, sem offset (TDD §12.4).
/// </summary>
public sealed class HorarioFuncionamento : EntidadeBase<HorarioFuncionamento>, IPertencenteTenant
{
    private HorarioFuncionamento()
    {
    }

    public Guid BarbeariaId { get; private set; }
    public DiaSemana DiaSemana { get; private set; }
    public TimeOnly? AbreAs { get; private set; }
    public TimeOnly? FechaAs { get; private set; }
    public bool Fechado { get; private set; }

    public static HorarioFuncionamento Criar(
        Guid barbeariaId,
        DiaSemana diaSemana,
        TimeOnly? abreAs,
        TimeOnly? fechaAs,
        bool fechado)
    {
        var horario = new HorarioFuncionamento
        {
            BarbeariaId = barbeariaId,
            DiaSemana = diaSemana
        };
        horario.AtualizarDados(abreAs, fechaAs, fechado);
        return horario;
    }

    public override IReadOnlyList<ErroValidacao> Validar() =>
        ValidarDados(BarbeariaId, DiaSemana, AbreAs, FechaAs, Fechado);

    public override void Atualizar(HorarioFuncionamento entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (BarbeariaId != entidadeAtualizada.BarbeariaId || DiaSemana != entidadeAtualizada.DiaSemana)
            throw new InvalidOperationException("O dia e a barbearia de um horário não podem ser alterados.");

        AtualizarDados(entidadeAtualizada.AbreAs, entidadeAtualizada.FechaAs, entidadeAtualizada.Fechado);
    }

    public void AtualizarDados(TimeOnly? abreAs, TimeOnly? fechaAs, bool fechado)
    {
        var erros = ValidarDados(BarbeariaId, DiaSemana, abreAs, fechaAs, fechado);
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Não foi possível salvar o horário: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        AbreAs = abreAs;
        FechaAs = fechaAs;
        Fechado = fechado;
    }

    public static IReadOnlyList<ErroValidacao> ValidarDados(
        Guid barbeariaId,
        DiaSemana diaSemana,
        TimeOnly? abreAs,
        TimeOnly? fechaAs,
        bool fechado)
    {
        var erros = new List<ErroValidacao>();

        if (barbeariaId == Guid.Empty)
            erros.Add(new ErroValidacao("barbeariaId", "Campo obrigatório."));

        if (!Enum.IsDefined(diaSemana))
            erros.Add(new ErroValidacao("diaSemana", "Dia da semana inválido."));

        if (fechado)
            return erros;

        if (abreAs is null)
            erros.Add(new ErroValidacao("abreAs", "Campo obrigatório."));

        if (fechaAs is null)
            erros.Add(new ErroValidacao("fechaAs", "Campo obrigatório."));

        if (abreAs.HasValue && fechaAs.HasValue && fechaAs <= abreAs)
            erros.Add(new ErroValidacao("fechaAs", "O fechamento deve ser posterior à abertura."));

        return erros;
    }
}
