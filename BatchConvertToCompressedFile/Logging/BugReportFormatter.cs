using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace BatchConvertToCompressedFile.Logging;

/// <summary>Collects operating-system details used in bug reports.</summary>
internal static class OsEnvironmentInfo
{
    /// <summary>Gets the marketing/version name of the current operating system.</summary>
    /// <returns>A human-readable OS version string.</returns>
    public static string GetOsVersionName()
    {
        if (OperatingSystem.IsWindows())
        {
            return RuntimeInformation.OSDescription;
        }

        if (OperatingSystem.IsLinux())
        {
            var prettyName = TryReadLinuxPrettyName();
            return string.IsNullOrWhiteSpace(prettyName) ? RuntimeInformation.OSDescription : prettyName;
        }

        if (OperatingSystem.IsMacOS())
        {
            return RuntimeInformation.OSDescription;
        }

        return RuntimeInformation.OSDescription;
    }

    /// <summary>Gets the platform label used in the bug report template.</summary>
    /// <returns><c>Windows Version</c>, <c>Linux Version</c>, <c>MacOsX Version</c>, or <c>OS Version</c>.</returns>
    public static string GetPlatformVersionLabel()
    {
        if (OperatingSystem.IsWindows()) return "Windows Version";
        if (OperatingSystem.IsLinux()) return "Linux Version";
        if (OperatingSystem.IsMacOS()) return "MacOsX Version";
        return "OS Version";
    }

    /// <summary>Gets whether the process is 64-bit.</summary>
    public static string Bitness => Environment.Is64BitProcess ? "64-bit" : "32-bit";

    private static string? TryReadLinuxPrettyName()
    {
        const string osReleasePath = "/etc/os-release";
        try
        {
            if (!File.Exists(osReleasePath)) return null;

            foreach (var line in File.ReadLines(osReleasePath))
            {
                if (!line.StartsWith("PRETTY_NAME=", StringComparison.Ordinal)) continue;

                var value = line["PRETTY_NAME=".Length..].Trim().Trim('"');
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (Exception ex)
        {
            Log.Debug("Failed to read {Path}: {Message}", osReleasePath, ex.Message);
        }

        return null;
    }
}

/// <summary>Builds the standardized bug report text required by the reporting API.</summary>
internal static class BugReportFormatter
{
    /// <summary>Builds a complete report for a Serilog event.</summary>
    /// <param name="logEvent">Serilog event to format.</param>
    /// <returns>The formatted bug report.</returns>
    public static string Build(Serilog.Events.LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        return Build(logEvent.RenderMessage(), logEvent.Exception);
    }

    /// <summary>Builds a complete report for a message and optional exception.</summary>
    /// <param name="message">Error message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <returns>The formatted bug report.</returns>
    public static string Build(string message, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(message);

        var builder = new StringBuilder();

        builder.AppendLine("=== Environment Details ===");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Application Name: {AppConfig.ApplicationName}");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Application Version: {AppConfig.ApplicationVersionString}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"OS Version: {Environment.OSVersion}");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Architecture: {RuntimeInformation.OSArchitecture}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Bitness: {OsEnvironmentInfo.Bitness}");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"{OsEnvironmentInfo.GetPlatformVersionLabel()}: {OsEnvironmentInfo.GetOsVersionName()}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Processor Count: {Environment.ProcessorCount}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Base Directory: {AppContext.BaseDirectory}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Temp Path: {Path.GetTempPath()}");
        builder.AppendLine();

        builder.AppendLine("=== Error Details ===");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Error message: {message}");
        builder.AppendLine();

        builder.AppendLine("=== Exception Details ===");
        if (exception is null)
        {
            builder.AppendLine("Type: None");
            builder.AppendLine("Message: None");
            builder.AppendLine("Source: None");
            builder.AppendLine("StackTrace: None");
        }
        else
        {
            AppendException(builder, exception);

            var inner = exception.InnerException;
            var depth = 0;
            while (inner is not null && depth < 10)
            {
                builder.AppendLine();
                builder.AppendLine("--- Inner Exception ---");
                AppendException(builder, inner);
                inner = inner.InnerException;
                depth++;
            }
        }

        return builder.ToString();
    }

    private static void AppendException(StringBuilder builder, Exception exception)
    {
        builder.AppendLine(CultureInfo.InvariantCulture, $"Type: {exception.GetType().FullName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Source: {exception.Source}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"StackTrace: {exception.StackTrace}");
    }
}
