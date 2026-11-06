namespace BatchConvertToCompressedFile.Models;

/// <summary>Result of processing a single file in a batch operation.</summary>
internal enum ProcessStatus
{
    /// <summary>The file was processed successfully.</summary>
    Success,

    /// <summary>The file could not be processed.</summary>
    Failed,

    /// <summary>The file was intentionally skipped (for example, the archive already exists).</summary>
    Skipped
}
