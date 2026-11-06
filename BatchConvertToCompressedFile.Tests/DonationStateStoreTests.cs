using BatchConvertToCompressedFile.Models;
using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class DonationStateStoreTests
{
    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsNotDonated()
    {
        using var temp = new TempDirectory();
        var store = new DonationStateStore(Path.Combine(temp.Path, "donation.json"));

        var state = await store.LoadAsync();

        Assert.False(state.Donated);
        Assert.Null(state.Email);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsState()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "nested", "donation.json");
        var store = new DonationStateStore(path);
        var state = new DonationState(true, "donor@example.com", DateTimeOffset.UtcNow);

        await store.SaveAsync(state);
        var loaded = await store.LoadAsync();

        Assert.True(File.Exists(path));
        Assert.True(loaded.Donated);
        Assert.Equal(state.Email, loaded.Email);
        Assert.NotNull(loaded.VerifiedAtUtc);
    }

    [Fact]
    public async Task LoadAsync_CorruptFile_ReturnsNotDonated()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "donation.json");
        await File.WriteAllTextAsync(path, "{not json");
        var store = new DonationStateStore(path);

        var state = await store.LoadAsync();

        Assert.False(state.Donated);
    }
}
