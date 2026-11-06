using System.Threading.Channels;
using BatchConvertToCompressedFile.Interfaces;
using Serilog.Core;
using Serilog.Events;

namespace BatchConvertToCompressedFile.Logging;

/// <summary>
/// Serilog sink that queues Warning and higher events and forwards them to the bug report API
/// on a background task without blocking the UI thread.
/// </summary>
internal sealed class BugReportApiSink : ILogEventSink, IDisposable
{
    private readonly Channel<LogEvent> _channel = Channel.CreateBounded<LogEvent>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    private readonly CancellationTokenSource _cancellation = new();
    private readonly IBugReportService _bugReportService;
    private readonly Task _processingTask;
    private int _disposed;

    /// <summary>Initializes a new sink.</summary>
    /// <param name="bugReportService">Service that posts reports to the API.</param>
    public BugReportApiSink(IBugReportService bugReportService)
    {
        _bugReportService =
            bugReportService ?? throw new ArgumentNullException(nameof(bugReportService));

        // Task.Run avoids capturing a UI synchronization context that would deadlock shutdown.
        _processingTask = Task.Run(() => ProcessQueueAsync(_cancellation.Token));
    }

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        if (logEvent.Level < LogEventLevel.Warning) return;

        _channel.Writer.TryWrite(logEvent);
    }

    /// <summary>Stops accepting events and waits briefly for queued reports to be sent.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        _channel.Writer.TryComplete();

        try
        {
            if (!_processingTask.Wait(TimeSpan.FromSeconds(5)))
            {
                _cancellation.Cancel();
                _processingTask.Wait(TimeSpan.FromSeconds(2));
            }
        }
        catch (Exception ex)
        {
            Log.Debug("Bug report sink shutdown failed: {Message}", ex.Message);
        }
        finally
        {
            if (_processingTask.IsCompleted)
            {
                _cancellation.Dispose();
            }
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _channel.Reader
                       .WaitToReadAsync(cancellationToken)
                       .ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var logEvent))
                {
                    try
                    {
                        var report = BugReportFormatter.Build(logEvent);
                        await _bugReportService
                            .SendBugReportAsync(report, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("Bug report sink could not process an event: {Message}", ex.Message);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }
}
