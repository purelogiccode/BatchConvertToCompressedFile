using BatchConvertToCompressedFile.Models;
using BatchConvertToCompressedFile.Services;
using SharpCompress.Archives;

namespace BatchConvertToCompressedFile.Tests;

public sealed class ArchiveServiceTests
{
    private readonly ArchiveService _service = new();

    [Theory]
    [InlineData(ArchiveFormat.Zip, ".zip")]
    [InlineData(ArchiveFormat.SevenZip, ".7z")]
    public async Task CompressFileAsync_CreatesReadableArchive(ArchiveFormat format, string extension)
    {
        using var temp = new TempDirectory();
        var input = temp.CreateFile("source.txt", 20_000);
        var output = Path.Combine(temp.Path, "output", "source" + extension);

        var status = await _service.CompressFileAsync(input, output, format, CancellationToken.None);

        Assert.Equal(ProcessStatus.Success, status);
        Assert.True(File.Exists(output));
        Assert.True(new FileInfo(output).Length > 0);
        Assert.True(await _service.VerifyArchiveAsync(output, CancellationToken.None));

        using var archive = ArchiveFactory.OpenArchive(output);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal("source.txt", entry.Key);
        Assert.Equal(new FileInfo(input).Length, entry.Size);
    }

    [Fact]
    public async Task CompressFileAsync_MissingSource_ReturnsFailed()
    {
        using var temp = new TempDirectory();
        var output = Path.Combine(temp.Path, "missing.zip");

        var status = await _service.CompressFileAsync(
            Path.Combine(temp.Path, "missing.txt"), output, ArchiveFormat.Zip, CancellationToken.None);

        Assert.Equal(ProcessStatus.Failed, status);
        Assert.False(File.Exists(output));
    }

    [Fact]
    public async Task VerifyArchiveAsync_CorruptZip_ReturnsFalse()
    {
        using var temp = new TempDirectory();
        var input = temp.CreateFile("source.txt", 40_000);
        var output = Path.Combine(temp.Path, "source.zip");

        Assert.Equal(
            ProcessStatus.Success,
            await _service.CompressFileAsync(input, output, ArchiveFormat.Zip, CancellationToken.None));

        var bytes = await File.ReadAllBytesAsync(output);
        for (var index = bytes.Length / 3; index < bytes.Length / 3 + 30; index++)
        {
            bytes[index] ^= 0xFF;
        }

        await File.WriteAllBytesAsync(output, bytes);

        Assert.False(await _service.VerifyArchiveAsync(output, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyArchiveAsync_NotAnArchive_ReturnsFalse()
    {
        using var temp = new TempDirectory();
        var path = temp.CreateFile("not-an-archive.7z", 512);

        Assert.False(await _service.VerifyArchiveAsync(path, CancellationToken.None));
    }

    [Fact]
    public async Task CompressFileAsync_HonorsCancellation()
    {
        using var temp = new TempDirectory();
        var input = temp.CreateFile("source.txt", 10_000);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _service.CompressFileAsync(
                input, Path.Combine(temp.Path, "source.zip"), ArchiveFormat.Zip, cancellation.Token));
    }
}
