using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Servicos;

/// <summary>
/// Serviço do catálogo de uma barbearia (TDD §8.1). Os serviços do catálogo
/// modelo não têm preço fixo; o preço é definido na manutenção do catálogo.
/// </summary>
public sealed class Servico : EntidadeBase<Servico>, IPertencenteTenant
{
    private Servico()
    {
    }

    public Guid BarbeariaId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int DuracaoMinutos { get; private set; }
    public decimal? Preco { get; private set; }
    public bool Ativo { get; private set; }
    public bool EhModelo { get; private set; }

    /// <summary>Cria a cópia do catálogo modelo para uma barbearia específica.</summary>
    public static Servico CriarModelo(Guid barbeariaId, string nome, int duracaoMinutos) =>
        new()
        {
            BarbeariaId = barbeariaId,
            Nome = nome,
            DuracaoMinutos = duracaoMinutos,
            Preco = null,
            Ativo = true,
            EhModelo = true
        };

    /// <summary>Cria um serviço no catálogo da barbearia (RF-SERV-002).</summary>
    public static Servico Criar(Guid barbeariaId, string nome, int duracaoMinutos, decimal preco) =>
        new()
        {
            BarbeariaId = barbeariaId,
            Nome = nome,
            DuracaoMinutos = duracaoMinutos,
            Preco = preco,
            Ativo = true,
            EhModelo = false
        };

    public override IReadOnlyList<ErroValidacao> Validar() =>
        ValidarDados(BarbeariaId, Nome, DuracaoMinutos, Preco, !EhModelo);

    /// <summary>Valida os dados antes de qualquer mutação da entidade persistida.</summary>
    public static IReadOnlyList<ErroValidacao> ValidarDados(
        Guid barbeariaId,
        string nome,
        int duracaoMinutos,
        decimal? preco,
        bool exigirPreco = true)
    {
        var erros = new List<ErroValidacao>();

        if (barbeariaId == Guid.Empty)
            erros.Add(new ErroValidacao("barbeariaId", "Campo obrigatório."));

        if (string.IsNullOrWhiteSpace(nome))
            erros.Add(new ErroValidacao("nome", "Campo obrigatório."));

        if (duracaoMinutos <= 0)
            erros.Add(new ErroValidacao("duracaoMinutos", "Informe uma duração válida."));

        if (exigirPreco && preco is null)
            erros.Add(new ErroValidacao("preco", "Campo obrigatório."));
        else if (preco < 0)
            erros.Add(new ErroValidacao("preco", "O preço não pode ser negativo."));

        return erros;
    }

    /// <summary>Edição completa permitida somente ao proprietário.</summary>
    public void AtualizarDados(string nome, int duracaoMinutos, decimal preco, bool ativo)
    {
        Nome = nome;
        DuracaoMinutos = duracaoMinutos;
        Preco = preco;
        Ativo = ativo;
        GarantirInvariantes();
    }

    /// <summary>Edição restrita permitida ao barbeiro: apenas preço e duração.</summary>
    public void AtualizarValores(int duracaoMinutos, decimal preco)
    {
        DuracaoMinutos = duracaoMinutos;
        Preco = preco;
        GarantirInvariantes();
    }

    /// <summary>
    /// Remove o serviço da oferta por desativação lógica (ADR-023). O registro
    /// permanece no banco para referência histórica e para o proprietário.
    /// </summary>
    public void Remover()
    {
        Ativo = false;
        GarantirInvariantes();
    }

    private void GarantirInvariantes()
    {
        var erros = Validar();
        if (erros.Count == 0)
            return;

        throw new InvalidOperationException(
            "Não foi possível atualizar o serviço: " + string.Join(" ", erros.Select(erro => erro.Mensagem)));
    }

    /// <summary>
    /// Atualização genérica não é permitida: nome, disponibilidade e preço
    /// pertencem a operações específicas da manutenção do catálogo (Task 5.2).
    /// </summary>
    public override void Atualizar(Servico entidadeAtualizada)
    {
        ArgumentNullException.ThrowIfNull(entidadeAtualizada);

        if (Id != entidadeAtualizada.Id ||
            BarbeariaId != entidadeAtualizada.BarbeariaId ||
            Nome != entidadeAtualizada.Nome ||
            DuracaoMinutos != entidadeAtualizada.DuracaoMinutos ||
            Preco != entidadeAtualizada.Preco ||
            Ativo != entidadeAtualizada.Ativo ||
            EhModelo != entidadeAtualizada.EhModelo)
            throw new InvalidOperationException("O serviço não pode ser alterado por atualização genérica.");
    }
}
