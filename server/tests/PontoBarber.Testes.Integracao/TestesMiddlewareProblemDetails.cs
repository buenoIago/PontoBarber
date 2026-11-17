using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PontoBarber.WebApp.Middleware;
using PontoBarber.Aplicacao.Abstracoes;

namespace PontoBarber.Testes.Integracao;

public class TestesMiddlewareProblemDetails
{
    [Fact]
    public async Task MiddlewareProblemDetails_ComErroDeValidacao_Retorna400ComErrosPorCampo()
    {
        var erros = new Dictionary<string, string[]>
        {
            ["nome"] = ["Campo obrigatório"],
            ["email"] = ["Formato inválido"]
        };

        var resposta = await ExecutarAsync(
            _ => Task.FromException(new ExcecaoValidacaoAplicacao(erros)));

        Assert.Equal(StatusCodes.Status400BadRequest, resposta.Status);
        Assert.Equal("application/problem+json", resposta.TipoConteudo);
        Assert.Equal(CodigosErro.ValidacaoFalhou, ObterTexto(resposta.Corpo, "code"));
        Assert.Equal("trace-teste-123", ObterTexto(resposta.Corpo, "traceId"));
        Assert.Equal("/api/v1/teste", ObterTexto(resposta.Corpo, "instance"));

        var campos = resposta.Corpo.GetProperty("errors").EnumerateObject()
            .ToDictionary(propriedade => propriedade.Name, propriedade => propriedade.Value);
        Assert.Equal(2, campos.Count);
        Assert.Equal("Campo obrigatório", campos["nome"][0].GetString());
        Assert.Equal("Formato inválido", campos["email"][0].GetString());
    }

    [Fact]
    public async Task MiddlewareProblemDetails_ComConflitoDeHorario_Retorna409ComCodigoEstavel()
    {
        var resposta = await ExecutarAsync(_ => Task.FromException(
            new ExcecaoConflitoAplicacao(CodigosErro.AgendaHorarioIndisponivel, "Horário indisponível")));

        Assert.Equal(StatusCodes.Status409Conflict, resposta.Status);
        Assert.Equal("application/problem+json", resposta.TipoConteudo);
        Assert.Equal(CodigosErro.AgendaHorarioIndisponivel, ObterTexto(resposta.Corpo, "code"));
        Assert.Equal("Horário indisponível", ObterTexto(resposta.Corpo, "detail"));
        Assert.Empty(resposta.Corpo.GetProperty("errors").EnumerateObject());
    }

    [Fact]
    public async Task MiddlewareProblemDetails_ComConcorrencia_Retorna409ComCodigoEstavel()
    {
        var resposta = await ExecutarAsync(
            _ => Task.FromException(new DbUpdateConcurrencyException("rowversion divergente")));

        Assert.Equal(StatusCodes.Status409Conflict, resposta.Status);
        Assert.Equal(CodigosErro.ConcorrenciaConflito, ObterTexto(resposta.Corpo, "code"));
        Assert.Equal("trace-teste-123", ObterTexto(resposta.Corpo, "traceId"));
    }

    [Fact]
    public async Task MiddlewareProblemDetails_ComFalhaNaoPrevista_NaoExpoeDetalhesInternos()
    {
        var resposta = await ExecutarAsync(
            _ => Task.FromException(new InvalidOperationException("detalhe sensível interno")));

        Assert.Equal(StatusCodes.Status500InternalServerError, resposta.Status);
        Assert.Equal(CodigosErro.ErroInterno, ObterTexto(resposta.Corpo, "code"));
        Assert.DoesNotContain("detalhe sensível interno", ObterTexto(resposta.Corpo, "detail"));
    }

    [Fact]
    public async Task MiddlewareProblemDetails_SemErro_NaoAlteraResposta()
    {
        var resposta = await ExecutarAsync(contexto =>
        {
            contexto.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        Assert.Equal(StatusCodes.Status204NoContent, resposta.Status);
        Assert.Null(resposta.TipoConteudo);
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
