using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Logging;
using BatchConvertToCompressedFile.Models;
using BatchConvertToCompressedFile.Services;
using BatchConvertToCompressedFile.ViewModels;

namespace BatchConvertToCompressedFile.Views;

/// <summary>Main application window.</summary>
internal partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly IUpdateService _updateService;
    private readonly ISystemIntegrationService _systemIntegration;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private bool _opened;
    private bool _isClosing;
    private bool _scrollPending;

    /// <summary>Initializes a new main window.</summary>
    /// <param name="viewModel">Window view model.</param>
    /// <param name="updateService">GitHub update service.</param>
    /// <param name="systemIntegration">Shell integration service.</param>
    public MainWindow(
        MainWindowViewModel viewModel,
        IUpdateService updateService,
        ISystemIntegrationService systemIntegration)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _systemIntegration = systemIntegration ?? throw new ArgumentNullException(nameof(systemIntegration));

        InitializeComponent();
        Title = AppConfig.DisplayName;
        VersionTextBlock.Text = $"Version {AppConfig.ApplicationVersionString}";
        DataContext = _viewModel;

        _viewModel.ErrorOccurred += OnViewModelErrorOccurred;
        _viewModel.BatchCompleted += OnViewModelBatchCompleted;
        _viewModel.LogEntries.CollectionChanged += OnLogEntriesCollectionChanged;
        UiLogSink.MessageLogged += OnLogMessageLogged;
    }

    private async void OnWindowOpenedAsync(object? sender, EventArgs e)
    {
        if (_opened) return;
        _opened = true;

        try
        {
            await _viewModel.InitializeAsync(_lifetimeCts.Token);
            await CheckForUpdatesAsync(manualCheck: false, _lifetimeCts.Token);
        }
        catch (OperationCanceledException) when (_lifetimeCts.IsCancellationRequested)
        {
            // The window was closed while initialization or the update check was running.
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during main window initialization");
        }
    }

    private async void OnBrowseCompressionInputClickAsync(object? sender, RoutedEventArgs e) =>
        await BrowseFolderAsync(
            "Select the folder containing files to compress",
            folder => _viewModel.CompressionInputFolder = folder);

    private async void OnBrowseCompressionOutputClickAsync(object? sender, RoutedEventArgs e) =>
        await BrowseFolderAsync(
            "Select the output folder for compressed archives",
            folder => _viewModel.CompressionOutputFolder = folder);

    private async void OnBrowseVerificationInputClickAsync(object? sender, RoutedEventArgs e) =>
        await BrowseFolderAsync(
            "Select the folder containing archives to verify",
            folder => _viewModel.VerificationInputFolder = folder);

    private async void OnBrowseSuccessFolderClickAsync(object? sender, RoutedEventArgs e) =>
        await BrowseFolderAsync(
            "Select the folder for successfully verified archives",
            folder => _viewModel.SuccessFolder = folder);

    private async void OnBrowseFailedFolderClickAsync(object? sender, RoutedEventArgs e) =>
        await BrowseFolderAsync(
            "Select the folder for failed archives",
            folder => _viewModel.FailedFolder = folder);

    private async void OnStartCompressionClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.StartCompressionAsync();
        }
        catch (Exception ex)
        {
            ExceptionLogging.LogHandled(ex, "Unhandled error while starting compression");
            await ShowErrorAsync("The compression operation could not be started.", ex);
        }
    }

    private async void OnStartVerificationClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.StartVerificationAsync();
        }
        catch (Exception ex)
        {
            ExceptionLogging.LogHandled(ex, "Unhandled error while starting verification");
            await ShowErrorAsync("The verification operation could not be started.", ex);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => _viewModel.Cancel();

    private async void OnVerifyDonationClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.VerifyDonationAsync(_lifetimeCts.Token);
        }
        catch (OperationCanceledException) when (_lifetimeCts.IsCancellationRequested)
        {
            // The window was closed while the donation check was running.
        }
        catch (Exception ex)
        {
            ExceptionLogging.LogHandled(ex, "Unhandled error while verifying a donation");
            await ShowErrorAsync("Could not verify the donation.", ex);
        }
    }

    private void OnDonateClick(object? sender, RoutedEventArgs e) => _viewModel.OpenDonationPage();

    private async void OnOpenRepositoryClickAsync(object? sender, RoutedEventArgs e)
    {
        if (_isClosing) return;

        try
        {
            if (!_systemIntegration.OpenUrl(AppConfig.RepositoryUrl))
            {
                await ShowErrorAsync($"Could not open {AppConfig.RepositoryUrl}.", null);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not open the repository URL");
        }
    }

    private async void OnOpenLogFolderClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folder = PathUtilities.GetLogFolder();
            Directory.CreateDirectory(folder);

            if (!_systemIntegration.OpenFolder(folder))
            {
                await ShowErrorAsync($"Could not open the log folder: {folder}", null);
            }
        }
        catch (Exception ex)
        {
            ExceptionLogging.LogHandled(ex, "Could not open the log folder");
            await ShowErrorAsync("Could not open the log folder.", ex);
        }
    }

    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();

    private async void OnAboutClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            var aboutWindow = new AboutWindow(_systemIntegration);
            await aboutWindow.ShowDialog(this);
        }
        catch (Exception ex)
        {
            ExceptionLogging.LogHandled(ex, "Could not open the About window");
            await ShowErrorAsync("Could not open the About window.", ex);
        }
    }

    private async void OnCheckForUpdatesClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await CheckForUpdatesAsync(manualCheck: true, _lifetimeCts.Token);
        }
        catch (OperationCanceledException) when (_lifetimeCts.IsCancellationRequested)
        {
            // The window was closed while the manual update check was running.
        }
        catch (Exception ex)
        {
            ExceptionLogging.LogHandled(ex, "Manual update check failed");
            await ShowErrorAsync("The update check failed.", ex);
        }
    }

    private async Task CheckForUpdatesAsync(bool manualCheck, CancellationToken cancellationToken)
    {
        var result = await _updateService.CheckForUpdateAsync(cancellationToken);
        if (_isClosing) return;

        if (!result.IsNewVersionAvailable)
        {
            if (manualCheck)
            {
                await MessageDialog.ShowAsync(
                    this,
                    "No updates found",
                    $"You are using the latest version ({AppConfig.ApplicationVersionString}).",
                    MessageDialogButtons.Ok,
                    MessageDialogIcon.Information);
            }

            return;
        }

        var targetUrl = result.DownloadUrl ?? result.ReleaseUrl ?? AppConfig.ReleasesUrl;
        var goToDownload = await MessageDialog.ShowAsync(
            this,
            "Update available",
            $"A new version ({result.LatestVersion}) is available.\n\n" +
            $"Current version: {AppConfig.ApplicationVersionString}\n" +
            $"Detected platform: {AppConfig.RuntimeIdentifier}\n\n" +
            "Would you like to open the download page?",
            MessageDialogButtons.YesNo,
            MessageDialogIcon.Information);

        if (goToDownload && !_systemIntegration.OpenUrl(targetUrl))
        {
            await ShowErrorAsync($"Could not open the update page: {targetUrl}", null);
        }
    }

    private async Task BrowseFolderAsync(string title, Action<string> applyFolder)
    {
        try
        {
            var folder = await PickFolderAsync(title);
            if (!string.IsNullOrWhiteSpace(folder) && !_isClosing)
            {
                applyFolder(folder);
            }
        }
        catch (Exception ex)
        {
            ExceptionLogging.LogHandled(ex, "Could not open the folder picker");
            await ShowErrorAsync("Could not open the folder picker.", ex);
        }
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }

    private void OnViewModelErrorOccurred(object? sender, string message)
    {
        if (_isClosing) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (_isClosing) return;
            ObserveFault(ShowErrorAsync(message, null), "Could not show the error dialog");
        });
    }

    private void OnViewModelBatchCompleted(object? sender, BatchCompletedEventArgs e)
    {
        if (_isClosing) return;

        var result = e.Result;
        var title = e.OperationType == OperationType.Compression
            ? "Compression complete"
            : "Verification complete";
        var summary =
            $"Total files: {result.TotalFiles}\n" +
            $"Successful: {result.SuccessCount}\n" +
            $"Failed: {result.FailedCount}\n" +
            $"Skipped: {result.SkippedCount}\n" +
            $"Elapsed: {result.Elapsed:hh\\:mm\\:ss}" +
            (result.Canceled ? "\n\nThe operation was canceled." : string.Empty);

        Dispatcher.UIThread.Post(() =>
        {
            if (_isClosing) return;

            ObserveFault(
                MessageDialog.ShowAsync(
                    this,
                    title,
                    summary,
                    MessageDialogButtons.Ok,
                    result.FailedCount > 0 ? MessageDialogIcon.Warning : MessageDialogIcon.Information),
                "Could not show the batch summary dialog");
        });
    }

    private void OnLogMessageLogged(string message)
    {
        if (_isClosing) return;

        Dispatcher.UIThread.Post(() => _viewModel.AppendLog(message));
    }

    private void OnLogEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isClosing || _scrollPending || e.Action != NotifyCollectionChangedAction.Add) return;

        _scrollPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _scrollPending = false;
            var scrollViewer = LogList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            scrollViewer?.ScrollToEnd();
        });
    }

    private Task ShowErrorAsync(string message, Exception? exception)
    {
        if (_isClosing || !IsVisible) return Task.CompletedTask;

        var details = exception is null ? message : $"{message}\n\n{exception.Message}";
        return MessageDialog.ShowAsync(
            this,
            "Something went wrong",
            details,
            MessageDialogButtons.Ok,
            MessageDialogIcon.Error);
    }

    private static void ObserveFault(Task task, string message)
    {
        _ = task.ContinueWith(
            faulted => Log.Debug(faulted.Exception, "{Message}", message),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;

        _lifetimeCts.Cancel();

        _viewModel.ErrorOccurred -= OnViewModelErrorOccurred;
        _viewModel.BatchCompleted -= OnViewModelBatchCompleted;
        _viewModel.LogEntries.CollectionChanged -= OnLogEntriesCollectionChanged;
        UiLogSink.MessageLogged -= OnLogMessageLogged;

        _viewModel.Cancel();
        _viewModel.Dispose();
    }
}
