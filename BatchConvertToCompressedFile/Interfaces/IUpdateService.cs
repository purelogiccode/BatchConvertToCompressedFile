using BatchConvertToCompressedFile.Models;

namespace BatchConvertToCompressedFile.Interfaces;

/// <summary>Checks GitHub for a newer application release.</summary>
internal interface IUpdateService
{
    /// <summary>Checks the latest GitHub release against the running version.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The update check result; never <see langword="null"/>.</returns>
    Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);
}
