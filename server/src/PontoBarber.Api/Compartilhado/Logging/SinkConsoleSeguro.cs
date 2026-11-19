using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace PontoBarber.Api.Compartilhado.Logging;

/// <summary>
/// Destino de console que usa exatamente o formato padrão de
/// <c>WriteTo.Console()</c>. A redação é aplicada antes por RedacaoLogEventSink.
/// </summary>
public sealed class SinkConsoleSeguro : ILogEventSink, IDisposable
{
    private readonly Logger _console = new LoggerConfiguration()
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
        .CreateLogger();

    public void Emit(LogEvent logEvent) => _console.Write(logEvent);

    public void Dispose() => _console.Dispose();
}
