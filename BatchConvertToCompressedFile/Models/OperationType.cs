namespace BatchConvertToCompressedFile.Models;

/// <summary>Identifies the batch operation being executed.</summary>
internal enum OperationType
{
    /// <summary>Compress files into individual archives.</summary>
    Compression,

    /// <summary>Verify the integrity of existing archives.</summary>
    Verification
}
