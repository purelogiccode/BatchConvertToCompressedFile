using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Logging;
using BatchConvertToCompressedFile.Models;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Common.Options;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Writers.SevenZip;
using SharpCompress.Writers.Zip;

namespace BatchConvertToCompressedFile.Services;

/// <summary>
/// Cross-platform archive creation and verification built on the managed SharpCompress library.
/// No native 7-Zip binaries are required on Windows, Linux, or macOS.
/// </summary>
internal sealed class ArchiveService : IArchiveService
{
    private const int BufferSize = 128 * 1024;
    private static readonly TimeSpan VerificationTimeout = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    public async Task<ProcessStatus> CompressFileAsync(
        string inputFile,
        string outputFile,
        ArchiveFormat format,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFile);

        var temporaryFile = CreateTemporaryFilePath(outputFile);

        try
        {
            if (!File.Exists(inputFile))
            {
                Log.Information("Compression source file no longer exists: {InputFile}", inputFile);
                return ProcessStatus.Failed;
            }

            if (File.Exists(outputFile))
            {
                Log.Information(
                    "Skipped {InputFile}: archive already exists at {OutputFile}", inputFile, outputFile);
                return ProcessStatus.Skipped;
            }

            var outputDirectory = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var archiveType = format == ArchiveFormat.SevenZip ? ArchiveType.SevenZip : ArchiveType.Zip;
            var writerOptions = CreateWriterOptions(format);

            await using (var writer = await WriterFactory.OpenAsyncWriter(
                             temporaryFile, archiveType, writerOptions, cancellationToken).ConfigureAwait(false))
            {
                await using var inputStream = new FileStream(
                    inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                await writer.WriteAsync(
                    Path.GetFileName(inputFile),
                    inputStream,
                    File.GetLastWriteTimeUtc(inputFile),
                    cancellationToken).ConfigureAwait(false);
            }

            try
            {
                // Move only after the archive is complete so a failed or canceled run never leaves
                // a partial archive at the user-visible destination.
                File.Move(temporaryFile, outputFile, overwrite: false);
            }
            catch (Exception ex) when (
                (ex is IOException or UnauthorizedAccessException) &&
                File.Exists(outputFile))
            {
                Log.Information(
                    "Skipped {InputFile}: archive appeared at {OutputFile} while compressing",
                    inputFile, outputFile);
                await TryDeleteFileAsync(temporaryFile, CancellationToken.None).ConfigureAwait(false);
                return ProcessStatus.Skipped;
            }

            Log.Information("Compressed {InputFile} to {OutputFile}", inputFile, outputFile);
            return ProcessStatus.Success;
        }
        catch (OperationCanceledException)
        {
            await TryDeleteFileAsync(temporaryFile, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex) when (ExceptionLogging.IsExpected(ex))
        {
            // Locked/permission-denied files are expected user conditions, not application bugs.
            Log.Information(ex, "Compression skipped for {InputFile}: {Message}", inputFile, ex.Message);
            await TryDeleteFileAsync(temporaryFile, CancellationToken.None).ConfigureAwait(false);
            return ProcessStatus.Failed;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while compressing {InputFile}", inputFile);
            await TryDeleteFileAsync(temporaryFile, CancellationToken.None).ConfigureAwait(false);
            return ProcessStatus.Failed;
        }
    }

    /// <inheritdoc />
    public async Task<bool> VerifyArchiveAsync(string archivePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);

        using var timeoutCts = new CancellationTokenSource(VerificationTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            if (!File.Exists(archivePath))
            {
                Log.Information("Archive no longer exists: {ArchivePath}", archivePath);
                return false;
            }

            await using var archive = await ArchiveFactory.OpenAsyncArchive(
                archivePath, ReaderOptions.ForFilePath, linkedCts.Token).ConfigureAwait(false);

            var buffer = new byte[BufferSize];

            await foreach (var entry in archive.EntriesAsync
                               .WithCancellation(linkedCts.Token)
                               .ConfigureAwait(false))
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                if (entry.IsDirectory) continue;

                // Reading every entry to completion forces decompression. SharpCompress does not
                // always validate the stored CRC, so calculate it ourselves when available.
                await using var entryStream = await entry
                    .OpenEntryStreamAsync(linkedCts.Token)
                    .ConfigureAwait(false);

                long totalBytes = 0;
                var crc = Crc32.InitialValue;

                int bytesRead;
                while ((bytesRead = await entryStream
                           .ReadAsync(buffer, linkedCts.Token)
                           .ConfigureAwait(false)) > 0)
                {
                    totalBytes += bytesRead;
                    crc = Crc32.Update(crc, buffer.AsSpan(0, bytesRead));
                }

                if (entry.Size >= 0 && totalBytes != entry.Size)
                {
                    throw new InvalidDataException(
                        $"Archive entry '{entry.Key}' has size {totalBytes}, expected {entry.Size}.");
                }

                if (entry.Crc is >= 0 and <= uint.MaxValue && Crc32.Finish(crc) != (uint)entry.Crc)
                {
                    throw new InvalidDataException(
                        $"Archive entry '{entry.Key}' failed its CRC check.");
                }
            }

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            Log.Information("Verification timed out for {ArchivePath}", archivePath);
            return false;
        }
        catch (Exception ex) when (ExceptionLogging.IsExpected(ex))
        {
            // A corrupt, encrypted, unsupported, or locked archive is an expected verification result.
            Log.Information(ex, "Verification failed for {ArchivePath}: {Message}", archivePath, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while verifying {ArchivePath}", archivePath);
            return false;
        }
    }

    private static string CreateTemporaryFilePath(string outputFile) =>
        $"{outputFile}.{Guid.NewGuid():N}.tmp";

    private static IWriterOptions CreateWriterOptions(ArchiveFormat format)
    {
        if (format == ArchiveFormat.SevenZip)
        {
            return new SevenZipWriterOptions(CompressionType.LZMA2)
            {
                CompressionLevel = 9,
                CompressHeader = true,
                LeaveStreamOpen = false,
                BufferSize = BufferSize
            };
        }

        return new ZipWriterOptions(CompressionType.Deflate)
        {
            CompressionLevel = 9,
            LeaveStreamOpen = false,
            UseZip64 = true,
            BufferSize = BufferSize
        };
    }

    private static async Task TryDeleteFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(
                () =>
                {
                    if (File.Exists(filePath)) File.Delete(filePath);
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not delete temporary archive {FilePath}", filePath);
        }
    }
}
