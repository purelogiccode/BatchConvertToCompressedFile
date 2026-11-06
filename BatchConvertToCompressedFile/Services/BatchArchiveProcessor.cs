using System.Diagnostics;
using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Models;

namespace BatchConvertToCompressedFile.Services;

/// <summary>Runs batch compression and verification workflows against the file system.</summary>
internal sealed class BatchArchiveProcessor : IBatchArchiveProcessor
{
    private static readonly HashSet<string> SupportedVerificationExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".7z", ".zip", ".rar" };

    private readonly IArchiveService _archiveService;

    /// <summary>Initializes a new processor.</summary>
    /// <param name="archiveService">Archive creation and verification service.</param>
    public BatchArchiveProcessor(IArchiveService archiveService)
    {
        _archiveService = archiveService ?? throw new ArgumentNullException(nameof(archiveService));
    }

    /// <inheritdoc />
    public async Task<BatchResult> CompressBatchAsync(
        CompressionRequest request,
        IProgress<BatchProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        var success = 0;
        var failed = 0;
        var skipped = 0;
        var total = 0;
        var canceled = false;

        try
        {
            ValidateInputFolder(request.InputFolder);
            Directory.CreateDirectory(request.OutputFolder);

            var files = await Task.Run(
                () => Directory.GetFiles(request.InputFolder, "*", SearchOption.TopDirectoryOnly),
                cancellationToken);

            total = files.Length;
            Log.Information(
                "Compression batch started: {FileCount} files, format {Format}, output {OutputFolder}",
                total, request.Format.ToDisplayName(), request.OutputFolder);

            if (total == 0)
            {
                Log.Information("No files found for compression in {InputFolder}", request.InputFolder);
                return new BatchResult(0, 0, 0, 0, stopwatch.Elapsed, false);
            }

            for (var index = 0; index < files.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var inputFile = files[index];
                var fileName = Path.GetFileName(inputFile);
                var outputFile = Path.Combine(
                    request.OutputFolder,
                    PathUtilities.SanitizeFileName(Path.GetFileNameWithoutExtension(inputFile)) +
                    request.Format.ToExtension());

                progress?.Report(new BatchProgress(
                    index + 1, total, fileName, $"Compressing file {index + 1} of {total}: {fileName}"));

                if (File.Exists(outputFile))
                {
                    skipped++;
                    Log.Information(
                        "Skipped {FileName}: archive already exists at {OutputFile}", fileName, outputFile);
                    continue;
                }

                var status = await _archiveService.CompressFileAsync(
                    inputFile, outputFile, request.Format, cancellationToken);

                switch (status)
                {
                    case ProcessStatus.Success:
                        success++;
                        if (request.DeleteOriginals)
                        {
                            await TryDeleteOriginalAsync(inputFile, cancellationToken);
                        }

                        break;
                    case ProcessStatus.Failed:
                        failed++;
                        break;
                    case ProcessStatus.Skipped:
                        skipped++;
                        break;
                }

                var completedBytes = SafeGetFileLength(outputFile);
                progress?.Report(new BatchProgress(
                    index + 1,
                    total,
                    fileName,
                    status == ProcessStatus.Success
                        ? $"Compressed {fileName}"
                        : $"Could not compress {fileName}",
                    completedBytes));
            }
        }
        catch (OperationCanceledException)
        {
            canceled = true;
            Log.Information("Compression batch canceled by the user");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Compression batch failed");
            throw;
        }

        stopwatch.Stop();
        var result = new BatchResult(total, success, failed, skipped, stopwatch.Elapsed, canceled);
        Log.Information(
            "Compression batch finished: {Success} succeeded, {Failed} failed, {Skipped} skipped in {Elapsed}",
            success, failed, skipped, stopwatch.Elapsed);
        return result;
    }

    /// <inheritdoc />
    public async Task<BatchResult> VerifyBatchAsync(
        VerificationRequest request,
        IProgress<BatchProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        var success = 0;
        var failed = 0;
        var skipped = 0;
        var total = 0;
        var canceled = false;

        try
        {
            ValidateInputFolder(request.InputFolder);

            if (request.MoveSuccessFiles && !string.IsNullOrWhiteSpace(request.SuccessFolder))
            {
                Directory.CreateDirectory(request.SuccessFolder);
            }

            if (request.MoveFailedFiles && !string.IsNullOrWhiteSpace(request.FailedFolder))
            {
                Directory.CreateDirectory(request.FailedFolder);
            }

            var searchOption = request.IncludeSubfolders
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var archives = await Task.Run(
                () => Directory.GetFiles(request.InputFolder, "*", searchOption)
                    .Where(file => SupportedVerificationExtensions.Contains(Path.GetExtension(file)))
                    .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                cancellationToken);

            total = archives.Length;
            Log.Information(
                "Verification batch started: {ArchiveCount} archives in {InputFolder}",
                total, request.InputFolder);

            if (total == 0)
            {
                Log.Information("No supported archives found in {InputFolder}", request.InputFolder);
                return new BatchResult(0, 0, 0, 0, stopwatch.Elapsed, false);
            }

            for (var index = 0; index < archives.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var archivePath = archives[index];
                var fileName = Path.GetFileName(archivePath);

                progress?.Report(new BatchProgress(
                    index + 1, total, fileName, $"Verifying file {index + 1} of {total}: {fileName}"));

                var archiveBytes = SafeGetFileLength(archivePath);
                var isValid = await _archiveService.VerifyArchiveAsync(archivePath, cancellationToken);
                if (isValid)
                {
                    success++;
                    Log.Information("Verification succeeded: {ArchivePath}", archivePath);

                    if (request.MoveSuccessFiles && !string.IsNullOrWhiteSpace(request.SuccessFolder))
                    {
                        await ArchiveFileMover.MoveAsync(
                            archivePath,
                            request.SuccessFolder,
                            request.InputFolder,
                            request.IncludeSubfolders,
                            cancellationToken);
                    }
                }
                else
                {
                    failed++;
                    Log.Information("Verification failed: {ArchivePath}", archivePath);

                    if (request.MoveFailedFiles && !string.IsNullOrWhiteSpace(request.FailedFolder))
                    {
                        await ArchiveFileMover.MoveAsync(
                            archivePath,
                            request.FailedFolder,
                            request.InputFolder,
                            request.IncludeSubfolders,
                            cancellationToken);
                    }
                }

                progress?.Report(new BatchProgress(
                    index + 1,
                    total,
                    fileName,
                    isValid ? $"Verified {fileName}" : $"Verification failed: {fileName}",
                    archiveBytes));
            }
        }
        catch (OperationCanceledException)
        {
            canceled = true;
            Log.Information("Verification batch canceled by the user");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Verification batch failed");
            throw;
        }

        stopwatch.Stop();
        var result = new BatchResult(total, success, failed, skipped, stopwatch.Elapsed, canceled);
        Log.Information(
            "Verification batch finished: {Success} succeeded, {Failed} failed in {Elapsed}",
            success, failed, stopwatch.Elapsed);
        return result;
    }

    private static void ValidateInputFolder(string inputFolder)
    {
        if (!Directory.Exists(inputFolder))
        {
            throw new DirectoryNotFoundException($"Input folder not found: {inputFolder}");
        }
    }

    private static async Task TryDeleteOriginalAsync(string inputFile, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => File.Delete(inputFile), cancellationToken);
            Log.Information("Deleted original file {InputFile}", inputFile);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not delete original file {InputFile}", inputFile);
        }
    }

    private static long SafeGetFileLength(string path)
    {
        try
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not read file length for {Path}", path);
            return 0;
        }
    }
}
