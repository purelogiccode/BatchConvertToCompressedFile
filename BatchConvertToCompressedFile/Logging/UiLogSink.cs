using Serilog.Core;
using Serilog.Events;

namespace BatchConvertToCompressedFile.Logging;

/// <summary>Feeds formatted log events to the in-app log viewer.</summary>
internal sealed class UiLogSink : ILogEventSink
{
    /// <summary>Raised for every emitted event. Subscribers are responsible for thread marshaling.</summary>
    public static event Action<string>? MessageLogged;

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        var handler = MessageLogged;
        if (handler is null) return;

        var exceptionText = logEvent.Exception is null
            ? string.Empty
            : $" — {logEvent.Exception.GetType().Name}: {logEvent.Exception.Message}";

        handler($"[{logEvent.Timestamp:HH:mm:ss.fff}] [{logEvent.Level.ToString().ToUpperInvariant()}] " +
                $"{logEvent.RenderMessage()}{exceptionText}");
    }
}
