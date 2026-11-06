using System.Collections.Specialized;
using System.Diagnostics;
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
    private bool _opened;

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
            await _viewModel.InitializeAsync();
            await CheckForUpdatesAsync(manualCheck: false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during main window initialization");
        }
    }

    private async void OnBrowseCompressionInputClickAsync(object? sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync("Select the folder containing files to compress");
        if (!string.IsNullOrWhiteSpace(folder)) _viewModel.CompressionInputFolder = folder;
    }

    private async void OnBrowseCompressionOutputClickAsync(object? sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync("Select the output folder for compressed archives");
        if (!string.IsNullOrWhiteSpace(folder)) _viewModel.CompressionOutputFolder = folder;
    }

    private async void OnBrowseVerificationInputClickAsync(object? sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync("Select the folder containing archives to verify");
        if (!string.IsNullOrWhiteSpace(folder)) _viewModel.VerificationInputFolder = folder;
    }

    private async void OnBrowseSuccessFolderClickAsync(object? sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync("Select the folder for successfully verified archives");
        if (!string.IsNullOrWhiteSpace(folder)) _viewModel.SuccessFolder = folder;
    }

    private async void OnBrowseFailedFolderClickAsync(object? sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync("Select the folder for failed archives");
        if (!string.IsNullOrWhiteSpace(folder)) _viewModel.FailedFolder = folder;
    }

    private async void OnStartCompressionClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.StartCompressionAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled error while starting compression");
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
            Log.Error(ex, "Unhandled error while starting verification");
            await ShowErrorAsync("The verification operation could not be started.", ex);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => _viewModel.Cancel();

    private async void OnVerifyDonationClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.VerifyDonationAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled error while verifying a donation");
            await ShowErrorAsync("Could not verify the donation.", ex);
        }
    }

    private void OnDonateClick(object? sender, RoutedEventArgs e) => _viewModel.OpenDonationPage();

    private void OnOpenRepositoryClick(object? sender, RoutedEventArgs e)
    {
        if (!_systemIntegration.OpenUrl(AppConfig.RepositoryUrl))
        {
            _ = ShowErrorAsync($"Could not open {AppConfig.RepositoryUrl}.", null);
        }
    }

    private void OnOpenLogFolderClick(object? sender, RoutedEventArgs e)
    {
        var folder = PathUtilities.GetLogFolder();
        Directory.CreateDirectory(folder);
        if (!_systemIntegration.OpenFolder(folder))
        {
            _ = ShowErrorAsync($"Could not open the log folder: {folder}", null);
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
            Log.Error(ex, "Could not open the About window");
            await ShowErrorAsync("Could not open the About window.", ex);
        }
    }

    private async void OnCheckForUpdatesClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await CheckForUpdatesAsync(manualCheck: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Manual update check failed");
            await ShowErrorAsync("The update check failed.", ex);
        }
    }

    private async Task CheckForUpdatesAsync(bool manualCheck)
    {
        var result = await _updateService.CheckForUpdateAsync();

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
        Dispatcher.UIThread.Post(() => _ = ShowErrorAsync(message, null));
    }

    private void OnViewModelBatchCompleted(object? sender, BatchCompletedEventArgs e)
    {
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

        Dispatcher.UIThread.Post(() => _ = MessageDialog.ShowAsync(
            this,
            title,
            summary,
            MessageDialogButtons.Ok,
            result.FailedCount > 0 ? MessageDialogIcon.Warning : MessageDialogIcon.Information));
    }

    private void OnLogMessageLogged(string message)
    {
        Dispatcher.UIThread.Post(() => _viewModel.AppendLog(message));
    }

    private void OnLogEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add) return;

        Dispatcher.UIThread.Post(() =>
        {
            var scrollViewer = LogList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            scrollViewer?.ScrollToEnd();
        });
    }

    private Task ShowErrorAsync(string message, Exception? exception)
    {
        var details = exception is null ? message : $"{message}\n\n{exception.Message}";
        return MessageDialog.ShowAsync(
            this,
            "Something went wrong",
            details,
            MessageDialogButtons.Ok,
            MessageDialogIcon.Error);
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        _viewModel.ErrorOccurred -= OnViewModelErrorOccurred;
        _viewModel.BatchCompleted -= OnViewModelBatchCompleted;
        _viewModel.LogEntries.CollectionChanged -= OnLogEntriesCollectionChanged;
        UiLogSink.MessageLogged -= OnLogMessageLogged;

        _viewModel.Cancel();
        _viewModel.Dispose();
    }
}
