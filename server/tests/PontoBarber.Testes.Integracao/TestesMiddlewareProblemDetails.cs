using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PontoBarber.Api.Middleware;
using PontoBarber.Aplicacao.Abstracoes;

namespace PontoBarber.Testes.Integracao;

[TestClass]
public class TestesMiddlewareProblemDetails
{
    [TestMethod]
    public async Task MiddlewareProblemDetails_ComErroDeValidacao_Retorna400ComErrosPorCampo()
    {
        var erros = new Dictionary<string, string[]>
        {
            ["nome"] = ["Campo obrigatório"],
            ["email"] = ["Formato inválido"]
        };

        var resposta = await ExecutarAsync(
            _ => Task.FromException(new ExcecaoValidacaoAplicacao(erros)));

        Assert.AreEqual(StatusCodes.Status400BadRequest, resposta.Status);
        Assert.AreEqual("application/problem+json", resposta.TipoConteudo);
        Assert.AreEqual(CodigosErro.ValidacaoFalhou, ObterTexto(resposta.Corpo, "code"));
        Assert.AreEqual("trace-teste-123", ObterTexto(resposta.Corpo, "traceId"));
        Assert.AreEqual("/api/v1/teste", ObterTexto(resposta.Corpo, "instance"));

        var campos = resposta.Corpo.GetProperty("errors").EnumerateObject()
            .ToDictionary(propriedade => propriedade.Name, propriedade => propriedade.Value);
        Assert.AreEqual(2, campos.Count);
        Assert.AreEqual("Campo obrigatório", campos["nome"][0].GetString());
        Assert.AreEqual("Formato inválido", campos["email"][0].GetString());
    }

    [TestMethod]
    public async Task MiddlewareProblemDetails_ComConflitoDeHorario_Retorna409ComCodigoEstavel()
    {
        var resposta = await ExecutarAsync(_ => Task.FromException(
            new ExcecaoConflitoAplicacao(CodigosErro.AgendaHorarioIndisponivel, "Horário indisponível")));

        Assert.AreEqual(StatusCodes.Status409Conflict, resposta.Status);
        Assert.AreEqual("application/problem+json", resposta.TipoConteudo);
        Assert.AreEqual(CodigosErro.AgendaHorarioIndisponivel, ObterTexto(resposta.Corpo, "code"));
        Assert.AreEqual("Horário indisponível", ObterTexto(resposta.Corpo, "detail"));
        Assert.AreEqual(0, resposta.Corpo.GetProperty("errors").EnumerateObject().Count());
    }

    [TestMethod]
    public async Task MiddlewareProblemDetails_ComConcorrencia_Retorna409ComCodigoEstavel()
    {
        var resposta = await ExecutarAsync(
            _ => Task.FromException(new DbUpdateConcurrencyException("rowversion divergente")));

        Assert.AreEqual(StatusCodes.Status409Conflict, resposta.Status);
        Assert.AreEqual(CodigosErro.ConcorrenciaConflito, ObterTexto(resposta.Corpo, "code"));
        Assert.AreEqual("trace-teste-123", ObterTexto(resposta.Corpo, "traceId"));
    }

    [TestMethod]
    public async Task MiddlewareProblemDetails_ComFalhaNaoPrevista_NaoExpoeDetalhesInternos()
    {
        var resposta = await ExecutarAsync(
            _ => Task.FromException(new InvalidOperationException("detalhe sensível interno")));

        Assert.AreEqual(StatusCodes.Status500InternalServerError, resposta.Status);
        Assert.AreEqual(CodigosErro.ErroInterno, ObterTexto(resposta.Corpo, "code"));
        Assert.IsFalse(ObterTexto(resposta.Corpo, "detail").Contains("detalhe sensível interno", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task MiddlewareProblemDetails_SemErro_NaoAlteraResposta()
    {
        var resposta = await ExecutarAsync(contexto =>
        {
            contexto.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        Assert.AreEqual(StatusCodes.Status204NoContent, resposta.Status);
        Assert.IsNull(resposta.TipoConteudo);
    }

    private static async Task<RespostaTeste> ExecutarAsync(RequestDelegate proximo)
    {
        var contexto = new DefaultHttpContext();
        contexto.TraceIdentifier = "trace-teste-123";
        contexto.Request.Path = "/api/v1/teste";

        var corpo = new MemoryStream();
        contexto.Response.Body = corpo;

        await new ProblemDetailsMiddleware(proximo).InvokeAsync(contexto);

        corpo.Position = 0;
        var json = await new StreamReader(corpo).ReadToEndAsync();
        JsonElement raiz = default;
        if (!string.IsNullOrWhiteSpace(json))
        {
            using var documento = JsonDocument.Parse(json);
            raiz = documento.RootElement.Clone();
        }

        return new RespostaTeste(contexto.Response.StatusCode, contexto.Response.ContentType, raiz);
    }

    private static string ObterTexto(JsonElement corpo, string propriedade) =>
        corpo.GetProperty(propriedade).GetString() ?? string.Empty;

    private sealed record RespostaTeste(int Status, string? TipoConteudo, JsonElement Corpo);
}
