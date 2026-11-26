using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace PontoBarber.Api.Compartilhado.Swagger;

/// <summary>
/// Ordena grupos e operações do documento OpenAPI na sequência de uso da API.
/// O Swagger UI respeita a ordem do array de tags e dos paths do documento.
/// </summary>
public sealed class OrdenarDocumentoSwaggerTransformer : IOpenApiDocumentTransformer
{
    private static readonly string[] _grupos =
    [
        "Autenticação",
        "Barbearias",
        "Horários de Funcionamento",
        "Profissionais",
        "Serviços",
        "Disponibilidade",
        "Bloqueios",
        "Agendamentos",
        "Meus Agendamentos",
        "Clientes",
        "Meu Perfil de Cliente",
        "Painel",
        "Notificações",
        "API",
    ];

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (document is null)
            return Task.CompletedTask;

        var ordem = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var indice = 0; indice < _grupos.Length; indice++)
            ordem[_grupos[indice]] = indice;

        var tags = new SortedSet<OpenApiTag>(new ComparadorGrupos(ordem));
        foreach (var nome in _grupos)
            tags.Add(new OpenApiTag { Name = nome });
        foreach (var tag in document!.Tags!)
            if (tag is not null && tag.Name is not null && !ordem.ContainsKey(tag.Name))
                tags.Add(tag);
        document.Tags = tags;

        // Ordem estável dentro de cada grupo: preserva a sequência de declaração
        // das operações, que já segue o fluxo natural de uso.
        var caminhos = new OpenApiPaths();
        foreach (var caminho in document.Paths.OrderBy(c => IndiceGrupo(c.Value, ordem)))
            if (caminho.Value is OpenApiPathItem item)
                caminhos.Add(caminho.Key, item);
        document.Paths = caminhos;

        return Task.CompletedTask;
    }

    private static int IndiceGrupo(IOpenApiPathItem item, Dictionary<string, int> ordem)
    {
        if (item.Operations is null)
            return int.MaxValue;

        foreach (var operacao in item.Operations.Values)
        {
            if (operacao is null)
                continue;
            foreach (var tag in operacao!.Tags!)
                if (tag is not null && tag.Name is not null && ordem.TryGetValue(tag.Name, out var indice))
                    return indice;
        }

        return int.MaxValue;
    }

    private sealed class ComparadorGrupos(Dictionary<string, int> ordem) : IComparer<OpenApiTag>
    {
        public int Compare(OpenApiTag? x, OpenApiTag? y)
        {
            var indiceX = Indice(x);
            var indiceY = Indice(y);
            if (indiceX != indiceY)
                return indiceX.CompareTo(indiceY);
            return string.Compare(x?.Name, y?.Name, StringComparison.Ordinal);
        }

        private int Indice(OpenApiTag? tag) =>
            tag?.Name is not null && ordem.TryGetValue(tag.Name, out var indice)
                ? indice
                : int.MaxValue;
    }
}
