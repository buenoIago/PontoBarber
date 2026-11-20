using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Profissionais;

/// <summary>
/// Vínculo entre um profissional e uma barbearia (ADR-006). O criador da
/// barbearia recebe o vínculo <see cref="CargoProfissional.Proprietario"/>.
/// </summary>
public sealed class VinculoProfissional : EntidadeBase<VinculoProfissional>, IPertencenteTenant
{
    private VinculoProfissional()
    {
    }

    public Guid BarbeariaId { get; private set; }
    public Guid ProfissionalId { get; private set; }
    public CargoProfissional Cargo { get; private set; }
    public StatusVinculo Situacao { get; private set; }
    public DateTime VinculadoEmUtc { get; private set; }
    public DateTime? RemovidoEmUtc { get; private set; }

    public static VinculoProfissional Criar(Guid barbeariaId, Guid profissionalId) =>
        CriarComCargo(barbeariaId, profissionalId, CargoProfissional.Proprietario);

    /// <summary>
    /// Cria o vínculo de um profissional convidado. O cargo é fixado pelo
    /// domínio como <see cref="CargoProfissional.Barbeiro"/>: o cliente não
    /// informa nem pode elevar privilégios (Task 4.3).
    /// </summary>
    public static VinculoProfissional CriarBarbeiro(Guid barbeariaId, Guid profissionalId) =>
        CriarComCargo(barbeariaId, profissionalId, CargoProfissional.Barbeiro);

    private static VinculoProfissional CriarComCargo(
        Guid barbeariaId,
        Guid profissionalId,
        CargoProfissional cargo) =>
        new()
        {
            BarbeariaId = barbeariaId,
            ProfissionalId = profissionalId,
            Cargo = cargo,
            Situacao = StatusVinculo.Ativo,
            VinculadoEmUtc = DateTime.UtcNow
        };

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();

        if (BarbeariaId == Guid.Empty)
            erros.Add(new ErroValidacao("barbeariaId", "Campo obrigatório."));

        if (ProfissionalId == Guid.Empty)
            erros.Add(new ErroValidacao("profissionalId", "Campo obrigatório."));

        return erros;
    }

    /// <summary>
    /// Reativa um vínculo inativo. Somente a transição Inativo → Ativo é
    /// permitida (TDD §8.1, RF-PROF-006).
    /// </summary>
    public void Ativar()
    {
        if (Situacao != StatusVinculo.Inativo)
            throw new InvalidOperationException("Somente um vínculo inativo pode ser ativado.");

        Situacao = StatusVinculo.Ativo;
    }

    /// <summary>
    /// Desativa um vínculo ativo, preservando a conta, o perfil e o histórico.
    /// Somente a transição Ativo → Inativo é permitida (RN-PROF-004).
    /// </summary>
    public void Desativar()
    {
        if (Situacao != StatusVinculo.Ativo)
            throw new InvalidOperationException("Somente um vínculo ativo pode ser desativado.");

        Situacao = StatusVinculo.Inativo;
    }

    /// <summary>
    /// Encerra o vínculo por desativação lógica: o registro permanece para
    /// histórico, mas deixa de conferir acesso à barbearia (ADR-023/RN-PROF-005).
    /// </summary>
    public void Remover()
    {
        if (Situacao == StatusVinculo.Removido)
            throw new InvalidOperationException("O vínculo já foi removido.");

        Situacao = StatusVinculo.Removido;
        RemovidoEmUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Não existe atualização genérica permitida para vínculo: cargo,
    /// situação e datas pertencem a operações específicas (Task 4.4).
    /// Alterações são rejeitadas; valores idênticos são aceitos sem efeito.
    /// </summary>
    public override void Atualizar(VinculoProfissional entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (Id != entidadeAtualizada.Id ||
            BarbeariaId != entidadeAtualizada.BarbeariaId ||
            ProfissionalId != entidadeAtualizada.ProfissionalId ||
            Cargo != entidadeAtualizada.Cargo ||
            Situacao != entidadeAtualizada.Situacao ||
            VinculadoEmUtc != entidadeAtualizada.VinculadoEmUtc ||
            RemovidoEmUtc != entidadeAtualizada.RemovidoEmUtc)
        {
            throw new InvalidOperationException(
                "O vínculo não pode ser alterado por atualização genérica.");
        }
    }
}
