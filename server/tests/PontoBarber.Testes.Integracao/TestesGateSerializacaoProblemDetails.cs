using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using PontoBarber.Api.Compartilhado.Erros;
using PontoBarber.Aplicacao.Compartilhado;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Gate obrigatório do ADR-035: compara a serialização contratual legada com a
/// proposta baseada em <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>
/// nativo. A comparação é semântica e estrutural: nomes/casing, presença,
/// nulos, erros por campo, status e Content-Type são contratuais; a ordem das
/// propriedades não é critério de compatibilidade.
/// </summary>
[TestClass]
public sealed class TestesGateSerializacaoProblemDetails
{
    private static readonly IReadOnlyDictionary<string, string[]> _semErros =
        new Dictionary<string, string[]>();

    private static readonly JsonSerializerOptions _opcoesLegado = new(JsonSerializerDefaults.Web);

    public static IEnumerable<object[]> Casos
    {
        get
        {
            yield return [NovoCaso(
                "400 com detail e errors por campo",
                400, CodigosErro.ValidacaoFalhou, "Requisição inválida", "Campo obrigatório",
                new Dictionary<string, string[]>
                {
                    ["nome"] = ["Campo obrigatório"],
                    ["email"] = ["Formato inválido"]
                })];
            yield return [NovoCaso(
                "400 com detail nulo",
                400, CodigosErro.ValidacaoFalhou, "Requisição inválida", null, _semErros)];
            yield return [NovoCaso(
                "400 com detail vazio",
                400, CodigosErro.ValidacaoFalhou, "Requisição inválida", string.Empty, _semErros)];
            yield return [NovoCaso(
                "401 não autenticado",
                401, CodigosErro.AutenticacaoCredenciaisInvalidas, "Não autenticado",
                "É necessário autenticar para acessar este recurso.", _semErros)];
            yield return [NovoCaso(
                "403 tenant negado",
                403, CodigosErro.TenantNegado, "Acesso negado",
                "O usuário não possui vínculo com a barbearia solicitada.", _semErros)];
            yield return [NovoCaso(
                "404 tenant não encontrado",
                404, CodigosErro.TenantNaoEncontrado, "Não encontrado",
                "A barbearia solicitada não foi encontrada.", _semErros)];
            yield return [NovoCaso(
                "409 conflito de agenda",
                409, CodigosErro.AgendaHorarioIndisponivel, "Conflito",
                "Horário indisponível", _semErros)];
            yield return [NovoCaso(
                "409 concorrência",
                409, CodigosErro.ConcorrenciaConflito, "Conflito de concorrência",
                "O registro foi alterado por outra operação.", _semErros)];
            yield return [NovoCaso(
                "422 validação por campo",
                422, CodigosErro.ValidacaoFalhou, "Entidade inválida", "Dados inválidos",
                new Dictionary<string, string[]>
                {
                    ["nome"] = ["Campo obrigatório", "Deve ter ao menos 3 caracteres"],
                    ["email"] = ["Formato inválido"]
                })];
            yield return [NovoCaso(
                "500 erro interno",
                500, CodigosErro.ErroInterno, "Erro interno",
                "Ocorreu um erro inesperado.", _semErros)];
        }
    }

    [TestMethod]
    [DynamicData(nameof(Casos))]
    public async Task SerializacaoNativa_DeveSerEquivalenteAContratoLegado(CasoGate caso)
    {
        var legado = await SerializarLegadoAsync(caso, "trace-legado-" + caso.Rastreio);
        var nativa = await SerializarNativaAsync(caso, "trace-nativa-" + caso.Rastreio);

        Assert.AreEqual(caso.Status, legado.Status, "status HTTP legado");
        Assert.AreEqual(caso.Status, nativa.Status, "status HTTP nativo");
        Assert.AreEqual("application/problem+json", legado.TipoConteudo, "Content-Type legado");
        Assert.AreEqual("application/problem+json", nativa.TipoConteudo, "Content-Type nativo");

        Assert.AreEqual(
            "trace-legado-" + caso.Rastreio,
            legado.Corpo["traceId"]?.GetValue<string>(),
            "traceId legado deve ser o TraceIdentifier do contexto");
        Assert.AreEqual(
            "trace-nativa-" + caso.Rastreio,
            nativa.Corpo["traceId"]?.GetValue<string>(),
            "traceId nativo deve ser o TraceIdentifier do contexto");

        var propriedadesContratuais = new[]
        {
            "type", "title", "status", "detail", "instance", "code", "traceId", "errors"
        };
        foreach (var propriedade in propriedadesContratuais)
        {
            Assert.IsTrue(legado.Corpo.ContainsKey(propriedade), $"propriedade legada ausente: {propriedade}");
            Assert.IsTrue(nativa.Corpo.ContainsKey(propriedade), $"propriedade nativa ausente: {propriedade}");
        }

        if (caso.Detalhe is null)
        {
            Assert.IsNull(legado.Corpo["detail"], "detail nulo legado");
            Assert.IsNull(nativa.Corpo["detail"], "detail nulo nativo");
        }

        var errosNativos = nativa.Corpo["errors"];
        Assert.IsNotNull(errosNativos);
        Assert.AreEqual(JsonValueKind.Object, errosNativos.GetValueKind(), "errors deve ser objeto");

        NormalizarTraceId(legado.Corpo, "trace-legado-" + caso.Rastreio);
        NormalizarTraceId(nativa.Corpo, "trace-nativa-" + caso.Rastreio);

        CollectionAssert.AreEquivalent(
            legado.Corpo.Select(propriedade => propriedade.Key).ToArray(),
            nativa.Corpo.Select(propriedade => propriedade.Key).ToArray(),
            "nomes/casing das propriedades");

        if (!JsonNode.DeepEquals(legado.Corpo, nativa.Corpo))
        {
            Assert.Fail(
                $"Serialização divergente ({caso.Nome}).{Environment.NewLine}" +
                $"Legado: {legado.Corpo.ToJsonString()}{Environment.NewLine}" +
                $"Nativo: {nativa.Corpo.ToJsonString()}");
        }
    }

    private static CasoGate NovoCaso(
        string nome,
        int status,
        string codigo,
        string titulo,
        string? detalhe,
        IReadOnlyDictionary<string, string[]> erros) =>
        new(nome, status, codigo, titulo, detalhe, erros, Guid.NewGuid().ToString("N"));

    private static DefaultHttpContext CriarContexto(string traceId)
    {
        var contexto = new DefaultHttpContext();
        contexto.TraceIdentifier = traceId;
        contexto.Request.Path = "/api/v1/teste";
        return contexto;
    }

    private static async Task<RespostaGate> SerializarNativaAsync(CasoGate caso, string traceId)
    {
        var contexto = CriarContexto(traceId);
        return await CapturarAsync(contexto, ctx =>
            EscritorProblemDetails.EscreverAsync(
                ctx, caso.Status, caso.Codigo, caso.Titulo, caso.Detalhe, caso.Erros));
    }

    private static async Task<RespostaGate> SerializarLegadoAsync(CasoGate caso, string traceId)
    {
        var legado = new ContratoLegado
        {
            Status = caso.Status,
            Title = caso.Titulo,
            Detail = caso.Detalhe,
            Instance = "/api/v1/teste",
            Code = caso.Codigo,
            TraceId = traceId,
            Errors = caso.Erros
        };

        var contexto = CriarContexto(traceId);
        return await CapturarAsync(contexto, ctx =>
        {
            ctx.Response.StatusCode = caso.Status;
            ctx.Response.ContentType = "application/problem+json";
            return ctx.Response.WriteAsync(JsonSerializer.Serialize(legado, _opcoesLegado));
        });
    }

    private static async Task<RespostaGate> CapturarAsync(HttpContext contexto, Func<HttpContext, Task> escrever)
    {
        var corpo = new MemoryStream();
        contexto.Response.Body = corpo;

        await escrever(contexto);

        corpo.Position = 0;
        var json = await new StreamReader(corpo).ReadToEndAsync();
        var objecto = JsonNode.Parse(json)!.AsObject();
        return new RespostaGate(contexto.Response.StatusCode, contexto.Response.ContentType, objecto);
    }

    private static void NormalizarTraceId(JsonObject corpo, string valorOriginal)
    {
        Assert.AreEqual(valorOriginal, corpo["traceId"]?.GetValue<string>());
        corpo["traceId"] = "<traceId>";
    }

    /// <summary>Espelho exato do registro pré-migração (ADR-035, gate de serialização).</summary>
    private sealed record ContratoLegado
    {
        public string Type { get; init; } = "about:blank";
        public string Title { get; init; } = string.Empty;
        public int Status { get; init; }
        public string? Detail { get; init; }
        public string Instance { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string TraceId { get; init; } = string.Empty;
        public IReadOnlyDictionary<string, string[]> Errors { get; init; } =
            new Dictionary<string, string[]>();
    }

    public sealed record CasoGate(
        string Nome,
        int Status,
        string Codigo,
        string Titulo,
        string? Detalhe,
        IReadOnlyDictionary<string, string[]> Erros,
        string Rastreio)
    {
        public override string ToString() => Nome;
    }

    private sealed record RespostaGate(int Status, string? TipoConteudo, JsonObject Corpo);
}
