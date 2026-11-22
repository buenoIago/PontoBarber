using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Jobs;

/// <summary>
/// Registro persistente de uma execução de processo periódico (TDD §8.1/§12.3).
/// O registro é criado no início e concluso ou marcado como falho ao final.
/// </summary>
public sealed class ExecucaoJob : EntidadeBase<ExecucaoJob>
{
    private ExecucaoJob()
    {
    }

    public string NomeJob { get; private set; } = string.Empty;
    public DateTime IniciadoEmUtc { get; private set; }
    public DateTime? FinalizadoEmUtc { get; private set; }
    public ResultadoExecucaoJob Resultado { get; private set; }
    public int QuantidadeProcessados { get; private set; }
    public string? Erro { get; private set; }

    public static ExecucaoJob Iniciar(string nomeJob, DateTime iniciadoEmUtc)
    {
        var execucao = new ExecucaoJob
        {
            NomeJob = nomeJob?.Trim() ?? string.Empty,
            IniciadoEmUtc = iniciadoEmUtc,
            Resultado = ResultadoExecucaoJob.EmExecucao
        };

        var erros = execucao.Validar();
        if (erros.Count > 0)
            throw new InvalidOperationException(
                "Não foi possível registrar a execução: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));

        return execucao;
    }

    public void Concluir(int quantidade, DateTime finalizadoEmUtc)
    {
        GarantirEmExecucao();
        if (finalizadoEmUtc < IniciadoEmUtc)
            throw new InvalidOperationException("O fim não pode ser anterior ao início.");
        if (quantidade < 0)
            throw new InvalidOperationException("A quantidade processada não pode ser negativa.");

        Resultado = ResultadoExecucaoJob.Sucesso;
        QuantidadeProcessados = quantidade;
        FinalizadoEmUtc = finalizadoEmUtc;
    }

    public void Falhar(string erroResumo, DateTime finalizadoEmUtc)
    {
        GarantirEmExecucao();
        if (finalizadoEmUtc < IniciadoEmUtc)
            throw new InvalidOperationException("O fim não pode ser anterior ao início.");

        Resultado = ResultadoExecucaoJob.Falha;
        Erro = string.IsNullOrWhiteSpace(erroResumo) ? "Falha não detalhada." : erroResumo.Trim();
        FinalizadoEmUtc = finalizadoEmUtc;
    }

    private void GarantirEmExecucao()
    {
        if (Resultado != ResultadoExecucaoJob.EmExecucao || FinalizadoEmUtc is not null)
            throw new InvalidOperationException("A execução já foi finalizada.");
    }

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (string.IsNullOrWhiteSpace(NomeJob))
            erros.Add(new ErroValidacao("nomeJob", "Campo obrigatório."));
        else if (NomeJob.Length > 120)
            erros.Add(new ErroValidacao("nomeJob", "Máximo de 120 caracteres."));
        if (IniciadoEmUtc.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("iniciadoEmUtc", "Informe o início em UTC."));
        if (Resultado == ResultadoExecucaoJob.EmExecucao && FinalizadoEmUtc is not null)
            erros.Add(new ErroValidacao("finalizadoEmUtc", "Execução incompleta não pode ter fim."));
        if (Resultado != ResultadoExecucaoJob.EmExecucao && FinalizadoEmUtc is null)
            erros.Add(new ErroValidacao("finalizadoEmUtc", "Execução concluída exige o fim."));
        if (Erro?.Length > 1000)
            erros.Add(new ErroValidacao("erro", "Máximo de 1000 caracteres."));

        return erros;
    }

    /// <summary>
    /// Atualização genérica só aceita valores idênticos; conclusão e falha
    /// passam por <see cref="Concluir"/> e <see cref="Falhar"/>.
    /// </summary>
    public override void Atualizar(ExecucaoJob entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (Id != entidadeAtualizada.Id ||
            NomeJob != entidadeAtualizada.NomeJob ||
            IniciadoEmUtc != entidadeAtualizada.IniciadoEmUtc ||
            FinalizadoEmUtc != entidadeAtualizada.FinalizadoEmUtc ||
            Resultado != entidadeAtualizada.Resultado ||
            QuantidadeProcessados != entidadeAtualizada.QuantidadeProcessados ||
            Erro != entidadeAtualizada.Erro)
            throw new InvalidOperationException("A execução não pode ser alterada por atualização genérica.");
    }
}
