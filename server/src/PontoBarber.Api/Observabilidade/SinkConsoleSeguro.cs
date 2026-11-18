using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Serilog.Core;
using Serilog.Events;

namespace PontoBarber.Api.Observabilidade;

/// <summary>
/// Sink de console que emite JSON com máscara adicional sobre mensagem,
/// exceção e propriedades renderizadas.
/// </summary>
public sealed class SinkConsoleSeguro(TextWriter? escritor = null) : ILogEventSink
{
    private static readonly JsonSerializerOptions _opcoes = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly TextWriter _escritor = escritor ?? Console.Out;

    public void Emit(LogEvent logEvent)
    {
        var objeto = new Dictionary<string, object?>
        {
            ["timestamp"] = logEvent.Timestamp,
            ["level"] = logEvent.Level.ToString(),
            ["message"] = RedacaoDadosSensiveis.MascararTexto(
                logEvent.RenderMessage(CultureInfo.InvariantCulture)),
            ["exception"] = logEvent.Exception is null
                ? null
                : RedacaoDadosSensiveis.MascararTexto(logEvent.Exception.ToString())
        };

        foreach (var propriedade in logEvent.Properties)
        {
            objeto[propriedade.Key] = RedacaoDadosSensiveis.EHNomeSensivel(propriedade.Key)
                ? RedacaoDadosSensiveis.ValorMascarado
                : RedacaoDadosSensiveis.MascararTexto(Renderizar(propriedade.Value));
        }

        _escritor.WriteLine(JsonSerializer.Serialize(objeto, _opcoes));
    }

    private static string Renderizar(LogEventPropertyValue valor)
    {
        if (valor is ScalarValue { Value: not null } escalar)
            return escalar.Value is string texto ? texto : escalar.Value.ToString() ?? string.Empty;

        using var writer = new StringWriter();
        valor.Render(writer, format: null, CultureInfo.InvariantCulture);
        return writer.ToString();
    }
}
