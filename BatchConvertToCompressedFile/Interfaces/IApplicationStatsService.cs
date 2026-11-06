namespace BatchConvertToCompressedFile.Interfaces;

/// <summary>Records anonymous application usage statistics.</summary>
internal interface IApplicationStatsService
{
    /// <summary>Sends one startup statistics hit. Failures are logged and never thrown.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the API accepted the hit.</returns>
    Task<bool> RecordStartupAsync(CancellationToken cancellationToken = default);
}
