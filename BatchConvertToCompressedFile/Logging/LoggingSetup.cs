using BatchConvertToCompressedFile.Services;
using Serilog.Events;

namespace BatchConvertToCompressedFile.Logging;

/// <summary>Configures the application-wide Serilog logger.</summary>
internal static class LoggingSetup
{
    /// <summary>Creates the configured logger and installs it as <see cref="Log.Logger"/>.</summary>
    /// <param name="bugReportSink">Sink that forwards Warning+ events to the bug report API.</param>
    /// <returns>The created logger.</returns>
    public static ILogger CreateLogger(BugReportApiSink bugReportSink)
    {
        ArgumentNullException.ThrowIfNull(bugReportSink);

        var configuration = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Sink(new UiLogSink())
            .WriteTo.Sink(bugReportSink);

        try
        {
            var logFolder = PathUtilities.GetLogFolder();
            Directory.CreateDirectory(logFolder);

            configuration.WriteTo.Async(sink => sink.File(
                Path.Combine(logFolder, "batch-convert-.log"),
                LogEventLevel.Information,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true));
        }
        catch (Exception ex)
        {
            // Logger setup must never prevent the application from starting.
            Serilog.Debugging.SelfLog.WriteLine("Could not configure the file log sink: {0}", ex);
        }

        Log.Logger = configuration.CreateLogger();
        return Log.Logger;
    }
}
