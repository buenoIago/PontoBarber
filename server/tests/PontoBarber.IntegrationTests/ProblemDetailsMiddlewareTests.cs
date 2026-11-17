using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PontoBarber.Api.Middleware;
using PontoBarber.Application.Abstractions;

namespace PontoBarber.IntegrationTests;

public class ProblemDetailsMiddlewareTests
{
    [Fact]
    public async Task ProblemDetailsMiddleware_ComErroDeValidacao_Retorna400ComErrosPorCampo()
    {
        var erros = new Dictionary<string, string[]>
        {
            ["nome"] = ["Campo obrigatório"],
            ["email"] = ["Formato inválido"]
        };

        var resposta = await ExecutarAsync(
            _ => Task.FromException(new ApplicationValidationException(erros)));

        Assert.Equal(StatusCodes.Status400BadRequest, resposta.Status);
        Assert.Equal("application/problem+json", resposta.ContentType);
        Assert.Equal(ErrorCodes.ValidationFailed, ObterTexto(resposta.Corpo, "code"));
        Assert.Equal("trace-teste-123", ObterTexto(resposta.Corpo, "traceId"));
        Assert.Equal("/api/v1/teste", ObterTexto(resposta.Corpo, "instance"));

        var campos = resposta.Corpo.GetProperty("errors").EnumerateObject()
            .ToDictionary(propriedade => propriedade.Name, propriedade => propriedade.Value);
        Assert.Equal(2, campos.Count);
        Assert.Equal("Campo obrigatório", campos["nome"][0].GetString());
        Assert.Equal("Formato inválido", campos["email"][0].GetString());
    }

    [Fact]
    public async Task ProblemDetailsMiddleware_ComConflitoDeHorario_Retorna409ComCodigoEstavel()
    {
        var resposta = await ExecutarAsync(_ => Task.FromException(
            new ApplicationConflictException(ErrorCodes.ScheduleSlotUnavailable, "Horário indisponível")));

        Assert.Equal(StatusCodes.Status409Conflict, resposta.Status);
        Assert.Equal("application/problem+json", resposta.ContentType);
        Assert.Equal(ErrorCodes.ScheduleSlotUnavailable, ObterTexto(resposta.Corpo, "code"));
        Assert.Equal("Horário indisponível", ObterTexto(resposta.Corpo, "detail"));
        Assert.Empty(resposta.Corpo.GetProperty("errors").EnumerateObject());
    }

    [Fact]
    public async Task ProblemDetailsMiddleware_ComConcorrencia_Retorna409ComCodigoEstavel()
    {
        var resposta = await ExecutarAsync(
            _ => Task.FromException(new DbUpdateConcurrencyException("rowversion divergente")));

        Assert.Equal(StatusCodes.Status409Conflict, resposta.Status);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, ObterTexto(resposta.Corpo, "code"));
        Assert.Equal("trace-teste-123", ObterTexto(resposta.Corpo, "traceId"));
    }

    [Fact]
    public async Task ProblemDetailsMiddleware_ComFalhaNaoPrevista_NaoExpoeDetalhesInternos()
    {
        var resposta = await ExecutarAsync(
            _ => Task.FromException(new InvalidOperationException("detalhe sensível interno")));

        Assert.Equal(StatusCodes.Status500InternalServerError, resposta.Status);
        Assert.Equal(ErrorCodes.InternalError, ObterTexto(resposta.Corpo, "code"));
        Assert.DoesNotContain("detalhe sensível interno", ObterTexto(resposta.Corpo, "detail"));
    }

    [Fact]
    public async Task ProblemDetailsMiddleware_SemErro_NaoAlteraResposta()
    {
        var resposta = await ExecutarAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        Assert.Equal(StatusCodes.Status204NoContent, resposta.Status);
        Assert.Null(resposta.ContentType);
    }

    private static async Task<RespostaProblema> ExecutarAsync(RequestDelegate proximo)
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-teste-123";
        context.Request.Path = "/api/v1/teste";

        var corpo = new MemoryStream();
        context.Response.Body = corpo;

        await new ProblemDetailsMiddleware(proximo).InvokeAsync(context);

        corpo.Position = 0;
        var json = await new StreamReader(corpo).ReadToEndAsync();
        JsonElement raiz = default;
        if (!string.IsNullOrWhiteSpace(json))
        {
            using var documento = JsonDocument.Parse(json);
            raiz = documento.RootElement.Clone();
        }

        return new RespostaProblema(context.Response.StatusCode, context.Response.ContentType, raiz);
    }

    private static string ObterTexto(JsonElement corpo, string propriedade) =>
        corpo.GetProperty(propriedade).GetString() ?? string.Empty;

    private sealed record RespostaProblema(int Status, string? ContentType, JsonElement Corpo);
}
