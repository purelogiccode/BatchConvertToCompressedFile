using BatchConvertToCompressedFile.Logging;

namespace BatchConvertToCompressedFile.Services;

/// <summary>Moves verified archives into success/failure folders with retry support.</summary>
internal static class ArchiveFileMover
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(300);

    /// <summary>Moves an archive, preserving its subfolder path when requested.</summary>
    /// <param name="sourceFile">Archive to move.</param>
    /// <param name="destinationRoot">Destination root folder.</param>
    /// <param name="inputRoot">Root folder of the verification scan.</param>
    /// <param name="preserveSubfolders">Whether the relative subfolder should be preserved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the file was moved.</returns>
    public static async Task<bool> MoveAsync(
        string sourceFile,
        string destinationRoot,
        string inputRoot,
        bool preserveSubfolders,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputRoot);

        var fileName = Path.GetFileName(sourceFile);
        var relativeDirectory = preserveSubfolders
            ? PathUtilities.GetRelativeDirectory(inputRoot, sourceFile)
            : string.Empty;

        var targetDirectory = string.IsNullOrEmpty(relativeDirectory)
            ? destinationRoot
            : Path.Combine(destinationRoot, relativeDirectory);

        if (!PathUtilities.IsSubPathOf(destinationRoot, targetDirectory))
        {
            Log.Information(
                "Not moving {FileName}: destination path escapes {DestinationRoot}",
                fileName, destinationRoot);
            return false;
        }

        var destinationFile = Path.Combine(targetDirectory, fileName);

        try
        {
            await Task.Run(
                () => Directory.CreateDirectory(targetDirectory),
                cancellationToken).ConfigureAwait(false);

            if (await Task.Run(
                    () => File.Exists(destinationFile),
                    cancellationToken).ConfigureAwait(false))
            {
                Log.Information(
                    "Not moving {FileName}: destination already exists at {DestinationFile}",
                    fileName, destinationFile);
                return false;
            }

            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await MoveFileAsync(sourceFile, destinationFile, cancellationToken)
                        .ConfigureAwait(false);

                    Log.Information("Moved {FileName} to {DestinationFile}", fileName, destinationFile);
                    return true;
                }
                catch (IOException) when (File.Exists(destinationFile))
                {
                    Log.Information(
                        "Not moving {FileName}: destination appeared at {DestinationFile}",
                        fileName, destinationFile);
                    return false;
                }
                catch (IOException) when (attempt < MaxRetries)
                {
                    Log.Debug(
                        "File {FileName} is in use; move retry {Attempt}/{MaxRetries} in {Delay} ms",
                        fileName, attempt, MaxRetries, RetryDelay.TotalMilliseconds);
                    await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ExceptionLogging.LogHandled(
                ex, "Failed to move {FileName} to {DestinationRoot}", fileName, destinationRoot);
        }

        return false;
    }

    private static Task MoveFileAsync(
        string sourceFile,
        string destinationFile,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () =>
            {
                try
                {
                    File.Move(sourceFile, destinationFile);
                }
                catch (IOException ex) when (IsCrossDeviceError(ex))
                {
                    // File.Move uses rename on Unix, which fails across volumes. Copy first and
                    // remove the source only after the copy completed.
                    File.Copy(sourceFile, destinationFile, overwrite: false);
                    File.Delete(sourceFile);
                }
            },
            cancellationToken);
    }

    private static bool IsCrossDeviceError(IOException exception) =>
        exception.HResult == 18 ||
        (OperatingSystem.IsWindows() && (exception.HResult & 0xFFFF) == 17);
}
