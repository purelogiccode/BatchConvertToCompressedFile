using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace BatchConvertToCompressedFile.Tests;

public sealed class BugReportApiSinkTests
{
    [Fact]
    public async Task Emit_WarningEvent_IsForwardedToApi()
    {
        var service = new RecordingBugReportService();
        using var sink = new BugReportApiSink(service);

        sink.Emit(CreateEvent(LogEventLevel.Warning, "warning message"));

        var report = await service.ReportReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("warning message", report);
        Assert.Contains("=== Environment Details ===", report);
    }

    [Fact]
    public async Task Emit_InformationEvent_IsNotForwarded()
    {
        var service = new RecordingBugReportService();
        using var sink = new BugReportApiSink(service);

        sink.Emit(CreateEvent(LogEventLevel.Information, "information message"));
        await Task.Delay(200);

        Assert.False(service.ReportReceived.Task.IsCompleted);
    }

    [Fact]
    public async Task Emit_ExceptionEvent_IncludesExceptionDetails()
    {
        var service = new RecordingBugReportService();
        using var sink = new BugReportApiSink(service);

        sink.Emit(CreateEvent(
            LogEventLevel.Error,
            "error message",
            new InvalidOperationException("kaput")));

        var report = await service.ReportReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("Type: System.InvalidOperationException", report);
        Assert.Contains("Message: kaput", report);
    }

    private static LogEvent CreateEvent(LogEventLevel level, string message, Exception? exception = null)
    {
        return new LogEvent(
            DateTimeOffset.Now,
            level,
            exception,
            new MessageTemplateParser().Parse(message),
            []);
    }

    private sealed class RecordingBugReportService : IBugReportService
    {
        public TaskCompletionSource<string> ReportReceived { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<bool> SendBugReportAsync(string message, CancellationToken cancellationToken = default)
        {
            ReportReceived.TrySetResult(message);
            return Task.FromResult(true);
        }
    }
}
