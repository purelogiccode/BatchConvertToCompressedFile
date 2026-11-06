using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class ArchiveFileMoverTests
{
    [Fact]
    public async Task MoveAsync_PreservesRelativeSubfolders()
    {
        using var input = new TempDirectory();
        using var destination = new TempDirectory();
        var source = input.CreateFile(Path.Combine("a", "b", "file.zip"));

        var moved = await ArchiveFileMover.MoveAsync(
            source, destination.Path, input.Path, preserveSubfolders: true, CancellationToken.None);

        Assert.True(moved);
        Assert.False(File.Exists(source));
        Assert.True(File.Exists(Path.Combine(destination.Path, "a", "b", "file.zip")));
    }

    [Fact]
    public async Task MoveAsync_ExistingDestination_DoesNotOverwrite()
    {
        using var input = new TempDirectory();
        using var destination = new TempDirectory();
        var source = input.CreateFile("file.zip");
        var existing = Path.Combine(destination.Path, "file.zip");
        await File.WriteAllTextAsync(existing, "existing content");

        var moved = await ArchiveFileMover.MoveAsync(
            source, destination.Path, input.Path, preserveSubfolders: false, CancellationToken.None);

        Assert.False(moved);
        Assert.True(File.Exists(source));
        Assert.Equal("existing content", await File.ReadAllTextAsync(existing));
    }

    [Fact]
    public async Task MoveAsync_HonorsCancellation()
    {
        using var input = new TempDirectory();
        using var destination = new TempDirectory();
        var source = input.CreateFile("file.zip");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ArchiveFileMover.MoveAsync(
                source, destination.Path, input.Path, preserveSubfolders: false, cancellation.Token));
    }

    [Fact]
    public async Task MoveAsync_OutsideInputRoot_DoesNotEscapeDestination()
    {
        using var input = new TempDirectory();
        using var destination = new TempDirectory();
        using var outside = new TempDirectory();
        var source = outside.CreateFile("file.zip");

        var moved = await ArchiveFileMover.MoveAsync(
            source, destination.Path, input.Path, preserveSubfolders: true, CancellationToken.None);

        Assert.False(moved);
        Assert.True(File.Exists(source));
        Assert.False(File.Exists(Path.Combine(destination.Path, "file.zip")));
    }
}
