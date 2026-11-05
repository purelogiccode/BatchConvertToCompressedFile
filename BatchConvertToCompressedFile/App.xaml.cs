using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using SevenZip;

namespace BatchConvertToCompressedFile;

public partial class App : IDisposable
{
    private const string NativeLibraryResourcePrefix = "BatchConvertToCompressedFile.Native";

    private readonly BugReportService? _bugReportService;

    public static BugReportService? SharedBugReportService { get; private set; }

    public App()
    {
        BugReportService? bugReportService = null;
        try
        {
            bugReportService = new BugReportService(AppConfig.BugReportApiUrl, AppConfig.BugReportApiKey,
                AppConfig.ApplicationName);
            SharedBugReportService = bugReportService;
            _bugReportService = SharedBugReportService;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }
        catch
        {
            // If initialization fails, ensure proper cleanup
            bugReportService?.Dispose();
            SharedBugReportService = null;
            throw;
        }

        // Initialize SevenZipSharp library path
        InitializeSevenZipSharp();

        // Register the Exit event handler
        Exit += App_Exit;
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        // Dispose of the shared BugReportService instance
        _bugReportService?.Dispose();
        SharedBugReportService = null;

        // Unregister event handlers to prevent memory leaks
        AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        DispatcherUnhandledException -= App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
    }

    private void InitializeSevenZipSharp()
    {
        try
        {
            var dllName = GetNativeLibraryName();
            var baseDirectory = AppContext.BaseDirectory;

            // Prefer a native library deployed next to the application. If it is missing, fall back
            // to the copy embedded in the application.
            string? dllPath = Path.Combine(baseDirectory, dllName);
            if (!File.Exists(dllPath))
            {
                dllPath = ExtractEmbeddedLibrary(dllName);
            }

            if (dllPath != null)
            {
                SevenZipBase.SetLibraryPath(dllPath);
                return;
            }

            // Notify developer. Extraction will likely fail if the embedded fallback is unavailable.
            var errorMessage =
                $"Could not find the required 7-Zip library: {dllName} in {baseDirectory}";
            if (_bugReportService != null)
            {
                _ = _bugReportService.SendBugReportAsync(errorMessage);
            }
        }
        catch (Exception ex)
        {
            // Notify developer
            if (_bugReportService != null)
            {
                _ = _bugReportService.SendBugReportAsync(ex.Message);
            }
        }
    }

    private static string GetNativeLibraryName()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            return "7z_arm64.dll";
        }

        return Environment.Is64BitProcess ? "7z_x64.dll" : "7z_x86.dll";
    }

    /// <summary>
    /// Extracts the architecture-specific 7-Zip library embedded in the application when it is not
    /// deployed alongside the executable (for example, when a launcher copies only the executable).
    /// </summary>
    private static string? ExtractEmbeddedLibrary(string dllName)
    {
        var assembly = typeof(App).Assembly;
        var resourceName = $"{NativeLibraryResourcePrefix}.{dllName}";
        using var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
        {
            return null;
        }

        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localApplicationData))
        {
            localApplicationData = Path.GetTempPath();
        }

        var targetDirectory = Path.Combine(localApplicationData, AppConfig.ApplicationName, "native", version);
        var targetPath = Path.Combine(targetDirectory, dllName);

        if (File.Exists(targetPath))
        {
            if (new FileInfo(targetPath).Length == resourceStream.Length)
            {
                return targetPath;
            }

            // Replace a stale or corrupted copy from a previous run.
            File.Delete(targetPath);
        }

        Directory.CreateDirectory(targetDirectory);
        var temporaryPath = Path.Combine(targetDirectory, $"{dllName}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var destinationStream = File.Create(temporaryPath))
            {
                resourceStream.CopyTo(destinationStream);
            }

            try
            {
                File.Move(temporaryPath, targetPath);
            }
            catch (IOException) when (File.Exists(targetPath) &&
                                      new FileInfo(targetPath).Length == resourceStream.Length)
            {
                // Another application instance extracted the same library first.
            }

            return targetPath;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            ReportException(exception, "AppDomain.UnhandledException");
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportException(e.Exception, "Application.DispatcherUnhandledException");
        e.Handled = true;
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ReportException(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private async void ReportException(Exception exception, string source)
    {
        try
        {
            var message = BuildExceptionReport(exception, source);
            if (_bugReportService != null)
            {
                await _bugReportService.SendBugReportAsync(message);
            }
        }
        catch
        {
            // Silently ignore
        }
    }

    internal static string BuildExceptionReport(Exception exception, string source)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Error Source: {source}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Date and Time: {DateTime.Now}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"OS Version: {Environment.OSVersion}");
        sb.AppendLine(CultureInfo.InvariantCulture, $".NET Version: {Environment.Version}");
        sb.AppendLine();
        sb.AppendLine("Exception Details:");
        AppendExceptionDetails(sb, exception);
        return sb.ToString();
    }

    internal static void AppendExceptionDetails(StringBuilder sb, Exception exception, int level = 0)
    {
        while (true)
        {
            var indent = new string(' ', level * 2);
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Type: {exception.GetType().FullName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Message: {exception.Message}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Source: {exception.Source}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}StackTrace:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}{exception.StackTrace}");

            if (exception.InnerException == null) break;

            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Inner Exception:");
            exception = exception.InnerException;
            level += 1;
        }
    }

    public void Dispose()
    {
        _bugReportService?.Dispose();
        SharedBugReportService = null;

        AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        DispatcherUnhandledException -= App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;

        GC.SuppressFinalize(this);
    }
}