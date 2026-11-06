using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Logging;
using BatchConvertToCompressedFile.Models;
using BatchConvertToCompressedFile.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BatchConvertToCompressedFile.ViewModels;

/// <summary>Backs the main window: batch settings, progress, statistics, and the donation banner.</summary>
internal sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private const int SpeedUpdateIntervalMs = 1000;

    private readonly IBatchArchiveProcessor _processor;
    private readonly IDonationService _donationService;
    private readonly IDonationStateStore _donationStateStore;
    private readonly ISystemIntegrationService _systemIntegration;
    private readonly SynchronizationContext? _synchronizationContext;
    private readonly Stopwatch _operationTimer = new();
    private readonly Stopwatch _speedTimer = new();
    private readonly Lock _speedLock = new();
    private readonly Timer _speedUpdateTimer;

    private CancellationTokenSource? _cancellation;
    private long _bytesSinceLastSpeedUpdate;
    private bool _disposed;

    /// <summary>Initializes a new main window view model.</summary>
    /// <param name="processor">Batch processor.</param>
    /// <param name="donationService">Donation verification service.</param>
    /// <param name="donationStateStore">Local donation state store.</param>
    /// <param name="systemIntegration">Shell integration service.</param>
    public MainWindowViewModel(
        IBatchArchiveProcessor processor,
        IDonationService donationService,
        IDonationStateStore donationStateStore,
        ISystemIntegrationService systemIntegration)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _donationService = donationService ?? throw new ArgumentNullException(nameof(donationService));
        _donationStateStore = donationStateStore ?? throw new ArgumentNullException(nameof(donationStateStore));
        _systemIntegration = systemIntegration ?? throw new ArgumentNullException(nameof(systemIntegration));
        _synchronizationContext = SynchronizationContext.Current;
        _speedUpdateTimer = new Timer(UpdateSpeedAndTime, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>Raised when a batch operation completes so the view can show a summary dialog.</summary>
    public event EventHandler<BatchCompletedEventArgs>? BatchCompleted;

    /// <summary>Raised when a validation or operation error should be shown to the user.</summary>
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>Gets the log lines shown in the main window.</summary>
    public ObservableCollection<string> LogEntries { get; } = [];

    /// <summary>Gets or sets the compression input folder.</summary>
    [ObservableProperty]
    private string _compressionInputFolder = string.Empty;

    /// <summary>Gets or sets the compression output folder.</summary>
    [ObservableProperty]
    private string _compressionOutputFolder = string.Empty;

    /// <summary>Gets or sets whether .7z output is selected.</summary>
    [ObservableProperty]
    private bool _isSevenZipFormat;

    /// <summary>Gets or sets whether .zip output is selected.</summary>
    [ObservableProperty]
    private bool _isZipFormat = true;

    partial void OnIsSevenZipFormatChanged(bool value)
    {
        if (value) IsZipFormat = false;
    }

    partial void OnIsZipFormatChanged(bool value)
    {
        if (value) IsSevenZipFormat = false;
    }

    /// <summary>Gets or sets whether original files are deleted after compression.</summary>
    [ObservableProperty]
    private bool _deleteOriginals;

    /// <summary>Gets or sets the verification input folder.</summary>
    [ObservableProperty]
    private string _verificationInputFolder = string.Empty;

    /// <summary>Gets or sets whether verification scans subfolders.</summary>
    [ObservableProperty]
    private bool _includeSubfolders = true;

    /// <summary>Gets or sets whether successful archives are moved.</summary>
    [ObservableProperty]
    private bool _moveSuccessFiles;

    /// <summary>Gets or sets the success folder.</summary>
    [ObservableProperty]
    private string _successFolder = string.Empty;

    /// <summary>Gets or sets whether failed archives are moved.</summary>
    [ObservableProperty]
    private bool _moveFailedFiles;

    /// <summary>Gets or sets the failed folder.</summary>
    [ObservableProperty]
    private string _failedFolder = string.Empty;

    /// <summary>Gets whether a batch operation is running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Gets or sets the status bar text.</summary>
    [ObservableProperty]
    private string _statusText = "Ready";

    /// <summary>Gets or sets the progress value.</summary>
    [ObservableProperty]
    private double _progressValue;

    /// <summary>Gets or sets the progress maximum.</summary>
    [ObservableProperty]
    private double _progressMaximum = 1;

    /// <summary>Gets or sets the progress text.</summary>
    [ObservableProperty]
    private string _progressText = string.Empty;

    /// <summary>Gets whether the progress controls are visible.</summary>
    [ObservableProperty]
    private bool _isProgressVisible;

    /// <summary>Gets whether the cancel button is visible.</summary>
    [ObservableProperty]
    private bool _isCancelVisible;

    /// <summary>Gets or sets the total file count statistic.</summary>
    [ObservableProperty]
    private int _totalFileCount;

    /// <summary>Gets or sets the successful file count statistic.</summary>
    [ObservableProperty]
    private int _successFileCount;

    /// <summary>Gets or sets the failed file count statistic.</summary>
    [ObservableProperty]
    private int _failedFileCount;

    /// <summary>Gets or sets the skipped file count statistic.</summary>
    [ObservableProperty]
    private int _skippedFileCount;

    /// <summary>Gets or sets the elapsed processing time text.</summary>
    [ObservableProperty]
    private string _processingTime = "00:00:00";

    /// <summary>Gets or sets the current write speed text.</summary>
    [ObservableProperty]
    private string _writeSpeed = "0.0 MB/s";

    /// <summary>Gets whether the donation banner is visible.</summary>
    [ObservableProperty]
    private bool _isDonationBannerVisible = true;

    /// <summary>Gets or sets the email entered for donation verification.</summary>
    [ObservableProperty]
    private string _donationEmail = string.Empty;

    /// <summary>Gets or sets the donation status message.</summary>
    [ObservableProperty]
    private string _donationMessage = string.Empty;

    /// <summary>Gets whether donation verification is in progress.</summary>
    [ObservableProperty]
    private bool _isVerifyingDonation;

    /// <summary>Loads persisted donation state and prepares the banner.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;

        try
        {
            var state = await _donationStateStore.LoadAsync(cancellationToken);
            IsDonationBannerVisible = !state.Donated;
            if (state.Donated)
            {
                Log.Information("Donation state loaded: donated");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not initialize the donation banner");
            IsDonationBannerVisible = true;
        }
    }

    /// <summary>Starts a batch compression operation.</summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    public async Task StartCompressionAsync()
    {
        try
        {
            if (_disposed || IsBusy) return;

            if (!TryValidateCompression(out var error))
            {
                ErrorOccurred?.Invoke(this, error);
                return;
            }

            var request = new CompressionRequest(
                CompressionInputFolder.Trim(),
                CompressionOutputFolder.Trim(),
                IsSevenZipFormat ? ArchiveFormat.SevenZip : ArchiveFormat.Zip,
                DeleteOriginals);

            await RunOperationAsync(OperationType.Compression, request);
        }
        catch (Exception ex)
        {
            ExceptionLogging.LogHandled(ex, "Could not start the compression operation");
            ErrorOccurred?.Invoke(this, $"Could not start compression: {ex.Message}");
        }
    }

    /// <summary>Starts a batch verification operation.</summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    public async Task StartVerificationAsync()
    {
        try
        {
            if (_disposed || IsBusy) return;

            if (!TryValidateVerification(out var error))
            {
                ErrorOccurred?.Invoke(this, error);
                return;
            }

            var request = new VerificationRequest(
                VerificationInputFolder.Trim(),
                IncludeSubfolders,
                MoveSuccessFiles,
                string.IsNullOrWhiteSpace(SuccessFolder) ? null : SuccessFolder.Trim(),
                MoveFailedFiles,
                string.IsNullOrWhiteSpace(FailedFolder) ? null : FailedFolder.Trim());

            await RunOperationAsync(OperationType.Verification, request);
        }
        catch (Exception ex)
        {
            ExceptionLogging.LogHandled(ex, "Could not start the verification operation");
            ErrorOccurred?.Invoke(this, $"Could not start verification: {ex.Message}");
        }
    }

    /// <summary>Requests cancellation of the running operation.</summary>
    public void Cancel()
    {
        if (_disposed || !IsBusy) return;

        _cancellation?.Cancel();
        StatusText = "Cancellation requested…";
        Log.Information("Cancellation requested by the user");
    }

    /// <summary>Verifies the donation email and hides the banner when the donation is confirmed.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task VerifyDonationAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || IsVerifyingDonation) return;

        var email = DonationEmail.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            DonationMessage = "Enter the email address used for the donation.";
            return;
        }

        IsVerifyingDonation = true;
        DonationMessage = "Verifying your donation…";

        try
        {
            var result = await _donationService.VerifyDonationAsync(email, cancellationToken);
            DonationMessage = result.Message;

            if (!result.Donated)
            {
                Log.Information("Donation verification did not find a donation");
                return;
            }

            await _donationStateStore.SaveAsync(
                new DonationState(true, null, DateTimeOffset.UtcNow), cancellationToken);
            IsDonationBannerVisible = false;
            Log.Information("Donation verified; donation banner hidden");
        }
        catch (OperationCanceledException)
        {
            Log.Information("Donation verification was canceled");
            DonationMessage = "Donation verification was canceled.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while verifying a donation");
            DonationMessage = "Could not verify the donation. Please try again.";
        }
        finally
        {
            IsVerifyingDonation = false;
        }
    }

    /// <summary>Opens the donation page in the default browser.</summary>
    public void OpenDonationPage()
    {
        if (_disposed) return;

        var url = _donationService.BuildDonationUrl(DonationEmail);
        if (!_systemIntegration.OpenUrl(url))
        {
            ErrorOccurred?.Invoke(this, $"Could not open the donation page: {url}");
        }
    }

    /// <summary>Adds a log line to the in-app viewer.</summary>
    /// <param name="line">Formatted log line.</param>
    public void AppendLog(string line)
    {
        if (_disposed) return;

        Post(() => AppendLogCore(line));
    }

    private void AppendLogCore(string line)
    {
        if (_disposed) return;

        LogEntries.Add(line);
        while (LogEntries.Count > 2000)
        {
            LogEntries.RemoveAt(0);
        }
    }

    private async Task RunOperationAsync<TRequest>(OperationType operationType, TRequest request)
    {
        IsBusy = true;
        IsProgressVisible = true;
        IsCancelVisible = true;
        ProgressValue = 0;
        ProgressMaximum = 1;
        ProgressText = string.Empty;
        StatusText = operationType == OperationType.Compression
            ? "Starting batch compression…"
            : "Starting batch verification…";
        ResetStatistics();
        _operationTimer.Restart();
        StartSpeedTracking();

        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();

        // Update the observable state synchronously on the UI thread. Progress<T> posts
        // asynchronously and can leave stale callbacks running after the batch completes.
        var progress = new SynchronousProgress<BatchProgress>(OnProgress);

        try
        {
            var result = request switch
            {
                CompressionRequest compression => await _processor.CompressBatchAsync(
                    compression, progress, _cancellation.Token),
                VerificationRequest verification => await _processor.VerifyBatchAsync(
                    verification, progress, _cancellation.Token),
                _ => throw new ArgumentOutOfRangeException(nameof(request))
            };

            // Let any progress posted from a background thread drain before applying final counts.
            await Task.Yield();

            TotalFileCount = result.TotalFiles;
            SuccessFileCount = result.SuccessCount;
            FailedFileCount = result.FailedCount;
            SkippedFileCount = result.SkippedCount;
            StatusText = result.Canceled ? "Operation canceled." : "Ready";
            BatchCompleted?.Invoke(this, new BatchCompletedEventArgs(operationType, result));
        }
        catch (OperationCanceledException)
        {
            StatusText = "Operation canceled.";
            Log.Information("{Operation} operation canceled", operationType);
        }
        catch (Exception ex)
        {
            StatusText = "Operation failed.";
            ExceptionLogging.LogHandled(ex, "{Operation} operation failed", operationType);
            ErrorOccurred?.Invoke(
                this, $"The {operationType.ToString().ToLowerInvariant()} operation failed: {ex.Message}");
        }
        finally
        {
            _operationTimer.Stop();
            StopSpeedTracking();
            IsBusy = false;
            IsProgressVisible = false;
            IsCancelVisible = false;
            ProgressText = string.Empty;
            UpdateProcessingTime();
        }
    }

    private void OnProgress(BatchProgress progress)
    {
        Post(() =>
        {
            ProgressMaximum = Math.Max(progress.TotalFiles, 1);
            ProgressValue = progress.CurrentFile;
            ProgressText = string.IsNullOrWhiteSpace(progress.Message)
                ? progress.CurrentFileName
                : progress.Message;
            TotalFileCount = progress.TotalFiles;
            SuccessFileCount = progress.SuccessCount;
            FailedFileCount = progress.FailedCount;
            SkippedFileCount = progress.SkippedCount;

            if (progress.CompletedBytes > 0)
            {
                AddBytesWritten(progress.CompletedBytes);
            }

            AppendLogCore($"[{DateTime.Now:HH:mm:ss.fff}] {progress.Message}");
        });
    }

    private void StartSpeedTracking()
    {
        lock (_speedLock)
        {
            _bytesSinceLastSpeedUpdate = 0;
            _speedTimer.Restart();
            _speedUpdateTimer.Change(SpeedUpdateIntervalMs, SpeedUpdateIntervalMs);
        }
    }

    private void StopSpeedTracking()
    {
        lock (_speedLock)
        {
            _speedUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _speedTimer.Stop();
            WriteSpeed = "0.0 MB/s";
        }
    }

    private void AddBytesWritten(long bytes)
    {
        lock (_speedLock)
        {
            _bytesSinceLastSpeedUpdate += bytes;
        }
    }

    private void UpdateSpeedAndTime(object? state)
    {
        double megabytesPerSecond;
        lock (_speedLock)
        {
            var elapsedSeconds = _speedTimer.Elapsed.TotalSeconds;
            var bytes = _bytesSinceLastSpeedUpdate;
            _bytesSinceLastSpeedUpdate = 0;
            _speedTimer.Restart();

            megabytesPerSecond = elapsedSeconds <= 0 ? 0 : bytes / elapsedSeconds / (1024.0 * 1024.0);
        }

        Post(() =>
        {
            WriteSpeed = string.Create(CultureInfo.InvariantCulture, $"{megabytesPerSecond:F1} MB/s");
            ProcessingTime = string.Create(
                CultureInfo.InvariantCulture, $"{_operationTimer.Elapsed:hh\\:mm\\:ss}");
        });
    }

    private void UpdateProcessingTime()
    {
        var elapsed = _operationTimer.Elapsed;
        Post(() => ProcessingTime = string.Create(CultureInfo.InvariantCulture, $"{elapsed:hh\\:mm\\:ss}"));
    }

    private void ResetStatistics()
    {
        TotalFileCount = 0;
        SuccessFileCount = 0;
        FailedFileCount = 0;
        SkippedFileCount = 0;
        ProcessingTime = "00:00:00";
        WriteSpeed = "0.0 MB/s";
    }

    private bool TryValidateCompression(out string error)
    {
        var inputFolder = CompressionInputFolder.Trim();
        var outputFolder = CompressionOutputFolder.Trim();

        if (string.IsNullOrWhiteSpace(inputFolder) || string.IsNullOrWhiteSpace(outputFolder))
        {
            error = "Please select both an input folder and an output folder for compression.";
            return false;
        }

        if (!Directory.Exists(inputFolder))
        {
            error = $"The input folder does not exist: {inputFolder}";
            return false;
        }

        if (File.Exists(outputFolder))
        {
            error = $"The output path is a file, not a folder: {outputFolder}";
            return false;
        }

        if (PathUtilities.AreSameFolder(inputFolder, outputFolder))
        {
            error = "Input and output folders must be different.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool TryValidateVerification(out string error)
    {
        var inputFolder = VerificationInputFolder.Trim();
        var successFolder = SuccessFolder.Trim();
        var failedFolder = FailedFolder.Trim();

        if (string.IsNullOrWhiteSpace(inputFolder))
        {
            error = "Please select the folder containing archives to verify.";
            return false;
        }

        if (!Directory.Exists(inputFolder))
        {
            error = $"The input folder does not exist: {inputFolder}";
            return false;
        }

        if (MoveSuccessFiles && string.IsNullOrWhiteSpace(successFolder))
        {
            error = "Select a success folder or disable the move-success option.";
            return false;
        }

        if (MoveSuccessFiles && File.Exists(successFolder))
        {
            error = $"The success path is a file, not a folder: {successFolder}";
            return false;
        }

        if (MoveFailedFiles && string.IsNullOrWhiteSpace(failedFolder))
        {
            error = "Select a failed folder or disable the move-failed option.";
            return false;
        }

        if (MoveFailedFiles && File.Exists(failedFolder))
        {
            error = $"The failed path is a file, not a folder: {failedFolder}";
            return false;
        }

        if (MoveSuccessFiles && MoveFailedFiles &&
            PathUtilities.AreSameFolder(successFolder, failedFolder))
        {
            error = "Success and failed folders must be different.";
            return false;
        }

        if (MoveSuccessFiles && PathUtilities.AreSameFolder(successFolder, inputFolder))
        {
            error = "The success folder must be different from the input folder.";
            return false;
        }

        if (MoveFailedFiles && PathUtilities.AreSameFolder(failedFolder, inputFolder))
        {
            error = "The failed folder must be different from the input folder.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void Post(Action action)
    {
        if (_disposed) return;

        if (_synchronizationContext is null || SynchronizationContext.Current == _synchronizationContext)
        {
            action();
            return;
        }

        _synchronizationContext.Post(static state => ((Action)state!)(), action);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _speedUpdateTimer.Dispose();
        _operationTimer.Stop();
        _speedTimer.Stop();
    }

    /// <summary>Invokes progress callbacks synchronously on the reporting thread.</summary>
    /// <typeparam name="T">Progress value type.</typeparam>
    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        private readonly Action<T> _handler =
            handler ?? throw new ArgumentNullException(nameof(handler));

        /// <inheritdoc />
        public void Report(T value) => _handler(value);
    }
}

/// <summary>Carries a completed batch result to the view.</summary>
internal sealed class BatchCompletedEventArgs : EventArgs
{
    /// <summary>Initializes a new event payload.</summary>
    /// <param name="operationType">Operation that completed.</param>
    /// <param name="result">Aggregated result.</param>
    public BatchCompletedEventArgs(OperationType operationType, BatchResult result)
    {
        OperationType = operationType;
        Result = result;
    }

    /// <summary>Gets the operation type.</summary>
    public OperationType OperationType { get; }

    /// <summary>Gets the aggregated result.</summary>
    public BatchResult Result { get; }
}
