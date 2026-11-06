using BatchConvertToCompressedFile.Models;

namespace BatchConvertToCompressedFile.Interfaces;

/// <summary>Executes batch compression and verification workflows.</summary>
internal interface IBatchArchiveProcessor
{
    /// <summary>Compresses every file in a folder into individual archives.</summary>
    /// <param name="request">Compression parameters.</param>
    /// <param name="progress">Optional progress sink.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated result.</returns>
    Task<BatchResult> CompressBatchAsync(
        CompressionRequest request,
        IProgress<BatchProgress>? progress,
        CancellationToken cancellationToken);

    /// <summary>Verifies every supported archive in a folder.</summary>
    /// <param name="request">Verification parameters.</param>
    /// <param name="progress">Optional progress sink.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated result.</returns>
    Task<BatchResult> VerifyBatchAsync(
        VerificationRequest request,
        IProgress<BatchProgress>? progress,
        CancellationToken cancellationToken);
}
