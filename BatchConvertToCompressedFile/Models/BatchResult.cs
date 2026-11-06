namespace BatchConvertToCompressedFile.Models;

/// <summary>Aggregated result of a completed batch operation.</summary>
/// <param name="TotalFiles">Total number of files considered.</param>
/// <param name="SuccessCount">Files processed successfully.</param>
/// <param name="FailedCount">Files that failed.</param>
/// <param name="SkippedCount">Files intentionally skipped.</param>
/// <param name="Elapsed">Elapsed wall-clock time.</param>
/// <param name="Canceled">Whether the user canceled the operation.</param>
internal sealed record BatchResult(
    int TotalFiles,
    int SuccessCount,
    int FailedCount,
    int SkippedCount,
    TimeSpan Elapsed,
    bool Canceled);
