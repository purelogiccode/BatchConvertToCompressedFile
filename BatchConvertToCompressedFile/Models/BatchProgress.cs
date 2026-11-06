namespace BatchConvertToCompressedFile.Models;

/// <summary>Progress information emitted while a batch operation runs.</summary>
/// <param name="CurrentFile">One-based index of the file currently being processed.</param>
/// <param name="TotalFiles">Total number of files in the batch.</param>
/// <param name="CurrentFileName">File name currently being processed.</param>
/// <param name="Message">Human-readable progress message.</param>
/// <param name="CompletedBytes">Bytes written by the most recent single-file operation.</param>
/// <param name="ItemStatus">Outcome of the current file, or <see langword="null"/> while it is running.</param>
/// <param name="SuccessCount">Cumulative number of successful files.</param>
/// <param name="FailedCount">Cumulative number of failed files.</param>
/// <param name="SkippedCount">Cumulative number of skipped files.</param>
internal sealed record BatchProgress(
    int CurrentFile,
    int TotalFiles,
    string CurrentFileName,
    string Message,
    long CompletedBytes = 0,
    ProcessStatus? ItemStatus = null,
    int SuccessCount = 0,
    int FailedCount = 0,
    int SkippedCount = 0);
