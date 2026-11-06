using BatchConvertToCompressedFile.Models;
using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class BatchArchiveProcessorTests
{
    private readonly BatchArchiveProcessor _processor = new(new ArchiveService());

    [Fact]
    public async Task CompressBatchAsync_CompressesEveryFile()
    {
        using var input = new TempDirectory();
        using var output = new TempDirectory();
        input.CreateFile("one.txt");
        input.CreateFile("two.bin");

        var progress = new List<BatchProgress>();
        var result = await _processor.CompressBatchAsync(
            new CompressionRequest(input.Path, output.Path, ArchiveFormat.Zip, DeleteOriginals: false),
            new Progress<BatchProgress>(progress.Add),
            CancellationToken.None);

        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.False(result.Canceled);
        Assert.True(File.Exists(Path.Combine(output.Path, "one.zip")));
        Assert.True(File.Exists(Path.Combine(output.Path, "two.zip")));
        Assert.True(File.Exists(Path.Combine(input.Path, "one.txt")));
    }

    [Fact]
    public async Task CompressBatchAsync_DeletesOriginalsAfterSuccess()
    {
        using var input = new TempDirectory();
        using var output = new TempDirectory();
        input.CreateFile("delete-me.txt");

        var result = await _processor.CompressBatchAsync(
            new CompressionRequest(input.Path, output.Path, ArchiveFormat.SevenZip, DeleteOriginals: true),
            progress: null,
            CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        Assert.False(File.Exists(Path.Combine(input.Path, "delete-me.txt")));
        Assert.True(File.Exists(Path.Combine(output.Path, "delete-me.7z")));
    }

    [Fact]
    public async Task CompressBatchAsync_SkipsExistingArchives()
    {
        using var input = new TempDirectory();
        using var output = new TempDirectory();
        input.CreateFile("existing.txt");
        await File.WriteAllTextAsync(Path.Combine(output.Path, "existing.zip"), "keep me");
        var progress = new RecordingProgress();

        var result = await _processor.CompressBatchAsync(
            new CompressionRequest(input.Path, output.Path, ArchiveFormat.Zip, DeleteOriginals: false),
            progress,
            CancellationToken.None);

        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal("keep me", await File.ReadAllTextAsync(Path.Combine(output.Path, "existing.zip")));
        Assert.Equal(ProcessStatus.Skipped, progress.Values[^1].ItemStatus);
        Assert.Equal(1, progress.Values[^1].SkippedCount);
    }

    [Fact]
    public async Task CompressBatchAsync_SameBaseName_ProducesUniqueArchives()
    {
        using var input = new TempDirectory();
        using var output = new TempDirectory();
        input.CreateFile("same.txt");
        input.CreateFile("same.md");

        var result = await _processor.CompressBatchAsync(
            new CompressionRequest(input.Path, output.Path, ArchiveFormat.Zip, DeleteOriginals: false),
            progress: null,
            CancellationToken.None);

        Assert.Equal(2, result.SuccessCount);
        Assert.True(File.Exists(Path.Combine(output.Path, "same.zip")));
        Assert.True(File.Exists(Path.Combine(output.Path, "same (2).zip")));
    }

    [Fact]
    public async Task CompressBatchAsync_DotFile_UsesVisibleArchiveName()
    {
        using var input = new TempDirectory();
        using var output = new TempDirectory();
        input.CreateFile(".gitignore");

        var result = await _processor.CompressBatchAsync(
            new CompressionRequest(input.Path, output.Path, ArchiveFormat.Zip, DeleteOriginals: false),
            progress: null,
            CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        Assert.True(File.Exists(Path.Combine(output.Path, "gitignore.zip")));
    }

    [Fact]
    public async Task CompressBatchAsync_EmptyFolder_ReturnsZeroTotals()
    {
        using var input = new TempDirectory();
        using var output = new TempDirectory();

        var result = await _processor.CompressBatchAsync(
            new CompressionRequest(input.Path, output.Path, ArchiveFormat.Zip, DeleteOriginals: false),
            progress: null,
            CancellationToken.None);

        Assert.Equal(0, result.TotalFiles);
        Assert.Equal(0, result.SuccessCount);
    }

    [Fact]
    public async Task CompressBatchAsync_MissingInputFolder_Throws()
    {
        using var output = new TempDirectory();

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _processor.CompressBatchAsync(
                new CompressionRequest(
                    Path.Combine(output.Path, "missing"), output.Path, ArchiveFormat.Zip, false),
                progress: null,
                CancellationToken.None));
    }

    [Fact]
    public async Task VerifyBatchAsync_CountsValidAndCorruptArchives()
    {
        using var input = new TempDirectory();
        var archiveService = new ArchiveService();
        var validSource = input.CreateFile("valid.txt", 10_000);
        var validArchive = Path.Combine(input.Path, "valid.zip");
        Assert.Equal(ProcessStatus.Success, await archiveService.CompressFileAsync(
            validSource, validArchive, ArchiveFormat.Zip, CancellationToken.None));

        var corruptArchive = input.CreateFile("corrupt.zip", 256);

        var result = await _processor.VerifyBatchAsync(
            new VerificationRequest(input.Path, false, false, null, false, null),
            progress: null,
            CancellationToken.None);

        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.True(File.Exists(validArchive));
        Assert.True(File.Exists(corruptArchive));
    }

    [Fact]
    public async Task VerifyBatchAsync_IgnoresUnsupportedExtensions()
    {
        using var input = new TempDirectory();
        input.CreateFile("notes.txt");
        input.CreateFile("image.png");

        var result = await _processor.VerifyBatchAsync(
            new VerificationRequest(input.Path, false, false, null, false, null),
            progress: null,
            CancellationToken.None);

        Assert.Equal(0, result.TotalFiles);
    }

    [Fact]
    public async Task VerifyBatchAsync_MovesSuccessfulArchivesAndPreservesSubfolders()
    {
        using var input = new TempDirectory();
        using var successFolder = new TempDirectory();
        var archiveService = new ArchiveService();

        var source = input.CreateFile("nested/deep/file.txt", 8_000);
        var archive = Path.Combine(input.Path, "nested", "deep", "file.zip");
        Assert.Equal(ProcessStatus.Success, await archiveService.CompressFileAsync(
            source, archive, ArchiveFormat.Zip, CancellationToken.None));
        File.Delete(source);

        var result = await _processor.VerifyBatchAsync(
            new VerificationRequest(input.Path, true, true, successFolder.Path, false, null),
            progress: null,
            CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        Assert.False(File.Exists(archive));
        Assert.True(File.Exists(Path.Combine(successFolder.Path, "nested", "deep", "file.zip")));
    }

    [Fact]
    public async Task VerifyBatchAsync_MovesFailedArchives()
    {
        using var input = new TempDirectory();
        using var failedFolder = new TempDirectory();
        var corrupt = input.CreateFile("corrupt.zip", 256);

        var result = await _processor.VerifyBatchAsync(
            new VerificationRequest(input.Path, false, false, null, true, failedFolder.Path),
            progress: null,
            CancellationToken.None);

        Assert.Equal(1, result.FailedCount);
        Assert.False(File.Exists(corrupt));
        Assert.True(File.Exists(Path.Combine(failedFolder.Path, "corrupt.zip")));
    }

    [Fact]
    public async Task VerifyBatchAsync_CanceledToken_ReturnsCanceledResult()
    {
        using var input = new TempDirectory();
        input.CreateFile("anything.zip");

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await _processor.VerifyBatchAsync(
            new VerificationRequest(input.Path, false, false, null, false, null),
            progress: null,
            cancellation.Token);

        Assert.True(result.Canceled);
    }

    private sealed class RecordingProgress : IProgress<BatchProgress>
    {
        public List<BatchProgress> Values { get; } = [];

        public void Report(BatchProgress value) => Values.Add(value);
    }
}
