using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using BatchConvertToCompressedFile.Logging;
using BatchConvertToCompressedFile.Services;
using BatchConvertToCompressedFile.ViewModels;
using BatchConvertToCompressedFile.Views;

namespace BatchConvertToCompressedFile;

/// <summary>Avalonia application: composition root, global exception handling, and shutdown.</summary>
// ReSharper disable once PartialTypeWithSinglePart
// The XAML compiler generates the other part of this class.
public partial class App : Application
{
    private BugReportApiSink? _bugReportSink;
    private BugReportService? _bugReportService;
    private ApplicationStatsService? _statsService;
    private UpdateService? _updateService;
    private DonationService? _donationService;
    private IClassicDesktopStyleApplicationLifetime? _desktopLifetime;

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        _bugReportService = new BugReportService(
            AppConfig.BugReportApiUrl, AppConfig.BugReportApiKey, AppConfig.ApplicationName);
        _bugReportSink = new BugReportApiSink(_bugReportService);
        LoggingSetup.CreateLogger(_bugReportSink);

        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;

        Log.Information(
            "{ApplicationName} {Version} starting on {Os} ({Architecture})",
            AppConfig.ApplicationName,
            AppConfig.ApplicationVersionString,
            OsEnvironmentInfo.GetOsVersionName(),
            AppConfig.RuntimeIdentifier);

        try
        {
            var archiveService = new ArchiveService();
            var processor = new BatchArchiveProcessor(archiveService);
            _donationService = new DonationService(
                AppConfig.DonationApiUrl, AppConfig.DonationApiKey, AppConfig.DonationApplicationSlug);
            var donationStateStore = new DonationStateStore();
            var systemIntegration = new SystemIntegrationService();
            _updateService = new UpdateService(AppConfig.UpdateApiUrl);
            _statsService = new ApplicationStatsService(
                AppConfig.StatsApiUrl,
                AppConfig.StatsApiKey,
                AppConfig.StatsApplicationId,
                AppConfig.ApplicationVersionString);

            var viewModel = new MainWindowViewModel(
                processor, _donationService, donationStateStore, systemIntegration);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _desktopLifetime = desktop;
                desktop.MainWindow = new MainWindow(viewModel, _updateService, systemIntegration);
                desktop.Exit += OnDesktopExit;
            }

            _ = RecordStartupStatisticsAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            throw;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task RecordStartupStatisticsAsync()
    {
        if (_statsService is null) return;

        try
        {
            await _statsService.RecordStartupAsync();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Startup statistics call ended with an exception");
        }
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        ShutdownServices();
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Log.Fatal(exception, "Unhandled AppDomain exception");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }

    private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI-thread exception");
        e.Handled = true;
    }

    private void ShutdownServices()
    {
        Log.Information("{ApplicationName} shutting down", AppConfig.ApplicationName);

        AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        Dispatcher.UIThread.UnhandledException -= OnDispatcherUnhandledException;

        if (_desktopLifetime is not null)
        {
            _desktopLifetime.Exit -= OnDesktopExit;
            _desktopLifetime = null;
        }

        // Drain queued bug reports while the API client is still alive, then close the logger.
        _bugReportSink?.Dispose();
        Log.CloseAndFlush();

        _statsService?.Dispose();
        _updateService?.Dispose();
        _donationService?.Dispose();
        _bugReportService?.Dispose();
    }
}
