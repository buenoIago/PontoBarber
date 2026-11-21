using PontoBarber.Dominio.Modulos.Servicos;

namespace PontoBarber.Aplicacao.Modulos.Servicos;

/// <summary>
/// Aplica o catálogo modelo a uma barbearia nova (Task 5.1). A operação é
/// idempotente: serviços já presentes com o mesmo nome não são recriados.
/// </summary>
public sealed class ServicoCatalogoModelo(IServicoRepositorio servicos)
{
    public async Task AplicarAsync(Guid barbeariaId, CancellationToken cancellationToken)
    {
        var existentes = await servicos.ListarPorBarbeariaAsync(barbeariaId, cancellationToken);
        var nomes = existentes.Select(servico => servico.Nome).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var modelos = CatalogoModeloServicos.Todos
            .Where(modelo => !nomes.Contains(modelo.Nome))
            .Select(modelo => Servico.CriarModelo(barbeariaId, modelo.Nome, modelo.DuracaoMinutos))
            .ToList();

        if (modelos.Count > 0)
            await servicos.AdicionarAsync(modelos, cancellationToken);
    }
}
