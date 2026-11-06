using BatchConvertToCompressedFile.Interfaces;
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

        try
        {
            if (!File.Exists(inputFile))
            {
                Log.Information("Compression source file no longer exists: {InputFile}", inputFile);
                return ProcessStatus.Failed;
            }

            var outputDirectory = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var archiveType = format == ArchiveFormat.SevenZip ? ArchiveType.SevenZip : ArchiveType.Zip;
            var writerOptions = CreateWriterOptions(format);

            await using (var writer = await WriterFactory.OpenAsyncWriter(
                             outputFile, archiveType, writerOptions, cancellationToken))
            {
                await using var inputStream = new FileStream(
                    inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                await writer.WriteAsync(
                    Path.GetFileName(inputFile),
                    inputStream,
                    File.GetLastWriteTimeUtc(inputFile),
                    cancellationToken);
            }

            Log.Information("Compressed {InputFile} to {OutputFile}", inputFile, outputFile);
            return ProcessStatus.Success;
        }
        catch (OperationCanceledException)
        {
            await TryDeletePartialArchiveAsync(outputFile, cancellationToken);
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Locked/permission-denied files are expected user conditions, not application bugs.
            Log.Information(ex, "Compression skipped for {InputFile}: {Message}", inputFile, ex.Message);
            await TryDeletePartialArchiveAsync(outputFile, CancellationToken.None);
            return ProcessStatus.Failed;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while compressing {InputFile}", inputFile);
            await TryDeletePartialArchiveAsync(outputFile, CancellationToken.None);
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
            await using var archive = await ArchiveFactory.OpenAsyncArchive(
                archivePath, ReaderOptions.ForFilePath, linkedCts.Token);

            await foreach (var entry in archive.EntriesAsync.WithCancellation(linkedCts.Token))
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                if (entry.IsDirectory) continue;

                // Reading every entry to completion forces decompression. SharpCompress does not
                // always validate the stored CRC, so calculate it ourselves when available.
                await using var entryStream = await entry.OpenEntryStreamAsync(linkedCts.Token);
                var buffer = new byte[BufferSize];
                long totalBytes = 0;
                var crc = Crc32.InitialValue;

                int bytesRead;
                while ((bytesRead = await entryStream.ReadAsync(buffer, linkedCts.Token)) > 0)
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
        catch (Exception ex)
        {
            // A corrupt, encrypted, unsupported, or locked archive is an expected verification result.
            Log.Information(ex, "Verification failed for {ArchivePath}: {Message}", archivePath, ex.Message);
            return false;
        }
    }

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

    private static async Task TryDeletePartialArchiveAsync(string outputFile, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() =>
            {
                if (File.Exists(outputFile)) File.Delete(outputFile);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not delete partial archive {OutputFile}", outputFile);
        }
    }
}
