using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Models;
using BatchConvertToCompressedFile.Services;
using BatchConvertToCompressedFile.ViewModels;

namespace BatchConvertToCompressedFile.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task InitializeAsync_WhenDonated_HidesBanner()
    {
        using var viewModel = CreateViewModel(new FakeDonationStateStore(
            new DonationState(true, "donor@example.com", DateTimeOffset.UtcNow)));

        await viewModel.InitializeAsync();

        Assert.False(viewModel.IsDonationBannerVisible);
    }

    [Fact]
    public async Task InitializeAsync_WhenNotDonated_ShowsBanner()
    {
        using var viewModel = CreateViewModel(new FakeDonationStateStore(DonationState.NotDonated));

        await viewModel.InitializeAsync();

        Assert.True(viewModel.IsDonationBannerVisible);
    }

    [Fact]
    public async Task VerifyDonationAsync_WhenDonated_SavesStateAndHidesBanner()
    {
        var store = new FakeDonationStateStore(DonationState.NotDonated);
        var donation = new FakeDonationService(new DonationVerificationResult(
            true,
            new DonationVerificationResponse(true, "slug", 10m, DateTime.UtcNow),
            "Thank you!"));
        using var viewModel = CreateViewModel(store, donation);
        viewModel.DonationEmail = "donor@example.com";

        await viewModel.VerifyDonationAsync();

        Assert.False(viewModel.IsDonationBannerVisible);
        Assert.NotNull(store.SavedState);
        Assert.True(store.SavedState!.Donated);
        Assert.Equal("donor@example.com", store.SavedState.Email);
    }

    [Fact]
    public async Task VerifyDonationAsync_WhenNotDonated_KeepsBannerVisible()
    {
        var store = new FakeDonationStateStore(DonationState.NotDonated);
        var donation = new FakeDonationService(new DonationVerificationResult(
            true,
            new DonationVerificationResponse(false, "slug", 0m, null),
            "No donation found."));
        using var viewModel = CreateViewModel(store, donation);
        viewModel.DonationEmail = "donor@example.com";

        await viewModel.VerifyDonationAsync();

        Assert.True(viewModel.IsDonationBannerVisible);
        Assert.Null(store.SavedState);
        Assert.Equal("No donation found.", viewModel.DonationMessage);
    }

    [Fact]
    public async Task VerifyDonationAsync_EmptyEmail_DoesNotCallService()
    {
        var donation = new FakeDonationService(new DonationVerificationResult(false, null, "unused"));
        using var viewModel = CreateViewModel(new FakeDonationStateStore(DonationState.NotDonated), donation);

        await viewModel.VerifyDonationAsync();

        Assert.Equal(0, donation.CallCount);
        Assert.True(viewModel.IsDonationBannerVisible);
        Assert.Contains("Enter the email", viewModel.DonationMessage);
    }

    [Fact]
    public async Task StartCompressionAsync_WithoutFolders_RaisesValidationError()
    {
        using var viewModel = CreateViewModel(new FakeDonationStateStore(DonationState.NotDonated));
        string? error = null;
        viewModel.ErrorOccurred += (_, message) => error = message;

        await viewModel.StartCompressionAsync();

        Assert.NotNull(error);
        Assert.Contains("input folder", error, StringComparison.OrdinalIgnoreCase);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task StartCompressionAsync_WithValidFolders_RunsProcessorAndReportsResult()
    {
        using var input = new TempDirectory();
        using var output = new TempDirectory();
        input.CreateFile("one.txt");
        var result = new BatchResult(1, 1, 0, 0, TimeSpan.FromSeconds(2), false);
        var processor = new FakeProcessor(result);
        using var viewModel = CreateViewModel(
            new FakeDonationStateStore(DonationState.NotDonated), processor: processor);
        viewModel.CompressionInputFolder = input.Path;
        viewModel.CompressionOutputFolder = output.Path;

        BatchCompletedEventArgs? completed = null;
        viewModel.BatchCompleted += (_, args) => completed = args;

        await viewModel.StartCompressionAsync();

        Assert.Equal(1, processor.CompressCalls);
        Assert.NotNull(completed);
        Assert.Equal(OperationType.Compression, completed!.OperationType);
        Assert.Equal(1, viewModel.SuccessFileCount);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task StartVerificationAsync_SameInputAndSuccessFolder_RaisesValidationError()
    {
        using var input = new TempDirectory();
        using var viewModel = CreateViewModel(new FakeDonationStateStore(DonationState.NotDonated));
        viewModel.VerificationInputFolder = input.Path;
        viewModel.MoveSuccessFiles = true;
        viewModel.SuccessFolder = input.Path;
        string? error = null;
        viewModel.ErrorOccurred += (_, message) => error = message;

        await viewModel.StartVerificationAsync();

        Assert.NotNull(error);
        Assert.Contains("different from the input folder", error);
    }

    [Fact]
    public void OpenDonationPage_OpensGeneratedUrl()
    {
        var system = new FakeSystemIntegration();
        using var viewModel = CreateViewModel(
            new FakeDonationStateStore(DonationState.NotDonated), systemIntegration: system);
        viewModel.DonationEmail = "donor@example.com";

        viewModel.OpenDonationPage();

        Assert.Single(system.OpenedUrls);
        Assert.Contains("donor%40example.com", system.OpenedUrls[0]);
    }

    private static MainWindowViewModel CreateViewModel(
        IDonationStateStore store,
        IDonationService? donation = null,
        IBatchArchiveProcessor? processor = null,
        ISystemIntegrationService? systemIntegration = null)
    {
        return new MainWindowViewModel(
            processor ?? new FakeProcessor(new BatchResult(0, 0, 0, 0, TimeSpan.Zero, false)),
            donation ?? new FakeDonationService(
                new DonationVerificationResult(false, null, "not used")),
            store,
            systemIntegration ?? new FakeSystemIntegration());
    }

    private sealed class FakeDonationStateStore(DonationState state) : IDonationStateStore
    {
        public DonationState? SavedState { get; private set; }

        public Task<DonationState> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(state);

        public Task SaveAsync(DonationState donationState, CancellationToken cancellationToken = default)
        {
            SavedState = donationState;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDonationService(DonationVerificationResult result) : IDonationService
    {
        public int CallCount { get; private set; }

        public Task<DonationVerificationResult> VerifyDonationAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(result);
        }

        public string BuildDonationUrl(string? email) =>
            $"https://example.test/donate?email={Uri.EscapeDataString(email ?? string.Empty)}";
    }

    private sealed class FakeProcessor(BatchResult result) : IBatchArchiveProcessor
    {
        public int CompressCalls { get; private set; }

        public int VerifyCalls { get; private set; }

        public Task<BatchResult> CompressBatchAsync(
            CompressionRequest request,
            IProgress<BatchProgress>? progress,
            CancellationToken cancellationToken)
        {
            CompressCalls++;
            return Task.FromResult(result);
        }

        public Task<BatchResult> VerifyBatchAsync(
            VerificationRequest request,
            IProgress<BatchProgress>? progress,
            CancellationToken cancellationToken)
        {
            VerifyCalls++;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeSystemIntegration : ISystemIntegrationService
    {
        public List<string> OpenedUrls { get; } = [];

        public List<string> OpenedFolders { get; } = [];

        public bool OpenUrl(string url)
        {
            OpenedUrls.Add(url);
            return true;
        }

        public bool OpenFolder(string folderPath)
        {
            OpenedFolders.Add(folderPath);
            return true;
        }
    }
}
