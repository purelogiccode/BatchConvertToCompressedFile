using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using BatchConvertToCompressedFile.Interfaces;
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
    private int _successFiles;
    private int _failedFiles;
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

        VersionText = $"Version {AppConfig.ApplicationVersionString}";
    }

    /// <summary>Raised when a batch operation completes so the view can show a summary dialog.</summary>
    public event EventHandler<BatchCompletedEventArgs>? BatchCompleted;

    /// <summary>Raised when a validation or operation error should be shown to the user.</summary>
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>Gets the log lines shown in the main window.</summary>
    public ObservableCollection<string> LogEntries { get; } = [];

    /// <summary>Gets the version label shown in the status bar.</summary>
    public string VersionText { get; }

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
        try
        {
            var state = await _donationStateStore.LoadAsync(cancellationToken);
            IsDonationBannerVisible = !state.Donated;
            if (state.Donated)
            {
                Log.Information("Donation state loaded: donated ({Email})", state.Email);
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
            if (IsBusy) return;

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
            Log.Error(ex, "Could not start the compression operation");
            ErrorOccurred?.Invoke(this, $"Could not start compression: {ex.Message}");
        }
    }

    /// <summary>Starts a batch verification operation.</summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    public async Task StartVerificationAsync()
    {
        try
        {
            if (IsBusy) return;

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
            Log.Error(ex, "Could not start the verification operation");
            ErrorOccurred?.Invoke(this, $"Could not start verification: {ex.Message}");
        }
    }

    /// <summary>Requests cancellation of the running operation.</summary>
    public void Cancel()
    {
        if (!IsBusy) return;

        _cancellation?.Cancel();
        StatusText = "Cancellation requested…";
        Log.Information("Cancellation requested by the user");
    }

    /// <summary>Verifies the donation email and hides the banner when the donation is confirmed.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task VerifyDonationAsync(CancellationToken cancellationToken = default)
    {
        if (IsVerifyingDonation) return;

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
                Log.Information("Donation verification did not find a donation for {Email}", email);
                return;
            }

            await _donationStateStore.SaveAsync(
                new DonationState(true, email, DateTimeOffset.UtcNow), cancellationToken);
            IsDonationBannerVisible = false;
            Log.Information("Donation verified for {Email}; donation banner hidden", email);
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

        Post(() =>
        {
            LogEntries.Add(line);
            while (LogEntries.Count > 2000)
            {
                LogEntries.RemoveAt(0);
            }
        });
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

        var progress = new Progress<BatchProgress>(OnProgress);

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

            TotalFileCount = result.TotalFiles;
            SuccessFileCount = result.SuccessCount;
            FailedFileCount = result.FailedCount;
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
            Log.Error(ex, "{Operation} operation failed", operationType);
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
        ProgressMaximum = Math.Max(progress.TotalFiles, 1);
        ProgressValue = progress.CurrentFile;
        ProgressText = progress.Message;
        TotalFileCount = progress.TotalFiles;
        SuccessFileCount = _successFiles;
        FailedFileCount = _failedFiles;

        if (progress.CompletedBytes > 0)
        {
            AddBytesWritten(progress.CompletedBytes);
        }

        var message = progress.Message;
        if (message.StartsWith("Verification failed", StringComparison.OrdinalIgnoreCase) ||
            message.StartsWith("Could not compress", StringComparison.OrdinalIgnoreCase))
        {
            _failedFiles++;
        }
        else if (message.StartsWith("Verified", StringComparison.OrdinalIgnoreCase) ||
                 message.StartsWith("Compressed", StringComparison.OrdinalIgnoreCase))
        {
            _successFiles++;
        }

        SuccessFileCount = _successFiles;
        FailedFileCount = _failedFiles;
        AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
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
        _successFiles = 0;
        _failedFiles = 0;
        TotalFileCount = 0;
        SuccessFileCount = 0;
        FailedFileCount = 0;
        ProcessingTime = "00:00:00";
        WriteSpeed = "0.0 MB/s";
    }

    private bool TryValidateCompression(out string error)
    {
        if (string.IsNullOrWhiteSpace(CompressionInputFolder) ||
            string.IsNullOrWhiteSpace(CompressionOutputFolder))
        {
            error = "Please select both an input folder and an output folder for compression.";
            return false;
        }

        if (!Directory.Exists(CompressionInputFolder))
        {
            error = $"The input folder does not exist: {CompressionInputFolder}";
            return false;
        }

        if (PathUtilities.AreSameFolder(CompressionInputFolder, CompressionOutputFolder))
        {
            error = "Input and output folders must be different.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool TryValidateVerification(out string error)
    {
        if (string.IsNullOrWhiteSpace(VerificationInputFolder))
        {
            error = "Please select the folder containing archives to verify.";
            return false;
        }

        if (!Directory.Exists(VerificationInputFolder))
        {
            error = $"The input folder does not exist: {VerificationInputFolder}";
            return false;
        }

        if (MoveSuccessFiles && string.IsNullOrWhiteSpace(SuccessFolder))
        {
            error = "Select a success folder or disable the move-success option.";
            return false;
        }

        if (MoveFailedFiles && string.IsNullOrWhiteSpace(FailedFolder))
        {
            error = "Select a failed folder or disable the move-failed option.";
            return false;
        }

        if (MoveSuccessFiles && MoveFailedFiles &&
            PathUtilities.AreSameFolder(SuccessFolder, FailedFolder))
        {
            error = "Success and failed folders must be different.";
            return false;
        }

        if (MoveSuccessFiles && PathUtilities.AreSameFolder(SuccessFolder, VerificationInputFolder))
        {
            error = "The success folder must be different from the input folder.";
            return false;
        }

        if (MoveFailedFiles && PathUtilities.AreSameFolder(FailedFolder, VerificationInputFolder))
        {
            error = "The failed folder must be different from the input folder.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void Post(Action action)
    {
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
        GC.SuppressFinalize(this);
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
