using System.Globalization;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace PontoBarber.WebApp.Observabilidade;

/// <summary>
/// Aplica redação ao LogEvent antes de encaminhá-lo a qualquer destino,
/// console ou OTLP. Trabalha mensagem, exceção e valores estruturados,
/// inclusive objetos desconstruídos e coleções aninhadas.
/// </summary>
public sealed class RedacaoLogEventSink(params ILogEventSink[] destinos) : ILogEventSink
{
    private static readonly MessageTemplateParser _parser = new();

    public void Emit(LogEvent logEvent)
    {
        var propriedadesReduzidas = logEvent.Properties
            .Select(propriedade => (
                Nome: propriedade.Key,
                Original: propriedade.Value,
                Reduzida: ReduzirValor(propriedade.Key, propriedade.Value)))
            .ToList();

        var mensagem = logEvent.RenderMessage(CultureInfo.InvariantCulture);
        foreach (var propriedade in propriedadesReduzidas)
        {
            var original = Renderizar(propriedade.Original).Trim('"');
            var reduzida = Renderizar(propriedade.Reduzida).Trim('"');

            if (!string.IsNullOrWhiteSpace(original) &&
                !string.Equals(original, reduzida, StringComparison.Ordinal))
            {
                mensagem = mensagem.Replace(original, reduzida, StringComparison.Ordinal);
            }
        }

        mensagem = RedacaoDadosSensiveis.MascararTexto(mensagem);

        var excecaoOriginal = logEvent.Exception;
        Exception? excecao = null;
        if (excecaoOriginal is not null)
        {
            excecao = new ExcecaoMascarada(
                RedacaoDadosSensiveis.MascararTexto(excecaoOriginal.ToString()));
        }

        var propriedades = propriedadesReduzidas
            .Select(propriedade => new LogEventProperty(propriedade.Nome, propriedade.Reduzida))
            .ToList();

        var evento = new LogEvent(
            logEvent.Timestamp,
            logEvent.Level,
            excecao,
            _parser.Parse(mensagem),
            propriedades);

        if (excecaoOriginal is not null)
        {
            evento.AddPropertyIfAbsent(new LogEventProperty(
                "ExceptionType",
                new ScalarValue(excecaoOriginal.GetType().FullName ?? excecaoOriginal.GetType().Name)));
        }

        foreach (var destino in destinos)
            destino.Emit(evento);
    }

    private static string Renderizar(LogEventPropertyValue valor)
    {
        using var escritor = new StringWriter();
        valor.Render(escritor, format: null, CultureInfo.InvariantCulture);
        return escritor.ToString();
    }

    private static LogEventPropertyValue ReduzirValor(string nome, LogEventPropertyValue valor)
    {
        if (RedacaoDadosSensiveis.EHNomeSensivel(nome))
            return new ScalarValue(RedacaoDadosSensiveis.ValorMascarado);

        return valor switch
        {
            ScalarValue { Value: string texto } => new ScalarValue(RedacaoDadosSensiveis.MascararTexto(texto)),
            SequenceValue sequencia => new SequenceValue(
                sequencia.Elements.Select(elemento => ReduzirValor(string.Empty, elemento))),
            StructureValue estrutura => new StructureValue(
                estrutura.Properties.Select(propriedade =>
                    new LogEventProperty(propriedade.Name, ReduzirValor(propriedade.Name, propriedade.Value))),
                estrutura.TypeTag),
            DictionaryValue dicionario => new DictionaryValue(
                dicionario.Elements.Select(elemento => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                    (ScalarValue)ReduzirValor(string.Empty, elemento.Key),
                    ReduzirValor(elemento.Key.Value?.ToString() ?? string.Empty, elemento.Value)))),
            _ => valor
        };
    }

    private sealed class ExcecaoMascarada(string mensagem) : Exception(mensagem);
}
