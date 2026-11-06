using BatchConvertToCompressedFile.Models;

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
        var fileName = Path.GetFileName(sourceFile);
        var relativeDirectory = preserveSubfolders
            ? PathUtilities.GetRelativeDirectory(inputRoot, sourceFile)
            : string.Empty;

        var targetDirectory = string.IsNullOrEmpty(relativeDirectory)
            ? destinationRoot
            : Path.Combine(destinationRoot, relativeDirectory);

        var destinationFile = Path.Combine(targetDirectory, fileName);

        try
        {
            Directory.CreateDirectory(targetDirectory);

            if (File.Exists(destinationFile))
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
                    File.Move(sourceFile, destinationFile);
                    Log.Information("Moved {FileName} to {DestinationFile}", fileName, destinationFile);
                    return true;
                }
                catch (IOException) when (attempt < MaxRetries)
                {
                    Log.Debug(
                        "File {FileName} is in use; move retry {Attempt}/{MaxRetries} in {Delay} ms",
                        fileName, attempt, MaxRetries, RetryDelay.TotalMilliseconds);
                    await Task.Delay(RetryDelay, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to move {FileName} to {DestinationRoot}", fileName, destinationRoot);
        }

        return false;
    }
}
