namespace BatchConvertToCompressedFile.Models;

/// <summary>Progress information emitted while a batch operation runs.</summary>
/// <param name="CurrentFile">One-based index of the file currently being processed.</param>
/// <param name="TotalFiles">Total number of files in the batch.</param>
/// <param name="CurrentFileName">File name currently being processed.</param>
/// <param name="Message">Human-readable progress message.</param>
/// <param name="CompletedBytes">Bytes written by the most recent single-file operation.</param>
internal sealed record BatchProgress(
    int CurrentFile,
    int TotalFiles,
    string CurrentFileName,
    string Message,
    long CompletedBytes = 0);
